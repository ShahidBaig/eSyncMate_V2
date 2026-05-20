CREATE OR ALTER VIEW [dbo].[VW_AllSCSInventoryFeedData] AS
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_TAR6266P   WITH (NOLOCK)
UNION ALL
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_WAL4001MP  WITH (NOLOCK)
UNION ALL
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_MAC0149M   WITH (NOLOCK)
UNION ALL
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_TAR6266PAH WITH (NOLOCK)
UNION ALL
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_LOW2221MP  WITH (NOLOCK)
UNION ALL
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_AMA1005    WITH (NOLOCK)
UNION ALL
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_KNO8068    WITH (NOLOCK)
UNION ALL
SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.SCSInventoryFeedData_MIC1300MP  WITH (NOLOCK)