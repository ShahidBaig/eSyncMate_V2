ALTER VIEW [dbo].[VW_Inventory] AS
SELECT  INV.BatchID,ItemCount,StartDate,FinishDate,Status,PageCount,   
CASE WHEN RouteType = 'SCSFullInventoryFeed' THEN 'Full inventory feed received from ERP'
	WHEN RouteType = 'SCSDifferentialInventoryFeed' THEN 'Differential inventory feed received from ERP'
	WHEN  RouteType = 'SCSUpdateInventory' THEN  'Inventory feed sent to Portal'
	WHEN  RouteType = 'WalmartUploadInventory' THEN  'Inventory feed sent to Customer Portal'
	WHEN  RouteType = 'TragetPlus Inventory' THEN  'Inventory feed sent to Customer Portal'

	ELSE RouteType END  RouteType, INV.CustomerID
FROM InventoryBatchWise INV WITH (NOLOCK)
GO