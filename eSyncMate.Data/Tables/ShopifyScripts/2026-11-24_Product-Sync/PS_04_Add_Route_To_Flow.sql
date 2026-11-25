/*==============================================================================
  PS 04 - Shopify (Task 00006), customer WKE1000: put the route "Shopify - Sync Products" into the Flow
          "Shopify - Inventory Sync", so it has a "Run Now" (Test Run) button on the Flows screen

  Same way as script 19 added the other routes: one FlowDetails row, In-Active, with the schedule the route
  has in dbo.Routes. The Flow, its Status and its other routes are not touched. No Hangfire job is created.

  @Apply = 0 (default) DRY RUN. Safe to run again: a route that is in a Flow already is left alone.
  Undo: PS_99_Revert_Sync_Products.sql removes this row together with the route.

  Stops (nothing changed):
    51940 the route of PS_02 is missing      51941 the Flow is missing
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID NVARCHAR(50)  = 'WKE1000';
DECLARE @Apply         BIT           = 0;   -- 0 = dry run, 1 = change
DECLARE @RouteName     NVARCHAR(200) = 'Shopify - Sync Products';
DECLARE @FlowTitle     NVARCHAR(200) = 'Shopify - Inventory Sync';

-- the Flow screen wants an end date
DECLARE @DefaultEndDate DATETIME = DATEADD(YEAR, 5, CAST(GETDATE() AS DATE));

BEGIN TRY
    DECLARE @RouteId INT = (SELECT Id FROM dbo.Routes WHERE Name = @RouteName AND CustomerName = @ErpCustomerID AND TypeId = 81);
    DECLARE @FlowId  INT = (SELECT TOP 1 Id FROM dbo.Flows
                             WHERE CustomerID = @ErpCustomerID AND Title = @FlowTitle AND ISNULL(Status, '') <> 'DELETED' ORDER BY Id);

    IF @RouteId IS NULL THROW 51940, 'Route "Shopify - Sync Products" (type 81) not found. Run PS_02 with @Apply = 1 first.', 1;
    IF @FlowId IS NULL  THROW 51941, 'Flow "Shopify - Inventory Sync" not found for the customer.', 1;

    DECLARE @Already INT = (SELECT COUNT(*) FROM dbo.FlowDetails WHERE RouteId = @RouteId AND ISNULL(Status, '') <> 'DELETED');

    SELECT 'Plan' AS Grid, @FlowId AS FlowId, @FlowTitle AS Flow, @RouteId AS RouteId, @RouteName AS Route,
           CASE WHEN @Already > 0 THEN 'already in a Flow - nothing to do' ELSE 'INSERT one FlowDetails row, In-Active' END AS [Action];

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to add the route to the Flow.';
        RETURN;
    END

    IF @Already = 0
    BEGIN
        INSERT INTO dbo.FlowDetails
            (FlowId, RouteId, Status, In_Out, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, CreatedDate, CreatedBy)
        SELECT @FlowId, e.Id, 'In-Active', '', e.FrequencyType,
               ISNULL(e.StartDate, CAST(GETDATE() AS DATE)), ISNULL(e.EndDate, @DefaultEndDate),
               ISNULL(e.RepeatCount, 0), ISNULL(e.WeekDays, ''), ISNULL(e.OnDay, ''), ISNULL(e.ExecutionTime, ''), GETDATE(), 1
          FROM dbo.Routes e
         WHERE e.Id = @RouteId;

        PRINT 'Route added to the Flow.';
    END

    -- what the Flows screen shows for this Flow
    SELECT 'Result' AS Grid, fl.Id AS FlowId, fl.Title, fl.Status AS FlowStatus, d.Id AS DetailId, d.RouteId, e.Name AS RouteName,
           d.Status AS DetailStatus, e.Status AS RouteStatus, e.JobID, d.FrequencyType, d.RepeatCount, d.StartDate, d.EndDate
      FROM dbo.Flows fl
      JOIN dbo.FlowDetails d ON d.FlowId = fl.Id AND ISNULL(d.Status, '') <> 'DELETED'
      JOIN dbo.Routes e      ON e.Id = d.RouteId
     WHERE fl.Id = @FlowId
     ORDER BY d.Id;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
