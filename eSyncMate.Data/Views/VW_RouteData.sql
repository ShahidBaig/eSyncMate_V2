ALTER VIEW [dbo].[VW_RouteData] AS
SELECT RD.[Id], [RouteId], [Type], [OrderId], RD.[CreatedDate], RD.[CreatedBy], RD.[ModifiedDate], RD.[ModifiedBy],
	[Type] + '-' + REPLACE(REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, RD.[CreatedDate], 127), '-', ''), ':', ''), ' ', ''), '.', '') + 
	CASE WHEN [Type] LIKE '%EDI%' THEN '.edi' WHEN [Type] LIKE '%JSON%' OR [Type] LIKE '%RESPONSE%' OR [Type] LIKE '%-NS%' OR [Type] LIKE '%Fields%' THEN '.json' ELSE '.txt' END [FileName]
	,R.[Name],R.[Status]
FROM [dbo].[RouteData]  RD WITH (NOLOCK)
INNER JOIN [dbo].[Routes] R WITH (NOLOCK) ON RD.RouteId = R.Id

GO
