DROP VIEW IF EXISTS [dbo].[VW_BatchWiseInventory]
GO

CREATE VIEW [dbo].[VW_BatchWiseInventory] AS
SELECT INV.*, BD.Id, BD.BatchID
FROM SCSInventoryFeed INV WITH (NOLOCK)
	INNER JOIN (
		SELECT MAX(Id) AS Id, CustomerId, ItemId, BatchID
		FROM SCSInventoryFeedData WITH (NOLOCK)
		GROUP BY CustomerId, ItemId, BatchID
	) BD ON INV.CustomerID = BD.CustomerId AND INV.ItemId = BD.ItemId
GO
