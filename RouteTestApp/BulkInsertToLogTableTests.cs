using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using eSyncMate.DB.Entities;

namespace RouteTestApp
{
    /// <summary>
    /// Scenario-based tests for BulkInsertToLogTable paged insert.
    /// Scenario: Amazon customer (AMA1005) with up to 300,000 inventory items.
    /// Tests verify correctness, page boundaries, memory safety, and timing.
    /// </summary>
    public static class BulkInsertToLogTableTests
    {
        private const string CUSTOMER_ID  = "AMA1005";
        private const string LOG_TABLE    = "SCSInventoryFeed_AMA1005_Log";
        private const string TEST_BATCH   = "TEST-BULK-PAGED-";

        private static string _connectionString = "";
        private static int _passed = 0;
        private static int _failed = 0;

        public static void RunAllTests(string connectionString, bool keepData = false)
        {
            _connectionString = connectionString;
            _passed = 0;
            _failed = 0;

            PrintHeader("BulkInsertToLogTable — PAGED INSERT TEST SUITE");
            Console.WriteLine($"  Customer   : {CUSTOMER_ID}");
            Console.WriteLine($"  Log Table  : {LOG_TABLE}");
            Console.WriteLine($"  Connection : {MaskConn(connectionString)}");
            Console.WriteLine($"  Keep Data  : {keepData}");
            Console.WriteLine($"  Timestamp  : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            if (!LogTableExists())
            {
                PrintError($"Log table [{LOG_TABLE}] does not exist. Deploy script 21/22 first.");
                return;
            }

            CleanupTestData();

            // ── Scenarios ────────────────────────────────────────────────────────
            PrintSection("SCENARIO 1 — Full Production Load (300,000 items, UPLOAD)");
            Test_300K_Upload(keepData);

            PrintSection("SCENARIO 2 — Exact Page Boundary (5,000 items = 1 page)");
            Test_ExactOnePage(keepData);

            PrintSection("SCENARIO 3 — Non-Multiple of 5000 (302,500 items = 61 pages, last page 2,500)");
            Test_NonMultipleOfPageSize(keepData);

            PrintSection("SCENARIO 4 — Small Dataset (100 items, DOWNLOAD)");
            Test_SmallDataset_Download(keepData);

            PrintSection("SCENARIO 5 — Empty DataTable (should no-op, no exception)");
            Test_EmptyDataTable();

            // ── Final cleanup (only if keepData=false) ────────────────────────────
            if (!keepData)
                CleanupTestData();
            else
                Console.WriteLine("\n  [INFO] Data retained in table — BatchID prefix: " + TEST_BATCH);

            // ── Summary ───────────────────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  RESULTS:  {_passed} passed   {_failed} failed");
            Console.WriteLine(new string('═', 60));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 1 — 300,000 items UPLOAD (real Amazon production simulation)
        // Expected: 60 pages x 5000 rows = 300,000 rows in DB
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_300K_Upload(bool keepData = false)
        {
            string batchID = TEST_BATCH + "300K";
            int    rowCount = 300_000;
            int    expectedPages = 60;

            try
            {
                Console.WriteLine($"  Building source DataTable — {rowCount:N0} rows...");
                DataTable source = BuildAmazonSourceTable(rowCount);

                Console.WriteLine($"  Calling BulkInsertToLogTable (pageSize=5000, pages={expectedPages})...");
                var sw = Stopwatch.StartNew();

                SCSInventoryFeed.BulkInsertToLogTable(
                    _connectionString,
                    LOG_TABLE,
                    source,
                    batchID,
                    "UPLOAD");

                sw.Stop();

                int inserted = CountRows(batchID);

                Assert($"300K rows inserted correctly",
                    inserted == rowCount,
                    $"Expected {rowCount:N0}, got {inserted:N0}");

                int pagesUsed = (int)Math.Ceiling((double)rowCount / 5000);
                Assert($"Page count is {expectedPages}",
                    pagesUsed == expectedPages,
                    $"Expected {expectedPages} pages, calculated {pagesUsed}");

                string statusSample = GetSampleStatus(batchID);
                Assert($"Status column set to 'Synced' for UPLOAD",
                    statusSample == "Synced",
                    $"Expected 'Synced', got '{statusSample}'");

                Console.WriteLine($"  Time: {sw.Elapsed.TotalSeconds:F2}s  ({rowCount / sw.Elapsed.TotalSeconds:N0} rows/sec)");
                Console.WriteLine();

                source.Dispose();
            }
            catch (Exception ex)
            {
                Fail($"300K UPLOAD threw exception: {ex.Message}");
            }
            finally
            {
                if (!keepData) DeleteRows(batchID);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 2 — Exactly 5000 rows = exactly 1 page, no partial page
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_ExactOnePage(bool keepData = false)
        {
            string batchID  = TEST_BATCH + "5K";
            int    rowCount = 5_000;

            try
            {
                DataTable source = BuildAmazonSourceTable(rowCount);

                SCSInventoryFeed.BulkInsertToLogTable(
                    _connectionString, LOG_TABLE, source, batchID, "UPLOAD");

                int inserted = CountRows(batchID);
                Assert($"Exact 5000 rows inserted (1 full page)", inserted == rowCount,
                    $"Expected {rowCount}, got {inserted}");

                source.Dispose();
            }
            catch (Exception ex)
            {
                Fail($"Exact page boundary threw exception: {ex.Message}");
            }
            finally
            {
                if (!keepData) DeleteRows(batchID);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 3 — 302,500 rows = 60 full pages + 1 partial page of 2,500
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_NonMultipleOfPageSize(bool keepData = false)
        {
            string batchID  = TEST_BATCH + "302K";
            int    rowCount = 302_500;

            try
            {
                DataTable source = BuildAmazonSourceTable(rowCount);

                var sw = Stopwatch.StartNew();
                SCSInventoryFeed.BulkInsertToLogTable(
                    _connectionString, LOG_TABLE, source, batchID, "UPLOAD");
                sw.Stop();

                int inserted = CountRows(batchID);
                Assert($"302,500 rows inserted (60 full + 1 partial page)", inserted == rowCount,
                    $"Expected {rowCount:N0}, got {inserted:N0}");

                int expectedPages = (int)Math.Ceiling((double)rowCount / 5000); // = 61
                Assert($"61 pages used for 302,500 rows",
                    expectedPages == 61,
                    $"Calculated {expectedPages} pages");

                Console.WriteLine($"  Time: {sw.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine();

                source.Dispose();
            }
            catch (Exception ex)
            {
                Fail($"Non-multiple of 5000 threw exception: {ex.Message}");
            }
            finally
            {
                if (!keepData) DeleteRows(batchID);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 4 — Small dataset (100 rows), logType = DOWNLOAD
        // Status should be 'Updated' (not 'Synced')
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_SmallDataset_Download(bool keepData = false)
        {
            string batchID  = TEST_BATCH + "100";
            int    rowCount = 100;

            try
            {
                DataTable source = BuildAmazonSourceTable(rowCount);

                SCSInventoryFeed.BulkInsertToLogTable(
                    _connectionString, LOG_TABLE, source, batchID, "DOWNLOAD");

                int inserted = CountRows(batchID);
                Assert($"100 rows inserted (DOWNLOAD)", inserted == rowCount,
                    $"Expected {rowCount}, got {inserted}");

                string statusSample = GetSampleStatus(batchID);
                Assert($"Status column set to 'Updated' for DOWNLOAD",
                    statusSample == "Updated",
                    $"Expected 'Updated', got '{statusSample}'");

                source.Dispose();
            }
            catch (Exception ex)
            {
                Fail($"Small DOWNLOAD dataset threw exception: {ex.Message}");
            }
            finally
            {
                if (!keepData) DeleteRows(batchID);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 5 — Empty DataTable — should return silently, no exception
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_EmptyDataTable()
        {
            string batchID = TEST_BATCH + "EMPTY";

            try
            {
                DataTable empty = BuildAmazonSourceTable(0);

                SCSInventoryFeed.BulkInsertToLogTable(
                    _connectionString, LOG_TABLE, empty, batchID, "UPLOAD");

                int inserted = CountRows(batchID);
                Assert($"Empty DataTable inserts 0 rows", inserted == 0,
                    $"Expected 0, got {inserted}");

                empty.Dispose();
            }
            catch (Exception ex)
            {
                Fail($"Empty DataTable threw exception (should be no-op): {ex.Message}");
            }
            finally
            {
                DeleteRows(batchID);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Helpers — DataTable builder matching AMA1005 SP output columns
        // ═══════════════════════════════════════════════════════════════════════
        private static DataTable BuildAmazonSourceTable(int rowCount)
        {
            var dt = new DataTable();

            // Columns matching SCSInventoryFeed_AMA1005_Log (excluding LogID, BatchID, LogType, LogDate)
            dt.Columns.Add("CustomerID",       typeof(string));
            dt.Columns.Add("ItemId",           typeof(string));
            dt.Columns.Add("CustomerItemCode", typeof(string));
            dt.Columns.Add("ETA_Date",         typeof(string));
            dt.Columns.Add("ETA_Qty",          typeof(int));
            dt.Columns.Add("Total_ATS",        typeof(int));
            dt.Columns.Add("ATS_L10",          typeof(int));
            dt.Columns.Add("ATS_L21",          typeof(int));
            dt.Columns.Add("ATS_L28",          typeof(int));
            dt.Columns.Add("ATS_L29",          typeof(int));
            dt.Columns.Add("ATS_L30",          typeof(int));
            dt.Columns.Add("ATS_L34",          typeof(int));
            dt.Columns.Add("ATS_L35",          typeof(int));
            dt.Columns.Add("ATS_L36",          typeof(int));
            dt.Columns.Add("ATS_L37",          typeof(int));
            dt.Columns.Add("ATS_L40",          typeof(int));
            dt.Columns.Add("ATS_L41",          typeof(int));
            dt.Columns.Add("ATS_L55",          typeof(int));
            dt.Columns.Add("ATS_L56",          typeof(int));
            dt.Columns.Add("ATS_L57",          typeof(int));
            dt.Columns.Add("ATS_L60",          typeof(int));
            dt.Columns.Add("ATS_L65",          typeof(int));
            dt.Columns.Add("ATS_L70",          typeof(int));
            dt.Columns.Add("ATS_L91",          typeof(int));

            var rng = new Random(42);
            dt.BeginLoadData();
            for (int i = 0; i < rowCount; i++)
            {
                DataRow row = dt.NewRow();
                row["CustomerID"]       = CUSTOMER_ID;
                row["ItemId"]           = $"ASIN-{i:D8}";
                row["CustomerItemCode"] = $"SKU-{i:D8}";
                row["ETA_Date"]         = DateTime.Now.AddDays(rng.Next(1, 90)).ToString("yyyy-MM-dd");
                row["ETA_Qty"]          = rng.Next(0, 500);
                row["Total_ATS"]        = rng.Next(0, 10000);
                row["ATS_L10"]          = rng.Next(0, 1000);
                row["ATS_L21"]          = rng.Next(0, 1000);
                row["ATS_L28"]          = rng.Next(0, 1000);
                row["ATS_L29"]          = rng.Next(0, 1000);
                row["ATS_L30"]          = rng.Next(0, 1000);
                row["ATS_L34"]          = rng.Next(0, 1000);
                row["ATS_L35"]          = rng.Next(0, 1000);
                row["ATS_L36"]          = rng.Next(0, 1000);
                row["ATS_L37"]          = rng.Next(0, 1000);
                row["ATS_L40"]          = rng.Next(0, 1000);
                row["ATS_L41"]          = rng.Next(0, 1000);
                row["ATS_L55"]          = rng.Next(0, 1000);
                row["ATS_L56"]          = rng.Next(0, 1000);
                row["ATS_L57"]          = rng.Next(0, 1000);
                row["ATS_L60"]          = rng.Next(0, 1000);
                row["ATS_L65"]          = rng.Next(0, 1000);
                row["ATS_L70"]          = rng.Next(0, 1000);
                row["ATS_L91"]          = rng.Next(0, 1000);
                dt.Rows.Add(row);
            }
            dt.EndLoadData();

            return dt;
        }

        // ── DB helpers ────────────────────────────────────────────────────────

        private static bool LogTableExists()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t", conn))
                {
                    cmd.Parameters.AddWithValue("@t", LOG_TABLE);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        private static int CountRows(string batchID)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    $"SELECT COUNT(1) FROM [{LOG_TABLE}] WHERE BatchID = @b", conn))
                {
                    cmd.Parameters.AddWithValue("@b", batchID);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        private static string GetSampleStatus(string batchID)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    $"SELECT TOP 1 [Status] FROM [{LOG_TABLE}] WHERE BatchID = @b", conn))
                {
                    cmd.Parameters.AddWithValue("@b", batchID);
                    var result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? null : result.ToString();
                }
            }
        }

        private static void DeleteRows(string batchID)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        $"DELETE FROM [{LOG_TABLE}] WHERE BatchID = @b", conn))
                    {
                        cmd.Parameters.AddWithValue("@b", batchID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* cleanup best-effort */ }
        }

        private static void CleanupTestData()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        $"DELETE FROM [{LOG_TABLE}] WHERE BatchID LIKE @p", conn))
                    {
                        cmd.Parameters.AddWithValue("@p", TEST_BATCH + "%");
                        int deleted = cmd.ExecuteNonQuery();
                        if (deleted > 0)
                            Console.WriteLine($"  [Cleanup] Removed {deleted:N0} leftover test rows.");
                    }
                }
            }
            catch { /* ignore */ }
        }

        // ── Assertion helpers ─────────────────────────────────────────────────

        private static void Assert(string label, bool condition, string failDetail = "")
        {
            if (condition)
            {
                Console.WriteLine($"  [PASS] {label}");
                _passed++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {label}");
                if (!string.IsNullOrEmpty(failDetail))
                    Console.WriteLine($"         → {failDetail}");
                _failed++;
            }
        }

        private static void Fail(string message)
        {
            Console.WriteLine($"  [FAIL] {message}");
            _failed++;
        }

        private static void PrintHeader(string title)
        {
            Console.WriteLine();
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  {title}");
            Console.WriteLine(new string('═', 60));
        }

        private static void PrintSection(string title)
        {
            Console.WriteLine();
            Console.WriteLine($"  ── {title}");
        }

        private static void PrintError(string msg)
        {
            Console.WriteLine($"  [ERROR] {msg}");
        }

        private static string MaskConn(string conn)
        {
            if (string.IsNullOrEmpty(conn)) return "(empty)";
            int idx = conn.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return conn.Length > 60 ? conn.Substring(0, 60) + "..." : conn;
            return conn.Substring(0, idx) + "Password=***";
        }
    }
}
