CREATE TABLE [RouteLog] 
(
	Id							INT  PRIMARY KEY NOT NULL,
	RouteId						INT NOT NULL,
	Type						INT NOT NULL,
	Message						NVARCHAR(250) NOT NULL ,
	Details						NVARCHAR(250)NOT NULL,
	CreatedDate					DATETIME NOT NULL,
	CreatedBy					INT NOT NULL,
	ModifiedDate				DATETIME NULL,
	ModifiedBy					INT NULL
)

GO
