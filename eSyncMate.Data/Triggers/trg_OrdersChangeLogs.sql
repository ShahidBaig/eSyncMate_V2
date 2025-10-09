IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_OrdersChangeLogs')
BEGIN
    EXEC('CREATE TRIGGER dbo.trg_OrdersChangeLogs
       ON dbo.Orders
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

		INSERT INTO dbo.OrdersLogs (
			ActionType, 
			[Id],
			[Status],
			[CustomerId],
			[InboundEDIId],
			[OrderDate],
			[OrderNumber],
			[VendorNumber],
			[OrderType],
			[ReferenceNo],
			[CustomerOrderNo],
			[ExternalId],
			[ShippingMethod],
			[ShipToId],
			[ShipToName],
			[ShipToAddress1],
			[ShipToAddress2],
			[ShipToCity],
			[ShipToState],
			[ShipToZip],
			[ShipToCountry],
			[ShipToEmail],
			[ShipToPhone],
			[BillToId],
			[BillToName],
			[BillToAddress1],
			[BillToAddress2],
			[BillToCity],
			[BillToState],
			[BillToZip],
			[BillToCountry],
			[BillToEmail],
			[BillToPhone],
			[BuyerId],
			[BuyerName],
			[BuyerAddress1],
			[BuyerAddress2],
			[BuyerCity],
			[BuyerState],
			[BuyerZip],
			[BuyerCountry],
			[BuyerEmail],
			[BuyerPhone],
			[IsStoreOrder],
			[CreatedDate],
			[CreatedBy]
		)
		SELECT 
			@ActionType,
			COALESCE(i.Id, d.Id),
			COALESCE(i.Status, d.Status),
			COALESCE(i.CustomerId, d.CustomerId),
			COALESCE(i.InboundEDIId, d.InboundEDIId),
			COALESCE(i.OrderDate, d.OrderDate), 
			COALESCE(i.OrderNumber, d.OrderNumber), 
			COALESCE(i.VendorNumber, d.VendorNumber), 
			COALESCE(i.OrderType, d.OrderType), 
			COALESCE(i.ReferenceNo, d.ReferenceNo), 
			COALESCE(i.CustomerOrderNo, d.CustomerOrderNo), 
			COALESCE(i.ExternalId, d.ExternalId), 
			COALESCE(i.ShippingMethod, d.ShippingMethod), 
			COALESCE(i.ShipToId, d.ShipToId), 
			COALESCE(i.ShipToName, d.ShipToName), 
			COALESCE(i.ShipToAddress1, d.ShipToAddress1), 
			COALESCE(i.ShipToAddress2, d.ShipToAddress2), 
			COALESCE(i.ShipToCity, d.ShipToCity), 
			COALESCE(i.ShipToState, d.ShipToState), 
			COALESCE(i.ShipToZip, d.ShipToZip), 
			COALESCE(i.ShipToCountry, d.ShipToCountry), 
			COALESCE(i.ShipToEmail, d.ShipToEmail), 
			COALESCE(i.ShipToPhone, d.ShipToPhone), 
			COALESCE(i.BillToId, d.BillToId), 
			COALESCE(i.BillToName, d.BillToName), 
			COALESCE(i.BillToAddress1, d.BillToAddress1), 
			COALESCE(i.BillToAddress2, d.BillToAddress2), 
			COALESCE(i.BillToCity, d.BillToCity), 
			COALESCE(i.BillToState, d.BillToState), 
			COALESCE(i.BillToZip, d.BillToZip), 
			COALESCE(i.BillToCountry, d.BillToCountry), 
			COALESCE(i.BillToEmail, d.BillToEmail), 
			COALESCE(i.BillToPhone, d.BillToPhone), 
			COALESCE(i.BuyerId, d.BuyerId), 
			COALESCE(i.BuyerName, d.BuyerName), 
			COALESCE(i.BuyerAddress1, d.BuyerAddress1), 
			COALESCE(i.BuyerAddress2, d.BuyerAddress2), 
			COALESCE(i.BuyerCity, d.BuyerCity), 
			COALESCE(i.BuyerState, d.BuyerState), 
			COALESCE(i.BuyerZip, d.BuyerZip), 
			COALESCE(i.BuyerCountry, d.BuyerCountry), 
			COALESCE(i.BuyerEmail, d.BuyerEmail), 
			COALESCE(i.BuyerPhone, d.BuyerPhone), 
			COALESCE(i.IsStoreOrder, d.IsStoreOrder), 
			COALESCE(i.CreatedDate, d.CreatedDate),
            COALESCE(i.CreatedBy, d.CreatedBy)
		FROM INSERTED i
		FULL OUTER JOIN DELETED d ON i.Id = d.Id;
    END')
END
GO
