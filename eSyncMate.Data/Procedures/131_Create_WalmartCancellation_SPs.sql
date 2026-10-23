/*==============================================================================
  131 - Stored procedures for the Walmart cancellation sync (Task 00005)

    Sp_SetWalmartPortalCancel         Route 1 (78): decides what a customer cancel means for
                                      one order (cases 1-13 of the case analysis) and saves
                                      the operator message
    Sp_GetWalmartOrdersToCancelInERP  Route 2 (79): source - the customer's PortalCancel orders;
                                      Re-Process: one order (@p_OrderId)
    Sp_SetWalmartERPCancelResult      Route 2 (79) + Re-Process: ERP result -> CANCELLED / CancelError

  Every status change is a conditional UPDATE from the status that was read, so a
  route running at the same moment is never overwritten. Each outcome saves one
  OrderData row Type 'WMCANCEL-MSG':
      {"OutPut":{"Success":..,"Message":"..","MessageCode":"M1".."M10","CancelDate":".."}}
  It shows on hover over the status badge in the Orders grid.

  Order lines are never changed. SP_UpdateOrderStatus is deliberately NOT altered:
  the production copy differs from the repo copy.
  Re-runnable (CREATE OR ALTER).
==============================================================================*/

CREATE OR ALTER PROCEDURE dbo.Sp_SaveWalmartCancelMessage
    @p_OrderId      INT,
    @p_OrderNumber  NVARCHAR(250),
    @p_Code         VARCHAR(5),
    @p_Success      BIT,
    @p_Message      NVARCHAR(2000),
    @p_CancelDate   VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.OrderData WHERE OrderId = @p_OrderId AND Type = 'WMCANCEL-MSG';

    INSERT INTO dbo.OrderData (OrderId, Type, OrderNumber, Data, CreatedDate, CreatedBy)
    VALUES (@p_OrderId, 'WMCANCEL-MSG', ISNULL(@p_OrderNumber, ''),
            CONCAT('{"OutPut":{"Success":', CASE WHEN @p_Success = 1 THEN 'true' ELSE 'false' END,
                   ',"Message":"', STRING_ESCAPE(@p_Message, 'json'),
                   '","MessageCode":"', @p_Code,
                   '","CancelDate":"', STRING_ESCAPE(ISNULL(@p_CancelDate, ''), 'json'), '"}}'),
            GETDATE(), 1);
END
GO

