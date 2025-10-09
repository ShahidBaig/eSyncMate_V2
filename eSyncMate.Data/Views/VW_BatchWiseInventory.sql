DROP VIEW IF EXISTS [dbo].[VW_BatchWiseInventory]
GO

CREATE VIEW [dbo].[VW_BatchWiseInventory] AS
SELECT  DISTINCT INV.*, [Data].BatchID
FROM SCSInventoryFeed INV WITH (NOLOCK)
	INNER JOIN SCSInventoryFeedData [Data] WITH (NOLOCK) ON INV.CustomerID = [Data].CustomerId AND INV.ItemId = [Data].ItemId
GO
