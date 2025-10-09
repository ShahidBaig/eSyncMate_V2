DELETE FROM PartnerGroups
GO

INSERT INTO PartnerGroups(Id, SourcePartyId, DestinationPartyId, Description, CreatedBy, CreatedDate)
VALUES (1, 3, 2, 'FREJI Especializados-GekoTech', 1, GETDATE())
GO

INSERT INTO PartnerGroups(Id, SourcePartyId, DestinationPartyId, Description, CreatedBy, CreatedDate)
VALUES (2, 4, 2, 'Logística y Distribución Pérez-GekoTech', 1, GETDATE())
GO

INSERT INTO PartnerGroups(Id, SourcePartyId, DestinationPartyId, Description, CreatedBy, CreatedDate)
VALUES (3, 5, 2, 'Saucedo Logistics', 1, GETDATE())
GO