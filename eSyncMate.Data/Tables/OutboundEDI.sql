CREATE TABLE [dbo].[OutboundEDI]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [orderId] INT NOT NULL, 
    [Status] VARCHAR(25) NOT NULL, 
    [Data] NVARCHAR(MAX) NOT NULL, 
    [CreatedDate] DATETIME NOT NULL, 
    [CreatedBy] INT NOT NULL
)
