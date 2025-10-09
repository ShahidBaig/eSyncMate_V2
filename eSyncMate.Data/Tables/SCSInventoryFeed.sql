CREATE TABLE SCSInventoryFeed (
    CustomerID			 VARCHAR(50) NOT NULL,
	ItemId				 VARCHAR(50) NOT NULL,
	CustomerItemCode	 VARCHAR(50) NOT NULL,
	ETA_Date			 NVARCHAR(30)  NULL,	
	ETA_Qty				 INT  NULL,
	Total_ATS			 INT  NULL,
	ATS_L10			     INT  NULL,
	ATS_L21			     INT  NULL,
	ATS_L28			     INT  NULL,
	ATS_L30			     INT  NULL,
	ATS_L34			     INT  NULL,
	ATS_L35			     INT  NULL,
	ATS_L36			     INT  NULL,
	ATS_L37			     INT  NULL,
	ATS_L40			     INT  NULL,
	ATS_L41			     INT  NULL,
	ATS_L55			     INT  NULL,
	ATS_L60			     INT  NULL,
	ATS_L70			     INT  NULL,
	ATS_L91			     INT  NULL,
	[Status]			VARCHAR(25) NULL,
	[CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
	[CreatedBy] INT NOT NULL,
	[ModifiedDate] DATETIME  NULL DEFAULT GETDATE(), 
	[ModifiedBy] INT NULL
    CONSTRAINT PK_SCSInventoryFeed PRIMARY KEY (CustomerID,ItemId,CustomerItemCode)
);

GO



	