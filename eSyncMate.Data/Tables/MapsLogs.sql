IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'MapsLogs')
BEGIN
	CREATE TABLE [dbo].[MapsLogs](
		[MId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NULL,
		[Name] [nvarchar](250) NOT NULL,
		[TypeId] [int] NOT NULL,
		[Map] [varchar](max) NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL,
		)
	END
GO
