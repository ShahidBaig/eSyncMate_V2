/*==============================================================================
  PS 05 - Shopify (Task 00006), customer WKE1000: fix the schedule of "Shopify - Sync Products"

  Why: PS_02 stored the schedule "Minutely, every 60". The scheduler builds a cron text with a minute step
  of 60 from that, which is not valid (minutes go up to 59), so activating the route on the Flows screen
  failed with an error in RecurringJobManager.ValidateCronExpression.

  Fix: the schedule becomes "Hourly, every 6" on the route and on its Flow row.
  Only the schedule of this one route is changed. Status and JobID are not changed.

  The first grid ("State") is always shown and is READ-ONLY: it also shows whether the failed save left any
  route of the Flow Active.

  @Apply = 0 (default) DRY RUN.
  Stops (nothing changed): 51945 the route is Active / has a job - set it In-Active on the Flows screen first.

  (No cron text is written in this header on purpose: a star followed by a slash ends a SQL comment.)
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID NVARCHAR(50)  = 'WKE1000';
DECLARE @Apply         BIT           = 0;   -- 0 = dry run, 1 = change
DECLARE @RouteName     NVARCHAR(200) = 'Shopify - Sync Products';
DECLARE @EveryHours    INT           = 6;   -- 1 - 23

BEGIN TRY
    DECLARE @RouteId INT = (SELECT Id FROM dbo.Routes WHERE Name = @RouteName AND CustomerName = @ErpCustomerID AND TypeId = 81);

    -- every route of the customer's inventory Flow: route and Flow row side by side
    SELECT 'State' AS Grid, fl.Title AS Flow, fl.Status AS FlowStatus, e.Id AS RouteId, e.Name AS Route,
           e.Status AS RouteStatus, e.JobID, e.FrequencyType AS RouteFrequency, e.RepeatCount AS RouteRepeat,
           d.Status AS FlowRowStatus, d.FrequencyType AS FlowRowFrequency, d.RepeatCount AS FlowRowRepeat,
           CASE WHEN e.Status = 'Active' OR e.JobID IS NOT NULL OR d.Status = 'Active' THEN 'CHECK - Active or has a job' ELSE 'In-Active' END AS [Check]
      FROM dbo.Flows fl
      JOIN dbo.FlowDetails d ON d.FlowId = fl.Id AND ISNULL(d.Status, '') <> 'DELETED'
      JOIN dbo.Routes e      ON e.Id = d.RouteId
     WHERE fl.CustomerID = @ErpCustomerID AND ISNULL(fl.Status, '') <> 'DELETED'
     ORDER BY fl.Id, d.Id;

    IF @RouteId IS NULL
        THROW 51944, 'Route "Shopify - Sync Products" (type 81) not found.', 1;

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to change the schedule.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Id = @RouteId AND (Status = 'Active' OR JobID IS NOT NULL))
       OR EXISTS (SELECT 1 FROM dbo.FlowDetails WHERE RouteId = @RouteId AND Status = 'Active')
        THROW 51945, 'The route is Active or has a job. Set it In-Active on the Flows screen first, then run again.', 1;

    BEGIN TRAN;

    UPDATE dbo.Routes
       SET FrequencyType = 'Hourly', RepeatCount = @EveryHours, ModifiedDate = GETDATE(), ModifiedBy = 1
     WHERE Id = @RouteId;

    UPDATE dbo.FlowDetails
       SET FrequencyType = 'Hourly', RepeatCount = @EveryHours, ModifiedDate = GETDATE(), ModifiedBy = 1
     WHERE RouteId = @RouteId AND ISNULL(Status, '') <> 'DELETED';

    COMMIT TRAN;

    SELECT 'Result' AS Grid, e.Id AS RouteId, e.Name, e.Status, e.JobID, e.FrequencyType, e.RepeatCount,
           d.Status AS FlowRowStatus, d.FrequencyType AS FlowRowFrequency, d.RepeatCount AS FlowRowRepeat
      FROM dbo.Routes e
      LEFT JOIN dbo.FlowDetails d ON d.RouteId = e.Id AND ISNULL(d.Status, '') <> 'DELETED'
     WHERE e.Id = @RouteId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
