/* ------------------------------------------------------------------
   72 — SP_GetErrorOrdersForRetry: feeder for the ErrorOrderRetry route

   Returns the error orders eligible for an automatic retry. One route covers every
   customer; the per-order ERPCustomerID is what ErrorOrderRetryRoute passes to
   SCSPlaceOrderRoute.ExecuteSingle (the same single-order reprocess as the button).

   Eligibility:
     - Order failed to place  -> Status = 'ERROR'
     - Never created in SPARS  -> ExternalId is empty (a created order has an SO#)
     - Under the retry cap     -> RetryCount < 3
     - Failure was retryable   -> latest ERP-ERROR payload CONTAINS Period / Timeout /
                                  "No Response" (only the SCS place-order flow writes ERP-ERROR,
                                  so this is naturally scoped to SPARS orders).

   Requires script 71 (Orders.RetryCount). Re-runnable via CREATE OR ALTER.
   ------------------------------------------------------------------ */

CREATE OR ALTER PROCEDURE dbo.SP_GetErrorOrdersForRetry
AS
BEGIN
    SET NOCOUNT ON;

    SELECT O.Id            AS OrderId,
           C.ERPCustomerID AS ERPCustomerID,
           O.OrderNumber   AS OrderNumber
    FROM dbo.Orders O WITH (NOLOCK)
        INNER JOIN dbo.Customers C WITH (NOLOCK) ON O.CustomerId = C.Id
    WHERE O.Status = 'ERROR'
      AND ISNULL(O.ExternalId, '') = ''
      AND ISNULL(O.RetryCount, 0) < 3
      AND EXISTS (
            SELECT 1
            FROM dbo.OrderData OD WITH (NOLOCK)
            WHERE OD.OrderId = O.Id
              AND OD.Type = 'ERP-ERROR'
              AND ( OD.Data LIKE '%Period%'
                 OR OD.Data LIKE '%Timeout%'
                 OR OD.Data LIKE '%No Response%' )
      )
    ORDER BY O.Id;
END
GO

