/*==============================================================================
  19 - Shopify (Task 00006): Flows (Setup > Flows), like every other customer

  Groups the customer's routes into Flows, so they show on the Flows screen (Route Details, Flow Diagram,
  Hub & Spoke) and can be activated / Test Run from there:

    <P> - Inventory Sync             Fetch Full Inventory from ERP, Fetch Differential Inventory from ERP,
                                     Upload Warehouse-Wise Inventory
    <P> - Order Processing           Get Orders, Place Orders in ERP
    <P> - Shipment & ASN             Fetch ASN from ERP, Send ASN Shipment Notification
    <P> - Cancellation Processing    Fetch Cancellations from ERP, Send Cancellation to Portal,
                                     Get Cancelled Orders, Send Cancellations to ERP

  Everything is created In-Active (Flow and every route in it), with the schedule of the route copied from
  dbo.Routes. Activate from the UI (Setup > Flows): that is what schedules the Hangfire jobs. Never activate
  with an UPDATE - that leaves JobID NULL.

  Run it LAST (after 04, 05, 07, 10 - 15, 17, 18): a route that does not exist yet is skipped and listed, and
  is added when the script is run again. Run it as often as needed:
    Flow missing           -> inserted In-Active
    Flow there             -> Description updated; its Status is left alone (the UI owns it)
    route not in a Flow    -> added In-Active to its Flow
    route already in a Flow (this one or another, not DELETED) -> left alone, with its Status and schedule
  No secret here.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID NVARCHAR(50)  = 'WKE1000';   -- same as script 02
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';    -- same as script 03 (route names start with this)

-- A route without an End Date in dbo.Routes gets this one in its Flow (the Flow screen wants an end date).
DECLARE @DefaultEndDate DATETIME = DATEADD(YEAR, 5, CAST(GETDATE() AS DATE));
/*==============================================================================*/

