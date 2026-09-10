/*
    103 — Re-process orders whose stored payload has been repaired
    -------------------------------------------------------------------------------------------
    Script 102 makes a broken API-JSON payload readable again, but it deliberately does NOT touch
    Orders.Status — the orders stay in ERROR and nothing picks them up. This flips them back so
    the SCSPlaceOrder route places them on its next run.

    RUN 102 FIRST. This script only ever moves an order whose API-JSON parses (ISJSON = 1); an
    order whose payload is still broken is listed and left alone, because flipping it would only
    send it round the same failure again — and on a Processor that does not yet have the per-order
    try/catch, one such order aborts the whole batch.

    WHY 'InProgress' AND NOT 'New'
      The batch route reads  New,InProgress   (SCSPlaceOrderRoute.cs:85)
      The Reprocess button reads  InProgress  (SCSPlaceOrderRoute.cs:222)
    so 'InProgress' is picked up by both. It is also the safer of the two: ProcessOrder only runs
    the SPARS Get_OrderInfo existence check for an InProgress order, so if the order somehow did
    reach the ERP it is recognised and NOT posted a second time. 'New' skips that check.
    This mirrors what the auto-retry route already does (ErrorOrderRetryRoute.cs:65).

    NEVER FLIPPED
      - payload still unreadable (ISJSON = 0)
      - ExternalId already set — the order is placed
      Both are reported in the result grid with the reason.

    @Statuses IS 'ERROR' ON PURPOSE. ACKERROR is an acknowledgement failure and ASNERROR is a
    shipment failure — neither is a placement failure, and moving one of those to InProgress asks
    SCSPlaceOrder to place an order that is not waiting to be placed. Widen it only deliberately.
    (An ASNERROR order is caught by the ExternalId guard anyway; an ACKERROR one is not.)

    HOW TO RUN
      1. Leave @WhatIf = 1 and run it. Nothing is written; the grid lists what WOULD change.
      2. Set @WhatIf = 0 and run it again to apply.
      3. Let the route run on its schedule, or use Reprocess on the Orders screen per order.

    SCOPING — @FromDate keeps ISJSON() off the whole table (OrderData.Data is NVARCHAR(MAX) and
    ISJSON reads the entire blob). @OrderNumbers narrows it further to a known list.
*/

SET NOCOUNT ON;

DECLARE @WhatIf        BIT           = 1;                             -- 1 = report only, 0 = apply
DECLARE @FromDate      DATETIME      = DATEADD(DAY, -90, GETDATE());  -- how far back, by OrderDate
DECLARE @OrderNumbers  NVARCHAR(MAX) = '';                            -- CSV of OrderNumber; '' = all in window
DECLARE @Statuses      NVARCHAR(200) = 'ERROR';                       -- CSV of Orders.Status to sweep; see the note above
DECLARE @ClearErpError BIT           = 1;                             -- drop the stale ERP-ERROR rows
DECLARE @ResetRetry    BIT           = 1;                             -- reset RetryCount so auto-retry can try again
DECLARE @NewStatus     VARCHAR(50)   = 'InProgress';

IF OBJECT_ID('tempdb..#Candidates') IS NOT NULL
    DROP TABLE #Candidates;

CREATE TABLE #Candidates
(
    OrderId     INT           NOT NULL PRIMARY KEY,
    OrderNumber VARCHAR(50)   NULL,
    OldStatus   VARCHAR(50)   NULL,
    ExternalId  VARCHAR(50)   NULL,
    OrderDate   DATETIME      NULL,
    DataId      INT           NULL,
    PayloadOk   BIT           NOT NULL DEFAULT (0),
    DoAction    VARCHAR(20)   NOT NULL DEFAULT ('SKIP'),
    Reason      NVARCHAR(200) NULL
);

/* ---------------------------------------------------------------------------------------------
   1. Collect the orders in scope. ERROR only by default — a New/InProgress order is already
      queued and needs nothing done to it.
   --------------------------------------------------------------------------------------------- */
INSERT INTO #Candidates (OrderId, OrderNumber, OldStatus, ExternalId, OrderDate, DataId, PayloadOk)
SELECT o.Id,
       o.OrderNumber,
       o.Status,
       o.ExternalId,
       o.OrderDate,
       d.Id,
       CAST(ISJSON(d.Data) AS BIT)
FROM   Orders    o
JOIN   OrderData d ON d.OrderId = o.Id AND d.Type = 'API-JSON'
WHERE  o.Status IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@Statuses, ','))
AND    o.OrderDate >= @FromDate
AND    (
           LTRIM(RTRIM(ISNULL(@OrderNumbers, ''))) = ''
           OR o.OrderNumber IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@OrderNumbers, ','))
       );

