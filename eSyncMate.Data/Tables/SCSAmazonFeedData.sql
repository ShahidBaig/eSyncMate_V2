CREATE TABLE SCSAmazonFeedData
(
    ID                BIGINT IDENTITY(1,1) PRIMARY KEY,
    BatchID           NVARCHAR(1000) NULL,
    ItemID            VARCHAR(250) NULL,
    CustomerID        VARCHAR(250) NULL,
    MessageID         BIGINT NULL,
    FeedDocumentID    NVARCHAR(1000) NULL,
	Data			  NVARCHAR(MAX),
	CreatedDate       DATETIME NOT NULL 
        CONSTRAINT DF_SCSAmazonFeedData_CreatedDate DEFAULT (GETDATE())
);



GO





ALTER TABLE InventoryBatchWiseFeedDetail
ADD Data NVARCHAR(MAX) NULL
GO