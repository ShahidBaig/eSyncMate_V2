
ALTER TABLE Temp_SCS_ItemTypeAttribute
ADD CustomerID NVARCHAR(500) NULL

GO


SELECT * INTO zdt_SCS_ItemTypeAttribute FROM [SCS_ItemTypeAttribute]



GO

DROP TABLE [SCS_ItemTypeAttribute]

GO


CREATE TABLE [dbo].[SCS_ItemTypeAttribute](
	[ID] [nvarchar](500) NOT NULL,
	[Name] [nvarchar](500) NULL,
	[Mapped_Property] [nvarchar](500) NULL,
	[Type] [nvarchar](250) NULL,
	[Item_Type_Id] [nvarchar](500) NULL,
	[Required] [varchar](50) NULL,
	[CustomerID] NVARCHAR(500) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [int] NULL
) ON [PRIMARY]
GO

INSERT INTO [SCS_ItemTypeAttribute] (ID,Name,Mapped_Property,Type,Item_Type_Id,Required,CustomerID,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy)
SELECT ID,Name,Mapped_Property,Type,Item_Type_Id,Required,'TAR6266P',CreatedDate,CreatedBy,ModifiedDate,ModifiedBy 
FROM  zdt_SCS_ItemTypeAttribute

GO