DECLARE @Found INT = @@ROWCOUNT;
PRINT 'Orders in scope (status ' + @Statuses + ', within the window): ' + CAST(@Found AS VARCHAR(10));

/* ---------------------------------------------------------------------------------------------
   2. Decide what happens to each one.
   --------------------------------------------------------------------------------------------- */
UPDATE #Candidates
SET    DoAction = 'SKIP',
       Reason   = 'Payload still does not parse - run 102 first, or repair with Re-Map Item IDs.'
WHERE  PayloadOk = 0;

UPDATE #Candidates
SET    DoAction = 'SKIP',
       Reason   = 'Already placed in the ERP (ExternalId is set) - not re-sent.'
WHERE  PayloadOk = 1
AND    ISNULL(ExternalId, '') <> '';

UPDATE #Candidates
SET    DoAction = 'REPROCESS',
       Reason   = 'Payload parses and the order was never placed.'
WHERE  PayloadOk = 1
AND    ISNULL(ExternalId, '') = '';

DECLARE @ToDo   INT = (SELECT COUNT(*) FROM #Candidates WHERE DoAction = 'REPROCESS');
DECLARE @Broken INT = (SELECT COUNT(*) FROM #Candidates WHERE DoAction = 'SKIP' AND PayloadOk = 0);
DECLARE @Placed INT = (SELECT COUNT(*) FROM #Candidates WHERE DoAction = 'SKIP' AND PayloadOk = 1);

PRINT 'To re-process                     : ' + CAST(@ToDo   AS VARCHAR(10));
PRINT 'Skipped, payload still unreadable : ' + CAST(@Broken AS VARCHAR(10));
PRINT 'Skipped, already placed           : ' + CAST(@Placed AS VARCHAR(10));

/* ---------------------------------------------------------------------------------------------
   3. Apply.
   --------------------------------------------------------------------------------------------- */
IF @WhatIf = 0 AND @ToDo > 0
BEGIN
    BEGIN TRANSACTION;

    BEGIN TRY
        UPDATE o
        SET    o.Status = @NewStatus
        FROM   Orders o
        JOIN   #Candidates c ON c.OrderId = o.Id
        WHERE  c.DoAction = 'REPROCESS';

        PRINT 'Orders moved to ' + @NewStatus + ': ' + CAST(@@ROWCOUNT AS VARCHAR(10));

        -- The stale placement error. ProcessOrder replaces an ERP-ERROR row when it fails again,
        -- but on success it only replaces ERP-JSON, so the old message would otherwise sit on the
        -- order for good.
        IF @ClearErpError = 1
        BEGIN
            DELETE d
            FROM   OrderData d
            JOIN   #Candidates c ON c.OrderId = d.OrderId
            WHERE  c.DoAction = 'REPROCESS'
            AND    d.Type = 'ERP-ERROR';

            PRINT 'Stale ERP-ERROR rows removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
        END

        -- Orders.RetryCount only exists where script 71 has been run. Left alone if it is missing,
        -- which is why this one UPDATE has to be dynamic - a direct reference would not compile.
        IF @ResetRetry = 1
        AND EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.Orders') AND name = 'RetryCount')
        BEGIN
            EXEC sp_executesql N'UPDATE o SET o.RetryCount = 0 FROM Orders o JOIN #Candidates c ON c.OrderId = o.Id WHERE c.DoAction = ''REPROCESS'';';

            PRINT 'RetryCount reset for the re-processed orders.';
        END

        COMMIT TRANSACTION;
        PRINT 'Applied.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @Msg NVARCHAR(MAX) = ERROR_MESSAGE();
        PRINT 'Rolled back - nothing was changed. ' + @Msg;
        THROW;
    END CATCH
END
ELSE IF @WhatIf = 1
BEGIN
    PRINT 'WhatIf = 1 - nothing was written. Set @WhatIf = 0 to apply.';
END

/* ---------------------------------------------------------------------------------------------
   4. What happened, or what would happen.
   --------------------------------------------------------------------------------------------- */
SELECT DoAction,
       OrderId,
       OrderNumber,
       OldStatus,
       NewStatus = CASE WHEN DoAction = 'REPROCESS' THEN @NewStatus ELSE OldStatus END,
       ExternalId,
       OrderDate,
       DataId,
       PayloadParses = PayloadOk,
       Reason
FROM   #Candidates
ORDER  BY DoAction, OrderDate DESC, OrderId;

DROP TABLE #Candidates;
