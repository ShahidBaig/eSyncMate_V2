
ALTER VIEW [dbo].[VW_SCSInventoryFeedData] AS
SELECT OD.Id, OD.CustomerId, OD.ItemId, OD.[Type], OD.[Data], OD.CreatedDate, 
	OD.[Type] + '-' + OD.CustomerId + '-' + OD.ItemId + '-' + REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, GETDATE(), 127), '-', ''), ':', ''), ' ', ''), '.', '') + 
	CASE WHEN OD.[Type] LIKE '%EDI%' THEN '.edi' WHEN OD.[Type] LIKE '%JSON%' OR OD.[Type] LIKE '%RESPONSE%' OR OD.[Type] LIKE '%-NS%' OR OD.[Type] LIKE '%Fields%' THEN '.json' ELSE '.txt' END [FileName],
	OD.BatchID
FROM SCSInventoryFeedData OD WITH (NOLOCK)
	INNER JOIN SCSInventoryFeed O WITH (NOLOCK) ON OD.CustomerId = O.CustomerID AND OD.ItemId = O.ItemId
GO