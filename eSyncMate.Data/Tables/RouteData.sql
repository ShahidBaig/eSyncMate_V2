CREATE TABLE [RouteData] 
(
	Id							INT  PRIMARY KEY NOT NULL,
	RouteId						INT NOT NULL,
	Type						NVARCHAR(250) NOT NULL,
	OrderId						INT  NULL,
	Data						NVARCHAR(250),
	CreatedDate					DATETIME NOT NULL,
	CreatedBy					INT NOT NULL,
	ModifiedDate				DATETIME NULL,
	ModifiedBy					INT NULL
)

GO
