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
    /// Comprehensive test suite for RouteExecutionLock — all scenarios.
    /// Tests both in-process and external process lock behavior.
    /// </summary>
    public static class RouteExecutionLockTests
    {
        private static string _connectionString = "";
        private static int _passed = 0;
        private static int _failed = 0;

        public static async Task RunAllTests(string connectionString)
        {
            _connectionString = connectionString;
            _passed = 0;
            _failed = 0;

            PrintHeader("ROUTE EXECUTION LOCK — TEST SUITE");
            Console.WriteLine($"  Connection: {MaskConnectionString(connectionString)}");
            Console.WriteLine($"  Timestamp:  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // Ensure table exists
            if (!EnsureTableExists())
            {
                PrintError("RouteExecutionLock table does not exist. Deploy script 29 first.");
                return;
            }

            // Clean up any leftover test data
            CleanupTestData();

            // ===== LEVEL 1: SELF-LOCK TESTS (RouteEngine/RouteWorker) =====
            PrintSection("LEVEL 1: SELF-LOCK (Same Route Overlap Prevention)");

            await Test01_AcquireLock_Success();
            await Test02_AcquireLock_AlreadyHeld_ReturnsNull();
            await Test03_ReleaseLock_Success();
            await Test04_ReleaseLock_ThenReAcquire();
            await Test05_DifferentRouteIds_IndependentLocks();

            // ===== LEVEL 2: CROSS-ROUTE LOCK TESTS (Full blocks Diff) =====
            PrintSection("LEVEL 2: CROSS-ROUTE (Full Feed Blocks Differential)");

            await Test06_FullFeedLock_BlocksDiffFeed();
            await Test07_FullFeedNotRunning_DiffFeedProceeds();
            await Test08_DiffFeedRunning_FullFeedProceeds();
            await Test09_DifferentCustomers_NoBlocking();
            await Test10_FullFeedCompletes_DiffFeedUnblocked();

            // ===== LEVEL 3: ERROR & RECOVERY TESTS =====
            PrintSection("LEVEL 3: ERROR HANDLING & RECOVERY");

            await Test11_ReleaseLock_InvalidToken_NoError();
            await Test12_StaleLockCleanup();
            await Test13_StaleLockIgnoredOnAcquire();
            await Test14_StaleLockIgnoredOnIsLocked();

            // ===== LEVEL 4: CONCURRENT & RACE CONDITION TESTS =====
            PrintSection("LEVEL 4: CONCURRENCY & RACE CONDITIONS");

            await Test15_ConcurrentAcquire_OnlyOneWins();
            await Test16_ConcurrentFullAndDiff_DiffBlocked();

            // ===== LEVEL 5: EXTERNAL PROCESS SIMULATION =====
            PrintSection("LEVEL 5: EXTERNAL PROCESS SIMULATION");

            await Test17_ExternalProcess_LockSurvives();
            await Test18_ExternalProcess_LockReleasedOnExit();

            // ===== VERIFICATION: Leave data in table for manual inspection =====
            await Test19_LeaveDataForVerification();

            // ===== SUMMARY =====
            // CleanupTestData(); // Skipped — data left in table for manual verification
            PrintSummary();
        }

        // ================================================================
        // LEVEL 1: SELF-LOCK TESTS
        // ================================================================

        static async Task Test01_AcquireLock_Success()
        {
            CleanupTestData();
            var lockEntity = CreateLock();
            string token = lockEntity.AcquireLock("TEST_CUST_001", 7, 1001);

            AssertNotNull(token, "TC-01: AcquireLock returns token on first call");
        }

        static async Task Test02_AcquireLock_AlreadyHeld_ReturnsNull()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            string token1 = lockEntity.AcquireLock("TEST_CUST_001", 7, 1001);
            string token2 = lockEntity.AcquireLock("TEST_CUST_001", 7, 1001);

            AssertNotNull(token1, "TC-02a: First acquire succeeds");
            AssertNull(token2, "TC-02b: Second acquire returns null (lock held)");
        }

        static async Task Test03_ReleaseLock_Success()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            string token = lockEntity.AcquireLock("TEST_CUST_001", 7, 1001);
            bool released = lockEntity.ReleaseLock(token);

            AssertTrue(released, "TC-03: ReleaseLock returns true");

            // Verify lock is inactive in DB
            bool stillLocked = lockEntity.IsLocked("TEST_CUST_001", 7);
            AssertFalse(stillLocked, "TC-03b: IsLocked returns false after release");
        }

        static async Task Test04_ReleaseLock_ThenReAcquire()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            string token1 = lockEntity.AcquireLock("TEST_CUST_001", 7, 1001);
            lockEntity.ReleaseLock(token1);

            string token2 = lockEntity.AcquireLock("TEST_CUST_001", 7, 1001);

            AssertNotNull(token2, "TC-04: Can re-acquire lock after release");
            AssertNotEqual(token1, token2, "TC-04b: New token is different from old");

            lockEntity.ReleaseLock(token2);
        }

        static async Task Test05_DifferentRouteIds_IndependentLocks()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            string token1 = lockEntity.AcquireLock("ROUTE", 1001, 1001); // Route 1001
            string token2 = lockEntity.AcquireLock("ROUTE", 1002, 1002); // Route 1002

            AssertNotNull(token1, "TC-05a: Route 1001 lock acquired");
            AssertNotNull(token2, "TC-05b: Route 1002 lock acquired (independent)");

            lockEntity.ReleaseLock(token1);
            lockEntity.ReleaseLock(token2);
        }

        // ================================================================
        // LEVEL 2: CROSS-ROUTE LOCK TESTS
        // ================================================================

        static async Task Test06_FullFeedLock_BlocksDiffFeed()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Full Feed acquires lock for customer
            string fullToken = lockEntity.AcquireLock("TEST_CUST_001", 7, 2001);  // TypeId 7 = Full

            // Differential Feed checks if Full Feed is running
            bool isBlocked = lockEntity.IsLocked("TEST_CUST_001", 7);

            AssertNotNull(fullToken, "TC-06a: Full Feed lock acquired");
            AssertTrue(isBlocked, "TC-06b: Differential Feed sees Full Feed is running → BLOCKED");

            lockEntity.ReleaseLock(fullToken);
        }

        static async Task Test07_FullFeedNotRunning_DiffFeedProceeds()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // No Full Feed running — Differential should proceed
            bool isBlocked = lockEntity.IsLocked("TEST_CUST_001", 7);

            AssertFalse(isBlocked, "TC-07: No Full Feed running → Differential proceeds");
        }

        static async Task Test08_DiffFeedRunning_FullFeedProceeds()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Differential Feed is running (TypeId 8)
            string diffToken = lockEntity.AcquireLock("TEST_CUST_001", 8, 2002);

            // Full Feed checks — it should NOT be blocked by Differential
            bool isFullBlocked = lockEntity.IsLocked("TEST_CUST_001", 8); // checking TypeId 8 lock
            // But Full Feed only checks for TypeId 7 lock, not TypeId 8
            bool isFullBlockedByFullLock = lockEntity.IsLocked("TEST_CUST_001", 7);

            AssertNotNull(diffToken, "TC-08a: Diff Feed lock acquired");
            AssertTrue(isFullBlocked, "TC-08b: TypeId 8 lock exists");
            AssertFalse(isFullBlockedByFullLock, "TC-08c: No TypeId 7 lock → Full Feed proceeds");

            // Full Feed can acquire its own lock
            string fullToken = lockEntity.AcquireLock("TEST_CUST_001", 7, 2001);
            AssertNotNull(fullToken, "TC-08d: Full Feed acquires lock while Diff is running");

            lockEntity.ReleaseLock(diffToken);
            lockEntity.ReleaseLock(fullToken);
        }

        static async Task Test09_DifferentCustomers_NoBlocking()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Full Feed running for Customer A
            string tokenA = lockEntity.AcquireLock("TEST_CUST_A", 7, 3001);

            // Differential Feed for Customer B checks — should NOT be blocked
            bool isBBlocked = lockEntity.IsLocked("TEST_CUST_B", 7);

            AssertNotNull(tokenA, "TC-09a: Customer A Full Feed lock acquired");
            AssertFalse(isBBlocked, "TC-09b: Customer B not blocked by Customer A");

            lockEntity.ReleaseLock(tokenA);
        }

        static async Task Test10_FullFeedCompletes_DiffFeedUnblocked()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Full Feed starts
            string fullToken = lockEntity.AcquireLock("TEST_CUST_001", 7, 2001);
            AssertTrue(lockEntity.IsLocked("TEST_CUST_001", 7), "TC-10a: Full Feed is running");

            // Full Feed completes
            lockEntity.ReleaseLock(fullToken);
            AssertFalse(lockEntity.IsLocked("TEST_CUST_001", 7), "TC-10b: Full Feed released");

            // Differential Feed can now proceed
            string diffToken = lockEntity.AcquireLock("TEST_CUST_001", 8, 2002);
            AssertNotNull(diffToken, "TC-10c: Differential Feed proceeds after Full Feed completes");

            lockEntity.ReleaseLock(diffToken);
        }

        // ================================================================
        // LEVEL 3: ERROR HANDLING & RECOVERY
        // ================================================================

        static async Task Test11_ReleaseLock_InvalidToken_NoError()
        {
            var lockEntity = CreateLock();
            bool released = lockEntity.ReleaseLock("INVALID-TOKEN-12345");

            AssertTrue(true, "TC-11: ReleaseLock with invalid token does not throw");
        }

        static async Task Test12_StaleLockCleanup()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Insert a lock with old timestamp (simulate crashed process)
            InsertStaleLock("TEST_CUST_STALE", 7, 9999, "STALE-TOKEN-001", 120); // 120 min old

            // Verify it exists
            bool existsBefore = IsActiveLockInDB("STALE-TOKEN-001");
            AssertTrue(existsBefore, "TC-12a: Stale lock exists in DB");

            // Run cleanup with 60 min timeout
            int cleaned = lockEntity.CleanStaleLocks(60);

            bool existsAfter = IsActiveLockInDB("STALE-TOKEN-001");
            AssertTrue(cleaned >= 1, "TC-12b: CleanStaleLocks cleaned at least 1 lock");
            AssertFalse(existsAfter, "TC-12c: Stale lock deactivated after cleanup");
        }

        static async Task Test13_StaleLockIgnoredOnAcquire()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Insert a stale lock (older than 60 min)
            InsertStaleLock("TEST_CUST_STALE2", 7, 8888, "STALE-TOKEN-002", 90);

            // AcquireLock should succeed because stale lock is > 60 min old
            string token = lockEntity.AcquireLock("TEST_CUST_STALE2", 7, 8889);

            AssertNotNull(token, "TC-13: AcquireLock succeeds even with stale lock (> 60 min)");

            lockEntity.ReleaseLock(token);
        }

        static async Task Test14_StaleLockIgnoredOnIsLocked()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Insert a stale lock (older than 60 min)
            InsertStaleLock("TEST_CUST_STALE3", 7, 7777, "STALE-TOKEN-003", 90);

            // IsLocked should return false because lock is > 60 min old
            bool isLocked = lockEntity.IsLocked("TEST_CUST_STALE3", 7);

            AssertFalse(isLocked, "TC-14: IsLocked returns false for stale lock (> 60 min)");
        }

        // ================================================================
        // LEVEL 4: CONCURRENCY & RACE CONDITION TESTS
        // ================================================================

        static async Task Test15_ConcurrentAcquire_OnlyOneWins()
        {
            CleanupTestData();
            int successCount = 0;
            string[] tokens = new string[10];

            // 10 threads trying to acquire the same lock simultaneously
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    var lockEntity = CreateLock();
                    tokens[idx] = lockEntity.AcquireLock("TEST_CUST_RACE", 7, 5001);
                    if (tokens[idx] != null)
                        Interlocked.Increment(ref successCount);
                });
            }

            await Task.WhenAll(tasks);

            AssertEqual(1, successCount, "TC-15: Only 1 out of 10 concurrent threads acquires the lock");

            // Cleanup — release the winning token
            foreach (var t in tokens)
            {
                if (t != null)
                {
                    var lockEntity = CreateLock();
                    lockEntity.ReleaseLock(t);
                }
            }
        }

        static async Task Test16_ConcurrentFullAndDiff_DiffBlocked()
        {
            CleanupTestData();
            var lockEntity = CreateLock();
            bool diffBlocked = false;

            // Full Feed acquires lock
            string fullToken = lockEntity.AcquireLock("TEST_CUST_CONC", 7, 6001);

            // Simulate 5 Differential Feed attempts concurrently
            var tasks = new Task[5];
            int blockedCount = 0;
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var diffLock = CreateLock();
                    bool isBlocked = diffLock.IsLocked("TEST_CUST_CONC", 7);
                    if (isBlocked)
                        Interlocked.Increment(ref blockedCount);
                });
            }

            await Task.WhenAll(tasks);

            AssertEqual(5, blockedCount, "TC-16: All 5 concurrent Differential Feeds are blocked");

            lockEntity.ReleaseLock(fullToken);
        }

        // ================================================================
        // LEVEL 5: EXTERNAL PROCESS SIMULATION
        // ================================================================

        static async Task Test17_ExternalProcess_LockSurvives()
        {
            CleanupTestData();

            // Simulate: Process A acquires lock, then "crashes" (just abandons it)
            var lockA = CreateLock();
            string token = lockA.AcquireLock("TEST_CUST_EXT", 7, 7001);
            AssertNotNull(token, "TC-17a: Process A acquires lock");

            // Simulate: Process B (new instance after restart) checks lock
            var lockB = CreateLock(); // new instance = separate process simulation
            bool isLocked = lockB.IsLocked("TEST_CUST_EXT", 7);
            AssertTrue(isLocked, "TC-17b: Process B sees lock from Process A (survives process boundary)");

            // Process B cannot acquire same lock
            string tokenB = lockB.AcquireLock("TEST_CUST_EXT", 7, 7001);
            AssertNull(tokenB, "TC-17c: Process B cannot acquire — lock held by Process A");

            // Cleanup
            lockA.ReleaseLock(token);
        }

        static async Task Test18_ExternalProcess_LockReleasedOnExit()
        {
            CleanupTestData();

            // Simulate: External process acquires lock, does work, releases on exit
            string token = null;

            // "External process" scope
            {
                var extLock = CreateLock();
                token = extLock.AcquireLock("TEST_CUST_EXT2", 7, 7002);
                AssertNotNull(token, "TC-18a: External process acquires lock");

                // Simulate work
                Thread.Sleep(100);

                // Simulate finally block in RouteWorker
                extLock.ReleaseLock(token);
            }

            // After external process exits, lock should be released
            var checkLock = CreateLock();
            bool isLocked = checkLock.IsLocked("TEST_CUST_EXT2", 7);
            AssertFalse(isLocked, "TC-18b: Lock released after external process completes");

            // New process can acquire
            string newToken = checkLock.AcquireLock("TEST_CUST_EXT2", 7, 7003);
            AssertNotNull(newToken, "TC-18c: New process can acquire after previous released");

            checkLock.ReleaseLock(newToken);
        }

        // ================================================================
        // LEVEL 6: LEAVE DATA FOR MANUAL VERIFICATION
        // ================================================================

        static async Task Test19_LeaveDataForVerification()
        {
            CleanupTestData();
            var lockEntity = CreateLock();

            // Scenario A: Full Feed running for TAR6266P (Active lock)
            string tokenA = lockEntity.AcquireLock("TAR6266P", 7, 101);
            AssertNotNull(tokenA, "TC-19a: Full Feed lock for TAR6266P — ACTIVE (left in table)");

            // Scenario B: Differential Feed blocked for TAR6266P — just check, don't acquire
            bool blocked = lockEntity.IsLocked("TAR6266P", 7);
            AssertTrue(blocked, "TC-19b: Diff Feed for TAR6266P is BLOCKED by Full Feed");

            // Scenario C: Full Feed for WAL4001MP (Active lock)
            string tokenC = lockEntity.AcquireLock("WAL4001MP", 7, 102);
            AssertNotNull(tokenC, "TC-19c: Full Feed lock for WAL4001MP — ACTIVE (left in table)");

            // Scenario D: Diff Feed for AMA1005 — no Full Feed running, so it proceeds
            bool ama1005Blocked = lockEntity.IsLocked("AMA1005", 7);
            AssertFalse(ama1005Blocked, "TC-19d: Diff Feed for AMA1005 NOT blocked (no Full Feed)");

            // Scenario E: Route-level self-lock (simulating RouteEngine)
            string tokenE = lockEntity.AcquireLock("ROUTE", 201, 201);
            AssertNotNull(tokenE, "TC-19e: Route 201 self-lock — ACTIVE (left in table)");

            // Scenario F: Route 201 already running — second attempt fails
            string tokenF = lockEntity.AcquireLock("ROUTE", 201, 201);
            AssertNull(tokenF, "TC-19f: Route 201 second attempt — BLOCKED (already running)");

            // Scenario G: Released lock — insert and release (shows IsActive=0)
            string tokenG = lockEntity.AcquireLock("LOW2221MP", 7, 103);
            lockEntity.ReleaseLock(tokenG);
            AssertFalse(lockEntity.IsLocked("LOW2221MP", 7), "TC-19g: LOW2221MP lock released (IsActive=0 in table)");

            // Scenario H: Stale lock — insert with old timestamp
            InsertStaleLock("TEST_STALE_DEMO", 7, 999, "STALE-DEMO-TOKEN", 90);
            AssertFalse(lockEntity.IsLocked("TEST_STALE_DEMO", 7), "TC-19h: Stale lock (90 min old) ignored by IsLocked");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ──────────────────────────────────────────────────────");
            Console.WriteLine("  DATA LEFT IN TABLE — Run this query to verify:");
            Console.WriteLine("  SELECT * FROM RouteExecutionLock ORDER BY Id DESC");
            Console.WriteLine("  ──────────────────────────────────────────────────────");
            Console.ResetColor();
            Console.WriteLine();
        }

        // ================================================================
        // HELPERS
        // ================================================================

        static RouteExecutionLock CreateLock()
        {
            var lockEntity = new RouteExecutionLock();
            lockEntity.UseConnection(_connectionString);
            return lockEntity;
        }

        static bool EnsureTableExists()
        {
            try
            {
                var conn = new DBConnector(_connectionString);
                var dt = new DataTable();
                conn.GetData("SELECT TOP 0 * FROM RouteExecutionLock", ref dt);
                return true;
            }
            catch { return false; }
        }

        static void CleanupTestData()
        {
            try
            {
                var conn = new DBConnector(_connectionString);
                conn.Execute("DELETE FROM RouteExecutionLock WHERE CustomerID LIKE 'TEST_%' OR CustomerID = 'ROUTE' OR CustomerID IN ('TAR6266P','WAL4001MP','AMA1005','LOW2221MP') OR RouteId IN (101,102,103,201,999)");
            }
            catch { }
        }

        static void InsertStaleLock(string customerID, int routeTypeId, int routeId, string lockToken, int minutesAgo)
        {
            var conn = new DBConnector(_connectionString);
            string sql = $@"INSERT INTO RouteExecutionLock (CustomerID, RouteTypeId, RouteId, LockToken, AcquiredAt, MachineName, IsActive)
                            VALUES ('{customerID}', {routeTypeId}, {routeId}, '{lockToken}', DATEADD(MINUTE, -{minutesAgo}, GETDATE()), 'TEST-MACHINE', 1)";
            conn.Execute(sql);
        }

        static bool IsActiveLockInDB(string lockToken)
        {
            var conn = new DBConnector(_connectionString);
            var dt = new DataTable();
            conn.GetData($"SELECT COUNT(1) as C FROM RouteExecutionLock WHERE LockToken = '{lockToken}' AND IsActive = 1", ref dt);
            return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["C"]) > 0;
        }

        static string MaskConnectionString(string cs)
        {
            // Mask password in connection string for display
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

        static void AssertNotNull(object value, string testName)
        {
            if (value != null) { PrintPass(testName); _passed++; }
            else { PrintFail(testName, "Expected NOT NULL, got NULL"); _failed++; }
        }

        static void AssertNull(object value, string testName)
        {
            if (value == null) { PrintPass(testName); _passed++; }
            else { PrintFail(testName, $"Expected NULL, got '{value}'"); _failed++; }
        }

        static void AssertTrue(bool value, string testName)
        {
            if (value) { PrintPass(testName); _passed++; }
            else { PrintFail(testName, "Expected TRUE, got FALSE"); _failed++; }
        }

        static void AssertFalse(bool value, string testName)
        {
            if (!value) { PrintPass(testName); _passed++; }
            else { PrintFail(testName, "Expected FALSE, got TRUE"); _failed++; }
        }

        static void AssertEqual(int expected, int actual, string testName)
        {
            if (expected == actual) { PrintPass(testName); _passed++; }
            else { PrintFail(testName, $"Expected {expected}, got {actual}"); _failed++; }
        }

        static void AssertNotEqual(string a, string b, string testName)
        {
            if (a != b) { PrintPass(testName); _passed++; }
            else { PrintFail(testName, $"Expected different values, both are '{a}'"); _failed++; }
        }

        // ================================================================
        // CONSOLE OUTPUT
        // ================================================================

        static void PrintHeader(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  {text,-59}║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
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
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  ✓ PASS  ");
            Console.ResetColor();
            Console.WriteLine(testName);
        }

        static void PrintFail(string testName, string reason)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  ✗ FAIL  ");
            Console.ResetColor();
            Console.Write(testName);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  → {reason}");
            Console.ResetColor();
        }

        static void PrintError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ERROR: {text}");
            Console.ResetColor();
        }

        static void PrintSummary()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ══════════════════════════════════════════════════════════");
            Console.ResetColor();

            Console.Write("  RESULTS: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{_passed} PASSED");
            Console.ResetColor();
            Console.Write("  |  ");

            if (_failed > 0) Console.ForegroundColor = ConsoleColor.Red;
            else Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{_failed} FAILED");
            Console.ResetColor();

            Console.Write("  |  ");
            Console.Write($"{_passed + _failed} TOTAL");
            Console.WriteLine();

            if (_failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ALL TESTS PASSED!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  {_failed} TEST(S) FAILED — REVIEW ABOVE");
            }

            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ══════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
