DROP VIEW IF EXISTS [dbo].[VW_BatchWiseInventory]
GO

CREATE VIEW [dbo].[VW_BatchWiseInventory] AS
SELECT
	INV.CustomerID,
	INV.ItemId,
	INV.CustomerItemCode,
	INV.ETA_Date,
	INV.ETA_Qty,
	INV.Total_ATS,
	INV.ATS_L10,
	INV.ATS_L21,
	INV.ATS_L28,
	INV.ATS_L29,
	INV.ATS_L30,
	INV.ATS_L34,
	INV.ATS_L35,
	INV.ATS_L36,
	INV.ATS_L37,
	INV.ATS_L40,
	INV.ATS_L41,
	INV.ATS_L55,
	INV.ATS_L56,
	INV.ATS_L57,
	INV.ATS_L60,
	INV.ATS_L65,
	INV.ATS_L70,
	INV.ATS_L91,
	INV.Status,
	BD.ActionDate   AS CreatedDate,
	INV.CreatedBy,
	BD.ActionDate   AS ModifiedDate,
	INV.ModifiedBy,
	BD.Id,
	BD.BatchID
FROM SCSInventoryFeed INV WITH (NOLOCK)
	INNER JOIN (
		SELECT
			MAX(Id)     AS Id,
			CustomerId,
			ItemId,
			BatchID,
			MIN(CASE WHEN [Type] IN ('ERP-RVD', 'JSON-SNT') THEN CreatedDate END) AS ActionDate
		FROM SCSInventoryFeedData WITH (NOLOCK)
		GROUP BY CustomerId, ItemId, BatchID
	) BD
		ON INV.CustomerID = BD.CustomerId
		AND INV.ItemId    = BD.ItemId
GO
