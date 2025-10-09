CREATE VIEW [dbo].[VW_Connectors]
AS
SELECT C.[Id], C.[TypeId], C.[Name],C.ConnectivityType, C.[Data], C.[CreatedDate], C.[CreatedBy], C.[ModifiedDate], C.[ModifiedBy],
	   CT.[Name] AS ConnectorType, CT.[Party]
FROM Connectors C WITH (NOLOCK)
	INNER JOIN ConnectorTypes CT WITH (NOLOCK) ON C.TypeId = CT.Id
GO