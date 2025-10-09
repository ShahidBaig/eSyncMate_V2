CREATE TABLE [dbo].[PriceDiscrepencies](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[CustomerID] [varchar](100) NULL,
	[ItemID] [varchar](100) NULL,
	[Data] [varchar](500) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL
) ON [PRIMARY]
GO





