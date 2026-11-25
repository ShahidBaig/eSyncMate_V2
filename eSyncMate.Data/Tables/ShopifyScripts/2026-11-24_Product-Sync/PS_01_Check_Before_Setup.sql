/*==============================================================================
  PS 01 - Shopify (Task 00006), customer WKE1000: what the "Shopify - Sync Products" route (RouteType 81) needs

  READ-ONLY: only SELECTs. No token, secret or connection string is shown.

  Why: the upload sent the UPC as the Shopify inventory item number and Shopify answered
  INVALID_INVENTORY_ITEM (test of 2026-11-24, item WESAS21704EO1). The Sync Products route reads the
  inventory items of the store and stores  ItemId -> gid://shopify/InventoryItem/...  in
  CustomerProductCatalogPrices; the upload then sends that id.

  Paste all grids back: the setup script is written from this output.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';

/*--- 1. Objects the route uses --------------------------------------------------------*/
SELECT '1. Objects' AS Grid, o.Name, o.Kind,
       CASE WHEN o.Kind = 'Table type' AND TYPE_ID('dbo.' + o.Name) IS NOT NULL THEN 'present'
            WHEN o.Kind <> 'Table type' AND OBJECT_ID('dbo.' + o.Name) IS NOT NULL THEN 'present'
            ELSE 'MISSING' END AS [State]
  FROM (VALUES ('CustomerProductCatalogPrices', 'Table'), ('Sp_SCS_GetProductCatalog', 'Procedure'),
               ('Sp_InsertMissingTargetCatalogPrices', 'Procedure'), ('TargetCatalogPriceType', 'Table type')) o (Name, Kind);

/*--- 2. Route type 81 -----------------------------------------------------------------*/
SELECT '2. Route type' AS Grid, T.*
  FROM dbo.RouteTypes T
 WHERE T.Id IN (80, 81);

/*--- 3. Branches of Sp_SCS_GetProductCatalog ------------------------------------------*/
SELECT '3. SP branches' AS Grid, b.Branch,
       CASE WHEN CHARINDEX('''' + b.Branch + '''', OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'))) > 0 THEN 'present' ELSE 'MISSING' END AS [State]
  FROM (VALUES ('ShopifyInventoryUpload'), ('ShopifyProductSync')) b (Branch);

/*--- 4. Shape of the table type and of the insert procedure (parameters) --------------*/
SELECT '4a. Table type columns' AS Grid, c.column_id, c.name, TYPE_NAME(c.user_type_id) AS DataType, c.max_length
  FROM sys.table_types tt
  JOIN sys.columns c ON c.object_id = tt.type_table_object_id
 WHERE tt.name = 'TargetCatalogPriceType'
 ORDER BY c.column_id;

SELECT '4b. Insert SP parameters' AS Grid, p.parameter_id, p.name, TYPE_NAME(p.user_type_id) AS DataType, p.is_readonly
  FROM sys.parameters p
 WHERE p.object_id = OBJECT_ID('dbo.Sp_InsertMissingTargetCatalogPrices')
 ORDER BY p.parameter_id;

/*--- 5. Routes of type 81 that exist already (any customer) ----------------------------*/
SELECT '5. Routes type 81' AS Grid, R.Id, R.Name, R.Status, R.CustomerName, R.SourceConnectorId, R.DestinationConnectorId, R.JobID
  FROM dbo.Routes R
 WHERE R.TypeId = 81;

/*--- 6. Connectors of the upload route: the sync route uses the same store connection --*/
SELECT '6. Upload route connectors' AS Grid, R.Id AS RouteId, R.Name AS Route, R.SourcePartyId, R.DestinationPartyId, R.PartyGroupId, R.RouteGroup,
       CASE WHEN c.Id = R.SourceConnectorId THEN 'Source' ELSE 'Destination' END AS UsedAs,
       c.Id AS ConnectorId, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain' ELSE 'Encrypted' END AS Storage,
       CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.ConnectivityType') END AS ConnType,
       CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.CommandType') END AS CommandType,
       CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(JSON_VALUE(c.Data, '$.Command'), JSON_VALUE(c.Data, '$.Url')) END AS UrlOrCommand,
       CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.CustomerID') END AS CustomerID
  FROM dbo.Routes R
  JOIN dbo.Connectors c ON c.Id IN (R.SourceConnectorId, R.DestinationConnectorId)
 WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 80;

/*--- 7. Connectors with "Shopify" in the name (to see whether sync connectors exist) ---*/
SELECT '7. Shopify connectors' AS Grid, c.Id, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain' ELSE 'Encrypted' END AS Storage
  FROM dbo.Connectors c
 WHERE c.Name LIKE 'Shopify%'
 ORDER BY c.Id;

/*--- 8. Ids the customer has in the product catalog now --------------------------------*/
SELECT '8. Catalog ids' AS Grid, COUNT(*) AS [Rows],
       SUM(CASE WHEN PR.ProductId LIKE 'gid://shopify/InventoryItem/%' THEN 1 ELSE 0 END) AS ShopifyIds
  FROM dbo.CustomerProductCatalogPrices PR
 WHERE PR.CustomerID = @ErpCustomerID;

/*--- 9. What the route will compare with the SKU of the Shopify store ------------------
      Today's rule: Shopify SKU = CustomerItemCode (the UPC); ItemId only when CustomerItemCode is blank.
      If the store keeps the item code in SKU and the UPC in Barcode, nothing will match with this rule.   */
SELECT TOP 5 '9. Compared with the Shopify SKU' AS Grid, F.ItemId, F.CustomerItemCode,
       ISNULL(NULLIF(LTRIM(RTRIM(F.CustomerItemCode)), ''), F.ItemId) AS ComparedWithShopifySku
  FROM dbo.SCSInventoryFeed F
 WHERE F.CustomerID = @ErpCustomerID
 ORDER BY CASE WHEN F.ItemId = 'WESAS21704EO1' THEN 0 ELSE 1 END, F.ItemId;
GO
