CREATE TABLE [Routes] 
(
	Id							INT  PRIMARY KEY NOT NULL,
	Name						NVARCHAR(100) NOT NULL,
	TypeId						INT NOT NULL,
	Status						NVARCHAR(50) NOT NULL,
	SourcePartyId				INT NOT NULL,
	DestinationPartyId			INT NOT NULL,
	SourceConnectorId			INT NOT NULL,
	DestinationConnectorId		INT NOT NULL,
	MapId						INT  NULL,
	PartyGroupId				INT NOT NULL,
	FrequencyType	     VARCHAR(100) NULL,
	StartDate			 DATETIME NULL,
	EndDate				 DATETIME NULL,
	RepeatCount			 INT NULL,
	WeekDays			 VARCHAR(250) NULL,
	OnDay				 VARCHAR(200) NULL,
	ExecutionTime		 VARCHAR(200) NULL,
	JobID				 VARCHAR(50) NULL,
	RouteGroup			 VARCHAR(250)NULL,
	CreatedDate			 DATETIME NOT NULL,
	CreatedBy			 INT NOT NULL,
	ModifiedDate		 DATETIME NULL,
	ModifiedBy			 INT NULL,
	CustomerName			VARCHAR(50) NULL
)

GO
