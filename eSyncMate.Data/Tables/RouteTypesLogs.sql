IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'RouteTypesLogs')
BEGIN
	CREATE TABLE [dbo].[RouteTypesLogs](
		[RTLId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NOT NULL,
		[Name] [nvarchar](250) NOT NULL,
		[Description] [nvarchar](500) NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL,
		[ModifiedDate] [datetime] NULL,
		[ModifiedBy] [int] NULL,
		)
	END
GO
