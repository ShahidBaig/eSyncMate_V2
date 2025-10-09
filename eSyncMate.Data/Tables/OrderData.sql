CREATE TABLE [dbo].[OrderData]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [OrderId] INT NOT NULL, 
    [Type] VARCHAR(25) NOT NULL, 
    [OrderNumber] NVARCHAR(250) NOT NULL, 
    [Data] NVARCHAR(MAX) NOT NULL,
    [Status] VARCHAR(25) NULL,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL
)
