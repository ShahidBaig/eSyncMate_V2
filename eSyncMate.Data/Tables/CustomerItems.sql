CREATE TABLE [dbo].[CustomerItems]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [CustomerId] INT NOT NULL, 
    [UPC] VARCHAR(25) NOT NULL, 
    [VendorStyle] VARCHAR(25) NOT NULL, 
    [Price] REAL NULL, 
    [Discount] REAL NULL, 
    [FromDate] DATETIME NULL,
    [ToDate] DATETIME NULL,
    [CreatedBy] INT NOT NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT(GETDATE()),

)
