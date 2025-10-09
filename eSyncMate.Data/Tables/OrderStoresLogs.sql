IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'OrderStoresLogs')
BEGIN
	CREATE TABLE [dbo].[OrderStoresLogs](
		[OSLId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NOT NULL,
		[OrderId] [int] NOT NULL,
		[CustomerId] [int] NOT NULL,
		[CustomerPO] [varchar](250) NOT NULL,
		[Status] [varchar](25) NOT NULL,
		[Data] [nvarchar](max) NOT NULL,
		[Response] [nvarchar](max) NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL
		)
	END
GO


