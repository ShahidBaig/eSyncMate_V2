CREATE PROCEDURE SP_CustomerProductCatalog
    @p_ERPCustomerID    NVARCHAR(250)  =  '',
	@p_FileName			NVARCHAR(250) = '',
	@p_CreatedBy		INT = 0,
    @p_DataTable        CustomerProductCatalogType READONLY -- Assuming you have a user-defined table type
AS
BEGIN
    DECLARE @l_ERPCustomerID    NVARCHAR(250)
    DECLARE @l_FileName			NVARCHAR(250)
    DECLARE @l_CreatedBy		INT

    BEGIN TRY
        SET @l_ERPCustomerID = @p_ERPCustomerID;
        SET @l_FileName		 = @p_FileName;
        SET @l_CreatedBy	 = @p_CreatedBy;
        
        DELETE FROM CustomerProductCatalog WHERE ERPCustomerID = @l_ERPCustomerID;

		INSERT INTO CustomerProductCatalog_Log (ERPCustomerID,FileName,CreatedDate,CreatedBy)
		VALUES (@l_ERPCustomerID,@l_FileName,GETDATE(),@l_CreatedBy)
        
        INSERT INTO CustomerProductCatalog (ERPCustomerID,TCIN, PartnerSKU, ProductTitle, ItemType, ItemTypeID, Relationship, PublishStatus, DataUpdatesStatus,
            Price, OfferPrice, MAPPrice, OfferDiscount, PriceLastUpdated, DistributionCenterName, DistributionCenterID, Inventory, InventoryLastUpdated,CreatedDate,CreatedBy)
        SELECT @l_ERPCustomerID,TCIN, PartnerSKU, ProductTitle, ItemType, ItemTypeID, Relationship, PublishStatus, DataUpdatesStatus,
            ISNULL(Price,0), ISNULL(OfferPrice,0), ISNULL(MAPPrice,0), ISNULL(OfferDiscount,0), PriceLastUpdated, DistributionCenterName, DistributionCenterID, Inventory, InventoryLastUpdated,GETDATE(),@l_CreatedBy
        FROM @p_DataTable;
    END TRY
    BEGIN CATCH
        
        DECLARE @ErrorMessage NVARCHAR(MAX);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;

        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();
        
        PRINT 'Error Message: ' + @ErrorMessage;
        THROW;
    END CATCH;
END