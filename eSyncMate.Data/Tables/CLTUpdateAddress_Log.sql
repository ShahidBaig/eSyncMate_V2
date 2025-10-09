CREATE TABLE [dbo].[CLTUpdateAddress_Log]
(
	ID						  INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[ShipmentId]			  [VARCHAR](250) NULL,
	[ShipperNo]				  [VARCHAR](250) NULL,
	[TrackStatus]			  [VARCHAR](250) NULL,
	[ShipFromAddress]		  [VARCHAR](250) NULL,
	[ShipFromCity]			  [VARCHAR](250) NULL,
	[ShipFromState]		      [VARCHAR](250) NULL,
	[ShipFromZip]             [VARCHAR](250) NULL,
	[ShipFromCountry]         [VARCHAR](250) NULL,
	LogDate					  DATETIME
	
)

GO

