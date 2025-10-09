CREATE VIEW [dbo].[VW_RouteTypes]
AS
SELECT  Id,Name,Description,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy
FROM RouteTypes  WITH (NOLOCK)
	
GO


