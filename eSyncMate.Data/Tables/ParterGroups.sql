CREATE TABLE [PartnerGroups] 
(
	Id							INT PRIMARY KEY NOT NULL,
	SourcePartyId				INT NOT NULL,
	DestinationPartyId			INT NOT NULL,
	Description					NVARCHAR(250) NOT NULL,
	CreatedDate					DATETIME NOT NULL,
	CreatedBy					INT NOT NULL,
	ModifiedDate				DATETIME NULL,
	ModifiedBy					INT NULL
)

GO