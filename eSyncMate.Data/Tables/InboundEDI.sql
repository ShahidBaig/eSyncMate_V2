CREATE TABLE [dbo].[InboundEDI]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Type] VARCHAR(25) NOT NULL DEFAULT('850'), 
    [Status] VARCHAR(25) NOT NULL, 
    [Data] NVARCHAR(MAX) NOT NULL, 
    [CreatedDate] DATETIME NOT NULL, 
    [CreatedBy] INT NOT NULL
)
