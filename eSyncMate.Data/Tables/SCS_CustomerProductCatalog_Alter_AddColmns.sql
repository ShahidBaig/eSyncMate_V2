
SELECT * 
INTO zdt_SCS_CustomerProductCatalog_Backup 
FROM SCS_CustomerProductCatalog

GO


DROP TABLE  [SCS_CustomerProductCatalog]

GO

CREATE TABLE [dbo].[SCS_CustomerProductCatalog](
	[ProductId] [int] IDENTITY(1,1) NOT NULL,
	[Brand] [varchar](250) NULL,
	[ItemID] [varchar](250) NULL,
	[UPC] [varchar](250) NULL,
	[ItemTypeName] [varchar](250) NULL,
	[ProductRelation] [varchar](250) NULL,
	[ParentID] [varchar](250) NULL,
	[ListPrice] [varchar](250) NULL,
	[MapPrice] [varchar](250) NULL,
	[OffPrice] [varchar](250) NULL,
	[Type] [varchar](50) NULL,
	[VariationType] [varchar](100) NULL,
	[UnListed] [bit] NULL,
	is_add_on                    VARCHAR(100) NULL,
	two_day_shipping_eligible    VARCHAR(100) NULL,
	shipping_exclusion           VARCHAR(200) NULL,
	seller_return_policy         VARCHAR(100) NULL,
	[JsonData] [varchar](max) NULL,
	[CustomerID] [varchar](250) NOT NULL,
	[SyncStatus] [varchar](100) NULL,
	[Id] [varchar](500) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL,
	
PRIMARY KEY CLUSTERED 
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


INSERT INTO SCS_CustomerProductCatalog (Brand,ItemID,UPC,ItemTypeName,ProductRelation,ParentID,ListPrice,MapPrice,OffPrice,Type,VariationType,JsonData,CustomerID,
SyncStatus,Id,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,UnListed)

SELECT Brand,ItemID,UPC,ItemTypeName,ProductRelation,ParentID,ListPrice,MapPrice,OffPrice,Type,VariationType,JsonData,CustomerID,
SyncStatus,Id,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,UnListed 
FROM zdt_SCS_CustomerProductCatalog_Backup 

GO