CREATE TABLE InventoryBatchWise
(
	BatchID			NVARCHAR(500) NOT NULL  PRIMARY KEY ,
	ItemCount		INT NULL,
	StartDate		DATETIME NULL,
	FinishDate	    DATETIME NULL,
	Status			VARCHAR(100) NULL,
	PageCount		INT NULL,
	RouteType		VARCHAR(500) NULL
)