IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ConnectorTypesLogs')
BEGIN
	CREATE TABLE [dbo].[ConnectorTypesLogs](
		[CTLId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NOT NULL,
		[Name] [nvarchar](250) NOT NULL,
		[Party] [nvarchar](250) NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL,
		)
	END
GO
