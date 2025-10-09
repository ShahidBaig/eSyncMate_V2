CREATE PROCEDURE [dbo].[SP_InsertSCSInventoryFeed]
    @p_CustomerID    NVARCHAR(250)  =  '',
	@p_BatchID		NVARCHAR(500) = ''
AS
BEGIN
    DECLARE @l_CustomerID    NVARCHAR(250)
    DECLARE @l_BatchID    NVARCHAR(500)

    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
        SET @l_BatchID = @p_BatchID;

		UPDATE Inv 
		SET INV.ETA_Date  = temp.ETA_Date,		 	
			INV.ETA_Qty	  = temp.ETA_Qty,			 
			INV.Total_ATS = temp.Total_ATS,		 
			INV.ATS_L10	= temp.ATS_L10,		     
			INV.ATS_L21	= temp.ATS_L21,			     
			INV.ATS_L28	= temp.ATS_L28,		     
			INV.ATS_L30	= temp.ATS_L30,			     
			INV.ATS_L34 = temp.ATS_L34,			     
			INV.ATS_L35	= temp.ATS_L35,		     
			INV.ATS_L36	= temp.ATS_L36,		     
			INV.ATS_L37	= temp.ATS_L37,			     
			INV.ATS_L40	= temp.ATS_L40,		     
			INV.ATS_L41	= temp.ATS_L41,			     
			INV.ATS_L55	= temp.ATS_L55,		     
			INV.ATS_L60	= temp.ATS_L60,		     
			INV.ATS_L70	= temp.ATS_L70,		     
			INV.ATS_L91 = temp.ATS_L91,
			INV.ModifiedDate = GETDATE(),
			INV.ModifiedBy = 1,
			INV.[Status] = 'UPDATED'
		FROM SCSInventoryFeed  Inv
			INNER JOIN Temp_SCSInventoryFeed temp ON Inv.CustomerID = temp.CustomerID AND Inv.ItemId = temp.ItemId 
		WHERE Inv.CustomerID = @l_CustomerID

        
		INSERT INTO SCSInventoryFeed(CustomerID,ItemId,CustomerItemCode,ETA_Date,ETA_Qty,Total_ATS,ATS_L10,ATS_L21,ATS_L28,ATS_L30,ATS_L34,ATS_L35,
									 ATS_L36,ATS_L37,ATS_L40,ATS_L41,ATS_L55,ATS_L60,ATS_L70,ATS_L91,CreatedDate,CreatedBy,[Status])
		SELECT temp.CustomerID,temp.ItemId,temp.CustomerItemCode,temp.ETA_Date,temp.ETA_Qty,temp.Total_ATS,temp.ATS_L10,temp.ATS_L21,temp.ATS_L28,temp.ATS_L30,temp.ATS_L34,temp.ATS_L35,
									 temp.ATS_L36,temp.ATS_L37,temp.ATS_L40,temp.ATS_L41,temp.ATS_L55,temp.ATS_L60,temp.ATS_L70,temp.ATS_L91,GETDATE(),1, 'NEW'
		FROM Temp_SCSInventoryFeed temp
			LEFT OUTER JOIN SCSInventoryFeed Inv ON temp.CustomerID = Inv.CustomerID AND temp.ItemId = Inv.ItemId
		WHERE temp.CustomerID = @l_CustomerID AND Inv.CustomerID IS NULL 


		INSERT INTO SCSInventoryFeedData( CustomerID,ItemId,CreatedDate,CreatedBy,BatchID,[Type],[Data])
		SELECT CustomerID,ItemId,GETDATE(),1,@l_BatchID,'ERP-RVD','{"quantity":' + CONVERT(VARCHAR, temp.Total_ATS) + '}'
		FROM  Temp_SCSInventoryFeed temp
		WHERE CustomerID = @l_CustomerID   

		DELETE FROM Temp_SCSInventoryFeed WHERE CustomerID = @l_CustomerID      
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

