/*==============================================================================
  13 - Shopify (Task 00006): Create ASN (eSyncMate -> Shopify)

    eSyncMate -> Shopify  "<P> - Send ASN Shipment Notification"  RouteType 83 (ShopifyASNShipmentNotificationRoute)
         source  <P> - eSyncMate Get Orders    from script 11 (SP_OrdersData, ASN mode -> the WKE1000 branch below)
         dest    <P> - Create Fulfillment      copy of "<P> - WHSW Inventory Upload" (same store / app / token)
                                               + Parameter notifyCustomer = @NotifyCustomer
         -> Shopify fulfillmentCreate per tracking number, units ASNSNT, order SHIPPED when all are sent

    1. RouteType 83 ShopifyASNShipmentNotification
    2. SP_OrdersData: branch @l_CustomerID IN ('WKE1000') patched into the LIVE procedure, right before the
       MIC1300MP branch. LevelOfService = the Shopify tracking company (exact Shopify spelling) from the ERP
       ShippingMethod text; ShippingMethod = the ERP text as it is.
    3. Connector, 4. Route (In-Active)

  No secret here. Needs scripts 02, 03, 11 (and 12 for the ERP side).
  Run it as often as needed: missing -> inserted, existing -> updated. The WKE1000 branch of the live SP is
  replaced (from its ELSE IF up to the MIC1300MP branch) by @Branch below. An encrypted connector is listed and
  left alone; an existing route keeps its Status, schedule and JobID.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID NVARCHAR(50)  = 'WKE1000';   -- same as scripts 02 / 03 / 11
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';

--- Shopify e-mails the customer the shipment (tracking number) when true
DECLARE @NotifyCustomer VARCHAR(5) = 'false';
/*==============================================================================*/

BEGIN TRY
    IF @ErpCustomerID LIKE '%[^A-Za-z0-9_]%'
        THROW 51460, '@ErpCustomerID may only contain letters, digits and underscore.', 1;

    IF EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 83 AND Name <> 'ShopifyASNShipmentNotification')
        THROW 51461, 'RouteType 83 already exists with another name - check before continuing.', 1;

    DECLARE @RtDescription NVARCHAR(500) = 'Shopify: ERP tracking numbers -> Shopify fulfillments (fulfillmentCreate per tracking number)';

    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 83)
        INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
        VALUES (83, 'ShopifyASNShipmentNotification', @RtDescription, GETDATE(), 1);
    ELSE
        UPDATE dbo.RouteTypes SET Description = @RtDescription, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = 83;

    SELECT Id, Name FROM dbo.RouteTypes WHERE Id IN (11, 83);
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

