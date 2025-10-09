CREATE TABLE ProductUploadPrices
(
	Id					INT IDENTITY(1,1) PRIMARY KEY,
	CustomerID			VARCHAR(500) NOT NULL,
	ItemId				VARCHAR(250) NOT NULL,
	[ListPrice]         [VARCHAR](250) NULL,
	[OffPrice]          [VARCHAR](250) NULL,
	PromoStartDate		DATETIME NULL,
	PromoEndDate		DATETIME NULL,
	Status				VARCHAR(100) NOT NULL,
	[OldListPrice]     [VARCHAR](250) NULL,
	[OldOffPrice]      [VARCHAR](250) NULL,
	[CreatedDate]		DATETIME NOT NULL, 
	[CreatedBy]			INT NOT NULL,
	[ModifiedDate]		DATETIME  NULL , 
	[ModifiedBy]		INT NULL
)

GO

