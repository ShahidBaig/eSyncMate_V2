CREATE VIEW [dbo].[VW_Inventory_ItemWise] AS
	SELECT  INV.BatchID,ItemCount,StartDate,FinishDate,Status,PageCount,   
	CASE WHEN RouteType = 'SCSFullInventoryFeed' THEN 'Full inventory feed received from ERP'
	WHEN RouteType = 'SCSDifferentialInventoryFeed' THEN 'Differential inventory feed received from ERP'
	WHEN  RouteType = 'SCSUpdateInventory' THEN  'Inventory feed sent to Portal'
	WHEN  RouteType = 'WalmartUploadInventory' THEN  'Inventory feed sent to Customer Portal'
	WHEN  RouteType = 'TragetPlus Inventory' THEN  'Inventory feed sent to Customer Portal'
	ELSE RouteType END  RouteType, INV.CustomerID, D.ItemId
FROM InventoryBatchWise INV WITH (NOLOCK)
	INNER JOIN SCSInventoryFeedData D WITH (NOLOCK) ON INV.BatchID = D.BatchID
GO
