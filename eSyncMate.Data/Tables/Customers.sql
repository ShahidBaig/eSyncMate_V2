CREATE TABLE [dbo].[Customers]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(250) NOT NULL, 
    [ERPCustomerID] NVARCHAR(250) NULL, 
    [ISACustomerID] NVARCHAR(250) NULL, 
    [ISA856ReceiverId] NVARCHAR(250) NULL, 
    [ISA810ReceiverId] NVARCHAR(250) NULL, 
    [Marketplace] NVARCHAR(250) NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL,
    [ModifiedDate] DATETIME NULL DEFAULT GETDATE(), 
    [ModifiedBy] INT NULL

)
