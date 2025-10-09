IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_ProductUploadPricesChangeLogs')
BEGIN
    EXEC('CREATE TRIGGER dbo.trg_ProductUploadPricesChangeLogs
       ON dbo.ProductUploadPrices
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

        INSERT INTO dbo.ProductUploadPricesLogs (ActionType, [Id], [CustomerID], [ItemId], [ListPrice], [OffPrice], [PromoStartDate], [PromoEndDate], [Status], [OldListPrice], [OldOffPrice], [CreatedDate], [CreatedBy], [ModifiedDate], [ModifiedBy])
        SELECT 
            @ActionType,
            COALESCE(i.Id, d.Id),
            COALESCE(i.CustomerID, d.CustomerID),
            COALESCE(i.ItemId, d.ItemId),
            COALESCE(i.ListPrice, d.ListPrice),
			COALESCE(i.OffPrice, d.OffPrice),
			COALESCE(i.PromoStartDate, d.PromoStartDate),
			COALESCE(i.PromoEndDate, d.PromoEndDate),
		    COALESCE(i.Status, d.Status),
			COALESCE(i.OldListPrice, d.OldListPrice),
			COALESCE(i.OldOffPrice, d.OldOffPrice),
            COALESCE(i.CreatedDate, d.CreatedDate),
            COALESCE(i.CreatedBy, d.CreatedBy),
            GETDATE(), 
            @ModUserId 
        FROM INSERTED i
        FULL OUTER JOIN DELETED d ON i.Id = d.Id;
    END')
END
GO
