CREATE TABLE [dbo].[ConnectorTypes]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(250) NOT NULL, 
    [Party] NVARCHAR(250) NOT NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL
)
