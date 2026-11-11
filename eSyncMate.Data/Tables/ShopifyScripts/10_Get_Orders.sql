/*==============================================================================
  10 - Shopify (Task 00006): "Shopify - Get Orders"

  Shopify orders -> eSyncMate Orders (Status New) + OrderData (API-JSON) + OrderDetail,
  like the Walmart / Lowes / Amazon Get Orders routes. Code: ShopifyGetOrdersRoute.

    1. RouteType 82 ShopifyGetOrders
    2. Connectors (copies of script 03's, so no secret / connection string is typed again):
         <P> - Get Orders                   copy of "<P> - WHSW Inventory Upload" (same store, app, token)
                                            + Parameters  query = @OrderQuery, days = @OrderDays
         <P> - eSyncMate Data Connection    the eSyncMate DB: @DbConnectionString, or (blank) a copy of
                                            "<P> - WHSW Inventory Get Data"
       Fill the CONFIG block, run, then clear @DbConnectionString again. Do not commit or e-mail a filled-in copy.
       If the source connector is already encrypted (saved from the UI), a NEW copy is made as it is (without the
       two Parameters: the code uses the same defaults); an existing one is left alone - change it in Setup > Connectors.
    3. Route "<P> - Get Orders": In-Active, Minutely @RepeatMinutes, customer -> eSyncMate, RouteGroup Orders.

  OrderNumber = the Shopify order id (e.g. 8690887852218); CustomerOrderNo = "#1001".
  Run it as often as needed: missing -> inserted, existing -> updated (RouteType description, both connectors with
  the current query / days, the route's parties / connectors / group). An encrypted connector is listed and left
  alone; an existing route keeps its Status, schedule and JobID.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID VARCHAR(50)    = 'WKE1000';   -- same as scripts 02 / 03 / 07
DECLARE @Prefix        NVARCHAR(100)  = 'Shopify';      -- same as script 03
DECLARE @OrderQuery    NVARCHAR(500)  = '(financial_status:paid OR financial_status:pending) fulfillment_status:unfulfilled status:open';   -- Shopify order search syntax; client 2026-10-26: Paid + Pending, Unfulfilled, Open
DECLARE @OrderDays     INT            = 10;             -- orders created in the last N days
DECLARE @RepeatMinutes INT            = 5;

--- eSyncMate DB for "<P> - eSyncMate Data Connection" (where the orders are saved)
---   format: Server=<server>,<port>;Database=ESYNCMATE;UID=<user>;PWD=<password>;
---   blank = copy the connection of "<P> - WHSW Inventory Get Data" (script 03)
DECLARE @DbConnectionString NVARCHAR(500) = '';
/*==============================================================================*/

