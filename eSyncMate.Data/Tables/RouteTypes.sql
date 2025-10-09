CREATE TABLE [RouteTypes] 
(
	Id							INT PRIMARY KEY NOT NULL,
	[Name]						NVARCHAR(250) NOT NULL,
	[Description]				NVARCHAR(500) NOT NULL,
	CreatedDate					DATETIME NOT NULL,
	CreatedBy					INT NOT NULL,
	ModifiedDate				DATETIME NULL,
	ModifiedBy					INT NULL
)

GO