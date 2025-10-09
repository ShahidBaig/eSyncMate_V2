CREATE TABLE [dbo].[SCS_ItemsType]
(
	ID	INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Brand] NVARCHAR(500)  NULL, 
    [Product_Subtype] NVARCHAR(500)  NULL, 
    [Item_Type] NVARCHAR(500), 
    [Item_Type_Id] NVARCHAR(500), 
    [Item_Type_Description] NVARCHAR(MAX) , 
    [CustomerID] NVARCHAR(500) , 
    [CreatedDate] DATETIME  NULL, 
    [CreatedBy] INT  NULL, 
    [ModifiedDate] DATETIME NULL , 
    [ModifiedBy] INT NULL 

)

GO
