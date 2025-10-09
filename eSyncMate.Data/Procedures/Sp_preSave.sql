
CREATE PROCEDURE [dbo].[sp_PreSave_UpdatePassword]
    @p_Email		 VARCHAR(50)  =  NULL,
    @p_Password     VARCHAR(250)  =  ''
AS
BEGIN
    DECLARE @l_Email   VARCHAR(50) = NULL
	DECLARE @l_Password    VARCHAR(50) = NULL

    BEGIN TRY

		SELECT @l_Email = Email , @l_Password = Password FROM Users WHERE Email = @p_Email

		IF @l_Email = @p_Email
		BEGIN
		 	IF @l_Password = @p_Password
			BEGIN
				UPDATE Users SET [Password] = @p_Password WHERE Email = @p_Email
			END
			ELSE 
			BEGIN
			  SELECT 'OLD Password does not matched' [Message], 404 AS Code
			END
		END
		ELSE
		BEGIN
			SELECT 'Email doest not exist' [Message], 404 AS Code
		END
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
