CREATE TABLE [dbo].[SCS_ItemTypeAttribute]
(
	ID					NVARCHAR(500) NOT NULL, 
	[Name]				NVARCHAR(500) NULL, 
	[Mapped_Property]	NVARCHAR(500) NULL,
	[Type]				NVARCHAR(250) NULL,
    [Item_Type_Id]		NVARCHAR(500) NULL, 
	[Required]			VARCHAR(50),
	[CreatedDate] DATETIME  NOT NULL, 
    [CreatedBy] INT  NOT NULL, 
    [ModifiedDate] DATETIME NULL , 
    [ModifiedBy] INT NULL 

)

GO



