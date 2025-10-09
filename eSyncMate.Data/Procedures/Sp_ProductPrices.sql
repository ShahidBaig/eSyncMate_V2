ALTER PROCEDURE [dbo].[Sp_ProductPrices]
    @p_CustomerID        NVARCHAR(500) = '',
    @p_UserNo        INT = 0
AS
BEGIN
    DECLARE @l_CustomerID		 NVARCHAR(500)
    DECLARE @l_UserNo		     INT
	DECLARE @Success BIT
	DECLARE @Message NVARCHAR(255)
	DECLARE @Description NVARCHAR(255)
    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
        SET @l_UserNo = @p_UserNo;

		IF @l_CustomerID = 'MAC0149M'
		BEGIN
			
				INSERT INTO CustomerProductCatalogPrices (CustomerID, ItemID, ProductID, Status)
				SELECT temp.CustomerID, temp.ItemID, temp.ID, 'APPROVED'
				FROM Temp_SCS_ProductPrices temp
					LEFT JOIN CustomerProductCatalogPrices CPC ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
				WHERE CPC.ItemID IS NULL;

				UPDATE CPC
				SET 
					CPC.ListPrice = temp.ListPrice,
					CPC.MapPrice = temp.MapPrice,
					CPC.OffPrice = temp.OffPrice,
					CPC.SyncStatus = CASE WHEN CPC.SyncStatus NOT IN ('NEW','UPDATED','Pending') THEN 'APPROVED' ELSE CPC.SyncStatus END
				FROM SCS_ProductPrices CPC
					INNER JOIN Temp_SCS_ProductPrices temp  ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
				WHERE temp.CustomerID = @l_CustomerID  

				INSERT INTO [SCS_ProductPrices] ([CustomerID],[ItemID],[ListPrice],[MapPrice],[OffPrice],[CreatedDate],    
															[CreatedBy],SyncStatus,Id
															)
				SELECT temp.[CustomerID],temp.[ItemID],temp.[ListPrice],temp.[MapPrice],temp.[OffPrice],GETDATE(),   
															@l_UserNo,'APPROVED',C.ProductID
				FROM Temp_SCS_ProductPrices temp
					LEFT OUTER JOIN SCS_ProductPrices CPC ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
					LEFT JOIN CustomerProductCatalogPrices C ON temp.CustomerID = C.CustomerID AND temp.ItemID = C.ItemId
				WHERE temp.CustomerID = @l_CustomerID AND CPC.SyncStatus IS NULL

				SET @Success = 1
				SET @Message = 'File has been processed successfully!.'
				SET @Description = ''


		END
		ELSE
		BEGIN
				IF EXISTS ( 
								SELECT temp.ItemID
								FROM  Temp_SCS_ProductPrices temp
									LEFT JOIN  CustomerProductCatalogPrices CPC  ON CPC.CustomerID = temp.CustomerID AND CPC.ItemID = temp.ItemID
								WHERE CPC.ItemID IS NULL
						  )

				BEGIN
						SELECT @Description = STRING_AGG(temp.ItemID, ',')
						FROM  Temp_SCS_ProductPrices temp
							LEFT JOIN  CustomerProductCatalogPrices CPC  ON CPC.CustomerID = temp.CustomerID AND CPC.ItemID = temp.ItemID
						WHERE CPC.ItemID IS NULL

					SET @Success = 0
					SET @Message = 'Item ID is missing.'
						
				END
				ELSE 
				BEGIN
					UPDATE CPC
					SET 
						CPC.ListPrice = temp.ListPrice,
						CPC.MapPrice = temp.MapPrice,
						CPC.OffPrice = temp.OffPrice,
						CPC.SyncStatus = CASE WHEN CPC.SyncStatus NOT IN ('NEW','UPDATED','Pending') THEN 'APPROVED' ELSE CPC.SyncStatus END
					FROM SCS_ProductPrices CPC
						INNER JOIN Temp_SCS_ProductPrices temp  ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
					WHERE temp.CustomerID = @l_CustomerID  

					INSERT INTO [SCS_ProductPrices] ([CustomerID],[ItemID],[ListPrice],[MapPrice],[OffPrice],[CreatedDate],    
															  [CreatedBy],SyncStatus,Id
															  )
					SELECT temp.[CustomerID],temp.[ItemID],temp.[ListPrice],temp.[MapPrice],temp.[OffPrice],GETDATE(),   
															  @l_UserNo,'APPROVED',C.ProductID
					FROM Temp_SCS_ProductPrices temp
						LEFT OUTER JOIN SCS_ProductPrices CPC ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
						LEFT JOIN CustomerProductCatalogPrices C ON temp.CustomerID = C.CustomerID AND temp.ItemID = C.ItemId
					WHERE temp.CustomerID = @l_CustomerID AND CPC.SyncStatus IS NULL

					SET @Success = 1
					SET @Message = 'File has been processed successfully!.'
					SET @Description = ''
				END
		END

		DELETE FROM [Temp_SCS_ProductPrices] WHERE CustomerID = @l_CustomerID

		SELECT @Success AS Success, @Message AS [Message],@Description [Description] 
			
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
