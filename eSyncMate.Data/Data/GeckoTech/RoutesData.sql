DELETE FROM [Routes]
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (1, 'FREJI 204', 100, 'Active', 3, 1, 2, 1, 1, 1, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (2, 'Logística 204', 100, 'Active', 4, 1, 3, 1, 1, 2, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (3, 'Saucedo 204', 100, 'Active', 5, 1, 4, 1, 1, 3, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (4, 'FREJI 990', 101, 'Active', 1, 3, 1, 5, 2, 1, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (5, 'Logística 990', 101, 'Active', 1, 4, 1, 6, 2, 2, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (6, 'Saucedo 990', 101, 'Active', 1, 5, 1, 7, 2, 3, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (7, 'FREJI 214', 102, 'Active', 1, 3, 1, 8, 3, 1, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (8, 'Logística 214', 102, 'Active', 1, 4, 1, 9, 3, 2, 1, GETDATE())
GO

INSERT INTO [Routes] (Id, [Name], TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, CreatedBy, CreatedDate)
VALUES (9, 'Saucedo 214', 102, 'Active', 1, 5, 1, 10, 3, 3, 1, GETDATE())
GO