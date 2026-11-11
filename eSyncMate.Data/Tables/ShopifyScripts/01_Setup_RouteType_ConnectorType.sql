/*==============================================================================
  01 - Shopify inventory (Task 00006): RouteType + ConnectorType

    80  ShopifyInventoryUpload  -> ShopifyInventoryUploadRoute
          UPDATED/NEW SCSInventoryFeed rows (ERP Full / Differential feed) -> Shopify
          inventorySetQuantities, absolute "available" per Shopify location
          (warehouse -> location from the Warehouse Mapping screen).

    ConnectorTypes 'Shopify' - only groups the Shopify connectors in Setup > Connectors.

  Same id as RouteTypesEnum.ShopifyInventoryUpload in CommonUtils.cs.
  Run it as often as needed: missing -> inserted, existing -> Description updated. RouteType 80 used by ANOTHER
  name stops the script. The ConnectorType has nothing to update.
==============================================================================*/

SET NOCOUNT ON;

BEGIN TRY
    DECLARE @Description NVARCHAR(500) = 'Shopify warehouse-wise inventory: ERP stock per warehouse -> Shopify available quantity per location';

    IF EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 80 AND Name <> 'ShopifyInventoryUpload')
        THROW 51340, 'RouteType 80 already exists with another name - check before continuing.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 80)
        INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
        VALUES (80, 'ShopifyInventoryUpload', @Description, GETDATE(), 1);
    ELSE
        UPDATE dbo.RouteTypes SET Description = @Description, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = 80;

    -- ConnectorTypes.Id is NOT identity
    IF NOT EXISTS (SELECT 1 FROM dbo.ConnectorTypes WHERE Name = 'Shopify')
        INSERT INTO dbo.ConnectorTypes (Id, Name, Party, CreatedDate, CreatedBy)
        SELECT ISNULL(MAX(Id), 0) + 1, 'Shopify', 'Shopify', GETDATE(), 1
          FROM dbo.ConnectorTypes WITH (UPDLOCK, HOLDLOCK);

    SELECT Id, Name, Description FROM dbo.RouteTypes WHERE Id = 80;
    SELECT Id, Name, Party FROM dbo.ConnectorTypes WHERE Name = 'Shopify';
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
