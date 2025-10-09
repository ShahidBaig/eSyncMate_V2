CREATE VIEW [dbo].[VW_PurchaseOrderData] AS 
SELECT OD.Id, OD.OrderId, OD.[Type], OD.[Data], OD.CreatedDate, 
	OD.[Type] + '-' + OD.OrderNumber + '-' + REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, GETDATE(), 127), '-', ''), ':', ''), ' ', ''), '.', '') + 
	CASE WHEN OD.[Type] LIKE '%EDI%' THEN '.edi' WHEN OD.[Type] LIKE '%JSON%' OR OD.[Type] LIKE '%RESPONSE%' OR OD.[Type] LIKE '%-NS%' OR OD.[Type] LIKE '%Fields%' THEN '.json' ELSE '.txt' END [FileName]
FROM PurchaseOrderData OD
	INNER JOIN PurchaseOrders O ON OD.OrderId = O.Id
GO


