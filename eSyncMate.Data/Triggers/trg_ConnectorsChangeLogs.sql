IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_ConnectorsChangeLogs')
BEGIN
    EXEC('CREATE TRIGGER dbo.trg_ConnectorsChangeLogs
       ON dbo.Connectors
       AFTER INSERT, DELETE, UPDATE
    AS 
    BEGIN
        SET NOCOUNT ON;

        DECLARE @ActionType NVARCHAR(10);
        DECLARE @ModUserId INT;

        IF EXISTS (SELECT * FROM INSERTED) AND EXISTS (SELECT * FROM DELETED)
        BEGIN
            SET @ActionType = ''UPDATE'';
            SELECT @ModUserId = ModifiedBy FROM INSERTED
        END
        ELSE IF EXISTS (SELECT * FROM INSERTED)
        BEGIN
            SET @ActionType = ''INSERT'';
            SELECT @ModUserId = CreatedBy FROM INSERTED
        END
        ELSE IF EXISTS (SELECT * FROM DELETED)
        BEGIN
            SET @ActionType = ''DELETE'';
            SELECT @ModUserId = ModifiedBy FROM DELETED
        END

        INSERT INTO dbo.ConnectorsLogs (ActionType, [Id], [TypeId], [Name], [Data], [CreatedDate], [CreatedBy], [ModifiedDate], [ModifiedBy])
        SELECT 
            @ActionType,
            COALESCE(i.Id, d.Id),
            COALESCE(i.TypeId, d.TypeId),
            COALESCE(i.Name, d.Name),
            COALESCE(i.Data, d.Data),
            COALESCE(i.CreatedDate, d.CreatedDate),
            COALESCE(i.CreatedBy, d.CreatedBy),
            GETDATE(), 
            @ModUserId 
        FROM INSERTED i
        FULL OUTER JOIN DELETED d ON i.Id = d.Id;
    END')
END
GO
