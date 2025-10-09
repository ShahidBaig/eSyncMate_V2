DROP TABLE SCS_ItemData
GO

CREATE TABLE SCS_ItemData 
(
	ID							INT IDENTITY(1,1) PRIMARY KEY,
	PrepareItemData_ID			INT NULL,
	PortalID					NVARCHAR(500)NULL,
	external_id					VARCHAR(500) NULL,
	relationship_type			NVARCHAR(500) NULL,
	parent_id			        VARCHAR(500) NULL,
	seller_id			        VARCHAR(500) NULL,
	quantity			        INT NULL,
	distribution_center_id		VARCHAR(500) NULL,
	list_price			        VARCHAR(500) NULL,
	offer_price			        VARCHAR(500) NULL,
	tcin			            VARCHAR(500) NULL,
	item_type_id			    VARCHAR(500) NULL,
	
)
GO


