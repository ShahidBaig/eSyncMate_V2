CREATE TABLE [dbo].[SCSInventoryFeedData_TAR6266P]

(
    [Id]          INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CustomerId]  VARCHAR(50)   NULL,
    [ItemId]      VARCHAR(50)   NULL,
    [Type]        VARCHAR(25)   NULL,
    [Data]        NVARCHAR(MAX) NULL,
    [BatchID]     NVARCHAR(500) NULL,
    [CreatedDate] DATETIME      NOT NULL DEFAULT GETDATE(),
    [CreatedBy]   INT           NOT NULL DEFAULT 1
)