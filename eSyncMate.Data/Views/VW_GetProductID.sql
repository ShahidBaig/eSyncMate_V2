
CREATE VIEW [dbo].[VW_GetProductID]
AS
SELECT C.*,CT.ProductID
FROM ProductUploadPrices C WITH (NOLOCK)
	INNER JOIN CustomerProductCatalogPrices CT WITH (NOLOCK) ON C.ItemId = CT.ItemId AND C.CustomerID = CT.CustomerID
GO

