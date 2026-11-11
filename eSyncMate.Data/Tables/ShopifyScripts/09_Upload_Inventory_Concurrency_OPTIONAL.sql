/*==============================================================================
  09 - Shopify (Task 00006): ShopifyUploadConcurrency

  inventorySetQuantities calls the upload route keeps in flight at the same time.
  Optional: without the row the code uses 3. One call is ~10 cost points; the Basic plan
  restores 100 a second, so 3-4 never runs the bucket dry. Higher plans (Plus) can take more.
  Read by RouteWorker on every run (no restart needed).
  Run it as often as needed: missing -> inserted, existing -> set to @Concurrency.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @Concurrency VARCHAR(10) = '3';

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = 'ShopifyUploadConcurrency')
    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES ('ShopifyUploadConcurrency', @Concurrency, GETDATE(), 1);
ELSE
    UPDATE dbo.ApplicationSettings SET TagValue = @Concurrency WHERE TagName = 'ShopifyUploadConcurrency';

SELECT TagName, TagValue FROM dbo.ApplicationSettings WHERE TagName = 'ShopifyUploadConcurrency';
GO
