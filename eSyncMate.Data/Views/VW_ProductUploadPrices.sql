
ALTER VIEW [dbo].[VW_ProductUploadPrices]
AS
SELECT  Id,CustomerID,ItemId,ListPrice,OffPrice,PromoStartDate,PromoEndDate,Status,OldListPrice,OldOffPrice,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,
MAPPrice,OldMAPPrice
FROM ProductUploadPrices  WITH (NOLOCK)
	
GO


