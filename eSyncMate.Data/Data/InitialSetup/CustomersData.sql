DELETE FROM [Customers]
GO

INSERT INTO [Customers] (Id, Name, ERPCustomerID, ISACustomerID, [ISA856ReceiverId], [ISA810ReceiverId], Marketplace, CreatedBy)
VALUES (1, 'Sams Club', 'SAMSCLUB', '925485USSM', '925485USSM', '925485US00', 'SAMSCLUB_DSV', 1)
GO

INSERT INTO [Customers] (Id, Name, ERPCustomerID, ISACustomerID, [ISA856ReceiverId], [ISA810ReceiverId], Marketplace, CreatedBy)
VALUES (2, 'Nordstrom Store', 'NORDSTROM', '007942915', '2062336664', '007942915', 'Nordstrom Rack Stores', 1)
GO

INSERT INTO [Customers] (Id, Name, ERPCustomerID, ISACustomerID, [ISA856ReceiverId], ISA810ReceiverId, Marketplace, CreatedDate, CreatedBy)
VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM Customers),'Target Store','TARGET','6111470100','6111470100','6111470100','Target Stores',GETDATE(),1)
GO

INSERT INTO [Customers] (Id, Name, ERPCustomerID, ISACustomerID, [ISA856ReceiverId], ISA810ReceiverId, Marketplace, CreatedDate, CreatedBy)
VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM Customers),'SCS','SCS Customer',NULL,NULL,NULL,'SCS Customer',GETDATE(),1)
GO

INSERT INTO [Customers] (Id, Name, ERPCustomerID, ISACustomerID, [ISA856ReceiverId], ISA810ReceiverId, Marketplace, CreatedDate, CreatedBy)
VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM Customers),'Target Plus','TARGET',NULL,NULL,NULL,'Target Plus',GETDATE(),1)
GO
