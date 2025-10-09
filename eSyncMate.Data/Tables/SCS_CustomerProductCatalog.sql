CREATE TABLE [dbo].[SCS_CustomerProductCatalog]
(
	[ProductId]		  INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Brand]			  [VARCHAR](250) NULL,
	[ItemID]		  [VARCHAR](250) NULL,
	[UPC]			  [VARCHAR](250) NULL,
	[ItemTypeName]	  [VARCHAR](250) NULL,
	[ProductRelation] [VARCHAR](250) NULL,
	[ParentID]		  [VARCHAR](250) NULL,
	[ListPrice]       [VARCHAR](250) NULL,
	[MapPrice]        [VARCHAR](250) NULL,
	[OffPrice]        [VARCHAR](250) NULL,
	[Type]            [VARCHAR](50) NULL,
	[VariationType]   [VARCHAR](100) NULL,
	[JsonData]        [VARCHAR](MAX) NULL,
	[CustomerID]	  [VARCHAR](250) NOT NULL,
	[SyncStatus]      [VARCHAR](100) NULL,
	[Id]			  VARCHAR(500)NULL,
	[CreatedDate]     DATETIME NOT  NULL, 
    [CreatedBy]       INT  NOT NULL, 
    [ModifiedDate]    DATETIME NULL , 
    [ModifiedBy]      INT NULL 
	
)

GO

