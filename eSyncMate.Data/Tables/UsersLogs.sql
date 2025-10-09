IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UsersLogs')
BEGIN
	CREATE TABLE [dbo].[UsersLogs](
		[ULId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NOT NULL,
		[FirstName] [nvarchar](50) NOT NULL,
		[LastName] [nvarchar](50) NOT NULL,
		[Email] [nvarchar](50) NOT NULL,
		[Mobile] [nvarchar](50) NULL,
		[Password] [nvarchar](100) NOT NULL,
		[Status] [nvarchar](50) NOT NULL,
		[CreatedDate] [datetime] NULL,
		[CreatedBy] [int] NULL,
		[UserType] [nvarchar](20) NOT NULL,
		[Company] [varchar](50) NULL
		)
	END
GO


