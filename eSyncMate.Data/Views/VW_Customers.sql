CREATE VIEW [dbo].[VW_Customers]
	AS 
	SELECT [Id], [Name], [ERPCustomerID], [ISACustomerID], [ISA810ReceiverId], [ISA856ReceiverId], [Marketplace], [CreatedDate], [CreatedBy],[ModifiedDate],[ModifiedBy] FROM Customers
GO
