CREATE VIEW [dbo].[VW_SCS_ItemsType]
AS
SELECT  DISTINCT Brand,Item_Type,Item_Type_Id,
CustomerID
FROM [SCS_ItemsType]  WITH (NOLOCK)
	
GO
