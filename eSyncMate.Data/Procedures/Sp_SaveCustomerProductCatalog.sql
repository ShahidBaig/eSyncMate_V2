
ALTER PROCEDURE [dbo].[Sp_SaveCustomerProductCatalog]
    @p_CustomerID        NVARCHAR(500) = '',
    @p_UserNo        INT = 0
AS
BEGIN
    DECLARE @l_CustomerID         NVARCHAR(500)
    DECLARE @l_UserNo         INT

    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
        SET @l_UserNo = @p_UserNo;

        -- ============================================================
        -- 1) Upsert existing items
        -- ============================================================
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


        -- ============================================================
        -- 2) ERROR: Parent ID Missing
        --    A Variation-Child item has no ParentID.
        --    "VC" is detected across ProductRelation / Type / VariationType.
        --    Actions: discrepancy (Rejected CSV) + SyncStatus='ERROR'
        --             + internal log in SCS_CustomerProductCatalogData.
        -- ============================================================
        -- 2a. Rejected-CSV discrepancy
        INSERT INTO ProductCatalogDiscrepencies (CustomerID,ItemID,[Data],CreatedDate,CreatedBy)
        SELECT @l_CustomerID, ItemID, '{
                "Message": "Parent ID Missing",
                "errors": ["Parent ID Missing"]
                }',GETDATE(),1
        FROM [Temp_SCS_CustomerProductCatalog]
        WHERE CustomerID = @l_CustomerID
            AND ( RTRIM(LTRIM(ISNULL(ProductRelation,''))) = 'VC'
               OR RTRIM(LTRIM(ISNULL([Type],'')))          = 'VC'
               OR RTRIM(LTRIM(ISNULL(VariationType,'')))   = 'VC' )
            AND ISNULL(ParentID,'') = ''

        -- 2b. Mark SyncStatus = 'ERROR'
        UPDATE CPC
        SET CPC.SyncStatus   = 'ERROR',
            CPC.ModifiedDate = GETDATE()
        FROM SCS_CustomerProductCatalog CPC
            INNER JOIN [Temp_SCS_CustomerProductCatalog] temp
                ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
        WHERE temp.CustomerID = @l_CustomerID
            AND ( RTRIM(LTRIM(ISNULL(temp.ProductRelation,''))) = 'VC'
               OR RTRIM(LTRIM(ISNULL(temp.[Type],'')))          = 'VC'
               OR RTRIM(LTRIM(ISNULL(temp.VariationType,'')))   = 'VC' )
            AND ISNULL(temp.ParentID,'') = ''

        -- 2c. Internal log
        INSERT INTO SCS_CustomerProductCatalogData (ProductId,[Type],[Data],CreatedDate,CreatedBy)
        SELECT CPC.ProductId,
               'Internal',
               N'{"Message":"Parent ID Missing","errors":["Parent ID is missing for this Variation Child (VC) item in the uploaded data."]}',
               GETDATE(),
               @l_UserNo
        FROM SCS_CustomerProductCatalog CPC
            INNER JOIN [Temp_SCS_CustomerProductCatalog] temp
                ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
        WHERE temp.CustomerID = @l_CustomerID
            AND ( RTRIM(LTRIM(ISNULL(temp.ProductRelation,''))) = 'VC'
               OR RTRIM(LTRIM(ISNULL(temp.[Type],'')))          = 'VC'
               OR RTRIM(LTRIM(ISNULL(temp.VariationType,'')))   = 'VC' )
            AND ISNULL(temp.ParentID,'') = ''


        -- ============================================================
        -- 3) ERROR: Parent Not Found
        --    VC item has a ParentID, but that ParentID does not exist
        --    in this upload or in the existing catalog.
        -- ============================================================
        IF EXISTS
        (
            SELECT temp.ItemID
            FROM Temp_SCS_CustomerProductCatalog temp
            WHERE RTRIM(LTRIM(temp.VariationType)) = 'VC' AND ISNULL(temp.ParentID,'') <> '' AND temp.CustomerID = @l_CustomerID
                AND NOT EXISTS (SELECT 1 FROM Temp_SCS_CustomerProductCatalog t2 WHERE t2.CustomerID = @l_CustomerID AND t2.ItemID = temp.ParentID)
                AND NOT EXISTS (SELECT 1 FROM SCS_CustomerProductCatalog c2 WHERE c2.CustomerID = @l_CustomerID AND c2.ItemID = temp.ParentID)
        )
        BEGIN
            INSERT INTO ProductCatalogDiscrepencies (CustomerID,ItemID,[Data],CreatedDate,CreatedBy)
            SELECT @l_CustomerID, temp.ItemID, '{
                    "Message": "Parent ID Missing",
                    "errors": ["Parent ID Missing"]
                    }',GETDATE(),1
            FROM Temp_SCS_CustomerProductCatalog temp
            WHERE RTRIM(LTRIM(temp.VariationType)) = 'VC' AND ISNULL(temp.ParentID,'') <> '' AND temp.CustomerID = @l_CustomerID
                AND NOT EXISTS (SELECT 1 FROM Temp_SCS_CustomerProductCatalog t2 WHERE t2.CustomerID = @l_CustomerID AND t2.ItemID = temp.ParentID)
                AND NOT EXISTS (SELECT 1 FROM SCS_CustomerProductCatalog c2 WHERE c2.CustomerID = @l_CustomerID AND c2.ItemID = temp.ParentID)

            UPDATE cat
            SET cat.SyncStatus = 'ERROR'
            FROM SCS_CustomerProductCatalog cat
            WHERE cat.CustomerID = @l_CustomerID AND ISNULL(cat.ParentID,'') <> ''
                AND NOT EXISTS (SELECT 1 FROM Temp_SCS_CustomerProductCatalog t2 WHERE t2.CustomerID = @l_CustomerID AND t2.ItemID = cat.ParentID)
                AND NOT EXISTS (SELECT 1 FROM SCS_CustomerProductCatalog c2 WHERE c2.CustomerID = @l_CustomerID AND c2.ItemID = cat.ParentID)
        END


        -- ============================================================
        -- 4) ERROR: Invalid UPC/Barcode  (must be exactly 12 numeric digits)
        --    A digit-only check is used (LIKE '%[^0-9]%') so values with
        --    '.', '+', ',', 'E', spaces or NULL are correctly rejected —
        --    ISNUMERIC() alone would let those through.
        -- ============================================================
        -- 4a. Rejected-CSV discrepancy
        INSERT INTO ProductCatalogDiscrepencies (CustomerID, ItemID, [Data], CreatedDate, CreatedBy)
        SELECT @l_CustomerID, ItemID, '{
                "Message": "Invalid UPC/Barcode",
                "errors": ["UPC/Barcode must be exactly 12 numeric digits"]
            }',GETDATE(),1
        FROM [Temp_SCS_CustomerProductCatalog]
        WHERE CustomerID = @l_CustomerID
            AND (LEN(ISNULL(UPC,'')) <> 12 OR ISNULL(UPC,'') LIKE '%[^0-9]%')
            AND  RTRIM(LTRIM(VariationType)) IN ('VC','SA')

        -- 4b. Mark SyncStatus = 'ERROR'
        UPDATE SCS_CustomerProductCatalog
        SET SyncStatus = 'ERROR'
        WHERE CustomerID = @l_CustomerID
            AND ItemID IN (
                SELECT ItemID
                FROM [Temp_SCS_CustomerProductCatalog]
                WHERE CustomerID = @l_CustomerID
                    AND (LEN(ISNULL(UPC,'')) <> 12 OR ISNULL(UPC,'') LIKE '%[^0-9]%')
                    AND  RTRIM(LTRIM(VariationType)) IN ('VC','SA')
            )

        -- 4c. Internal log
        INSERT INTO SCS_CustomerProductCatalogData (ProductId,[Type],[Data],CreatedDate,CreatedBy)
        SELECT CPC.ProductId,
               'Internal',
               N'{"Message":"Invalid UPC/Barcode","errors":["UPC/Barcode must be exactly 12 numeric digits"]}',
               GETDATE(),
               @l_UserNo
        FROM SCS_CustomerProductCatalog CPC
            INNER JOIN [Temp_SCS_CustomerProductCatalog] temp
                ON temp.CustomerID = CPC.CustomerID AND temp.ItemID = CPC.ItemID
        WHERE temp.CustomerID = @l_CustomerID
            AND (LEN(ISNULL(temp.UPC,'')) <> 12 OR ISNULL(temp.UPC,'') LIKE '%[^0-9]%')
            AND  RTRIM(LTRIM(temp.VariationType)) IN ('VC','SA')


        -- ============================================================
        -- 5) ERROR: Invalid Variation Type  (must be VAP, SA or VC)
        -- ============================================================
        INSERT INTO ProductCatalogDiscrepencies (CustomerID, ItemID, [Data], CreatedDate, CreatedBy)
        SELECT @l_CustomerID, ItemID, '{
                "Message": "Invalid Variation Type",
                "errors": ["Variation Type may only be VAP SA or VC."]
            }',GETDATE(),1
        FROM [Temp_SCS_CustomerProductCatalog]
        WHERE CustomerID = @l_CustomerID
            AND  (ISNULL(VariationType,'') = '' OR VariationType NOT IN ('VAP', 'SA', 'VC'))

        UPDATE SCS_CustomerProductCatalog
        SET SyncStatus = 'ERROR'
        WHERE CustomerID = @l_CustomerID
            AND ItemID IN (
                SELECT ItemID
                FROM [Temp_SCS_CustomerProductCatalog]
                WHERE CustomerID = @l_CustomerID
                    AND  (ISNULL(VariationType,'') = '' OR VariationType NOT IN ('VAP', 'SA', 'VC'))
            )


        -- ============================================================
        -- 6) Result message
        -- ============================================================
        IF EXISTS (
                        SELECT TOP 1 ItemID FROM [Temp_SCS_CustomerProductCatalog]
                        WHERE CustomerID = @l_CustomerID
                            AND ( RTRIM(LTRIM(ISNULL(ProductRelation,''))) = 'VC'
                               OR RTRIM(LTRIM(ISNULL([Type],'')))          = 'VC'
                               OR RTRIM(LTRIM(ISNULL(VariationType,'')))   = 'VC' )
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
