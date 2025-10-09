CREATE TABLE [dbo].[CustomersFeed]
(
	[Id] INT NOT NULL, 
    [ItemID] NVARCHAR(250) NOT NULL, 
    [ItemDescription] NVARCHAR(250) NULL, 
    [UPC] NVARCHAR(20) NULL, 
    [SpecialID] NVARCHAR(550) NULL, 
    [AvailableQty] INT NOT NULL, 
    [OnHandQty] INT NOT NULL, 
    [OnOrderQty] INT NOT NULL, 
    CONSTRAINT [PK_CustomerFeed] PRIMARY KEY ([Id],[ItemID])
)
