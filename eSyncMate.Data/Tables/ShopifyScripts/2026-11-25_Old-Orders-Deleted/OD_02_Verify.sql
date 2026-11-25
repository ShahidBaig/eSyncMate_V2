/*==============================================================================
  OD 02 - Shopify (Task 00006), customer WKE1000: verify the older orders marked DELETED (READ ONLY)
==============================================================================*/

SET NOCOUNT ON;

DECLARE @CustomerId INT      = 15;
DECLARE @From       DATETIME = '2026-11-25 12:40';

SELECT 'Orders since 12:40' AS Grid, O.Status, COUNT(*) AS Orders
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId AND O.CreatedDate >= @From
 GROUP BY O.Status;

SELECT 'Backup' AS Grid,
       CASE WHEN OBJECT_ID('dbo.ZDB_OD01_Orders', 'U') IS NOT NULL THEN 'dbo.ZDB_OD01_Orders exists (OD_99 can revert)'
            ELSE 'no backup table' END AS [State];

SELECT 'Kept orders' AS Grid, O.Id, O.OrderNumber AS [Shopify PO], O.Status, O.CreatedDate
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId AND O.OrderNumber IN ('175273', '175274');
GO
