CREATE TABLE [dbo].[Maps]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(250) NOT NULL, 
    [TypeId] INT NOT NULL, 
    [Map] NVARCHAR(MAX) NOT NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL,
    [ModifiedDate] DATETIME  NULL DEFAULT GETDATE(), 
    [ModifiedBy] INT NULL

)
