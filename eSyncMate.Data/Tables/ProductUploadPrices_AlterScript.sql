

SELECT * INTO zdt_ProductUploadPrices FROM ProductUploadPrices

GO

DROP TABLE [ProductUploadPrices]

GO


CREATE TABLE [dbo].[ProductUploadPrices](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomerID] [varchar](500) NOT NULL,
	[ItemId] [varchar](250) NOT NULL,
	[ListPrice] [varchar](250) NULL,
	[OffPrice] [varchar](250) NULL,
	[MAPPrice] [varchar](250) NULL,
	[PromoStartDate] [datetime] NULL,
	[PromoEndDate] [datetime] NULL,
	[Status] [varchar](100) NOT NULL,
	[OldListPrice] [varchar](250) NULL,
	[OldOffPrice] [varchar](250) NULL,
	[OldMAPPrice] [varchar](250) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT INTO [ProductUploadPrices] (CustomerID,ItemId,ListPrice,OffPrice,PromoStartDate,PromoEndDate,Status,OldListPrice,OldOffPrice,CreatedDate,
CreatedBy,ModifiedDate,ModifiedBy)
SELECT CustomerID,ItemId,ListPrice,OffPrice,PromoStartDate,PromoEndDate,Status,OldListPrice,OldOffPrice,CreatedDate,
CreatedBy,ModifiedDate,ModifiedBy FROM zdt_ProductUploadPrices

GO

DROP TABLE [Temp_ProductUploadPrices]

GO

CREATE TABLE [dbo].[Temp_ProductUploadPrices](
	[ItemId] [varchar](250) NULL,
	[ListPrice] [varchar](250) NULL,
	[OffPrice] [varchar](250) NULL,
	[MAPPrice] [varchar](250) NULL,
	[PromoStartDate] [varchar](100) NULL,
	[PromoEndDate] [varchar](100) NULL,
	[OldListPrice] [varchar](250) NULL,
	[OldOffPrice] [varchar](250) NULL,
	[OldMAPPrice] [varchar](250) NULL,
	[CustomerID] [varchar](500) NULL
) ON [PRIMARY]
GO

DROP TABLE [ProductUploadPricesLogs]

GO

CREATE TABLE [dbo].[ProductUploadPricesLogs](
	[PLId] [int] IDENTITY(1,1) NOT NULL,
	[ActionType] [varchar](10) NOT NULL,
	[Id] [int] NOT NULL,
	[CustomerID] [varchar](500) NOT NULL,
	[ItemId] [varchar](250) NOT NULL,
	[ListPrice] [varchar](250) NULL,
	[OffPrice] [varchar](250) NULL,
	[MAPPrice] [varchar](250) NULL,
	[PromoStartDate] [datetime] NULL,
	[PromoEndDate] [datetime] NULL,
	[Status] [varchar](100) NOT NULL,
	[OldListPrice] [varchar](250) NULL,
	[OldOffPrice] [varchar](250) NULL,
	[OldMAPPrice] [varchar](250) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL
) ON [PRIMARY]
GO


