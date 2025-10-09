CREATE TABLE [dbo].[CarrierLoadTenderData]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [CarrierLoadTenderId] INT NOT NULL, 
    [Type] VARCHAR(25) NOT NULL, 
	[Data] NVARCHAR(MAX) NOT NULL, 
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(), 
    [CreatedBy] INT NOT NULL
)