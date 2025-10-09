CREATE VIEW [dbo].[VW_CustomerConnectors] AS 
SELECT CC.*, C.Name ConnectorName, C.Data ConnectorData, CT.Name ConnectorType, CT.Party ConnectorParty
FROM CustomerConnectors CC 
	INNER JOIN Customers CU ON CC.CustomerId = CU.Id
	INNER JOIN Connectors C ON CC.ConnectorId = C.Id
	INNER JOIN ConnectorTypes CT ON C.TypeId = CT.Id
GO
