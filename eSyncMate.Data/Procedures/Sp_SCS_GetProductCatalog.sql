ALTER PROCEDURE [dbo].[Sp_SCS_GetProductCatalog]
    @p_CustomerID        VARCHAR(500) = '',
    @p_RouteTypeID       VARCHAR(500) = '',
    @p_UserNo        INT = 0
AS
BEGIN
    DECLARE @l_CustomerID			NVARCHAR(500)
    DECLARE @l_UserNo				INT
	DECLARE @l_RouteTypeID			VARCHAR(500)
	DECLARE @l_lastRecurrecnceDate	DATETIME
    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
		SET @l_RouteTypeID = @p_RouteTypeID

		IF @l_RouteTypeID = 'ProductCatalog'
		BEGIN
			SELECT ProductId,Brand,ItemID,UPC,ItemTypeName,ProductRelation,ParentID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,Type,VariationType,JsonData,CustomerID,SyncStatus,id,UnListed,RetryCount
			FROM SCS_CustomerProductCatalog WITH (NOLOCK) 
			WHERE CustomerID  = @l_CustomerID AND SyncStatus IN ('UPDATED','NEW','UNLISTED','PENDING')
		END
		ELSE IF  @l_RouteTypeID = 'ProductCatalogStatus'
		BEGIN
			SELECT ProductId,Brand,ItemID,UPC,ItemTypeName,ProductRelation,ParentID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,Type,VariationType,JsonData,CustomerID,SyncStatus,id,UnListed,
				is_add_on,two_day_shipping_eligible,shipping_exclusion,seller_return_policy
			FROM SCS_CustomerProductCatalog  WITH (NOLOCK)
			WHERE CustomerID  = @l_CustomerID AND SyncStatus IN ('PENDING')
		END
		ELSE IF  @l_RouteTypeID = 'SCSItemPrices'
		BEGIN
			SELECT ProductId,Brand,ItemID,UPC,ItemTypeName,ProductRelation,ParentID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,Type,VariationType,JsonData,CustomerID,SyncStatus,id,UnListed
			FROM SCS_CustomerProductCatalog WITH (NOLOCK) 
			WHERE CustomerID  = @l_CustomerID AND SyncStatus IN ('APPROVED') AND (VariationType IS NULL OR ISNULL(VariationType,'') NOT IN ('VAP'))
		END
		ELSE IF  @l_RouteTypeID = 'SCSBulkItemPrices'
		BEGIN
			SELECT SP.ProductId,SP.ItemID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,SP.CustomerID,SyncStatus,SP.id,UnListed
			FROM SCS_ProductPrices SP WITH (NOLOCK) 
				--INNER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON SP.CustomerID = PR.CustomerID AND SP.ItemId = PR.ItemId
			WHERE SP.CustomerID  = @l_CustomerID AND SyncStatus IN ('APPROVED') 
		END
		ELSE IF  @l_RouteTypeID = 'SCSUpdateInventory'
		BEGIN
			SELECT  INV.CustomerID,INV.ItemId,INV.CustomerItemCode,INV.Total_ATS,INV.ATS_L10,INV.ATS_L21,INV.ATS_L28,INV.ATS_L30,INV.ATS_L34,INV.ATS_L35,INV.ATS_L36,INV.ATS_L37,INV.ATS_L40,
				   INV.ATS_L41,INV.ATS_L55,INV.ATS_L60,INV.ATS_L70,INV.ATS_L91,PR.Status SyncStatus,PR.id,PR.ProductId
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				INNER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
			WHERE INV.CustomerID  = @l_CustomerID AND PR.Status IN ('APPROVED','APPROVED_PR') AND INV.[Status] IN ('UPDATED','NEW')
		END
		ELSE IF  @l_RouteTypeID = 'WalmartUploadInventory'
		BEGIN
			SELECT INV.CustomerID,INV.ItemId,INV.CustomerItemCode,INV.Total_ATS,INV.ATS_L10,INV.ATS_L21,INV.ATS_L28,INV.ATS_L30,INV.ATS_L34,INV.ATS_L35,INV.ATS_L36,INV.ATS_L37,INV.ATS_L40,
				   INV.ATS_L41,INV.ATS_L55,INV.ATS_L60,INV.ATS_L70,INV.ATS_L91,ISNULL(PR.id,-1) id,ISNULL(PR.ProductId,INV.CustomerItemCode) ProductId,INV.ATS_L29,INV.ATS_L65,INV.ATS_L56,
					INV.ATS_L57
			FROM SCSInventoryFeed INV WITH (NOLOCK)
			LEFT OUTER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
			WHERE  INV.CustomerID  = @l_CustomerID AND INV.[Status] IN ('UPDATED','NEW')
		END
		ELSE IF  @l_RouteTypeID = 'MacysInventoryUpload'
		BEGIN
			SELECT INV.CustomerID,INV.ItemId,INV.CustomerItemCode,INV.Total_ATS,PR.id,PR.ProductId,REPLACE(PP.ListPrice, '$', '') AS ListPrice
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				INNER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
				INNER JOIN SCS_ProductPrices PP WITH (NOLOCK) ON INV.CustomerID = PP.CustomerID AND INV.ItemId = PP.ItemId
			WHERE  INV.CustomerID  = @l_CustomerID AND INV.[Status] IN ('UPDATED','NEW')
		END
		ELSE IF  @l_RouteTypeID = 'MacysBulkItemPrices'
		BEGIN
			SELECT SP.ProductId,SP.ItemID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,SP.CustomerID,SyncStatus,SP.id,UnListed,INV.Total_ATS,Inv.CustomerItemCode
			FROM SCS_ProductPrices SP WITH (NOLOCK) 
				INNER JOIN SCSInventoryFeed INV WITH (NOLOCK) ON SP.CustomerID = INV.CustomerID AND SP.ItemId = INV.ItemId
			WHERE SP.CustomerID  = @l_CustomerID AND SyncStatus IN ('APPROVED') 
		END
		ELSE IF  @l_RouteTypeID = 'TargetPlusInventoryFeedWHSWise'
		BEGIN
			SELECT  INV.CustomerID,INV.ItemId,INV.CustomerItemCode,PR.Status SyncStatus,PR.id,PR.ProductId,TPS.WHSID,
			CASE WHEN TPS.WHSID = 'L10' THEN INV.ATS_L10
				 WHEN TPS.WHSID = 'L21' THEN INV.ATS_L21
				 WHEN TPS.WHSID = 'L28' THEN INV.ATS_L28
				 WHEN TPS.WHSID = 'L35' THEN INV.ATS_L35
				 WHEN TPS.WHSID = 'L36' THEN INV.ATS_L36
				 WHEN TPS.WHSID = 'L37' THEN INV.ATS_L37
				 WHEN TPS.WHSID = 'L40' THEN INV.ATS_L40
				 WHEN TPS.WHSID = 'L41' THEN INV.ATS_L41
				 WHEN TPS.WHSID = 'L55' THEN INV.ATS_L55
				 WHEN TPS.WHSID = 'L60' THEN INV.ATS_L60
				 WHEN TPS.WHSID = 'L70' THEN INV.ATS_L70
				 WHEN TPS.WHSID = 'L29' THEN ISNULL(INV.ATS_L29,0)
				 WHEN TPS.WHSID = 'L65' THEN ISNULL(INV.ATS_L65,0)
				 WHEN TPS.WHSID = 'L56' THEN ISNULL(INV.ATS_L56,0)
				 WHEN TPS.WHSID = 'L57' THEN ISNULL(INV.ATS_L57,0)

				 END Total_ATS,TPS.ShipNode
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				INNER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
				INNER JOIN TargetPlusShipNodes TPS WITH (NOLOCK) ON INV.CustomerID = TPS.CustomerID
			WHERE INV.CustomerID  = @l_CustomerID AND PR.Status IN ('APPROVED','APPROVED_PR') AND INV.[Status] IN ('UPDATED','NEW')

		END
		ELSE IF  @l_RouteTypeID = 'LowesInventoryUpload'
		BEGIN
			SELECT INV.CustomerID,INV.ItemId,INV.CustomerItemCode,INV.Total_ATS,PR.id,PR.ProductId,REPLACE(PP.ListPrice, '$', '') AS ListPrice
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				INNER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
				INNER JOIN SCS_ProductPrices PP WITH (NOLOCK) ON INV.CustomerID = PP.CustomerID AND INV.ItemId = PP.ItemId
			WHERE  INV.CustomerID  = @l_CustomerID AND INV.[Status] IN ('UPDATED','NEW')
		END
		ELSE IF  @l_RouteTypeID = 'LowesBulkItemPrices'
		BEGIN
			SELECT SP.ProductId,SP.ItemID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,SP.CustomerID,SyncStatus,SP.id,UnListed,INV.Total_ATS,Inv.CustomerItemCode
			FROM SCS_ProductPrices SP WITH (NOLOCK) 
				INNER JOIN SCSInventoryFeed INV WITH (NOLOCK) ON SP.CustomerID = INV.CustomerID AND SP.ItemId = INV.ItemId
			WHERE SP.CustomerID  = @l_CustomerID AND SyncStatus IN ('APPROVED') 
		END
		ELSE IF  @l_RouteTypeID = 'AmazonInventoryUpload'
		BEGIN
			SELECT INV.CustomerID,INV.CustomerItemCode AS CustomerItemCode,INV.ItemId,INV.CustomerItemCode,INV.Total_ATS,PR.id,PR.ProductId,0 AS ListPrice
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				INNER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
				--INNER JOIN SCS_ProductPrices PP WITH (NOLOCK) ON INV.CustomerID = PP.CustomerID AND INV.ItemId = PP.ItemId
			WHERE  INV.CustomerID  = @l_CustomerID AND INV.[Status] IN ('UPDATED','NEW') AND INV.CustomerItemCode NOT IN ('B0BMB7WJDV FLU4095A','B07JJYN64G BER219G-8','B082PJZ6R2 EVK256E-8','B01M0FB151 ASG741A-8','B0CGBQV2RG TSN102B-7SQ')
		END
		ELSE IF  @l_RouteTypeID = 'KnotInventoryUpload'
		BEGIN
			SELECT INV.CustomerID,INV.ItemId,INV.CustomerItemCode,INV.Total_ATS,PR.id,PR.ProductId,REPLACE(PP.ListPrice, '$', '') AS ListPrice
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				LEFT OUTER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
				INNER JOIN SCS_ProductPrices PP WITH (NOLOCK) ON INV.CustomerID = PP.CustomerID AND INV.ItemId = PP.ItemId
			WHERE  INV.CustomerID  = @l_CustomerID AND INV.[Status] IN ('UPDATED','NEW')
		END
		ELSE IF  @l_RouteTypeID = 'KnotBulkItemPrices'
		BEGIN
			SELECT SP.ProductId,SP.ItemID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,SP.CustomerID,SyncStatus,SP.id,UnListed,INV.Total_ATS,Inv.CustomerItemCode
			FROM SCS_ProductPrices SP WITH (NOLOCK) 
				INNER JOIN SCSInventoryFeed INV WITH (NOLOCK) ON SP.CustomerID = INV.CustomerID AND SP.ItemId = INV.ItemId
			WHERE SP.CustomerID  = @l_CustomerID AND SyncStatus IN ('APPROVED') 
		END
		ELSE IF  @l_RouteTypeID = 'MichealInventoryUpload'
		BEGIN
			SELECT INV.CustomerID,INV.ItemId,INV.CustomerItemCode,INV.Total_ATS,PR.id,PR.ProductId
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				LEFT OUTER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
				--INNER JOIN SCS_ProductPrices PP WITH (NOLOCK) ON INV.CustomerID = PP.CustomerID AND INV.ItemId = PP.ItemId
			WHERE  INV.CustomerID  = @l_CustomerID AND INV.[Status] IN ('UPDATED','NEW')
		END
		ELSE IF  @l_RouteTypeID = 'MichealBulkItemPrices'
		BEGIN
			SELECT SP.ProductId,SP.ItemID,REPLACE(REPLACE(CONVERT(VARCHAR,ListPrice), ',', ''), '$', '') ListPrice,REPLACE(REPLACE(CONVERT(VARCHAR,MapPrice), ',', ''), '$', '') MapPrice,
				REPLACE(REPLACE(CONVERT(VARCHAR,OffPrice), ',', ''), '$', '') OffPrice,SP.CustomerID,SyncStatus,SP.id,UnListed,INV.Total_ATS,Inv.CustomerItemCode
			FROM SCS_ProductPrices SP WITH (NOLOCK) 
				INNER JOIN SCSInventoryFeed INV WITH (NOLOCK) ON SP.CustomerID = INV.CustomerID AND SP.ItemId = INV.ItemId
			WHERE SP.CustomerID  = @l_CustomerID AND SyncStatus IN ('APPROVED') 
		END
		ELSE IF  @l_RouteTypeID = 'AmazonWHSWInventoryUpload'
		BEGIN
			SELECT INV.CustomerID,INV.CustomerItemCode AS CustomerItemCode,INV.ItemId,INV.CustomerItemCode,PR.id,PR.ProductId,0 AS ListPrice
			,INV.Total_ATS,INV.ATS_L10,INV.ATS_L21,INV.ATS_L28,INV.ATS_L30,INV.ATS_L34,INV.ATS_L35,INV.ATS_L36,INV.ATS_L37,INV.ATS_L40,
				   INV.ATS_L41,INV.ATS_L55,INV.ATS_L60,INV.ATS_L70,INV.ATS_L91,INV.ATS_L56,INV.ATS_L57,INV.ATS_L65,INV.ATS_L29	
			FROM SCSInventoryFeed INV WITH (NOLOCK)
				INNER JOIN CustomerProductCatalogPrices PR WITH (NOLOCK) ON INV.CustomerID = PR.CustomerID AND INV.ItemId = PR.ItemId
				--INNER JOIN SCS_ProductPrices PP WITH (NOLOCK) ON INV.CustomerID = PP.CustomerID AND INV.ItemId = PP.ItemId
			WHERE  INV.CustomerID  = @l_CustomerID AND INV.[Status] IN ('UPDATED','NEW') --AND INV.CustomerItemCode IN ('B0BMB7WJDV FLU4095A','B07JJYN64G BER219G-8','B082PJZ6R2 EVK256E-8','B01M0FB151 ASG741A-8','B0CGBQV2RG TSN102B-7SQ')
		END
		ELSE IF  @l_RouteTypeID = 'AmazonInventoryStatus'
		BEGIN
			SELECT BatchID,Status,FeedDocumentID,CustomerID 
			FROM InventoryBatchWiseFeedDetail WITH (NOLOCK)
			WHERE CustomerID = @l_CustomerID AND Status = 'NEW'
		END

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


