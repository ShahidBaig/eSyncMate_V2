IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'RoutesLogs')
BEGIN
	CREATE TABLE [dbo].[RoutesLogs](
		[RLId] [int] IDENTITY(1,1) NOT NULL,
		[ActionType] [varchar](10) NOT NULL,
		[Id] [int] NOT NULL,
		[TypeId] [int] NOT NULL,
		[Status] [nvarchar](50) NOT NULL,
		[SourcePartyId] [int] NOT NULL,
		[DestinationPartyId] [int] NOT NULL,
		[SourceConnectorId] [int] NOT NULL,
		[DestinationConnectorId] [int] NOT NULL,
		[MapId] [int] NULL,
		[PartyGroupId] [int] NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[CreatedBy] [int] NOT NULL,
		[ModifiedDate] [datetime] NULL,
		[ModifiedBy] [int] NULL,
		[FrequencyType] [varchar](100) NULL,
		[StartDate] [datetime] NULL,
		[EndDate] [datetime] NULL,
		[RepeatCount] [int] NULL,
		[WeekDays] [varchar](250) NULL,
		[OnDay] [varchar](200) NULL,
		[ExecutionTime] [varchar](200) NULL,
		[JobID] [varchar](50) NULL,
		[Name] [varchar](100) NULL,
		[RouteGroup] [varchar](50) NULL,
		[CustomerName] [varchar](50) NULL,
		)
	END
GO


