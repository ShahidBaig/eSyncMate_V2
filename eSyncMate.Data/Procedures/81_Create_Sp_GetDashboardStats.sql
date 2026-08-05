/*
===============================================================================
  Script:      81_Create_Sp_GetDashboardStats.sql
  Description: Dashboard order statistics — customer-wise, status-wise and
               partner+status breakdowns, returned as three result sets in a
               single round trip.

               Replaces the three inline queries that OrdersController
               .GetDashboardStats used to build by hand. Two changes:

               1. Each status now reports the date of the event it actually
                  means — ASN sent / created in ERP / cancelled — instead of
                  Orders.CreatedDate, which was the same ingestion timestamp on
                  every tile. On client production the Shipped tile was 7h15m
                  earlier than the real last shipment.

               2. Counts stay scoped to Orders.CreatedDate so they continue to
                  match the drilldown exactly. Only the DATE comes from the
                  event window, so a shipment is no longer hidden just because
                  its order was created before the window (9.4% of shipments
                  over a 7-day window on production).

               OrderData is the only place a status transition is timestamped:
               Orders has no ModifiedDate column, and OrderDetail.ModifiedDate
               is written as a 1900-01-01 sentinel on every row.

  Analysis:    TaskManagement/Dashboard-StatusWise-Dates-Analysis.html
  Requires:    82_Add_Index_OrderData_Type_OrderId_CreatedDate.sql (recommended)
  Target:      All environments
  Date:        2026-07-30
===============================================================================
*/

