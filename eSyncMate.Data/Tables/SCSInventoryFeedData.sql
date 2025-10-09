CREATE TABLE [dbo].[SCSInventoryFeedData]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [CustomerId] VARCHAR(50) NOT NULL, 
    [ItemId] VARCHAR(50) NOT NULL, 
    [Type] VARCHAR(25) NOT NULL, 
    [Data] NVARCHAR(MAX) NOT NULL,
    BatchID				 NVARCHAR(500),
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL
)
