ALTER VIEW [dbo].[VW_Users]
	AS SELECT [Id], [FirstName], [LastName], [Email], [Mobile], [Password], [Status], [CreatedDate], [CreatedBy], [UserType],[Company], [CustomerName], [IsSetupAllowed], [USERID]
	FROM Users WITH (NOLOCK)
GO