ALTER PROCEDURE [dbo].[Sp_SaveCustomerProductCatalog]
    @p_CustomerID        NVARCHAR(500) = '',
    @p_UserNo        INT = 0
AS
BEGIN
    DECLARE @l_CustomerID		 NVARCHAR(500)
    DECLARE @l_UserNo		 INT
    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
        SET @l_UserNo = @p_UserNo;

		UPDATE CPC
		SET CPC.Brand = temp.Brand,
			CPC.UPC = temp.UPC,
			CPC.ProductRelation = temp.ProductRelation,
			CPC.ParentID = temp.ParentID,
			CPC.ListPrice = temp.ListPrice,
			CPC.MapPrice = temp.MapPrice,
			CPC.OffPrice = temp.OffPrice,
			CPC.SyncStatus = CASE WHEN temp.UnListed IN ('1','True','Y','Yes','y','YES','TRUE') THEN 'UNLISTED' ELSE  CASE WHEN ISNULL(CPC.Id,'') = '' THEN 'NEW' ELSE 'UPDATED' END END,
			CPC.JsonData = temp.JsonData,
			CPC.UnListed = CASE WHEN temp.UnListed IN('1','True','Y','Yes','y','YES','TRUE') THEN 1 ELSE 0 END,
			CPC.ModifiedDate = GETDATE(),
			CPC.ItemTypeName = temp.ItemTypeName,
			CPC.Type = temp.Type,
			CPC.VariationType = temp.VariationType,
			CPC.is_add_on = temp.is_add_on,
			CPC.two_day_shipping_eligible = temp.two_day_shipping_eligible,
			CPC.shipping_exclusion = temp.shipping_exclusion,
			CPC.seller_return_policy = temp.seller_return_policy,
			CPC.RetryCount = 0
		FROM SCS_CustomerProductCatalog CPC
			INNER JOIN [Temp_SCS_CustomerProductCatalog] temp  ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
		WHERE temp.CustomerID = @l_CustomerID

		INSERT INTO [SCS_CustomerProductCatalog] ([CustomerID],[Brand],[ItemID],[UPC],[ItemTypeName],[ProductRelation],[ParentID],[ListPrice],[MapPrice],[OffPrice],[JsonData],[CreatedDate],    
												  [CreatedBy],SyncStatus,Type,VariationType,Id,UnListed,is_add_on,two_day_shipping_eligible,shipping_exclusion,seller_return_policy,RetryCount)
		SELECT temp.[CustomerID],temp.[Brand],temp.[ItemID],temp.[UPC],temp.[ItemTypeName],temp.[ProductRelation],temp.[ParentID],temp.[ListPrice],temp.[MapPrice],temp.[OffPrice],temp.[JsonData],GETDATE(),   
												  @l_UserNo,CASE WHEN temp.UnListed IN ('1','True','Y','Yes','y','YES','TRUE') THEN 'UNLISTED' ELSE CASE WHEN C.ItemID IS NULL THEN 'NEW' ELSE 'UPDATED' END END,temp.Type,temp.VariationType,C.ProductID,
												  CASE WHEN temp.UnListed IN ('1','True','Y','Yes','y','YES','TRUE') THEN 1 ELSE 0 END,temp.is_add_on,temp.two_day_shipping_eligible,temp.shipping_exclusion,temp.seller_return_policy,0
		FROM [Temp_SCS_CustomerProductCatalog] temp
			LEFT OUTER JOIN SCS_CustomerProductCatalog CPC ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
			LEFT JOIN CustomerProductCatalogPrices C ON temp.CustomerID = C.CustomerID AND temp.ItemID = C.ItemId
		WHERE temp.CustomerID = @l_CustomerID AND CPC.ItemID IS NULL


		INSERT INTO ProductCatalogDiscrepencies (CustomerID,ItemID,[Data],CreatedDate,CreatedBy)
		SELECT @l_CustomerID,ItemID, '{
				"Message": "Parent ID Missing",
				"errors": ["Parent ID Missing"]
				}
		         ',GETDATE(),1 FROM [Temp_SCS_CustomerProductCatalog] WHERE CustomerID = @l_CustomerID AND  RTRIM(LTRIM(VariationType)) = 'VC' 
				  AND ISNULL(ParentID,'') = '' 


		IF EXISTS 
		(
			SELECT ItemID 
			FROM Temp_SCS_CustomerProductCatalog 
			WHERE RTRIM(LTRIM(VariationType)) = 'VC' AND ISNULL(ParentID,'') <> '' AND CustomerID = @l_CustomerID AND 
				ParentID NOT IN (SELECT ItemID FROM Temp_SCS_CustomerProductCatalog WHERE  CustomerID = @l_CustomerID ) AND 
				ParentID NOT IN (SELECT ItemID FROM SCS_CustomerProductCatalog  WHERE  CustomerID = @l_CustomerID)
		
		) 
		BEGIN
			INSERT INTO ProductCatalogDiscrepencies (CustomerID,ItemID,[Data],CreatedDate,CreatedBy)
			SELECT @l_CustomerID,ItemID, '{
					"Message": "Parent ID Missing",
					"errors": ["Parent ID Missing"]
					}
					 ',GETDATE(),1 
					 FROM Temp_SCS_CustomerProductCatalog 
					WHERE RTRIM(LTRIM(VariationType)) = 'VC' AND ISNULL(ParentID,'') <> '' AND CustomerID = @l_CustomerID AND 
						ParentID NOT IN (SELECT ItemID FROM Temp_SCS_CustomerProductCatalog WHERE  CustomerID = @l_CustomerID ) AND 
						ParentID NOT IN (SELECT ItemID FROM SCS_CustomerProductCatalog  WHERE  CustomerID = @l_CustomerID)
		
		
			UPDATE SCS_CustomerProductCatalog 
			SET SyncStatus = 'ERROR'
			WHERE ISNULL(ParentID,'') <> '' AND CustomerID = @l_CustomerID AND 
					ParentID NOT IN (SELECT ItemID FROM Temp_SCS_CustomerProductCatalog WHERE CustomerID = @l_CustomerID) AND 
				  ParentID NOT IN (SELECT ItemID FROM SCS_CustomerProductCatalog WHERE  CustomerID = @l_CustomerID) 
		END

		-- Insert UPC length/format errors
		INSERT INTO ProductCatalogDiscrepencies (CustomerID, ItemID, [Data], CreatedDate, CreatedBy)
		SELECT 
			@l_CustomerID,
			ItemID,
			'{
				"Message": "Invalid UPC/Barcode",
				"errors": ["UPC/Barcode must be exactly 12 numeric digits"]
			}',
			GETDATE(),
			1
		FROM [Temp_SCS_CustomerProductCatalog]
		WHERE CustomerID = @l_CustomerID
			AND (LEN(UPC) <> 12 OR ISNUMERIC(UPC) = 0)
			AND  RTRIM(LTRIM(VariationType)) IN ('VC','SA')

		-- Set SyncStatus = 'ERROR' for invalid UPCs
		UPDATE SCS_CustomerProductCatalog
		SET SyncStatus = 'ERROR'
		WHERE CustomerID = @l_CustomerID
			AND ItemID IN (
				SELECT ItemID 
				FROM [Temp_SCS_CustomerProductCatalog]
				WHERE CustomerID = @l_CustomerID
				AND (LEN(UPC) <> 12 OR ISNUMERIC(UPC) = 0)
				AND  RTRIM(LTRIM(VariationType)) IN ('VC','SA')
			)


		IF EXISTS (		
						SELECT TOP 1 ItemID FROM [Temp_SCS_CustomerProductCatalog] WHERE CustomerID = @l_CustomerID AND  RTRIM(LTRIM(VariationType)) = 'VC' 
						AND ISNULL(ParentID,'') = ''
				  ) 
		BEGIN
			  SELECT  '201' AS Code,'The data has been updated successfully, but the parent ID is not set on some items. You can download the Rejected CSV and check the data.' AS [Message], 'The data has been updated successfully, but the parent ID is not set on some items. You can download the Rejected CSV and check the data.' AS [Description]
		END
		ELSE
		BEGIN
			  SELECT  '200' AS Code,'The data has been updated successfully.' AS [Message], 'The data has been updated successfully.' AS [Description]
		END

		DELETE FROM [Temp_SCS_CustomerProductCatalog] WHERE CustomerID = @l_CustomerID	
    END TRY
    BEGIN CATCH        
        DECLARE @ErrorMessage NVARCHAR(MAX);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;

        SELECT  '400' AS Code,'Invalid product catalog data file.' AS [Message], 'Invalid product catalog data file.' AS [Description]
        
        PRINT 'Error Message: ' + @ErrorMessage;
        THROW;
    END CATCH;
END
