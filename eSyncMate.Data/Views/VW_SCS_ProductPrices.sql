ALTER VIEW [dbo].[VW_SCS_ProductPrices]
AS
	SELECT ProductId,ItemID,ListPrice,MapPrice,OffPrice,CustomerID,SyncStatus,Id,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,UnListed,
	CASE WHEN ModifiedDate IS NULL THEN CreatedDate ELSE ModifiedDate END ActivityDate
	FROM SCS_ProductPrices WITH (NOLOCK)
	



GO



