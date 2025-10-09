CREATE PROCEDURE [dbo].[InsertCustomerProductCatalog]
    --@p_ERPCustomerID    VARCHAR(250)  =  '',
    @p_DataTable        CustomerProductCatalogType READONLY -- Assuming you have a user-defined table type
AS
BEGIN
    --DECLARE @l_ERPCustomerID    VARCHAR(250);

    BEGIN TRY
        --SET @l_ERPCustomerID = @p_ERPCustomerID;

        -- Delete existing records for the given ERPCustomerID
        DELETE FROM CustomerProductCatalog ---WHERE ERPCustomerID = @l_ERPCustomerID;

        -- Insert new records from the DataTable parameter
        INSERT INTO CustomerProductCatalog (TCIN, PartnerSKU, ProductTitle, ItemType, ItemTypeID, Relationship, PublishStatus, DataUpdatesStatus,
            Price, OfferPrice, MAPPrice, OfferDiscount, PriceLastUpdated, DistributionCenterName, DistributionCenterID, Inventory, InventoryLastUpdated)
        SELECT TCIN, PartnerSKU, ProductTitle, ItemType, ItemTypeID, Relationship, PublishStatus, DataUpdatesStatus,
            ISNULL(Price,0), ISNULL(OfferPrice,0), ISNULL(MAPPrice,0), ISNULL(OfferDiscount,0), PriceLastUpdated, DistributionCenterName, DistributionCenterID, Inventory, InventoryLastUpdated
        FROM @p_DataTable;
    END TRY
    BEGIN CATCH
        -- Handle the exception here
        DECLARE @ErrorMessage NVARCHAR(MAX);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;

        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- You can log the error or perform additional actions as needed
        -- For example, print the error message
        PRINT 'Error Message: ' + @ErrorMessage;

        -- Re-throw the error (optional)
        THROW;
    END CATCH;
END

