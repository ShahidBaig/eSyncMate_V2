ALTER VIEW [dbo].[VW_Users]
  AS
  SELECT
      U.[Id], U.[FirstName], U.[LastName], U.[Email], U.[Mobile], U.[Password],
      U.[Status], U.[CreatedDate], U.[CreatedBy], U.[UserType], U.[Company],
      U.[CustomerName], U.[IsSetupAllowed], U.[USERID],
      U.[MFAEnabled], U.[MFASecret],
      ISNULL(R.[Name], '') AS RoleName
  FROM Users U WITH (NOLOCK)
  LEFT JOIN UserRoles UR WITH (NOLOCK) ON U.Id = UR.UserId
  LEFT JOIN Roles R WITH (NOLOCK) ON UR.RoleId = R.Id
  WHERE U.[Status] != 'DELETED'
GO