BEGIN TRY
    IF OBJECT_ID('dbo.Flows', 'U') IS NULL OR OBJECT_ID('dbo.FlowDetails', 'U') IS NULL
        THROW 51950, 'Tables Flows / FlowDetails are missing on this server (the Flows feature is not deployed here).', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID)
        THROW 51951, 'Customer missing. Run script 02 first.', 1;

    /*--- The Flows and their routes ---------------------------------------------*/
    DECLARE @f TABLE (Seq INT, Title NVARCHAR(1000), Description NVARCHAR(MAX));

    INSERT INTO @f VALUES
      (1, @Prefix + N' - Inventory Sync',
          N'Manages inventory synchronization for the ' + @Prefix + N' store. Includes full inventory fetch from ERP, differential inventory updates, and warehouse-wise inventory upload to the Shopify locations.'),
      (2, @Prefix + N' - Order Processing',
          N'Handles the order lifecycle for the ' + @Prefix + N' store. Retrieves new customer orders from Shopify and places them into the ERP system for fulfillment.'),
      (3, @Prefix + N' - Shipment & ASN',
          N'Manages shipment notifications for the ' + @Prefix + N' store. Fetches ASN data from the ERP system and creates the Shopify fulfillments with the tracking numbers.'),
      (4, @Prefix + N' - Cancellation Processing',
          N'Handles order cancellations for the ' + @Prefix + N' store in both directions: cancellations from the ERP are sent to Shopify, and orders cancelled in Shopify are cancelled in the ERP.');

    DECLARE @r TABLE (FlowSeq INT, Seq INT, RouteName NVARCHAR(200));

    INSERT INTO @r VALUES
      (1, 1, @Prefix + N' - Fetch Full Inventory from ERP'),            -- script 04
      (1, 2, @Prefix + N' - Fetch Differential Inventory from ERP'),    -- script 05
      (1, 3, @Prefix + N' - Upload Warehouse-Wise Inventory'),          -- script 07
      (2, 1, @Prefix + N' - Get Orders'),                               -- script 10
      (2, 2, @Prefix + N' - Place Orders in ERP'),                      -- script 11
      (3, 1, @Prefix + N' - Fetch ASN from ERP'),                       -- script 12
      (3, 2, @Prefix + N' - Send ASN Shipment Notification'),           -- script 13
      (4, 1, @Prefix + N' - Fetch Cancellations from ERP'),             -- script 14
      (4, 2, @Prefix + N' - Send Cancellation to Portal'),              -- script 15
      (4, 3, @Prefix + N' - Get Cancelled Orders'),                     -- script 17
      (4, 4, @Prefix + N' - Send Cancellations to ERP');                -- script 18

    -- only this customer's routes: the same name under another customer is not touched
    DECLARE @x TABLE (FlowSeq INT, Seq INT, RouteName NVARCHAR(200), RouteId INT);

    INSERT INTO @x
    SELECT r.FlowSeq, r.Seq, r.RouteName, e.Id
      FROM @r r
      LEFT JOIN dbo.Routes e ON e.Name = r.RouteName AND e.CustomerName = @ErpCustomerID;

    IF NOT EXISTS (SELECT 1 FROM @x WHERE RouteId IS NOT NULL)
        THROW 51952, 'None of the customer''s routes exist yet. Run scripts 04 - 18 first (same @ErpCustomerID and @Prefix).', 1;

    BEGIN TRAN;

    /*--- 1. Flows (Flows.Id IS identity) ------------------------------------------*/
    -- a Flow is only made when at least one of its routes exists
    INSERT INTO dbo.Flows (CustomerID, Title, Description, Status, CreatedDate, CreatedBy, SequenceNo)
    SELECT @ErpCustomerID, f.Title, f.Description, 'In-Active', GETDATE(), 1, 0
      FROM @f f
     WHERE EXISTS (SELECT 1 FROM @x x WHERE x.FlowSeq = f.Seq AND x.RouteId IS NOT NULL)
       AND NOT EXISTS (SELECT 1 FROM dbo.Flows e WHERE e.CustomerID = @ErpCustomerID AND e.Title = f.Title AND ISNULL(e.Status, '') <> 'DELETED')
     ORDER BY f.Seq;

    PRINT 'Flows inserted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    UPDATE e
       SET e.Description = f.Description, e.ModifiedDate = GETDATE(), e.ModifiedBy = 1
      FROM dbo.Flows e
      JOIN @f f ON f.Title = e.Title
     WHERE e.CustomerID = @ErpCustomerID AND ISNULL(e.Status, '') <> 'DELETED' AND ISNULL(e.Description, '') <> f.Description;

    PRINT 'Flows updated (Description only; Status left as it is): ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    /*--- 2. Flow details: a route goes in once, with the schedule it has in dbo.Routes --*/
    INSERT INTO dbo.FlowDetails
        (FlowId, RouteId, Status, In_Out, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, CreatedDate, CreatedBy)
    SELECT fl.Id, x.RouteId, 'In-Active', '', e.FrequencyType,
           ISNULL(e.StartDate, CAST(GETDATE() AS DATE)), ISNULL(e.EndDate, @DefaultEndDate),
           ISNULL(e.RepeatCount, 0), ISNULL(e.WeekDays, ''), ISNULL(e.OnDay, ''), ISNULL(e.ExecutionTime, ''), GETDATE(), 1
      FROM @x x
      JOIN @f f        ON f.Seq = x.FlowSeq
      JOIN dbo.Flows fl ON fl.CustomerID = @ErpCustomerID AND fl.Title = f.Title AND ISNULL(fl.Status, '') <> 'DELETED'
      JOIN dbo.Routes e ON e.Id = x.RouteId
     WHERE x.RouteId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.FlowDetails d WHERE d.RouteId = x.RouteId AND ISNULL(d.Status, '') <> 'DELETED')
     ORDER BY x.FlowSeq, x.Seq;

    PRINT 'Routes added to their Flow: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    COMMIT TRAN;

    /*--- Verification -------------------------------------------------------------*/
    -- what the Flows screen will show for the customer
    SELECT fl.Id AS FlowId, fl.Title, fl.Status AS FlowStatus, d.Id AS DetailId, d.RouteId, e.Name AS RouteName, e.TypeId,
           d.Status AS DetailStatus, e.Status AS RouteStatus, d.FrequencyType, d.RepeatCount, d.ExecutionTime, d.StartDate, d.EndDate
      FROM dbo.Flows fl
      LEFT JOIN dbo.FlowDetails d ON d.FlowId = fl.Id AND ISNULL(d.Status, '') <> 'DELETED'
      LEFT JOIN dbo.Routes e      ON e.Id = d.RouteId
     WHERE fl.CustomerID = @ErpCustomerID AND ISNULL(fl.Status, '') <> 'DELETED'
     ORDER BY fl.Id, d.Id;

    -- NotCreatedYet: run the script that makes the route, then this script again
    -- InAnotherFlow: the route was put in a Flow by hand; left where it is
    SELECT x.RouteName,
           CASE WHEN x.RouteId IS NULL THEN 'NotCreatedYet - run its script, then 19 again'
                ELSE 'InAnotherFlow: ' + ISNULL((SELECT TOP 1 fl.Title FROM dbo.FlowDetails d JOIN dbo.Flows fl ON fl.Id = d.FlowId
                                                  WHERE d.RouteId = x.RouteId AND ISNULL(d.Status, '') <> 'DELETED'), '') END AS Note
      FROM @x x
      JOIN @f f ON f.Seq = x.FlowSeq
     WHERE x.RouteId IS NULL
        OR NOT EXISTS (SELECT 1 FROM dbo.FlowDetails d JOIN dbo.Flows fl ON fl.Id = d.FlowId
                        WHERE d.RouteId = x.RouteId AND ISNULL(d.Status, '') <> 'DELETED'
                          AND fl.CustomerID = @ErpCustomerID AND fl.Title = f.Title)
     ORDER BY x.FlowSeq, x.Seq;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  1. Setup > Flows: choose the customer - the four Flows are listed, all In-Active.
  2. View a Flow > Route Details: every route has a Test Run button. A route that is In-Active does nothing on a
     Test Run (the notification still says "completed"): activate the route first.
  3. Activate from the Flows screen (the whole Flow on the General tab, or one route on its row): that sets the
     route Active and schedules its Hangfire job. Check the Start / End dates of each route there before activating.
==============================================================================*/
