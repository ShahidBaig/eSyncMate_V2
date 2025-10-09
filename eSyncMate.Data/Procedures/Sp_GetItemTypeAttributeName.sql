
ALTER PROCEDURE [dbo].[Sp_GetItemTypeAttributeName]
    @p_Itemtype        NVARCHAR(500) = '',
	@p_CustomerID	   NVARCHAR(500) = ''
AS
BEGIN
    DECLARE @l_Itemtype		 NVARCHAR(500)
    DECLARE @l_UserNo		 INT
    DECLARE @l_Item_Type_Id		 NVARCHAR(500)
    DECLARE @l_CustomerID		 NVARCHAR(500)


    
    BEGIN TRY
        SET @l_Itemtype = @p_Itemtype;
        SET @l_CustomerID = @p_CustomerID;	
		

	   SELECT @l_Item_Type_Id = Item_Type_Id 
	   FROM SCS_ItemsType 
	   WHERE Item_Type = @l_Itemtype AND CustomerID = @l_CustomerID

	   SELECt * 
	   FROM SCS_ItemTypeAttribute 
	   WHERE Item_Type_Id = @l_Item_Type_Id AND CustomerID = @l_CustomerID
	
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
