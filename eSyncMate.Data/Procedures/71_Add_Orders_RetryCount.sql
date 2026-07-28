/* ------------------------------------------------------------------
   71 — Orders.RetryCount for the ErrorOrderRetry sweep

   Counts how many times the auto-retry route has re-sent an error order to SPARS.
   The sweep only picks orders with RetryCount < 3; on each attempt the route does
   UPDATE Orders SET RetryCount = ISNULL(RetryCount,0)+1. After 3 attempts the order
   is left as ERROR for manual review.

   VW_Orders is SELECT *-style, so it must be refreshed after the ALTER (eSyncMate gotcha).
   Re-runnable: guarded.
   ------------------------------------------------------------------ */

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Orders') AND name = 'RetryCount')
BEGIN
    ALTER TABLE dbo.Orders ADD RetryCount INT NOT NULL CONSTRAINT DF_Orders_RetryCount DEFAULT (0);
END
GO

EXEC sp_refreshview 'dbo.VW_Orders';
GO

-- Verify
SELECT name, system_type_id, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.Orders') AND name = 'RetryCount';
GO