CREATE OR ALTER PROCEDURE dbo.Sp_SetWalmartPortalCancel
    @p_CustomerID       VARCHAR(50),
    @p_OrderNumber      VARCHAR(50),
    @p_IsPartial        BIT          = 0,     -- 1 = only some lines cancelled by the customer
    @p_CancelledLines   INT          = NULL,
    @p_TotalLines       INT          = NULL,
    @p_CancelDate       VARCHAR(30)  = NULL   -- MM/dd/yyyy hh:mm AM, already formatted by the route
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @l_OrderId     INT;
    DECLARE @l_Status      VARCHAR(50);
    DECLARE @l_ExternalId  VARCHAR(50);
    DECLARE @l_Shipped     BIT = 0;
    DECLARE @l_NewStatus   VARCHAR(50) = NULL;
    DECLARE @l_Result      VARCHAR(20);
    DECLARE @l_Code        VARCHAR(5) = NULL;
    DECLARE @l_Message     NVARCHAR(2000) = NULL;
    DECLARE @l_Rows        INT = 0;
    DECLARE @l_Date        VARCHAR(30) = ISNULL(NULLIF(@p_CancelDate, ''), FORMAT(GETDATE(), 'MM/dd/yyyy hh:mm tt'));

    SELECT TOP 1 @l_OrderId = O.Id, @l_Status = O.Status, @l_ExternalId = O.ExternalId
      FROM dbo.Orders O
      JOIN dbo.Customers C ON C.Id = O.CustomerId
     WHERE C.ERPCustomerID = @p_CustomerID
       AND O.OrderNumber = @p_OrderNumber
     ORDER BY O.Id DESC;

    IF @l_OrderId IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.OrderDetail D WHERE D.OrderId = @l_OrderId AND ISNULL(D.[Status], '') IN ('ASNRVD', 'ASNSNT'))
        SET @l_Shipped = 1;

    IF @l_OrderId IS NULL
        SET @l_Result = 'NOTFOUND';                                                     -- case 12
    ELSE IF @l_Status IN ('CANCELLED', 'PortalCancel', 'CancelError', 'InProgress')
        SET @l_Result = 'SKIPPED';                                                      -- cases 4, 9, 10, 11
    ELSE IF @p_IsPartial = 1
    BEGIN                                                                               -- case 13
        SELECT @l_NewStatus = 'CancelError', @l_Code = 'M10',
               @l_Message = CONCAT('The customer cancelled only part of this order on ', @l_Date,
                                   CASE WHEN @p_CancelledLines IS NOT NULL AND @p_TotalLines IS NOT NULL
                                        THEN CONCAT(' (', @p_CancelledLines, ' of ', @p_TotalLines, ' lines)') ELSE '' END,
                                   '. Partial cancellations are not processed automatically, so nothing was sent to ERP and no order lines were changed. Please update the order in ERP manually.');
    END
    ELSE IF @l_Status = 'New' OR (@l_Status = 'ACKERROR' AND ISNULL(@l_ExternalId, '') = '')
        SELECT @l_NewStatus = 'CANCELLED', @l_Code = 'M1',                             -- cases 1, 3
               @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, ' before it was placed in ERP. The order has been cancelled in eSyncMate and will not be sent to ERP.');
    ELSE IF @l_Status = 'ERROR'
        SELECT @l_NewStatus = 'CANCELLED', @l_Code = 'M2',                             -- case 2
               @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, '. It had not been placed in ERP because the earlier placement attempt failed. The order has been cancelled in eSyncMate and will not be retried.');
    ELSE IF @l_Status = 'ASNERROR' OR (@l_Status IN ('SYNCED', 'ACKERROR') AND @l_Shipped = 1)
        SELECT @l_NewStatus = 'CancelError', @l_Code = 'M6',                           -- cases 6, 7
               @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, ', but ERP had already shipped it (shipment details received). The cancellation was not sent to ERP. Please review this order with the warehouse; use Re-Process only if ERP should still cancel it.');
    ELSE IF @l_Status IN ('SYNCED', 'ACKERROR')
        SELECT @l_NewStatus = 'PortalCancel', @l_Code = 'M3',                          -- case 5
               @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, '. A cancellation request is being sent to ERP.');
    ELSE IF @l_Status IN ('SHIPPED', 'INVOICED')
        SELECT @l_NewStatus = 'CancelError', @l_Code = 'M7',                           -- case 8
               @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, ', but it was already ', @l_Status,
                                   ' and the shipment had been confirmed to the customer. The cancellation was not sent to ERP. Please review this order with the warehouse; use Re-Process only if ERP should still cancel it.');
    ELSE
        SET @l_Result = 'SKIPPED';                                                      -- any other status: left alone

    IF @l_NewStatus IS NOT NULL
    BEGIN
        BEGIN TRAN;

        UPDATE dbo.Orders
           SET Status = @l_NewStatus
         WHERE Id = @l_OrderId
           AND Status = @l_Status;

        SET @l_Rows = @@ROWCOUNT;

        DECLARE @l_Success BIT = CASE WHEN @l_NewStatus = 'CancelError' THEN 0 ELSE 1 END;

        IF @l_Rows = 1
            EXEC dbo.Sp_SaveWalmartCancelMessage @l_OrderId, @p_OrderNumber, @l_Code, @l_Success, @l_Message, @l_Date;

        COMMIT TRAN;

        -- another route changed the status between the read and the update: nothing done, next run decides
        SET @l_Result = CASE WHEN @l_Rows = 0 THEN 'SKIPPED' ELSE UPPER(@l_NewStatus) END;
    END

    SELECT @l_OrderId     AS OrderId,
           @p_OrderNumber AS OrderNumber,
           @l_Status      AS PreviousStatus,
           @l_Result      AS Result,                 -- CANCELLED / PORTALCANCEL / CANCELERROR / SKIPPED / NOTFOUND
           CASE WHEN @l_Rows = 1 THEN @l_Code END AS MessageCode,
           CASE WHEN @l_Rows = 1 THEN @l_Message END AS Message;
END
GO

-- @p_OrderId = 0: Route 2 source, the customer's PortalCancel orders.
-- @p_OrderId > 0: Re-Process, that one order in any status (the caller checks it).
-- HasShipped = an ERP ASN already arrived for a line; MessageCode = the latest cancel message (M10 = partial).
CREATE OR ALTER PROCEDURE dbo.Sp_GetWalmartOrdersToCancelInERP
    @p_CustomerID   VARCHAR(50),
    @p_OrderId      INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT O.Id, O.OrderNumber, O.ExternalId, O.Status,
           CASE WHEN EXISTS (SELECT 1 FROM dbo.OrderDetail D WITH (NOLOCK)
                              WHERE D.OrderId = O.Id AND ISNULL(D.[Status], '') IN ('ASNRVD', 'ASNSNT'))
                THEN 1 ELSE 0 END AS HasShipped,
           MSG.MessageCode
      FROM dbo.Orders O WITH (NOLOCK)
      JOIN dbo.Customers C WITH (NOLOCK) ON C.Id = O.CustomerId
      OUTER APPLY (SELECT TOP 1 JSON_VALUE(OD.Data, '$.OutPut.MessageCode') AS MessageCode
                     FROM dbo.OrderData OD WITH (NOLOCK)
                    WHERE OD.OrderId = O.Id AND OD.Type = 'WMCANCEL-MSG'
                    ORDER BY OD.Id DESC) MSG
     WHERE C.ERPCustomerID = @p_CustomerID
       AND ((@p_OrderId = 0 AND O.Status = 'PortalCancel') OR (@p_OrderId > 0 AND O.Id = @p_OrderId))
     ORDER BY O.Id;
