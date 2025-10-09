IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CustomersLogs')
BEGIN
    CREATE TABLE [dbo].[CustomersLogs](
        [CLId] INT IDENTITY(1,1) PRIMARY KEY,
		[ActionType] VARCHAR(10),
        [Id] INT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [ERPCustomerID] NVARCHAR(250) NULL,
        [ISACustomerID] NVARCHAR(250) NULL,
        [ISA810ReceiverId] NVARCHAR(250) NULL,
        [Marketplace] NVARCHAR(250) NULL,
        [CreatedDate] DATETIME NOT NULL,
        [CreatedBy] INT NOT NULL,
        [ISA856ReceiverId] NVARCHAR(500) NULL,
        [ModifiedDate] DATETIME NULL,
        [ModifiedBy] INT NULL
    )
END
GO