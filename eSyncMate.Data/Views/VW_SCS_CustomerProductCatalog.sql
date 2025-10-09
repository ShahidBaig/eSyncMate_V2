ALTER VIEW [dbo].[VW_SCS_CustomerProductCatalog]
AS
	SELECT
	c.ProductId,
	C.ID,
	C.CustomerID,
	C.Brand,
	C.ItemID,
	C.UPC,
	C.ItemTypeName,
	C.ProductRelation,
	C.ParentID,
	C.ListPrice,
	C.MapPrice,
	C.OffPrice,
	C.JsonData,
	ISNULL(C.ModifiedDate,C.CreatedDate) CreatedDate,
	C.CreatedBy,
	C.ModifiedBy,
	CASE 
        WHEN C.SyncStatus = 'DELETED' THEN 'SYNCED'
        ELSE C.SyncStatus
    END AS SyncStatus
	FROM [SCS_CustomerProductCatalog] C WITH (NOLOCK)
	
GO


