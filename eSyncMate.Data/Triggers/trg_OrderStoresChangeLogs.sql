IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_OrderStoresChangeLogs')
BEGIN
    EXEC('CREATE TRIGGER dbo.trg_OrderStoresChangeLogs
       ON dbo.OrderStores
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

        INSERT INTO dbo.OrderStoresLogs (ActionType, [Id], [OrderId],[CustomerId],[CustomerPO],[Status],[Data],[Response],[CreatedDate], [CreatedBy])
        SELECT 
            @ActionType,
            COALESCE(i.Id, d.Id),
            COALESCE(i.OrderId, d.OrderId),
            COALESCE(i.CustomerId, d.CustomerId),
			COALESCE(i.CustomerPO, d.CustomerPO),
			COALESCE(i.Status, d.Status),
            COALESCE(i.Data, d.Data),
			COALESCE(i.Response, d.Response),
            COALESCE(i.CreatedDate, d.CreatedDate),
            COALESCE(i.CreatedBy, d.CreatedBy)
        FROM INSERTED i
        FULL OUTER JOIN DELETED d ON i.Id = d.Id;
    END')
END
GO
