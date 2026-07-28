/* ------------------------------------------------------------------
   70 — New RouteType: ErrorOrderRetry (Id 74)

   Scheduled sweep that auto-retries error orders which failed to place in
   SPARS due to Period / Timeout / "No Response from SPARS". For each such
   order it runs the same single-order reprocess (SCSPlaceOrderRoute.ExecuteSingle),
   up to 3 attempts (Orders.RetryCount), then leaves it as ERROR for manual review.

   VW_Routes uses INNER JOINs (incl. RouteTypes), so this row must exist before a
   Route of TypeId 74 will appear/manage in the UI.

   Re-runnable: guarded insert.
   ------------------------------------------------------------------ */

IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 74)
BEGIN
    INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
    VALUES (74, 'ErrorOrderRetry', 'Auto-retry error orders (Period / Timeout / No Response from SPARS)', GETDATE(), 1);
END

-- Verify
SELECT Id, Name, Description FROM dbo.RouteTypes WHERE Id = 74;


