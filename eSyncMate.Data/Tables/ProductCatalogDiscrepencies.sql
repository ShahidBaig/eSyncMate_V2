CREATE TABLE [dbo].[ProductCatalogDiscrepencies](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[CustomerID] [varchar](100) NULL,
	[ItemID] [varchar](100) NULL,
	[Data] [varchar](100) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL
) ON [PRIMARY]
GO


