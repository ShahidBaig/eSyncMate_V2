CREATE PROCEDURE [dbo].[Sp_DeleteItemsData]
    @p_CustomerID        NVARCHAR(500) = '',
    @p_ItemType          VARCHAR(500) = '',
	@p_UserID            INT = 0

AS
BEGIN
    DECLARE @l_CustomerID		 NVARCHAR(500)
    DECLARE @l_UserID		 INT
    DECLARE @l_ItemType		 VARCHAR(500)

    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;	
        SET @l_UserID = @p_UserID;
        SET @l_ItemType = @p_ItemType;

		DELETE D
		FROM SCS_ItemData D
			INNER JOIN SCS_PrepareItemData T ON D.PrepareItemData_ID = T.ID
		WHERE D.item_type_id = @l_ItemType AND T.CustomerID = @l_CustomerID AND T.UserID = @l_UserID

		UPDATE SCS_PrepareItemData SET Status = 'FINISHED' WHERE ItemTypeID = @l_ItemType AND CustomerID = @l_CustomerID AND UserID = @l_UserID
	
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