BEGIN TRY
    IF EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 82 AND Name <> 'ShopifyGetOrders')
        THROW 51440, 'RouteType 82 already exists with another name - check before continuing.', 1;

    DECLARE @CustomerId  INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @eSyncMateId INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PgId        INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @CustomerId AND DestinationPartyId = @eSyncMateId);
    DECLARE @UploadId    INT = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Upload');
    DECLARE @GetDataId   INT = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Get Data');
    DECLARE @SrcName     NVARCHAR(200) = @Prefix + ' - Get Orders';
    DECLARE @DstName     NVARCHAR(200) = @Prefix + ' - eSyncMate Data Connection';
    DECLARE @RouteName   VARCHAR(100)  = @Prefix + ' - Get Orders';

    IF @CustomerId IS NULL OR @PgId IS NULL THROW 51441, 'Customer / partner group missing. Run script 02 first.', 1;
    IF @UploadId IS NULL OR @GetDataId IS NULL THROW 51442, 'Shopify connectors missing. Run script 03 first.', 1;

    BEGIN TRAN;

    DECLARE @RtDescription NVARCHAR(500) = 'Shopify: paid or pending / unfulfilled / open orders -> eSyncMate Orders, OrderData (API-JSON), OrderDetail';

    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 82)
        INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
        VALUES (82, 'ShopifyGetOrders', @RtDescription, GETDATE(), 1);
    ELSE
        UPDATE dbo.RouteTypes SET Description = @RtDescription, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = 82;

    /*--- 2. Connectors (Connectors.Id is NOT identity) ------------------------*/
    DECLARE @SrcData VARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Id = @UploadId);

    -- the values go in through JSON_MODIFY, which escapes them; an encrypted source is copied as it is (code defaults apply)
    IF ISJSON(@SrcData) = 1
    BEGIN
        SET @SrcData = JSON_MODIFY(@SrcData, '$.Parmeters', JSON_QUERY('[{"Name":"query","Value":""},{"Name":"days","Value":""}]'));
        SET @SrcData = JSON_MODIFY(@SrcData, '$.Parmeters[0].Value', @OrderQuery);
        SET @SrcData = JSON_MODIFY(@SrcData, '$.Parmeters[1].Value', CAST(@OrderDays AS VARCHAR(10)));
    END

    -- with @DbConnectionString: a plain SQL Server connector of its own; blank: a copy of the Get Data connector
    DECLARE @DstData VARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Id = @GetDataId);

    IF LTRIM(RTRIM(ISNULL(@DbConnectionString, ''))) <> ''
        SET @DstData = JSON_MODIFY(JSON_MODIFY(
            '{"ConnectivityType":"SqlServer","CommandType":"","Command":"","KeyFieldName":"","DataFieldName":"","CustomerID":"","JsonDataCollectionName":"","ConnectionString":""}',
            '$.ConnectionString', @DbConnectionString), '$.CustomerID', @ErpCustomerID);

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));
    -- a multi-row VALUES list takes no subqueries
    DECLARE @SrcTypeId INT = (SELECT TypeId FROM dbo.Connectors WHERE Id = @UploadId);
    DECLARE @DstTypeId INT = (SELECT TypeId FROM dbo.Connectors WHERE Id = @GetDataId);

    INSERT INTO @c VALUES
      (1, @SrcName, @SrcTypeId, @SrcData),
      (2, @DstName, @DstTypeId, @DstData);

    /*--- Upsert (same block in every Task 00005 / 00006 script) ----------------
      A new name is inserted. An existing plain-JSON connector gets this payload, but a secret / connection
      string left blank here keeps its stored value. An encrypted connector (saved from Setup > Connectors),
      or an encrypted payload copied from one, cannot be read by SQL: left as it is and listed below.          */
    DECLARE @u_Nm NVARCHAR(200), @u_TypeId INT, @u_New NVARCHAR(MAX), @u_Old NVARCHAR(MAX), @u_Ins INT = 0, @u_Upd INT = 0;
    DECLARE @u_Encrypted TABLE (Name NVARCHAR(200));
    DECLARE u_cc CURSOR LOCAL FAST_FORWARD FOR SELECT Nm, TypeId, Payload FROM @c ORDER BY Seq;

    OPEN u_cc;
    FETCH NEXT FROM u_cc INTO @u_Nm, @u_TypeId, @u_New;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = @u_Nm)
        BEGIN
            INSERT INTO dbo.Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
            SELECT ISNULL(MAX(Id), 0) + 1, @u_TypeId, @u_Nm, @u_New, GETDATE(), 1 FROM dbo.Connectors WITH (UPDLOCK, HOLDLOCK);
            SET @u_Ins += 1;
        END
        ELSE
        BEGIN
            SET @u_Old = ISNULL((SELECT TOP 1 Data FROM dbo.Connectors WHERE Name = @u_Nm), '{}');

            IF ISJSON(@u_Old) = 0 OR ISJSON(@u_New) = 0
                INSERT INTO @u_Encrypted VALUES (@u_Nm);
            ELSE
            BEGIN
                IF ISNULL(JSON_VALUE(@u_New, '$.ConnectionString'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConnectionString'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConnectionString', JSON_VALUE(@u_Old, '$.ConnectionString'));
                IF ISNULL(JSON_VALUE(@u_New, '$.ConsumerKey'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConsumerKey'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConsumerKey', JSON_VALUE(@u_Old, '$.ConsumerKey'));
                IF ISNULL(JSON_VALUE(@u_New, '$.ConsumerSecret'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConsumerSecret'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConsumerSecret', JSON_VALUE(@u_Old, '$.ConsumerSecret'));
                IF ISNULL(JSON_VALUE(@u_New, '$.Token'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.Token'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.Token', JSON_VALUE(@u_Old, '$.Token'));
                IF ISNULL(JSON_VALUE(@u_New, '$.TokenSecret'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.TokenSecret'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.TokenSecret', JSON_VALUE(@u_Old, '$.TokenSecret'));
                IF ISNULL(JSON_VALUE(@u_New, '$.Password'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.Password'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.Password', JSON_VALUE(@u_Old, '$.Password'));
                IF ISNULL(JSON_VALUE(@u_New, '$.BaseUrl'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.BaseUrl'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.BaseUrl', JSON_VALUE(@u_Old, '$.BaseUrl'));

                UPDATE dbo.Connectors SET TypeId = @u_TypeId, Data = @u_New, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Name = @u_Nm;
                SET @u_Upd += 1;
            END
        END

        FETCH NEXT FROM u_cc INTO @u_Nm, @u_TypeId, @u_New;
    END

    CLOSE u_cc;
    DEALLOCATE u_cc;

    DECLARE @u_Enc INT = (SELECT COUNT(*) FROM @u_Encrypted);   -- PRINT takes no subquery

    PRINT CONCAT('Connectors inserted: ', @u_Ins, ', updated: ', @u_Upd, ', encrypted and NOT changed: ', @u_Enc);

    SELECT Name AS EncryptedConnector_NotChanged_EditInUI FROM @u_Encrypted;

    /*--- 3. Route -------------------------------------------------------------*/
    DECLARE @SrcId INT = (SELECT Id FROM dbo.Connectors WHERE Name = @SrcName);
    DECLARE @DstId INT = (SELECT Id FROM dbo.Connectors WHERE Name = @DstName);

    -- an existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 82, SourcePartyId = @CustomerId, DestinationPartyId = @eSyncMateId,
               SourceConnectorId = @SrcId, DestinationConnectorId = @DstId, MapId = 0, PartyGroupId = @PgId,
               RouteGroup = 'Orders', CustomerName = @ErpCustomerID, ModifiedDate = GETDATE(), ModifiedBy = 1
         WHERE Name = @RouteName;

        PRINT 'Updated route ' + @RouteName + ' (Status / schedule / JobID left as they are)';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Routes
            (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId,
             CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, JobID,
             Name, RouteGroup, CustomerName)
        SELECT ISNULL(MAX(Id), 0) + 1, 82, 'In-Active', @CustomerId, @eSyncMateId, @SrcId, @DstId, 0, @PgId,
               GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, @RepeatMinutes, '', '', '', NULL,
               @RouteName, 'Orders', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @RouteName;
    END

    COMMIT TRAN;

    /*--- Verification (no secrets shown) --------------------------------------*/
    SELECT Id, Name, Description FROM dbo.RouteTypes WHERE Id = 82;

    SELECT c.Id, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.ConnectivityType') END AS ConnectivityType,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_QUERY(c.Data, '$.Parmeters') END AS Parameters,
           CASE WHEN ISJSON(c.Data) = 0 THEN NULL
                WHEN JSON_VALUE(c.Data, '$.ConnectivityType') <> 'SqlServer' THEN NULL
                WHEN ISNULL(JSON_VALUE(c.Data, '$.ConnectionString'), '') = '' THEN 'BLANK - fill it in the UI'
                ELSE 'filled' END AS ConnectionString
      FROM dbo.Connectors c WHERE c.Name IN (@SrcName, @DstName);

    SELECT r.Id, r.Name, r.TypeId, r.Status, r.CustomerName, sc.Name AS SourceConnector, dc.Name AS DestinationConnector, r.FrequencyType, r.RepeatCount
      FROM dbo.Routes r
      JOIN dbo.Connectors sc ON sc.Id = r.SourceConnectorId
      JOIN dbo.Connectors dc ON dc.Id = r.DestinationConnectorId
     WHERE r.Name = @RouteName;

    -- VW_Routes uses INNER JOINs: the route must show up here too
    SELECT Id, Name FROM dbo.VW_Routes WHERE Name = @RouteName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  1. Deploy BOTH Processor and RouteWorker (RouteType 82).
  2. Test Run "Shopify - Get Orders". Route log line:
     "Shopify orders for [<customer>] (...): N read in N page(s), N new saved (N with Status ERROR), N already in eSyncMate, N not saved."
  3. Check:
       SELECT Id, OrderNumber, CustomerOrderNo, Status, ShipToName, ShipToCity, ShipToCountry, ShippingMethod
         FROM Orders WHERE CustomerId = (SELECT Id FROM Customers WHERE ERPCustomerID = 'WKE1000');
  4. Activate from the UI (Setup > Routes or the Flow), never with an UPDATE - that leaves JobID NULL.
==============================================================================*/
