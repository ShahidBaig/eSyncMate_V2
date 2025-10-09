CREATE VIEW [dbo].[VW_TempSCSInventoryFeed]
AS
SELECT CustomerID,ItemId,CustomerItemCode,ETA_Date,ETA_Qty,Total_ATS ,ATS_L10,ATS_L21,
	   ATS_L28,ATS_L30,ATS_L34,ATS_L35,ATS_L36,ATS_L37,ATS_L40,ATS_L41,ATS_L55,ATS_L60
	   ATS_L70,ATS_L91,CurrentPage,TotalPages,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy
FROM Temp_SCSInventoryFeed  WITH (NOLOCK)
GO
