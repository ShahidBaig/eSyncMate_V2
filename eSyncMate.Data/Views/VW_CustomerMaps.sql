CREATE VIEW [dbo].[VW_CustomerMaps] AS 
SELECT cm.*, m.Name MapName, m.Map, mt.Id MapTypeId, mt.Name MapTypeName 
FROM CustomerMaps cm
	INNER JOIN Maps m ON cm.MapId = m.Id
	INNER JOIN MapTypes mt ON m.TypeId = mt.Id
GO
