ALTER PROCEDURE [dbo].[Sp_SaveItemTypeAttributes]
    @p_ItemTypeId        NVARCHAR(500) = '',
    @p_UserNo			 INT = 0,
	@p_CustomerID		 NVARCHAR(500) = ''
AS
BEGIN
    DECLARE @l_ItemTypeId		 NVARCHAR(500)
    DECLARE @l_UserNo			 INT
    DECLARE @l_CustomerID		 NVARCHAR(500)

    
    BEGIN TRY
        SET @l_ItemTypeId = @p_ItemTypeId;
        SET @l_UserNo = @p_UserNo;
        SET @l_UserNo = @p_UserNo;
		SET @l_CustomerID = @p_CustomerID

		
		UPDATE IT
		SET IT.Name					= temp.Name,
			IT.Mapped_Property		= temp.Mapped_Property,
			IT.Type					= temp.Type,
			IT.ModifiedDate			= GETDATE(),
			IT.ModifiedBy			= 1,
			IT.Required				= temp.Required

		FROM  SCS_ItemTypeAttribute IT
			INNER JOIN Temp_SCS_ItemTypeAttribute temp ON temp.ID = IT.ID AND temp.Item_Type_Id = IT.Item_Type_Id AND IT.CustomerID = temp.CustomerID
		WHERE temp.CustomerID = @l_CustomerID

		INSERT INTO SCS_ItemTypeAttribute(ID,Name,Mapped_Property,Type,Item_Type_Id,CreatedDate,CreatedBy,Required,CustomerID)
		SELECT temp.ID,temp.Name,temp.Mapped_Property,temp.Type,temp.Item_Type_Id,GETDATE(),1,temp.Required,@l_CustomerID
		FROM Temp_SCS_ItemTypeAttribute temp WITH (NOLOCK)
			LEFT OUTER JOIN SCS_ItemTypeAttribute IT WITH (NOLOCK) ON temp.ID = IT.ID AND IT.Item_Type_Id = temp.Item_Type_Id  AND IT.CustomerID = temp.CustomerID
		WHERE  IT.ID IS NULL  AND temp.CustomerID = @l_CustomerID
        
        DELETE FROM Temp_SCS_ItemTypeAttribute  WHERE ISNULL(Item_Type_Id,'') = @p_ItemTypeId AND CustomerID = @l_CustomerID
		 
	
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
