CREATE PROCEDURE [dbo].[Sp_SaveItemsType]
    @p_ReportID        NVARCHAR(250) = '',
    @p_UserNo          INT = 0,
	@p_CustomerID	   NVARCHAR(500) = ''
AS
BEGIN
    DECLARE @l_ReportID		 NVARCHAR(250)
    DECLARE @l_UserNo		 INT
    DECLARE @l_CustomerID	 NVARCHAR(500)

    
    BEGIN TRY
        SET @l_ReportID = @p_ReportID;
        SET @l_UserNo = @p_UserNo;
		SET @l_CustomerID = @p_CustomerID


		UPDATE SCS_ItemsTypeReport SET Status = 'COMPLETE' WHERE ReportID = @l_ReportID AND CustomerID = @l_CustomerID

		UPDATE IT
		SET IT.Brand					= temp.Brand,
			IT.Product_Subtype			= temp.Product_Subtype,
			IT.Item_Type				= temp.Item_Type,
			IT.Item_Type_Description	= temp.Item_Type_Description,
			IT.ModifiedDate				= GETDATE(),
			IT.ModifiedBy	            = @l_UserNo
		FROM  SCS_ItemsType IT
			INNER JOIN Temp_SCS_ItemsType temp ON temp.Item_Type_Id = IT.Item_Type_Id AND temp.CustomerID = IT.CustomerID
		WHERE  temp.CustomerID = @l_CustomerID

		INSERT INTO SCS_ItemsType(Brand,Product_Subtype,Item_Type,Item_Type_Id,Item_Type_Description,CreatedDate,CreatedBy,CustomerID)
		SELECT temp.Brand,temp.Product_Subtype,temp.Item_Type,temp.Item_Type_Id,temp.Item_Type_Description,GETDATE(),@l_UserNo,temp.CustomerID
		FROM Temp_SCS_ItemsType temp WITH (NOLOCK)
			LEFT OUTER JOIN SCS_ItemsType IT WITH (NOLOCK) ON temp.Item_Type_Id = IT.Item_Type_Id  AND temp.CustomerID = IT.CustomerID
		WHERE  IT.Item_Type_Id IS NULL AND temp.CustomerID = @l_CustomerID

		DELETE FROM [Temp_SCS_ItemsType]  WHERE ReportID = @l_ReportID AND CustomerID = @l_CustomerID
	
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
GO