IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'PartnerGroupsLogs')
BEGIN
	CREATE TABLE [dbo].[PartnerGroupsLogs](
		[PGLId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NOT NULL,
		[SourcePartyId] [int] NOT NULL,
		[DestinationPartyId] [int] NOT NULL,
		[Description] [nvarchar](250) NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL,
		[ModifiedDate] [datetime] NULL,
		[ModifiedBy] [int] NULL,
		)
	END
GO


