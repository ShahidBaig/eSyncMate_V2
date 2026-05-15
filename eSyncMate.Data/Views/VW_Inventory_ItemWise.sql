CREATE VIEW [dbo].[VW_Inventory_ItemWise] AS
SELECT
    INV.BatchID, INV.ItemCount, INV.StartDate, INV.FinishDate,
    INV.Status, INV.PageCount,
    CASE
        WHEN INV.RouteType = 'SCSFullInventoryFeed'         THEN 'Full inventory feed received from ERP'
        WHEN INV.RouteType = 'SCSDifferentialInventoryFeed' THEN 'Differential inventory feed received from ERP'
        WHEN INV.RouteType = 'SCSUpdateInventory'           THEN 'Inventory feed sent to Portal'
        WHEN INV.RouteType = 'WalmartUploadInventory'       THEN 'Inventory feed sent to Customer Portal'
        WHEN INV.RouteType = 'TragetPlus Inventory'         THEN 'Inventory feed sent to Customer Portal'
        ELSE INV.RouteType
    END AS RouteType,
    INV.RouteType AS OrignalRouteType,
    INV.CustomerID,
    D.ItemId
FROM InventoryBatchWise INV WITH (NOLOCK)
    INNER JOIN VW_AllSCSInventoryFeedData D ON INV.BatchID = D.BatchID