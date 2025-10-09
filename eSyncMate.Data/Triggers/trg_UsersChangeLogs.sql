IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_UsersChangeLogs')
BEGIN
    EXEC('CREATE TRIGGER dbo.trg_UsersChangeLogs
       ON dbo.Users
       AFTER INSERT, DELETE, UPDATE
    AS 
    BEGIN
        SET NOCOUNT ON;

        DECLARE @ActionType NVARCHAR(10);
        DECLARE @ModUserId INT;

        IF EXISTS (SELECT * FROM INSERTED) AND EXISTS (SELECT * FROM DELETED)
        BEGIN
            SET @ActionType = ''UPDATE'';
            --SELECT @ModUserId = ModifiedBy FROM INSERTED
        END
        ELSE IF EXISTS (SELECT * FROM INSERTED)
        BEGIN
            SET @ActionType = ''INSERT'';
            SELECT @ModUserId = CreatedBy FROM INSERTED
        END
        ELSE IF EXISTS (SELECT * FROM DELETED)
        BEGIN
            SET @ActionType = ''DELETE'';
            --SELECT @ModUserId = ModifiedBy FROM DELETED
        END

        INSERT INTO dbo.UsersLogs (ActionType, [Id], [FirstName], [LastName], [Email],[Mobile],[Password],[Status], [CreatedDate], [CreatedBy],[UserType],[Company])
        SELECT 
            @ActionType,
            COALESCE(i.Id, d.Id),
            COALESCE(i.FirstName, d.FirstName),
            COALESCE(i.LastName, d.LastName),
            COALESCE(i.Email, d.Email),
			COALESCE(i.Mobile, d.Mobile),
			COALESCE(i.Password, d.Password),
			COALESCE(i.Status, d.Status),
            COALESCE(i.CreatedDate, d.CreatedDate),
            COALESCE(i.CreatedBy, d.CreatedBy),
			COALESCE(i.UserType, d.UserType),
            COALESCE(i.Company, d.Company)
        FROM INSERTED i
        FULL OUTER JOIN DELETED d ON i.Id = d.Id;
    END')
END
GO