IF OBJECT_ID('dbo.Sp_GetDashboardStats', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.Sp_GetDashboardStats AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE [dbo].[Sp_GetDashboardStats]
    @p_FromDate         VARCHAR(30)  = '',   -- '' together with @p_ToDate => last 24 hours
    @p_ToDate           VARCHAR(30)  = '',   -- inclusive date; upper bound becomes ToDate + 1 day
    @p_AllowedCustomers VARCHAR(MAX) = '',   -- role scope, plain CSV of ERPCustomerID, '' = unrestricted
    @p_ERPCustomerIDs   VARCHAR(MAX) = ''    -- screen filter, plain CSV of ERPCustomerID, '' = all
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @l_From DATETIME;
    DECLARE @l_To   DATETIME;   -- NULL = open upper bound, matching the previous default

    IF NULLIF(LTRIM(RTRIM(@p_FromDate)), '') IS NOT NULL
       AND NULLIF(LTRIM(RTRIM(@p_ToDate)), '') IS NOT NULL
    BEGIN
        SET @l_From = TRY_CONVERT(DATETIME, @p_FromDate);
        SET @l_To   = TRY_CONVERT(DATETIME, @p_ToDate);

        -- A date-only value ('2026-07-30') parses to midnight and means "the whole of that
        -- day", so the upper bound moves to the next midnight — the original behaviour.
        -- A value carrying a time ('2026-07-30 04:32:10') is an exact instant and is used
        -- as-is: that is what lets the 24H preset mean 24 hours instead of two whole days.
        IF @l_To IS NOT NULL AND @l_To = CAST(@l_To AS DATE)
            SET @l_To = DATEADD(DAY, 1, @l_To);
    END

    -- Bad or missing dates fall back to the original default rather than returning nothing
    IF @l_From IS NULL
    BEGIN
        SET @l_From = DATEADD(HOUR, -24, GETDATE());
        SET @l_To   = NULL;
    END

    ---------------------------------------------------------------------------
    -- Customer scoping. Both lists are ANDed, exactly as the controller used to
    -- accumulate its two IN() fragments. An empty list means "no restriction".
    ---------------------------------------------------------------------------
    DECLARE @l_Allowed TABLE (ERPCustomerID VARCHAR(50) NOT NULL PRIMARY KEY);
    DECLARE @l_Filter  TABLE (ERPCustomerID VARCHAR(50) NOT NULL PRIMARY KEY);

    IF NULLIF(LTRIM(RTRIM(@p_AllowedCustomers)), '') IS NOT NULL
        INSERT INTO @l_Allowed (ERPCustomerID)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM   STRING_SPLIT(@p_AllowedCustomers, ',')
        WHERE  LTRIM(RTRIM(value)) <> '';

    IF NULLIF(LTRIM(RTRIM(@p_ERPCustomerIDs)), '') IS NOT NULL
        INSERT INTO @l_Filter (ERPCustomerID)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM   STRING_SPLIT(@p_ERPCustomerIDs, ',')
        WHERE  LTRIM(RTRIM(value)) <> '';

    DECLARE @l_HasAllowed BIT = CASE WHEN EXISTS (SELECT 1 FROM @l_Allowed) THEN 1 ELSE 0 END;
    DECLARE @l_HasFilter  BIT = CASE WHEN EXISTS (SELECT 1 FROM @l_Filter)  THEN 1 ELSE 0 END;

    ---------------------------------------------------------------------------
    -- 1) Events that OCCURRED inside the window, whatever the order's creation
    --    date. The Type whitelist is a performance guard as much as a filter:
    --    ERPASN-ERR and ERPCANLN-ERR alone are ~83% of every row in OrderData.
    ---------------------------------------------------------------------------
    CREATE TABLE #Evt
    (
        OrderId      INT      NOT NULL PRIMARY KEY,
        ReceivedAt   DATETIME NULL,
        AckAt        DATETIME NULL,
        ErpSentAt    DATETIME NULL,
        ErpCreatedAt DATETIME NULL,
        AsnSentAt    DATETIME NULL,
        CancelledAt  DATETIME NULL,
        InvoicedAt   DATETIME NULL,
        ErrOrderAt   DATETIME NULL,   -- ERP place-order failure (ERP side)
        ErrAsnAt     DATETIME NULL,   -- ASN send failure (PARTNER side)
        ErrAckAt     DATETIME NULL    -- acknowledgement failure (PARTNER side)
    );

    INSERT INTO #Evt (OrderId, ReceivedAt, AckAt, ErpSentAt, ErpCreatedAt, AsnSentAt, CancelledAt,
                      InvoicedAt, ErrOrderAt, ErrAsnAt, ErrAckAt)
    SELECT  D.OrderId,
            MAX(CASE WHEN D.Type = 'API-JSON'                               THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type IN ('API-ACK-SNT','API-ACK')               THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type = 'ERP-SNT'                                THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type = 'ERP-JSON'                               THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type = 'ASN-SNT'                                THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type IN ('ERPCancelOrder-JSON','ERPCANLN-JSON') THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type IN ('INV-JSON','810-JSON')                 THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type = 'ERP-ERROR'                              THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type = 'ASN-ERR'                                THEN D.CreatedDate END),
            MAX(CASE WHEN D.Type = 'ACK-ERR'                                THEN D.CreatedDate END)
    FROM    OrderData D WITH (NOLOCK)
    WHERE   D.CreatedDate >= @l_From
      AND   (@l_To IS NULL OR D.CreatedDate < @l_To)
      AND   D.Type IN ('API-JSON','API-ACK-SNT','API-ACK','ERP-SNT','ERP-JSON',
                       'ASN-SNT','ERPCancelOrder-JSON','ERPCANLN-JSON','INV-JSON','810-JSON',
                       -- Error events, each paired with the status by the code path that sets it:
                       --   ERROR    <- SCSPlaceOrderRoute 'SCSPlaceOrderError'   writes ERP-ERROR
                       --   ASNERROR <- ASNShipmentNotificationRoute 'SCSASNError' writes ASN-ERR
                       --   ACKERROR <- *GetOrdersRoute '...ACKError'              writes ACK-ERR
                       -- ERPASN-ERR is deliberately NOT here. It is written by SCSASNRoute (the ERP
                       -- ASN *fetch*), which never sets ASNERROR, and it is ~83% of all OrderData
                       -- rows. Its high correlation with ASNERROR orders is not causation.
                       'ERP-ERROR','ASN-ERR','ACK-ERR')
    GROUP BY D.OrderId;

    ---------------------------------------------------------------------------
    -- 2) Per order: the event date its CURRENT status actually means.
    --
    --    EXACTLY ONE event per status — no COALESCE between two different things.
    --    SHIPPED reports ASN-SNT: "when did we send the shipment notification to
    --    the partner". OrderDetail.ShippedDate (the ERP-reported physical ship
    --    date) is deliberately NOT used — mixing the two meant a single caption
    --    had to describe two different events, which is exactly how a tile ends
    --    up lying about its own number. Same reason ACKERROR no longer falls back
    --    to the ASN error.
    --
    --    Status is normalised on letters only because it arrives both raw
    --    ('ASNERROR') and as a display name ('Partially Shipped').
    ---------------------------------------------------------------------------
    CREATE TABLE #OrdEv
    (
        ERPCustomerID VARCHAR(50)   NULL,
        Sts           VARCHAR(50)   NULL,
        EventAt       DATETIME      NULL
    );

    INSERT INTO #OrdEv (ERPCustomerID, Sts, EventAt)
    SELECT  V.ERPCustomerID,
            ISNULL(NULLIF(V.DisplayStatus, ''), ISNULL(V.Status, '')),
            CASE UPPER(REPLACE(ISNULL(NULLIF(V.DisplayStatus, ''), ISNULL(V.Status, '')), ' ', ''))
                WHEN 'SHIPPED'            THEN E.AsnSentAt
                WHEN 'PARTIALLYSHIPPED'   THEN E.AsnSentAt
                WHEN 'SYNCED'             THEN E.ErpCreatedAt
                WHEN 'CANCELLED'          THEN E.CancelledAt
                WHEN 'PARTIALLYCANCELLED' THEN E.CancelledAt
                WHEN 'ACKNOWLEDGED'       THEN E.AckAt
                WHEN 'INVOICED'           THEN E.InvoicedAt
                WHEN 'INPROGRESS'         THEN E.ErpSentAt
                WHEN 'NEW'                THEN E.ReceivedAt
                WHEN 'ERROR'              THEN E.ErrOrderAt
                WHEN 'ASNERROR'           THEN E.ErrAsnAt
                WHEN 'ACKERROR'           THEN E.ErrAckAt
                ELSE NULL   -- anything unmapped keeps the CreatedDate fallback
            END
    FROM    #Evt E
            JOIN VW_Orders V ON V.Id = E.OrderId
    WHERE   V.Status <> 'DELETED'
      AND   (@l_HasAllowed = 0 OR V.ERPCustomerID IN (SELECT ERPCustomerID FROM @l_Allowed))
      AND   (@l_HasFilter  = 0 OR V.ERPCustomerID IN (SELECT ERPCustomerID FROM @l_Filter));

    ---------------------------------------------------------------------------
    -- 3) Orders in the window — drives every count, unchanged from before
    ---------------------------------------------------------------------------
    CREATE TABLE #OrdWin
    (
        ERPCustomerID VARCHAR(50)   NULL,
        CustomerName  NVARCHAR(250) NULL,
        Sts           VARCHAR(50)   NULL,
        CreatedDate   DATETIME      NOT NULL
    );

    INSERT INTO #OrdWin (ERPCustomerID, CustomerName, Sts, CreatedDate)
    SELECT  V.ERPCustomerID,
            V.CustomerName,
            ISNULL(NULLIF(V.DisplayStatus, ''), ISNULL(V.Status, '')),
            V.CreatedDate
    FROM    VW_Orders V
    WHERE   V.CreatedDate >= @l_From
      AND   (@l_To IS NULL OR V.CreatedDate < @l_To)
      AND   V.Status <> 'DELETED'
      AND   (@l_HasAllowed = 0 OR V.ERPCustomerID IN (SELECT ERPCustomerID FROM @l_Allowed))
      AND   (@l_HasFilter  = 0 OR V.ERPCustomerID IN (SELECT ERPCustomerID FROM @l_Filter));

    ---------------------------------------------------------------------------
    -- RESULT SET 1 — customer-wise counts.
    -- This tile is status-agnostic, so its date is the customer's most recent
    -- event of any kind.
    ---------------------------------------------------------------------------
    SELECT      ISNULL(C.CustomerName, '')  AS CustomerName,
                ISNULL(C.ERPCustomerID, '') AS ERPCustomerID,
                C.OrderCount,
                COALESCE(E.LastEventDate, C.LastCreatedDate) AS LastOrderDate,
                -- Tells the UI which date it actually got, so the tile label can never
                -- claim "shipped" over what is really the ingestion date.
                CASE WHEN E.LastEventDate IS NOT NULL THEN 'EVENT' ELSE 'CREATED' END AS LastOrderDateBasis
    FROM        (SELECT CustomerName, ERPCustomerID,
                        COUNT(*) AS OrderCount, MAX(CreatedDate) AS LastCreatedDate
                 FROM   #OrdWin
                 GROUP BY CustomerName, ERPCustomerID) C
    LEFT JOIN   (SELECT ERPCustomerID, MAX(EventAt) AS LastEventDate
                 FROM   #OrdEv
                 GROUP BY ERPCustomerID) E ON E.ERPCustomerID = C.ERPCustomerID
    ORDER BY    C.OrderCount DESC;

    ---------------------------------------------------------------------------
    -- RESULT SET 2 — status-wise counts (the upper KPI tiles)
    ---------------------------------------------------------------------------
    SELECT      C.Sts AS Status,
                C.StatusCount,
                COALESCE(E.LastEventDate, C.LastCreatedDate) AS LastOrderDate,
                CASE WHEN E.LastEventDate IS NOT NULL THEN 'EVENT' ELSE 'CREATED' END AS LastOrderDateBasis
    FROM        (SELECT Sts, COUNT(*) AS StatusCount, MAX(CreatedDate) AS LastCreatedDate
                 FROM   #OrdWin
                 GROUP BY Sts) C
    LEFT JOIN   (SELECT Sts, MAX(EventAt) AS LastEventDate
                 FROM   #OrdEv
                 GROUP BY Sts) E ON E.Sts = C.Sts
    ORDER BY    C.StatusCount DESC;

    ---------------------------------------------------------------------------
    -- RESULT SET 3 — partner + status breakdown (the customer-wise expanded
    -- tiles). This is where "when did we last ship for THIS customer" lands.
    ---------------------------------------------------------------------------
    SELECT      ISNULL(C.ERPCustomerID, '') AS ERPCustomerID,
                C.Sts AS Status,
                C.StatusCount,
                COALESCE(E.LastEventDate, C.LastCreatedDate) AS LastOrderDate,
                CASE WHEN E.LastEventDate IS NOT NULL THEN 'EVENT' ELSE 'CREATED' END AS LastOrderDateBasis
    FROM        (SELECT ERPCustomerID, Sts,
                        COUNT(*) AS StatusCount, MAX(CreatedDate) AS LastCreatedDate
                 FROM   #OrdWin
                 GROUP BY ERPCustomerID, Sts) C
    LEFT JOIN   (SELECT ERPCustomerID, Sts, MAX(EventAt) AS LastEventDate
                 FROM   #OrdEv
                 GROUP BY ERPCustomerID, Sts) E
                  ON E.ERPCustomerID = C.ERPCustomerID AND E.Sts = C.Sts
    ORDER BY    C.ERPCustomerID, C.StatusCount DESC;

    DROP TABLE #OrdWin;
    DROP TABLE #OrdEv;
    DROP TABLE #Evt;
END
GO
