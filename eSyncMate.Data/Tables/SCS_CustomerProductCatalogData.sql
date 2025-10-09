CREATE TABLE [SCS_CustomerProductCatalogData] 
(
	Id							INT  PRIMARY KEY NOT NULL,
	ProductId					INT NOT NULL,
	Type						NVARCHAR(500) NOT NULL,
	Data						NVARCHAR(MAX),
	CreatedDate					DATETIME NOT NULL,
	CreatedBy					INT NOT NULL,
	ModifiedDate				DATETIME NULL,
	ModifiedBy					INT NULL
)

GO
