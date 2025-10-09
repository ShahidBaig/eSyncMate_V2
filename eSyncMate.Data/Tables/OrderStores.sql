CREATE TABLE [dbo].[OrderStores]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [OrderId] int NOT NULL, 
    [CustomerId] int NOT NULL, 
    [CustomerPO] VARCHAR(250) NOT NULL, 
    [Status] VARCHAR(25) NOT NULL, 
    [Data] NVARCHAR(MAX) NOT NULL, 
    [Response] NVARCHAR(MAX) NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL
)
