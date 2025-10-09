CREATE VIEW [dbo].[VW_PurchaseOrders] AS 
SELECT O.*, C.Name CustomerName
FROM PurchaseOrders O
	INNER JOIN Customers C ON O.CustomerId = C.Id
GO


