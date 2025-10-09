CREATE TABLE [dbo].[SCS_ItemsTypeReport]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY, 
	[CustomerID] NVARCHAR(250) NOT NULL , 
    [ReportID] NVARCHAR(500) NOT NULL, 
    [Status] NVARCHAR(250) NOT NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL, 
    [ModifiedDate] DATETIME NULL , 
    [ModifiedBy] INT NULL 

)

GO

