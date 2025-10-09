CREATE VIEW [dbo].[VW_SCS_CustomerProductCatalogData]
AS
	SELECT CPD.[Id], CPD.[ProductId], CPD.[Data], CPD.[CreatedDate], CPD.[CreatedBy], CPD.[ModifiedDate], CPD.[ModifiedBy],  CPC.[ItemTypeName] ,
	CPD.Type + '-' + REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, CPC.[CreatedDate], 127), '-', ''), ':', ''), ' ', ''), '.', '') + 
	CASE WHEN CPD.Type LIKE '%EDI%' THEN '.edi' WHEN CPD.Type LIKE '%JSON%' OR CPD.Type LIKE '%RESPONSE%' OR CPD.Type LIKE '%-NS%' OR CPD.Type LIKE '%Fields%' THEN '.json' ELSE '.txt' END [FileName],
	 CASE WHEN  CPD.Type = 'RSP-JSON' THEN 'Product data response received from customer portal'
				WHEN CPD.Type = 'REQ-JSON' THEN 'Product data sent to customer portal'
		ELSE 
		CPD.Type 
		END Type,
	CPC.ItemID
	FROM SCS_CustomerProductCatalogData CPD WITH (NOLOCK) 
	INNER JOIN SCS_CustomerProductCatalog CPC WITH (NOLOCK) ON CPD.ProductId = CPC.ProductId
	WHERE CPD.CreatedDate >= ISNULL(CPC.ModifiedDate, CPC.CreatedDate)
GO