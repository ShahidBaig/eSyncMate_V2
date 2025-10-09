CREATE VIEW [dbo].[VW_PurchaseOrderDetail] AS 
SELECT OD.*
FROM PurchaseOrderDetail OD
	INNER JOIN PurchaseOrders O ON OD.OrderId = O.Id
GO


