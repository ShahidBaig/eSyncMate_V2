CREATE VIEW [dbo].[VW_RouteLog]
AS
SELECT RL.[Id], [RouteId],[Type], [Message], [Details], RL.[CreatedDate], RL.[CreatedBy], RL.[ModifiedDate], RL.[ModifiedBy],R.Status,R.Name,
CASE WHEN [Type] = 1 THEN 'Info'
	 WHEN [Type] = 2 THEN 'Warning'
	 WHEN [Type] = 3 THEN 'Exception'
	 WHEN [Type] = 4 THEN 'Debug'
	 WHEN [Type] = 5 THEN 'Error'
	 END
TypeName

FROM [dbo].[RouteLog] RL WITH (NOLOCK)
INNER JOIN Routes R ON RL.RouteId = R.Id

GO


