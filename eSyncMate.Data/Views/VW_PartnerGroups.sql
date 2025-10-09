CREATE VIEW [dbo].[VW_PartnerGroups]
AS
SELECT  
PG.Id,PG.SourcePartyId,PG.DestinationPartyId,PG.Description,PG.CreatedDate,PG.CreatedBy,PG.ModifiedDate,
PG.ModifiedBy,CS.Name SourceParty, CST.Name DestinationParty
	  
FROM PartnerGroups PG WITH (NOLOCK)
	INNER JOIN Customers CS WITH (NOLOCK) ON PG.SourcePartyId = CS.Id
	INNER JOIN Customers CST WITH (NOLOCK) ON PG.DestinationPartyId = CST.Id

GO

