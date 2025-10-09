DELETE FROM ConnectorTypes
GO

INSERT INTO ConnectorTypes (Id, [Name], [Party], CreatedBy)
VALUES (1, 'Orders', 'NetSuite', 1)
GO

INSERT INTO ConnectorTypes (Id, [Name], [Party], CreatedBy)
VALUES (2, 'ASNs', 'NetSuite', 1)
GO

INSERT INTO ConnectorTypes (Id, [Name], [Party], CreatedBy)
VALUES (3, 'Invoices', 'NetSuite', 1)
GO

INSERT INTO ConnectorTypes (Id, [Name], [Party], CreatedBy)
VALUES (4, 'Order Status', 'NetSuite', 1)
GO