CREATE TABLE [Temp_SCSInventoryFeed] 
(
	CustomerID			 VARCHAR(50) NULL,
	ItemId				 VARCHAR(50) NULL,
	CustomerItemCode	 VARCHAR(50) NULL,
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
	CurrentPage          INT NULL,
	TotalPages			 INT NULL,
	CreatedDate			 DATETIME NULL,
	CreatedBy			 INT NULL,
	ModifiedDate		 DATETIME NULL,
	ModifiedBy			 INT NULL
)

GO
