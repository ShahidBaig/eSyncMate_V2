IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ProductUploadPricesLogs')
BEGIN
	CREATE TABLE [dbo].[ProductUploadPricesLogs](
		[PLId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int]  NOT NULL,
		[CustomerID] [varchar](500) NOT NULL,
		[ItemId] [varchar](250) NOT NULL,
		[ListPrice] [varchar](250) NULL,
		[OffPrice] [varchar](250) NULL,
		[PromoStartDate] [datetime] NULL,
		[PromoEndDate] [datetime] NULL,
		[Status] [varchar](100) NOT NULL,
		[OldListPrice] [varchar](250) NULL,
		[OldOffPrice] [varchar](250) NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL,
		[ModifiedDate] [datetime] NULL,
		[ModifiedBy] [int] NULL,
		)
	END
GO