END
GO

-- Route 2 (@p_IsReprocess = 0): PortalCancel -> CANCELLED (M4) / CancelError (M5, or M6 when @p_Shipped = 1, ERP not called)
-- Re-Process (@p_IsReprocess = 1): CancelError -> CANCELLED (M8) / stays CancelError (M9)
CREATE OR ALTER PROCEDURE dbo.Sp_SetWalmartERPCancelResult
    @p_OrderId      INT,
    @p_Success      BIT,
    @p_ERPMessage   NVARCHAR(1000) = NULL,
    @p_Shipped      BIT = 0,
    @p_IsReprocess  BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @l_From        VARCHAR(50) = CASE WHEN @p_IsReprocess = 1 THEN 'CancelError' ELSE 'PortalCancel' END;
    DECLARE @l_NewStatus   VARCHAR(50) = CASE WHEN @p_Success = 1 THEN 'CANCELLED' ELSE 'CancelError' END;
    DECLARE @l_OrderNumber NVARCHAR(250);
    DECLARE @l_Date        VARCHAR(30);
    DECLARE @l_Now         VARCHAR(30) = FORMAT(GETDATE(), 'MM/dd/yyyy hh:mm tt');
    DECLARE @l_ERP         NVARCHAR(1000) = ISNULL(NULLIF(LTRIM(RTRIM(@p_ERPMessage)), ''), 'no reason given');
    DECLARE @l_Code        VARCHAR(5);
    DECLARE @l_Message     NVARCHAR(2000);
    DECLARE @l_Rows        INT;

    -- the message templates add their own full stop
    WHILE RIGHT(@l_ERP, 1) = '.' AND LEN(@l_ERP) > 1
        SET @l_ERP = LEFT(@l_ERP, LEN(@l_ERP) - 1);

    SELECT @l_OrderNumber = OrderNumber FROM dbo.Orders WHERE Id = @p_OrderId;

    -- the customer's cancellation date, saved by Route 1 with the first message
    SELECT TOP 1 @l_Date = NULLIF(JSON_VALUE(Data, '$.OutPut.CancelDate'), '')
      FROM dbo.OrderData
     WHERE OrderId = @p_OrderId AND Type = 'WMCANCEL-MSG'
     ORDER BY Id DESC;

    SET @l_Date = ISNULL(@l_Date, @l_Now);

    IF @p_IsReprocess = 1 AND @p_Success = 1
        SELECT @l_Code = 'M8', @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, '. The cancellation was sent to ERP again on ', @l_Now, ' and the order was cancelled in ERP successfully.');
    ELSE IF @p_IsReprocess = 1
        SELECT @l_Code = 'M9', @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, '. The cancellation was sent to ERP again on ', @l_Now, ', but ERP could not cancel it: ', @l_ERP, '. Please check the order in ERP.');
    ELSE IF @p_Success = 1
        SELECT @l_Code = 'M4', @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, ', and the order was cancelled in ERP successfully.');
    ELSE IF @p_Shipped = 1
        SELECT @l_Code = 'M6', @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, ', but ERP had already shipped it (shipment details received). The cancellation was not sent to ERP. Please review this order with the warehouse; use Re-Process only if ERP should still cancel it.');
    ELSE
        SELECT @l_Code = 'M5', @l_Message = CONCAT('The customer cancelled this order on ', @l_Date, ', but ERP could not cancel it: ', @l_ERP, '. Please use Re-Process to send the cancellation again, or cancel the order in ERP manually.');

    BEGIN TRAN;

    UPDATE dbo.Orders
       SET Status = @l_NewStatus
     WHERE Id = @p_OrderId
       AND Status = @l_From;

    SET @l_Rows = @@ROWCOUNT;

    IF @l_Rows = 1
        EXEC dbo.Sp_SaveWalmartCancelMessage @p_OrderId, @l_OrderNumber, @l_Code, @p_Success, @l_Message, @l_Date;

    COMMIT TRAN;

    SELECT @l_Rows AS Updated, @l_NewStatus AS NewStatus, @l_Code AS MessageCode, @l_Message AS Message;
END
GO

SELECT name, create_date, modify_date
  FROM sys.procedures
 WHERE name IN ('Sp_SaveWalmartCancelMessage', 'Sp_SetWalmartPortalCancel', 'Sp_GetWalmartOrdersToCancelInERP', 'Sp_SetWalmartERPCancelResult')
 ORDER BY name;
GO