/*--- 2. SP_OrdersData WKE1000 ASN branch (one batch with the CONFIG block: no GO in this script) ---*/
DECLARE @Branch NVARCHAR(MAX) = N'ELSE IF @l_CustomerID IN (''' + @ErpCustomerID + N''')
            BEGIN
                -- Task 00006: Shopify. LevelOfService = Shopify tracking company (exact spelling Shopify knows),
                -- ShippingMethod = the ERP text as it is, order_line_id = the Shopify line item gid
                -- Carriers from the client (2026-10-26): FedEx, UPS + LTL R+L Carriers, Estes, XPO Logistics, Pilot Freight, Purolator
                SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate, SUM(D.LineQty) LineQty,
                    MAX(CASE WHEN D.ShippingMethod LIKE ''FEDEX%'' OR D.ShippingMethod LIKE ''FED EX%'' OR D.ShippingMethod LIKE ''FDX%'' THEN ''FedEx''
                             WHEN D.ShippingMethod LIKE ''UPS%'' THEN ''UPS''
                             WHEN D.ShippingMethod LIKE ''USPS%'' THEN ''USPS''
                             WHEN D.ShippingMethod LIKE ''DHL%'' THEN ''DHL Express''
                             WHEN D.ShippingMethod LIKE ''Canada Post%'' THEN ''Canada Post''
                             WHEN D.ShippingMethod LIKE ''Purolator%'' THEN ''Purolator''
                             WHEN D.ShippingMethod LIKE ''Canpar%'' THEN ''Canpar''
                             WHEN D.ShippingMethod LIKE ''R+L%'' OR D.ShippingMethod LIKE ''R&L%'' OR D.ShippingMethod LIKE ''R and L%'' OR D.ShippingMethod LIKE ''RL Carrier%'' THEN ''R+L Carriers''
                             WHEN D.ShippingMethod LIKE ''Estes%'' THEN ''Estes''
                             WHEN D.ShippingMethod LIKE ''XPO%'' THEN ''XPO Logistics''
                             WHEN D.ShippingMethod LIKE ''Pilot%'' THEN ''Pilot Freight''
                             ELSE ''FedEx'' END) LevelOfService,
                    MAX(D.ShippingMethod) ShippingMethod,
                    O.ExternalId AS SellerOrderId, MAX(D.ItemID) ItemID, MAX(D.order_line_id) AS order_line_id, MAX(D.Id) AS OrderDetailID
                FROM Orders O WITH (NOLOCK)
                    INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
                    INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
                WHERE C.ERPCustomerID = @l_CustomerID AND (O.Status = @l_OrderStatus OR @l_OrderId <> 0) AND ISNULL(D.TrackingNo, '''') <> ''''
                    AND (ISNULL(D.[Status], '''') = ''ASNRVD'' OR (@l_OrderId <> 0 AND ISNULL(D.[Status], '''') = ''ASNSNT''))
                    AND (@l_OrderId = 0 OR O.Id = @l_OrderId)
                GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate
            END
            ';

BEGIN TRY
    DECLARE @def NVARCHAR(MAX), @needle NVARCHAR(200), @pos INT;

    SELECT @def = definition FROM sys.sql_modules WHERE object_id = OBJECT_ID('dbo.SP_OrdersData');

    IF @def IS NULL
        THROW 51462, 'Cannot read SP_OrdersData (missing, or the login lacks VIEW DEFINITION). Run as a privileged login.', 1;

    DECLARE @variants TABLE (Needle NVARCHAR(200), Ord INT);
    INSERT INTO @variants VALUES
        (N'ELSE IF @l_CustomerID IN (''MIC1300MP'')', 1),
        (N'ELSE IF @l_CustomerID IN(''MIC1300MP'')',  2),
        (N'ELSE IF @l_CustomerID = ''MIC1300MP''',    3);

    SELECT TOP 1 @needle = Needle FROM @variants WHERE CHARINDEX(Needle, @def) > 0 ORDER BY Ord;

    IF @needle IS NULL
        THROW 51463, 'Could not find the MIC1300MP ASN branch in SP_OrdersData. Change the WKE1000 branch by hand (see MANUAL FALLBACK).', 1;

    IF (LEN(@def) - LEN(REPLACE(@def, @needle, N''))) / LEN(@needle) <> 1
        THROW 51464, 'The MIC1300MP branch appears more than once in SP_OrdersData. Change the WKE1000 branch by hand (see MANUAL FALLBACK).', 1;

    SET @pos = CHARINDEX(@needle, @def);

    -- the branch this script wrote before: from its ELSE IF up to the MIC1300MP branch it was put in front of
    DECLARE @Own   NVARCHAR(200) = N'ELSE IF @l_CustomerID IN (''' + @ErpCustomerID + N''')';
    DECLARE @Start INT = CHARINDEX(@Own, @def);

    IF @Start > 0
    BEGIN
        IF CHARINDEX(@Own, @def, @Start + 1) > 0 OR @Start > @pos
            THROW 51469, 'The WKE1000 branch is not right before the MIC1300MP branch (or is there twice). Change it by hand (see MANUAL FALLBACK).', 1;

        -- nothing but our branch between the two: no other customer's ELSE IF in between
        IF CHARINDEX(N'ELSE IF', SUBSTRING(@def, @Start + LEN(@Own), @pos - @Start - LEN(@Own))) > 0
            THROW 51470, 'Another branch sits between the WKE1000 and MIC1300MP branches. Change the WKE1000 branch by hand (see MANUAL FALLBACK).', 1;

        SET @def = STUFF(@def, @Start, @pos - @Start, @Branch);
        PRINT 'Replacing the ' + @ErpCustomerID + ' ASN branch of SP_OrdersData.';
    END
    ELSE IF CHARINDEX(N'''' + @ErpCustomerID + N'''', @def) > 0
        THROW 51471, 'SP_OrdersData names the customer in another form than this script writes. Change the branch by hand (see MANUAL FALLBACK).', 1;
    ELSE
    BEGIN
        SET @def = STUFF(@def, @pos, 0, @Branch);
        PRINT 'Adding the ' + @ErpCustomerID + ' ASN branch before the MIC1300MP branch.';
    END

    -- CREATE or ALTER header -> ALTER (only the first keyword; a comment block may come before it)
    DECLARE @Create INT = PATINDEX(N'%CREATE%PROC%', @def);
    DECLARE @Alter  INT = PATINDEX(N'%ALTER%PROC%', @def);

    IF @Create = 0 AND @Alter = 0
        THROW 51465, 'SP_OrdersData text has no CREATE / ALTER PROCEDURE header. Change the branch by hand (see MANUAL FALLBACK).', 1;

    -- "CREATE OR ALTER PROCEDURE" is already fine as it is
    IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter) AND SUBSTRING(@def, @Create, 15) <> N'CREATE OR ALTER'
        SET @def = STUFF(@def, @Create, LEN('CREATE'), N'ALTER');

    EXEC sp_executesql @def;

    PRINT 'SP_OrdersData altered.';

    SELECT CASE WHEN CHARINDEX(N'''' + @ErpCustomerID + N'''', definition) > 0 THEN 'Branch present' ELSE 'Branch MISSING' END AS SP_OrdersData_ShopifyBranch
      FROM sys.sql_modules WHERE object_id = OBJECT_ID('dbo.SP_OrdersData');
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

/*--- 3. Connectors + 4. Routes ---------------------------------------------------*/
BEGIN TRY
    DECLARE @SparsId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'SPARS');
    DECLARE @EsyncId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PartnerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @PgEsync   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @EsyncId);
    DECLARE @PgSpars   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @SparsId);
    DECLARE @UploadId  INT = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Upload');

    IF @PartnerId IS NULL OR @PgEsync IS NULL OR @PgSpars IS NULL THROW 51466, 'Customer / partner groups missing. Run script 02 first.', 1;
    IF @UploadId IS NULL THROW 51467, 'Connector "<P> - WHSW Inventory Upload" missing. Run script 03 first.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Get Orders')
        THROW 51468, 'Connector "<P> - eSyncMate Get Orders" missing. Run script 11 first.', 1;

    BEGIN TRAN;

    -- copy of the Shopify connector (encrypted or not) + notifyCustomer when it is plain JSON
    DECLARE @Ship VARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Id = @UploadId);
    DECLARE @ShipPlain BIT = CASE WHEN ISJSON(@Ship) = 1 THEN 1 ELSE 0 END;

    IF @ShipPlain = 1
    BEGIN
        SET @Ship = JSON_MODIFY(@Ship, '$.Parmeters', JSON_QUERY('[{"Name":"notifyCustomer","Value":""}]'));
        SET @Ship = JSON_MODIFY(@Ship, '$.Parmeters[0].Value', CASE WHEN LOWER(@NotifyCustomer) = 'true' THEN 'true' ELSE 'false' END);
    END

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));
    DECLARE @ShipTypeId INT = (SELECT TypeId FROM dbo.Connectors WHERE Id = @UploadId);

    INSERT INTO @c VALUES
      (1, @Prefix + ' - Create Fulfillment',    @ShipTypeId, @Ship);

    IF @ShipPlain = 0
        PRINT 'The Shopify connector is encrypted: a NEW "Create Fulfillment" is copied as it is (notifyCustomer NOT added, the code default false applies).';

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

    DECLARE @r TABLE (Seq INT, TypeId INT, Nm VARCHAR(100), Grp VARCHAR(250), Src INT, Dst INT, SrcConn NVARCHAR(200), DstConn NVARCHAR(200),
                      Pg INT, Rep INT);

    INSERT INTO @r VALUES
      (1, 83, @Prefix + ' - Send ASN Shipment Notification',   'ASN''s', @EsyncId, @PartnerId,
          @Prefix + ' - eSyncMate Get Orders',  @Prefix + ' - Create Fulfillment', @PgEsync, 25);

    -- an existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone
    UPDATE e
       SET e.TypeId = r.TypeId, e.SourcePartyId = r.Src, e.DestinationPartyId = r.Dst,
           e.SourceConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = r.SrcConn),
           e.DestinationConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = r.DstConn),
           e.MapId = 0, e.PartyGroupId = r.Pg, e.RouteGroup = r.Grp, e.CustomerName = @ErpCustomerID,
           e.ModifiedDate = GETDATE(), e.ModifiedBy = 1
      FROM dbo.Routes e
      JOIN @r r ON e.Name = r.Nm;

    PRINT 'Routes updated (Status / schedule / JobID left as they are): ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    DECLARE @rbase INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK));

    INSERT INTO dbo.Routes
        (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId,
         CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, JobID,
         Name, RouteGroup, CustomerName)
    SELECT @rbase + ROW_NUMBER() OVER (ORDER BY r.Seq), r.TypeId, 'In-Active', r.Src, r.Dst,
           (SELECT Id FROM dbo.Connectors WHERE Name = r.SrcConn), (SELECT Id FROM dbo.Connectors WHERE Name = r.DstConn),
           0, r.Pg, GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, r.Rep, '', '', '', NULL,
           r.Nm, r.Grp, @ErpCustomerID
      FROM @r r
     WHERE NOT EXISTS (SELECT 1 FROM dbo.Routes e WHERE e.Name = r.Nm);

    PRINT 'Routes inserted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ----------------------------------*/
    SELECT c.Id, c.TypeId, c.Name,
           CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.ConnectivityType') END AS ConnectivityType,
           CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(JSON_VALUE(c.Data, '$.Command'), JSON_VALUE(c.Data, '$.Url')) END AS CommandOrUrl,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_QUERY(c.Data, '$.Parmeters') END AS Parameters,
           CASE WHEN ISJSON(c.Data) = 0 THEN NULL
                WHEN JSON_VALUE(c.Data, '$.ConnectivityType') = 'SqlServer'
                     THEN CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.ConnectionString'), '') = '' THEN 'ConnectionString BLANK' ELSE 'filled' END
                WHEN ISNULL(JSON_VALUE(c.Data, '$.BaseUrl'), '') = '' OR ISNULL(JSON_VALUE(c.Data, '$.ConsumerSecret'), '') = '' THEN 'BaseUrl / secret BLANK'
                ELSE 'filled' END AS Values_
      FROM dbo.Connectors c JOIN @c x ON x.Nm = c.Name
     ORDER BY c.Id;

    SELECT r.Id, r.Name, r.TypeId, r.Status, sp.Name AS SourceParty, dp.Name AS DestinationParty,
           sc.Name AS SourceConnector, dc.Name AS DestinationConnector, r.FrequencyType, r.RepeatCount, r.RouteGroup
      FROM dbo.Routes r
      JOIN dbo.Customers sp ON sp.Id = r.SourcePartyId
      JOIN dbo.Customers dp ON dp.Id = r.DestinationPartyId
      JOIN dbo.Connectors sc ON sc.Id = r.SourceConnectorId
      JOIN dbo.Connectors dc ON dc.Id = r.DestinationConnectorId
     WHERE r.Name IN (SELECT Nm FROM @r)
     ORDER BY r.Id;

    -- VW_Routes uses INNER JOINs: the route must show up here too
    SELECT COUNT(*) AS VisibleInVW_Routes FROM dbo.VW_Routes WHERE Name IN (SELECT Nm FROM @r);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  1. Deploy Processor AND RouteWorker (RouteType 83).
  2. "Fetch ASN from ERP" (script 12) must have brought the tracking numbers (units ASNRVD).
  3. Test Run "Send ASN Shipment Notification". Route log line:
       "Shopify ASN for [<customer>]: N order(s), N tracking number(s) sent, N failed ..."
     OrderDetail units ASNSNT, Orders.Status SHIPPED, Shopify shows the order Fulfilled with the tracking number.
  4. Activate from the UI (Setup > Routes or a Flow), never with an UPDATE - that leaves JobID NULL.

  MANUAL FALLBACK (step 2 stopped with an error): open SP_OrdersData, and in the ASN part (the
  ELSE IF @l_CustomerID ... chain) put the @Branch text above in place of the existing WKE1000 branch,
  or before the last plain ELSE when there is none.
==============================================================================*/
