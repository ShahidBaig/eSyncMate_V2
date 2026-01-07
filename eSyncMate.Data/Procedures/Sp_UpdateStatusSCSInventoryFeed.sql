CREATE PROCEDURE [dbo].[Sp_UpdateStatusSCSInventoryFeed]
    @p_CustomerID        NVARCHAR(500) = '',
    @p_BatchID           NVARCHAR(500) = '',
	@p_FeedDocumentID	 NVARCHAR(500) = '',
	@p_MessageID	     BIGINT = ''
   

AS
BEGIN
    DECLARE @l_CustomerID		 NVARCHAR(500)
    DECLARE @l_BatchID		     NVARCHAR(500)
    DECLARE @l_FeedDocumentID	 NVARCHAR(500)
    DECLARE @l_MessageID	    BIGINT
    DECLARE @l_ItemID	         NVARCHAR(500)

    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;	
        SET @l_BatchID = @p_BatchID;
        SET @l_FeedDocumentID = @p_FeedDocumentID;
        SET @l_MessageID = @p_MessageID;

		SELECT @l_ItemID = ItemID 
		FROM SCSAmazonFeedData 
		WHERE BatchID = @l_BatchID AND FeedDocumentID = @l_FeedDocumentID AND MessageID = @l_MessageID

		UPDATE SCSInventoryFeed 
		SET Status = 'UPDATED',ModifiedDate = GETDATE() 
		WHERE CustomerID = @l_CustomerID 
		AND ItemId = @l_ItemID

	
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
