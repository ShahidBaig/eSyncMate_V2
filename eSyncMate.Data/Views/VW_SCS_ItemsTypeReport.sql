CREATE VIEW [dbo].[VW_SCS_ItemsTypeReport]
AS
SELECT Id,CustomerID,ReportID,Status,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy
FROM SCS_ItemsTypeReport  WITH (NOLOCK)
	
GO


