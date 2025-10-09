CREATE TABLE [dbo].[Connectors]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [TypeId] INT NOT NULL, 
    [Name] NVARCHAR(250) NOT NULL, 
    [Data] VARCHAR(MAX) NOT NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL, 
    [ModifiedDate] DATETIME NULL DEFAULT GETDATE(), 
    [ModifiedBy] INT NULL 

)
