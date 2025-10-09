ALTER PROCEDURE [dbo].[Sp_ProductCatalogRejected]
    @p_CustomerID        NVARCHAR(500) = '',
    @p_UserNo        INT = 0
AS
BEGIN
    DECLARE @l_CustomerID		 NVARCHAR(500)
    DECLARE @l_UserNo		 INT
    DECLARE @l_TagValue		 VARCHAR(250)

    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;	
        SET @l_UserNo = @p_UserNo;

		SELECT @l_TagValue = ISNULL(TagValue,'') FROM ApplicationSettings WHERE TagName = 'ProductMarkErrorResolve'

		SELECT PR.Data,CAT.CustomerID,CAT.ItemID,PR.Type ,CAT.ItemTypeName,CAT.CustomerID AS ErrorSource 
		FROM SCS_CustomerProductCatalogData PR WITH (NOLOCK)
			INNER JOIN SCS_CustomerProductCatalog CAT WITH (NOLOCK) ON PR.ProductId = CAT.ProductId
		WHERE CAT.CustomerID = @l_CustomerID AND  ((CAT.SyncStatus = 'REJECTED' AND PR.Type = 'RSP-JSON' AND PR.[Data] LIKE '%"REJECTED"%') OR (CAT.SyncStatus = 'ERROR' AND PR.Type = 'REQ-ERR')) AND
				PR.CreatedDate >= CASE WHEN @l_TagValue <> '' THEN CAST(@l_TagValue AS DATETIME) ELSE  PR.CreatedDate END 
		UNION ALL
		SELECT  [Data],PR.CustomerID,PR.ItemID,'' Type,'' ItemTypeName,'eSyncmate' AS ErrorSource
		FROM [dbo].[ProductCatalogDiscrepencies] PR WITH (NOLOCK)
		WHERE PR.CustomerID = @l_CustomerID   AND PR.CreatedDate >= CASE WHEN @l_TagValue <> '' THEN CAST(@l_TagValue AS DATETIME) ELSE  PR.CreatedDate END 

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
