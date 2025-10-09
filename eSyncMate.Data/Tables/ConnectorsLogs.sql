IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ConnectorsLogs')
BEGIN
	CREATE TABLE [dbo].[ConnectorsLogs](
		[CLId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NULL,
		[TypeId] [int] NOT NULL,
		[Name] [nvarchar](250) NOT NULL,
		[Data] [varchar](max) NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL,
		[ModifiedDate] [datetime] NULL,
		[ModifiedBy] [int] NULL,
		)
	END
GO


