CREATE VIEW [dbo].[VW_Temp_PurchaseOrders] AS 
SELECT MAX(C.Name) CustomerName, MAX(PD.ItemID) ItemID, PD.OrderQty,MAX(P.Id) AS Id, MAX(P.PONumber) PONumber, MAX(P.Status) Status, MAX(P.OrderDate) OrderDate, MAX(P.SupplierID) SupplierID , MAX(P.VExpectedDate) VExpectedDate
FROM PurchaseOrders P 
	INNER JOIN PurchaseOrderDetail PD ON P.Id = PD.OrderId
	INNER JOIN Suppliers C ON P.SupplierID = C.SupplierID
	GROUP BY P.PONumber, PD.OrderQty
GO