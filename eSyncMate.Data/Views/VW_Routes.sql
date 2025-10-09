CREATE VIEW [dbo].[VW_Routes]
AS
SELECT  
	RT.Id,RT.Status,RT.SourcePartyId,RT.DestinationPartyId,RT.SourceConnectorId,RT.DestinationConnectorId,RT.MapId,RT.PartyGroupId PartnerGroupId,
	CS.Name SourceParty,CST.Name DestinationParty,SC.Name SourceConnector ,DC.Name DestinationConnector,
	MP.Name Map,PG.Description PartnerGroup,ROT.Name RouteType,RT.ModifiedBy,RT.CreatedBy,RT.ModifiedDate,RT.CreatedDate,
	RT.TypeId,
	RT.FrequencyType,
	RT.StartDate,
	RT.EndDate,
	RT.RepeatCount,
	RT.WeekDays,
	RT.OnDay,
	RT.ExecutionTime,
	RT.JobID,
	RT.Name,
	RT.RouteGroup,
	RT.CustomerName
FROM Routes RT WITH (NOLOCK)
	INNER JOIN Customers CS WITH (NOLOCK) ON RT.SourcePartyId = CS.Id
	INNER JOIN Customers CST WITH (NOLOCK) ON RT.DestinationPartyId = CST.Id
	INNER JOIN Connectors SC WITH (NOLOCK) ON RT.SourceConnectorId = SC.Id
	INNER JOIN Connectors DC WITH (NOLOCK) ON RT.DestinationConnectorId = DC.Id
	LEFT OUTER JOIN Maps MP WITH (NOLOCK) ON RT.MapId = MP.Id
	INNER JOIN PartnerGroups PG WITH (NOLOCK) ON RT.PartyGroupId = PG.Id
	INNER JOIN RouteTypes ROT WITH (NOLOCK) ON RT.TypeId = ROT.Id



GO


