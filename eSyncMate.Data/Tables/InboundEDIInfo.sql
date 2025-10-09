CREATE TABLE [dbo].[InboundEDIInfo]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [InboundEDIId] INT NOT NULL, 
    [ISASenderQual] VARCHAR(5) NULL,
    [ISASenderId] VARCHAR(50) NULL,
    [ISAReceiverQual] VARCHAR(5) NULL,
    [ISAReceiverId] VARCHAR(50) NULL,
    [ISAEdiVersion] VARCHAR(15) NULL,
    [GSEdiVersion] VARCHAR(15) NULL,
    [ISAUsageIndicator] VARCHAR(1) NULL,
    [ISAControlNumber] VARCHAR(25) NULL,
    [GSSenderId] VARCHAR(50) NULL,
    [GSReceiverId] VARCHAR(50) NULL,
    [GSControlNumber] VARCHAR(25) NULL,
    [SegmentSeparator] VARCHAR(5) NULL,
    [ElementSeparator] VARCHAR(5) NULL,
    [CreatedDate] datetime NOT NULL,
    [CreatedBy] int NOT NULL
)
