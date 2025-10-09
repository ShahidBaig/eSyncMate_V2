CREATE VIEW [dbo].[VW_Maps]
AS
SELECT M.[Id], M.[Name], M.[TypeId], M.[Map], M.[CreatedDate], M.[CreatedBy],M.[ModifiedDate],M.[ModifiedBy],
	   MT.[Name] AS MapType
FROM Maps M WITH (NOLOCK)
	INNER JOIN MapTypes MT WITH (NOLOCK) ON M.TypeId = MT.Id
GO