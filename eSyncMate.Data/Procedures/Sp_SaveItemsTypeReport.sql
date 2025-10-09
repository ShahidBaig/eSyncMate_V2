CREATE PROCEDURE [dbo].[Sp_SaveItemsTypeReport]
    @p_CustomerID    NVARCHAR(250) = '',
    @p_Status        NVARCHAR(250) = '',
    @p_ReportID        NVARCHAR(250) = '',
    @p_UserNo        INT = 0
AS
BEGIN
    DECLARE @l_CustomerID    NVARCHAR(250)
    DECLARE @l_Status		 NVARCHAR(250)
    DECLARE @l_ReportID		 NVARCHAR(250)
    DECLARE @l_UserNo		 INT
    
    BEGIN TRY
        SET @l_CustomerID = @p_CustomerID;
        SET @l_Status = @p_Status;
        SET @l_ReportID = @p_ReportID;
        SET @l_UserNo = @p_UserNo;

		INSERT INTO [SCS_ItemsTypeReport] ([CustomerID],ReportID,[Status],CreatedDate,CreatedBy)
		VALUES (@l_CustomerID,@l_ReportID,@l_Status,GETDATE(),@l_UserNo)

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

