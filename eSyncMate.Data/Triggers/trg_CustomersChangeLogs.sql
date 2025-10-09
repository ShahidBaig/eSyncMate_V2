IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_CustomersChangeLogs')
BEGIN
    EXEC('CREATE TRIGGER dbo.trg_CustomersChangeLogs
       ON dbo.Customers
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

        INSERT INTO dbo.CustomersLogs (ActionType, Id, Name, ERPCustomerID, ISACustomerID, ISA810ReceiverId, Marketplace, CreatedDate, CreatedBy, ISA856ReceiverId, ModifiedDate, ModifiedBy)
        SELECT 
            @ActionType,
            COALESCE(i.Id, d.Id),
            COALESCE(i.Name, d.Name),
            COALESCE(i.ERPCustomerID, d.ERPCustomerID),
            COALESCE(i.ISACustomerID, d.ISACustomerID),
            COALESCE(i.ISA810ReceiverId, d.ISA810ReceiverId),
            COALESCE(i.Marketplace, d.Marketplace),
            COALESCE(i.CreatedDate, d.CreatedDate),
            COALESCE(i.CreatedBy, d.CreatedBy),
            COALESCE(i.ISA856ReceiverId, d.ISA856ReceiverId),
            GETDATE(), 
            @ModUserId 
        FROM INSERTED i
        FULL OUTER JOIN DELETED d ON i.Id = d.Id;
    END')
END
GO
