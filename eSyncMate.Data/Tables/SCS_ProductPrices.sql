CREATE TABLE [dbo].[SCS_ProductPrices](
	[ProductId] [int] IDENTITY(1,1) NOT NULL,
	[ItemID] [varchar](250) NULL,
	[ListPrice] [varchar](250) NULL,
	[MapPrice] [varchar](250) NULL,
	[OffPrice] [varchar](250) NULL,
	[CustomerID] [varchar](250) NOT NULL,
	[SyncStatus] [varchar](100) NULL,
	[Id] [varchar](500) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL,
	[UnListed] [bit] NULL
	)

	GO