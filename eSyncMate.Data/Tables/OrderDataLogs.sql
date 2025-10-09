CREATE TABLE [dbo].[OrderDataLogs]
(
	Id INT IDENTITY (1,1),
	OrderId INT,
	Type VARCHAR(25),
	OrderNumber VARCHAR(500),
	Data NVARCHAR(MAX),
	Status VARCHAR(25),
	CreatedDate DATETIME,
	ModifiedDate DATETIME DEFAULT GETUTCDATE()
)
