CREATE TABLE [InventoryBatchWiseFeedDetail] 
(
    ID				INT IDENTITY(1,1) PRIMARY KEY,
	BatchID			VARCHAR(500) NULL,
    Status			VARCHAR(250) NULL,
    FeedDocumentID  VARCHAR(250) NULL,
    CustomerID      VARCHAR(250) NULL,
	CreatedDate		DATETIME NULL
);

GO