DROP TABLE [Temp_SCS_CustomerProductCatalog]
GO
CREATE TABLE [dbo].[Temp_SCS_CustomerProductCatalog]
(
	
	[Brand]						 [VARCHAR](250) NULL,
	[ItemID]					 [VARCHAR](250) NULL,
	[UPC]						 [VARCHAR](250) NULL,
	[ItemTypeName]				 [VARCHAR](250) NULL,
	[ProductRelation]			 [VARCHAR](250) NULL,
	[ParentID]					 [VARCHAR](250) NULL,
	[ListPrice]					 [VARCHAR](250) NULL,
	[MapPrice]					 [VARCHAR](250) NULL,
	[OffPrice]					 [VARCHAR](250) NULL,
	[Type]						 [VARCHAR](50) NULL,
	[VariationType]				 [VARCHAR](100) NULL,
	[UnListed]					 VARCHAR(50) NULL,
	is_add_on                    VARCHAR(100) NULL,
	two_day_shipping_eligible    VARCHAR(100) NULL,
	shipping_exclusion           VARCHAR(200) NULL,
	seller_return_policy         VARCHAR(100) NULL,
	[JsonData]        [VARCHAR](MAX) NULL,
	[CustomerID]	  [VARCHAR](250) NULL
	
)

GO

