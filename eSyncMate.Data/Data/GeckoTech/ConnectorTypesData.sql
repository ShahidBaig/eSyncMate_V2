DELETE FROM ConnectorTypes
GO

INSERT INTO ConnectorTypes (Id, [Name], [Party], CreatedBy)
VALUES (1, 'SQLServer', 'SCS', 1)
GO

INSERT INTO ConnectorTypes (Id, [Name], [Party], CreatedBy)
VALUES (2, 'Rest', 'SCS', 1)
GO

INSERT INTO ConnectorTypes (Id, [Name], [Party], CreatedBy)
VALUES (3, 'SFTP', 'SCS', 1)
GO
