CREATE TABLE [dbo].[CustomerConnectors]
(
	[Id] INT NOT NULL PRIMARY KEY, 
	[Name] [varchar](250) NOT NULL,
	[Address] [varchar](250) NOT NULL,
	[City] [varchar](250) NOT NULL,
	[State] [varchar](250) NOT NULL,
	[Zip] [varchar](250) NOT NULL,
	[Contact] [varchar](250) NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ModifiedBy] [int] NULL,
	[ModifiedDate] [datetime] NULL,
)
