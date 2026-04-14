using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;

namespace RouteTestApp
{
    /// <summary>
    /// Test suite for Flow-based inventory route coordination.
    /// Tests: skip logic, wait logic, Full Feed priority, non-inventory routes unaffected.
    /// </summary>
    public static class FlowInventoryCoordinationTests
    {
        private static string _connectionString = "";
        private static int _passed = 0;
        private static int _failed = 0;

        // Test data IDs — use high numbers to avoid conflicts
        private const int TEST_FLOW_ID = 99901;
        private const int TEST_ROUTE_UPLOAD = 99901;      // Inventory upload route
        private const int TEST_ROUTE_FULL_FEED = 99902;   // Full Feed route
        private const int TEST_ROUTE_DIFF_FEED = 99903;   // Differential Feed route
        private const int TEST_ROUTE_NON_INV = 99904;     // Non-inventory route (Orders)
        private const int TEST_ROUTE_NO_FLOW = 99905;     // Inventory route not in any flow

        public static async Task RunAllTests(string connectionString)
        {
            _connectionString = connectionString;
            _passed = 0;
            _failed = 0;

            PrintHeader("FLOW-BASED INVENTORY COORDINATION — TEST SUITE");
            Console.WriteLine($"  Connection: {MaskConnectionString(connectionString)}");
            Console.WriteLine($"  Timestamp:  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // Setup test data
            if (!SetupTestData())
            {
                PrintError("Failed to setup test data. Ensure Flows, FlowDetails, Routes, RouteExecutionLock tables exist.");
                return;
            }

            try
            {
                // ===== LEVEL 1: HELPER METHODS =====
                PrintSection("LEVEL 1: InventoryRouteHelper");

                Test_IsInventoryRoute();
                Test_IsFullFeedRoute();
                Test_NonInventoryRoute();

                // ===== LEVEL 2: FLOW LOOKUP =====
                PrintSection("LEVEL 2: Flow Lookup (Sp_GetFlowIdForRoute)");

                Test_GetFlowIdForRoute_Found();
                Test_GetFlowIdForRoute_NotFound();

                // ===== LEVEL 3: RUNNING CHECK =====
                PrintSection("LEVEL 3: Running Check (Sp_IsOtherFlowInventoryRouteRunning)");

                Test_NoOthersRunning();
                Test_OtherInventoryRouteRunning();
                Test_SameRouteNotBlocking();
                Test_NonInventoryRouteNotBlocking();
                Test_ExpiredLockNotBlocking();

                // ===== LEVEL 4: COORDINATION LOGIC =====
                PrintSection("LEVEL 4: Coordination Logic Simulation");

                Test_NonFullFeed_Skips_WhenOthersRunning();
                Test_FullFeed_Waits_WhenOthersRunning();
                Test_InventoryRoute_NoFlow_RunsNormally();
                Test_NonInventoryRoute_NoCheck();

                // ===== LEVEL 5: CONCURRENCY =====
                PrintSection("LEVEL 5: Concurrent Scenarios");

                await Test_ConcurrentInventoryRoutes_OnlyOneProceeds();

                // ===== LEVEL 6: UPLOAD ROUTE — DIFFERENTIAL/FULL-FEED COORDINATION =====
                PrintSection("LEVEL 6: Upload Route — Diff/Full-Feed Coordination");

                Test_IsUploadRoute();
                Test_GetDifferentialTypeIds();
                Test_GetFullFeedTypeIds();
                Test_Upload_NothingRunning_Proceeds();
                Test_Upload_DiffRunning_Waits();
                Test_Upload_FullFeedRunning_Skips();
                Test_Upload_OnlyOtherUploadRunning_Skips();
                Test_Upload_DiffAndOtherUpload_WaitsForDiff();
                Test_Upload_DiffFinishes_ThenProceeds();
                Test_FullFeed_StillTopPriority();

                PrintSummary();
            }
            finally
            {
                CleanupTestData();
            }
        }

        // ================================================================
        // LEVEL 1: InventoryRouteHelper
        // ================================================================

        static void Test_IsInventoryRoute()
        {
            // TypeId 7 = SCSFullInventoryFeed, 27 = WalmartUploadInventory, 48 = AmazonInventoryUpload
            AssertTrue(InventoryRouteHelper.IsInventoryRoute(7), "FC-01a: TypeId 7 (Full Feed) is inventory");
            AssertTrue(InventoryRouteHelper.IsInventoryRoute(27), "FC-01b: TypeId 27 (Walmart Upload) is inventory");
            AssertTrue(InventoryRouteHelper.IsInventoryRoute(48), "FC-01c: TypeId 48 (Amazon Upload) is inventory");
            AssertTrue(InventoryRouteHelper.IsInventoryRoute(8), "FC-01d: TypeId 8 (Diff Feed) is inventory");
        }

        static void Test_IsFullFeedRoute()
        {
            AssertTrue(InventoryRouteHelper.IsFullFeedRoute(7), "FC-02a: TypeId 7 is Full Feed");
            AssertFalse(InventoryRouteHelper.IsFullFeedRoute(8), "FC-02b: TypeId 8 is NOT Full Feed");
            AssertFalse(InventoryRouteHelper.IsFullFeedRoute(27), "FC-02c: TypeId 27 is NOT Full Feed");
        }

        static void Test_NonInventoryRoute()
        {
            // TypeId 2 = GetOrders, 5 = ASN, 6 = CreateInvoice
            AssertFalse(InventoryRouteHelper.IsInventoryRoute(2), "FC-03a: TypeId 2 (GetOrders) is NOT inventory");
            AssertFalse(InventoryRouteHelper.IsInventoryRoute(5), "FC-03b: TypeId 5 (ASN) is NOT inventory");
            AssertFalse(InventoryRouteHelper.IsInventoryRoute(6), "FC-03c: TypeId 6 (Invoice) is NOT inventory");
        }

        // ================================================================
        // LEVEL 2: Flow Lookup
        // ================================================================

        static void Test_GetFlowIdForRoute_Found()
        {
            var coord = CreateCoordinator();
            long flowId = coord.GetFlowIdForRoute(TEST_ROUTE_UPLOAD);
            AssertTrue(flowId == TEST_FLOW_ID, $"FC-04: GetFlowIdForRoute({TEST_ROUTE_UPLOAD}) returns FlowId={TEST_FLOW_ID} (got {flowId})");
        }

        static void Test_GetFlowIdForRoute_NotFound()
        {
            var coord = CreateCoordinator();
            long flowId = coord.GetFlowIdForRoute(TEST_ROUTE_NO_FLOW);
            AssertTrue(flowId == 0, $"FC-05: GetFlowIdForRoute({TEST_ROUTE_NO_FLOW}) returns 0 — not in any flow (got {flowId})");
        }

        // ================================================================
        // LEVEL 3: Running Check
        // ================================================================

        static void Test_NoOthersRunning()
        {
            ClearAllTestLocks();
            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool running = coord.IsOtherFlowInventoryRouteRunning(TEST_FLOW_ID, TEST_ROUTE_UPLOAD, typeIds);
            AssertFalse(running, "FC-06: No others running — returns false");
        }

        static void Test_OtherInventoryRouteRunning()
        {
            ClearAllTestLocks();
            // Simulate: Diff Feed is running (has active lock)
            InsertActiveLock("TEST_COORD", 8, TEST_ROUTE_DIFF_FEED);

            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool running = coord.IsOtherFlowInventoryRouteRunning(TEST_FLOW_ID, TEST_ROUTE_UPLOAD, typeIds);
            AssertTrue(running, "FC-07: Diff Feed running in same Flow — returns true");

            ClearAllTestLocks();
        }

        static void Test_SameRouteNotBlocking()
        {
            ClearAllTestLocks();
            // Simulate: Upload route itself has a lock — should NOT block itself
            InsertActiveLock("TEST_COORD", 27, TEST_ROUTE_UPLOAD);

            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool running = coord.IsOtherFlowInventoryRouteRunning(TEST_FLOW_ID, TEST_ROUTE_UPLOAD, typeIds);
            AssertFalse(running, "FC-08: Own lock does NOT block itself (ExcludeRouteId works)");

            ClearAllTestLocks();
        }

        static void Test_NonInventoryRouteNotBlocking()
        {
            ClearAllTestLocks();
            // Simulate: Non-inventory route (Orders, TypeId=2) has a lock in same flow
            InsertActiveLock("TEST_COORD", 2, TEST_ROUTE_NON_INV);

            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool running = coord.IsOtherFlowInventoryRouteRunning(TEST_FLOW_ID, TEST_ROUTE_UPLOAD, typeIds);
            AssertFalse(running, "FC-09: Non-inventory route lock does NOT block inventory routes");

            ClearAllTestLocks();
        }

        static void Test_ExpiredLockNotBlocking()
        {
            ClearAllTestLocks();
            // Simulate: Diff Feed lock that expired (older than 60 min)
            InsertExpiredLock("TEST_COORD", 8, TEST_ROUTE_DIFF_FEED, 90);

            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool running = coord.IsOtherFlowInventoryRouteRunning(TEST_FLOW_ID, TEST_ROUTE_UPLOAD, typeIds);
            AssertFalse(running, "FC-10: Expired lock (90 min old) does NOT block");

            ClearAllTestLocks();
        }

        // ================================================================
        // LEVEL 4: Coordination Logic Simulation
        // ================================================================

        static void Test_NonFullFeed_Skips_WhenOthersRunning()
        {
            ClearAllTestLocks();
            // Diff Feed is running
            InsertActiveLock("TEST_COORD", 8, TEST_ROUTE_DIFF_FEED);

            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            long flowId = coord.GetFlowIdForRoute(TEST_ROUTE_UPLOAD);
            bool othersRunning = coord.IsOtherFlowInventoryRouteRunning(flowId, TEST_ROUTE_UPLOAD, typeIds);

            // Simulate: Upload route (TypeId=27, NOT Full Feed) should SKIP
            bool isFullFeed = InventoryRouteHelper.IsFullFeedRoute(27);
            bool shouldSkip = othersRunning && !isFullFeed;

            AssertTrue(shouldSkip, "FC-11: Non-Full-Feed inventory route SKIPS when others running");

            ClearAllTestLocks();
        }

        static void Test_FullFeed_Waits_WhenOthersRunning()
        {
            ClearAllTestLocks();
            // Upload is running
            InsertActiveLock("TEST_COORD", 27, TEST_ROUTE_UPLOAD);

            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            long flowId = coord.GetFlowIdForRoute(TEST_ROUTE_FULL_FEED);
            bool othersRunning = coord.IsOtherFlowInventoryRouteRunning(flowId, TEST_ROUTE_FULL_FEED, typeIds);

            // Simulate: Full Feed (TypeId=7) should WAIT, not skip
            bool isFullFeed = InventoryRouteHelper.IsFullFeedRoute(7);
            bool shouldWait = othersRunning && isFullFeed;

            AssertTrue(shouldWait, "FC-12a: Full Feed WAITS when others running");

            // Simulate: other route finishes (clear lock)
            ClearAllTestLocks();

            // Re-check — now should proceed
            othersRunning = coord.IsOtherFlowInventoryRouteRunning(flowId, TEST_ROUTE_FULL_FEED, typeIds);
            AssertFalse(othersRunning, "FC-12b: After others finish, Full Feed proceeds");
        }

        static void Test_InventoryRoute_NoFlow_RunsNormally()
        {
            var coord = CreateCoordinator();
            long flowId = coord.GetFlowIdForRoute(TEST_ROUTE_NO_FLOW);

            // Route not in any flow — should run normally (no coordination)
            bool shouldCoordinate = flowId > 0;
            AssertFalse(shouldCoordinate, "FC-13: Inventory route NOT in a flow — no coordination, runs normally");
        }

        static void Test_NonInventoryRoute_NoCheck()
        {
            // TypeId=2 (GetOrders) — not inventory, no check at all
            bool isInventory = InventoryRouteHelper.IsInventoryRoute(2);
            AssertFalse(isInventory, "FC-14: Non-inventory route — no coordination check");
        }

        // ================================================================
        // LEVEL 5: Concurrency
        // ================================================================

        static async Task Test_ConcurrentInventoryRoutes_OnlyOneProceeds()
        {
            ClearAllTestLocks();

            // Simulate: 3 inventory routes in same flow try to start simultaneously
            // Only the first one that acquires its lock should proceed
            // Others should see "others running" and skip

            var lockEntity = new RouteExecutionLock();
            lockEntity.UseConnection(_connectionString);

            // Route 1 acquires lock first
            string token1 = lockEntity.AcquireLock("TEST_COORD", 27, TEST_ROUTE_UPLOAD);
            AssertNotNull(token1, "FC-15a: First route acquires lock");

            // Route 2 tries — its own lock succeeds (different RouteTypeId)
            string token2 = lockEntity.AcquireLock("TEST_COORD", 8, TEST_ROUTE_DIFF_FEED);
            // But flow check should block it
            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool othersRunning = coord.IsOtherFlowInventoryRouteRunning(TEST_FLOW_ID, TEST_ROUTE_DIFF_FEED, typeIds);
            AssertTrue(othersRunning, "FC-15b: Second route sees first route running in Flow — blocked");

            // Cleanup
            if (token1 != null) lockEntity.ReleaseLock(token1);
            if (token2 != null) lockEntity.ReleaseLock(token2);
            ClearAllTestLocks();
        }

        // ================================================================
        // LEVEL 6: Upload Route — Differential / Full-Feed Coordination
        // ================================================================
        // Logic under test (RouteEngine.cs):
        //   if (FullFeed)              → wait for everyone (top priority)
        //   else if (Upload) {
        //        if FullFeed running    → SKIP
        //        else if Diff running   → WAIT (poll, then proceed)
        //        else                   → SKIP (only other uploads)
        //   }
        //   else                        → SKIP (original)

        /// <summary>Simulates the elseif(Upload) branch and returns "WAIT", "SKIP" or "PROCEED".</summary>
        static string SimulateUploadBranch(int uploadRouteId, long flowId, RouteAbortFlag coord)
        {
            int[] inventoryTypeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool othersRunning = coord.IsOtherFlowInventoryRouteRunning(flowId, uploadRouteId, inventoryTypeIds);
            if (!othersRunning) return "PROCEED";

            int[] fullFeedTypeIds = InventoryRouteHelper.GetFullFeedTypeIds();
            if (coord.IsOtherFlowInventoryRouteRunning(flowId, uploadRouteId, fullFeedTypeIds))
                return "SKIP"; // Full Feed has top priority

            int[] diffTypeIds = InventoryRouteHelper.GetDifferentialTypeIds();
            if (coord.IsOtherFlowInventoryRouteRunning(flowId, uploadRouteId, diffTypeIds))
                return "WAIT"; // Diff is running — wait for it

            return "SKIP"; // only other uploads — original behavior
        }

        static void Test_IsUploadRoute()
        {
            AssertTrue(InventoryRouteHelper.IsUploadRoute(27),  "FC-16a: WalmartUploadInventory (27) is upload");
            AssertTrue(InventoryRouteHelper.IsUploadRoute(48),  "FC-16b: AmazonInventoryUpload (48) is upload");
            AssertTrue(InventoryRouteHelper.IsUploadRoute(43),  "FC-16c: LowesInventoryUpload (43) is upload");
            AssertTrue(InventoryRouteHelper.IsUploadRoute(33),  "FC-16d: MacysInventoryUpload (33) is upload");
            AssertTrue(InventoryRouteHelper.IsUploadRoute(52),  "FC-16e: KnotInventoryUpload (52) is upload");
            AssertTrue(InventoryRouteHelper.IsUploadRoute(57),  "FC-16f: MichealInventoryUpload (57) is upload");
            AssertTrue(InventoryRouteHelper.IsUploadRoute(38),  "FC-16g: TargetPlusInventoryFeedWHSWise (38) is upload");
            AssertFalse(InventoryRouteHelper.IsUploadRoute(7),  "FC-16h: Full Feed (7) is NOT upload");
            AssertFalse(InventoryRouteHelper.IsUploadRoute(8),  "FC-16i: Diff Feed (8) is NOT upload");
            AssertFalse(InventoryRouteHelper.IsUploadRoute(2),  "FC-16j: GetOrders (2) is NOT upload");
        }

        static void Test_GetDifferentialTypeIds()
        {
            int[] ids = InventoryRouteHelper.GetDifferentialTypeIds();
            AssertTrue(ids.Length == 1 && ids[0] == 8, "FC-17: GetDifferentialTypeIds returns [8]");
        }

        static void Test_GetFullFeedTypeIds()
        {
            int[] ids = InventoryRouteHelper.GetFullFeedTypeIds();
            AssertTrue(ids.Length == 1 && ids[0] == 7, "FC-18: GetFullFeedTypeIds returns [7]");
        }

        static void Test_Upload_NothingRunning_Proceeds()
        {
            ClearAllTestLocks();
            var coord = CreateCoordinator();
            string result = SimulateUploadBranch(TEST_ROUTE_UPLOAD, TEST_FLOW_ID, coord);
            AssertTrue(result == "PROCEED", $"FC-19: Upload runs normally when nothing is running (got {result})");
        }

        static void Test_Upload_DiffRunning_Waits()
        {
            ClearAllTestLocks();
            InsertActiveLock("TEST_COORD", 8, TEST_ROUTE_DIFF_FEED); // Diff feed running
            var coord = CreateCoordinator();
            string result = SimulateUploadBranch(TEST_ROUTE_UPLOAD, TEST_FLOW_ID, coord);
            AssertTrue(result == "WAIT", $"FC-20: Upload WAITS when Differential is running (got {result})");
            ClearAllTestLocks();
        }

        static void Test_Upload_FullFeedRunning_Skips()
        {
            ClearAllTestLocks();
            InsertActiveLock("TEST_COORD", 7, TEST_ROUTE_FULL_FEED); // Full Feed running
            var coord = CreateCoordinator();
            string result = SimulateUploadBranch(TEST_ROUTE_UPLOAD, TEST_FLOW_ID, coord);
            AssertTrue(result == "SKIP", $"FC-21: Upload SKIPS when Full Feed is running (top priority) (got {result})");
            ClearAllTestLocks();
        }

        static void Test_Upload_OnlyOtherUploadRunning_Skips()
        {
            ClearAllTestLocks();
            // Insert a lock for a DIFFERENT upload route in the same flow.
            // Need to use a route ID that exists in FlowDetails — reuse FULL_FEED slot but with TypeId=27 won't work
            // because the SP joins on Routes.TypeId. Instead, temporarily insert a 4th upload route record.
            const int OTHER_UPLOAD_ROUTE = 99906;
            try
            {
                var conn = new DBConnector(_connectionString);
                conn.Execute($@"
                    INSERT INTO [Routes] (Id, Name, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, FrequencyType, CreatedDate, CreatedBy)
                    VALUES ({OTHER_UPLOAD_ROUTE}, 'TEST - Other Upload', 48, 'Active', 1, 1, 1, 1, 1, 1, 'Minutely', GETDATE(), 1);
                    INSERT INTO [FlowDetails] (FlowId, RouteId, Status, CreatedDate, CreatedBy)
                    VALUES ({TEST_FLOW_ID}, {OTHER_UPLOAD_ROUTE}, 'Active', GETDATE(), 1);");

                InsertActiveLock("TEST_COORD", 48, OTHER_UPLOAD_ROUTE); // Other upload running
                var coord = CreateCoordinator();
                string result = SimulateUploadBranch(TEST_ROUTE_UPLOAD, TEST_FLOW_ID, coord);
                AssertTrue(result == "SKIP", $"FC-22: Upload SKIPS when only another upload is running — original behavior (got {result})");
            }
            finally
            {
                ClearAllTestLocks();
                try
                {
                    var conn = new DBConnector(_connectionString);
                    conn.Execute($"DELETE FROM FlowDetails WHERE RouteId = {OTHER_UPLOAD_ROUTE}");
                    conn.Execute($"DELETE FROM Routes WHERE Id = {OTHER_UPLOAD_ROUTE}");
                }
                catch { }
            }
        }

        static void Test_Upload_DiffAndOtherUpload_WaitsForDiff()
        {
            ClearAllTestLocks();
            const int OTHER_UPLOAD_ROUTE = 99907;
            try
            {
                var conn = new DBConnector(_connectionString);
                conn.Execute($@"
                    INSERT INTO [Routes] (Id, Name, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, FrequencyType, CreatedDate, CreatedBy)
                    VALUES ({OTHER_UPLOAD_ROUTE}, 'TEST - Other Upload 2', 48, 'Active', 1, 1, 1, 1, 1, 1, 'Minutely', GETDATE(), 1);
                    INSERT INTO [FlowDetails] (FlowId, RouteId, Status, CreatedDate, CreatedBy)
                    VALUES ({TEST_FLOW_ID}, {OTHER_UPLOAD_ROUTE}, 'Active', GETDATE(), 1);");

                InsertActiveLock("TEST_COORD", 8, TEST_ROUTE_DIFF_FEED);    // Diff running
                InsertActiveLock("TEST_COORD", 48, OTHER_UPLOAD_ROUTE);     // Plus another upload running
                var coord = CreateCoordinator();
                string result = SimulateUploadBranch(TEST_ROUTE_UPLOAD, TEST_FLOW_ID, coord);
                AssertTrue(result == "WAIT", $"FC-23: Upload WAITS when Diff + another upload are running (Diff takes precedence) (got {result})");
            }
            finally
            {
                ClearAllTestLocks();
                try
                {
                    var conn = new DBConnector(_connectionString);
                    conn.Execute($"DELETE FROM FlowDetails WHERE RouteId = {OTHER_UPLOAD_ROUTE}");
                    conn.Execute($"DELETE FROM Routes WHERE Id = {OTHER_UPLOAD_ROUTE}");
                }
                catch { }
            }
        }

        static void Test_Upload_DiffFinishes_ThenProceeds()
        {
            ClearAllTestLocks();
            InsertActiveLock("TEST_COORD", 8, TEST_ROUTE_DIFF_FEED);
            var coord = CreateCoordinator();

            string before = SimulateUploadBranch(TEST_ROUTE_UPLOAD, TEST_FLOW_ID, coord);
            AssertTrue(before == "WAIT", $"FC-24a: Upload starts in WAIT state while Diff runs (got {before})");

            // Simulate Diff finishing
            ClearAllTestLocks();
            string after = SimulateUploadBranch(TEST_ROUTE_UPLOAD, TEST_FLOW_ID, coord);
            AssertTrue(after == "PROCEED", $"FC-24b: After Diff finishes, Upload proceeds (got {after})");
        }

        static void Test_FullFeed_StillTopPriority()
        {
            // Confirm the existing Full Feed branch still wins regardless of upload-branch additions.
            ClearAllTestLocks();
            InsertActiveLock("TEST_COORD", 27, TEST_ROUTE_UPLOAD); // Upload running

            var coord = CreateCoordinator();
            int[] typeIds = InventoryRouteHelper.GetInventoryTypeIds();
            bool othersRunning = coord.IsOtherFlowInventoryRouteRunning(TEST_FLOW_ID, TEST_ROUTE_FULL_FEED, typeIds);

            // Full Feed branch comes BEFORE upload branch in the if/else chain — so Full Feed waits.
            bool isFullFeed = InventoryRouteHelper.IsFullFeedRoute(7);
            bool fullFeedShouldWait = othersRunning && isFullFeed;
            AssertTrue(fullFeedShouldWait, "FC-25: Full Feed branch still wins — waits for others (unchanged)");
            ClearAllTestLocks();
        }

        // ================================================================
        // TEST DATA SETUP & CLEANUP
        // ================================================================

        static bool SetupTestData()
        {
            try
            {
                var conn = new DBConnector(_connectionString);

                // Clean any previous test data
                CleanupTestData();

                // Create test Flow (Flows has IDENTITY)
                conn.Execute($@"
                    SET IDENTITY_INSERT [Flows] ON;
                    INSERT INTO [Flows] (Id, CustomerID, Title, Description, Status, SequenceNo, CreatedDate, CreatedBy)
                    VALUES ({TEST_FLOW_ID}, 'TEST_COORD', 'Test Flow for Coordination', 'Automated test', 'Active', 1, GETDATE(), 1);
                    SET IDENTITY_INSERT [Flows] OFF;");

                // Create test Routes (need real route records for the SP joins)
                conn.Execute($@"
                    INSERT INTO [Routes] (Id, Name, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, FrequencyType, CreatedDate, CreatedBy)
                    VALUES ({TEST_ROUTE_UPLOAD}, 'TEST - Upload Inventory', 27, 'Active', 1, 1, 1, 1, 1, 1, 'Minutely', GETDATE(), 1);
                    INSERT INTO [Routes] (Id, Name, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, FrequencyType, CreatedDate, CreatedBy)
                    VALUES ({TEST_ROUTE_FULL_FEED}, 'TEST - Full Feed', 7, 'Active', 1, 1, 1, 1, 1, 1, 'Minutely', GETDATE(), 1);
                    INSERT INTO [Routes] (Id, Name, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, FrequencyType, CreatedDate, CreatedBy)
                    VALUES ({TEST_ROUTE_DIFF_FEED}, 'TEST - Diff Feed', 8, 'Active', 1, 1, 1, 1, 1, 1, 'Minutely', GETDATE(), 1);
                    INSERT INTO [Routes] (Id, Name, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId, FrequencyType, CreatedDate, CreatedBy)
                    VALUES ({TEST_ROUTE_NON_INV}, 'TEST - Get Orders', 2, 'Active', 1, 1, 1, 1, 1, 1, 'Minutely', GETDATE(), 1);");

                // Create FlowDetails linking routes to flow (Upload, FullFeed, DiffFeed, NonInv — all in same flow)
                conn.Execute($@"
                    INSERT INTO [FlowDetails] (FlowId, RouteId, Status, CreatedDate, CreatedBy)
                    VALUES ({TEST_FLOW_ID}, {TEST_ROUTE_UPLOAD}, 'Active', GETDATE(), 1);
                    INSERT INTO [FlowDetails] (FlowId, RouteId, Status, CreatedDate, CreatedBy)
                    VALUES ({TEST_FLOW_ID}, {TEST_ROUTE_FULL_FEED}, 'Active', GETDATE(), 1);
                    INSERT INTO [FlowDetails] (FlowId, RouteId, Status, CreatedDate, CreatedBy)
                    VALUES ({TEST_FLOW_ID}, {TEST_ROUTE_DIFF_FEED}, 'Active', GETDATE(), 1);
                    INSERT INTO [FlowDetails] (FlowId, RouteId, Status, CreatedDate, CreatedBy)
                    VALUES ({TEST_FLOW_ID}, {TEST_ROUTE_NON_INV}, 'Active', GETDATE(), 1);");

                // TEST_ROUTE_NO_FLOW (99905) — intentionally NOT added to any flow

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Setup Error: {ex.Message}");
                return false;
            }
        }

        static void CleanupTestData()
        {
            try
            {
                var conn = new DBConnector(_connectionString);
                conn.Execute($"DELETE FROM FlowDetails WHERE FlowId = {TEST_FLOW_ID}");
                conn.Execute($"DELETE FROM Flows WHERE Id = {TEST_FLOW_ID}");
                conn.Execute($"DELETE FROM RouteExecutionLock WHERE CustomerID = 'TEST_COORD'");
                conn.Execute($"DELETE FROM Routes WHERE Id IN ({TEST_ROUTE_UPLOAD},{TEST_ROUTE_FULL_FEED},{TEST_ROUTE_DIFF_FEED},{TEST_ROUTE_NON_INV})");
            }
            catch { }
        }

        static void ClearAllTestLocks()
        {
            try
            {
                var conn = new DBConnector(_connectionString);
                conn.Execute("DELETE FROM RouteExecutionLock WHERE CustomerID = 'TEST_COORD'");
            }
            catch { }
        }

        static void InsertActiveLock(string customerID, int routeTypeId, int routeId)
        {
            var conn = new DBConnector(_connectionString);
            conn.Execute($@"INSERT INTO RouteExecutionLock (CustomerID, RouteTypeId, RouteId, LockToken, AcquiredAt, MachineName, IsActive)
                VALUES ('{customerID}', {routeTypeId}, {routeId}, '{Guid.NewGuid()}', GETDATE(), 'TEST-MACHINE', 1)");
        }

        static void InsertExpiredLock(string customerID, int routeTypeId, int routeId, int minutesAgo)
        {
            var conn = new DBConnector(_connectionString);
            conn.Execute($@"INSERT INTO RouteExecutionLock (CustomerID, RouteTypeId, RouteId, LockToken, AcquiredAt, MachineName, IsActive)
                VALUES ('{customerID}', {routeTypeId}, {routeId}, '{Guid.NewGuid()}', DATEADD(MINUTE, -{minutesAgo}, GETDATE()), 'TEST-MACHINE', 1)");
        }

        // ================================================================
        // HELPERS
        // ================================================================

        static RouteAbortFlag CreateCoordinator()
        {
            var coord = new RouteAbortFlag();
            coord.UseConnection(_connectionString);
            return coord;
        }

        static string MaskConnectionString(string cs)
        {
            var parts = cs.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].TrimStart().StartsWith("PWD", StringComparison.OrdinalIgnoreCase) ||
                    parts[i].TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase))
                {
                    var eqIdx = parts[i].IndexOf('=');
                    if (eqIdx >= 0) parts[i] = parts[i].Substring(0, eqIdx + 1) + "****";
                }
            }
            return string.Join(";", parts);
        }

        // ================================================================
        // ASSERTIONS
        // ================================================================

        static void AssertTrue(bool value, string testName)
        { if (value) { PrintPass(testName); _passed++; } else { PrintFail(testName, "Expected TRUE, got FALSE"); _failed++; } }

        static void AssertFalse(bool value, string testName)
        { if (!value) { PrintPass(testName); _passed++; } else { PrintFail(testName, "Expected FALSE, got TRUE"); _failed++; } }

        static void AssertNotNull(object value, string testName)
        { if (value != null) { PrintPass(testName); _passed++; } else { PrintFail(testName, "Expected NOT NULL, got NULL"); _failed++; } }

        // ================================================================
        // CONSOLE OUTPUT
        // ================================================================

        static void PrintHeader(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  {text,-63}║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        static void PrintSection(string text)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ━━━ {text} ━━━");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void PrintPass(string testName)
        { Console.ForegroundColor = ConsoleColor.Green; Console.Write("  ✓ PASS  "); Console.ResetColor(); Console.WriteLine(testName); }

        static void PrintFail(string testName, string reason)
        { Console.ForegroundColor = ConsoleColor.Red; Console.Write("  ✗ FAIL  "); Console.ResetColor(); Console.Write(testName); Console.ForegroundColor = ConsoleColor.DarkGray; Console.WriteLine($"  → {reason}"); Console.ResetColor(); }

        static void PrintError(string text)
        { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"  ERROR: {text}"); Console.ResetColor(); }

        static void PrintSummary()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.Write("  RESULTS: ");
            Console.ForegroundColor = ConsoleColor.Green; Console.Write($"{_passed} PASSED"); Console.ResetColor();
            Console.Write("  |  ");
            Console.ForegroundColor = _failed > 0 ? ConsoleColor.Red : ConsoleColor.Green;
            Console.Write($"{_failed} FAILED"); Console.ResetColor();
            Console.Write($"  |  {_passed + _failed} TOTAL");
            Console.WriteLine();
            if (_failed == 0) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  ALL TESTS PASSED!"); }
            else { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"  {_failed} TEST(S) FAILED"); }
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
