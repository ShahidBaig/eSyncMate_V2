CREATE TABLE [OrderDetail] 
(
	Id							INT  NOT NULL,
	OrderId					    INT NOT NULL,
	[LineNo]					INT NOT NULL,
	UnitPrice					REAL,
	LineQty						INT NOT NULL,
	ASNQty						INT  NULL,
	CancelQty					INT  NULL,
	CreatedDate					DATETIME NOT NULL,
	CreatedBy					INT NOT NULL,
	ModifiedDate				DATETIME NULL,
	ModifiedBy					INT NULL,
	Status						VARCHAR(50) ,
    ItemID						VARCHAR(250) NULL,
	PRIMARY KEY (Id, OrderId)
)

GO


