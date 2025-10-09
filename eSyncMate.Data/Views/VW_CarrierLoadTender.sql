CREATE VIEW [dbo].[VW_CarrierLoadTender] AS 
SELECT O.*, C.[Name] CustomerName
FROM CarrierLoadTender O WITH (NOLOCK)
	INNER JOIN Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
GO



