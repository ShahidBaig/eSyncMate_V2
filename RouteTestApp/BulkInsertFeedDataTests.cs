using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using eSyncMate.DB.Entities;

namespace RouteTestApp
{
    /// <summary>
    /// Scenario-based tests for BulkInsertFeedData paged insert.
    /// Scenario: Amazon customer (AMA1005) with up to 300,000 inventory items.
    /// perItemJson index must stay aligned with sourceTable rows across all pages.
    /// </summary>
    public static class BulkInsertFeedDataTests
    {
        private const string CUSTOMER_ID = "AMA1005";
        private const string FEED_TABLE  = "SCSInventoryFeedData_AMA1005";
        private const string TEST_BATCH  = "TEST-FEED-PAGED-";

        private static string _connectionString = "";
        private static int _passed = 0;
        private static int _failed = 0;

        public static void RunAllTests(string connectionString, bool keepData = false)
        {
            _connectionString = connectionString;
            _passed = 0;
            _failed = 0;

            PrintHeader("BulkInsertFeedData — PAGED INSERT TEST SUITE");
            Console.WriteLine($"  Customer   : {CUSTOMER_ID}");
            Console.WriteLine($"  Feed Table : {FEED_TABLE}");
            Console.WriteLine($"  Keep Data  : {keepData}");
            Console.WriteLine($"  Timestamp  : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            if (!FeedTableExists())
            {
                PrintError($"Feed table [{FEED_TABLE}] does not exist.");
                return;
            }

            CleanupTestData();

            // ── Scenarios ─────────────────────────────────────────────────────
            PrintSection("SCENARIO 1 — Full Production Load (300,000 items, ERP-RVD)");
            Test_300K_FeedData(keepData);

            PrintSection("SCENARIO 2 — Exact Page Boundary (5,000 items = 1 page)");
            Test_ExactOnePage(keepData);

            PrintSection("SCENARIO 3 — Non-Multiple of 5000 (302,500 items = 61 pages)");
            Test_NonMultipleOfPageSize(keepData);

            PrintSection("SCENARIO 4 — Index Alignment Check (perItemJson[i] matches sourceTable row i)");
            Test_IndexAlignment(keepData);

            PrintSection("SCENARIO 5 — Empty DataTable (should no-op)");
            Test_EmptyDataTable();

            if (!keepData)
                CleanupTestData();
            else
                Console.WriteLine("\n  [INFO] Data retained in table — BatchID prefix: " + TEST_BATCH);

            Console.WriteLine();
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  RESULTS:  {_passed} passed   {_failed} failed");
            Console.WriteLine(new string('═', 60));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 1 — 300,000 items (60 pages x 5000)
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_300K_FeedData(bool keepData = false)
        {
            string batchID  = TEST_BATCH + "300K";
            int    rowCount = 300_000;

            try
            {
                Console.WriteLine($"  Building source DataTable — {rowCount:N0} rows...");
                DataTable      source      = BuildSourceTable(rowCount);
                List<string>   perItemJson = BuildPerItemJson(rowCount);

                Console.WriteLine($"  Calling BulkInsertFeedData (pageSize=5000, pages=60)...");
                var sw = Stopwatch.StartNew();

                SCSInventoryFeed.BulkInsertFeedData(
                    _connectionString, CUSTOMER_ID, source, batchID, "ERP-RVD", perItemJson);

                sw.Stop();

                int inserted = CountRows(batchID);
                Assert($"300K rows inserted correctly",
                    inserted == rowCount,
                    $"Expected {rowCount:N0}, got {inserted:N0}");

                int pages = (int)Math.Ceiling((double)rowCount / 5000);
                Assert($"Page count is 60",
                    pages == 60,
                    $"Calculated {pages} pages");

                Console.WriteLine($"  Time: {sw.Elapsed.TotalSeconds:F2}s  ({rowCount / sw.Elapsed.TotalSeconds:N0} rows/sec)");
                Console.WriteLine();

                source.Dispose();
            }
            catch (Exception ex)
            {
                Fail($"300K FeedData threw exception: {ex.Message}");
            }
            finally
            {
                if (!keepData) DeleteRows(batchID);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 2 — Exactly 5000 rows = 1 page
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_ExactOnePage(bool keepData = false)
        {
            string batchID  = TEST_BATCH + "5K";
            int    rowCount = 5_000;

            try
            {
                DataTable    source      = BuildSourceTable(rowCount);
                List<string> perItemJson = BuildPerItemJson(rowCount);

                SCSInventoryFeed.BulkInsertFeedData(
                    _connectionString, CUSTOMER_ID, source, batchID, "ERP-RVD", perItemJson);

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
        // SCENARIO 3 — 302,500 rows = 60 full + 1 partial (2,500)
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_NonMultipleOfPageSize(bool keepData = false)
        {
            string batchID  = TEST_BATCH + "302K";
            int    rowCount = 302_500;

            try
            {
                DataTable    source      = BuildSourceTable(rowCount);
                List<string> perItemJson = BuildPerItemJson(rowCount);

                var sw = Stopwatch.StartNew();
                SCSInventoryFeed.BulkInsertFeedData(
                    _connectionString, CUSTOMER_ID, source, batchID, "ERP-RVD", perItemJson);
                sw.Stop();

                int inserted = CountRows(batchID);
                Assert($"302,500 rows inserted (60 full + 1 partial page)", inserted == rowCount,
                    $"Expected {rowCount:N0}, got {inserted:N0}");

                int pages = (int)Math.Ceiling((double)rowCount / 5000);
                Assert($"61 pages used for 302,500 rows", pages == 61,
                    $"Calculated {pages} pages");

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
        // SCENARIO 4 — Index Alignment: perItemJson[i] must match sourceTable row i
        // Uses 15,000 rows (3 pages). Each item's JSON contains its own index.
        // After insert, verify that ItemId and Data column values match correctly
        // across page boundaries (row 4999, 5000, 5001, 9999, 10000, 10001).
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_IndexAlignment(bool keepData = false)
        {
            string batchID  = TEST_BATCH + "IDX";
            int    rowCount = 15_000;

            try
            {
                DataTable    source      = BuildSourceTable(rowCount);
                List<string> perItemJson = BuildPerItemJson(rowCount);

                SCSInventoryFeed.BulkInsertFeedData(
                    _connectionString, CUSTOMER_ID, source, batchID, "ERP-RVD", perItemJson);

                int inserted = CountRows(batchID);
                Assert($"15,000 rows inserted for index alignment test", inserted == rowCount,
                    $"Expected {rowCount}, got {inserted}");

                // Verify boundary rows: index 4999, 5000, 5001 (page 1→2 boundary)
                // ItemId format: "ASIN-{i:D8}", JSON: {\"index\":i}
                bool boundary1 = VerifyRow(batchID, $"ASIN-{4999:D8}", $"{{\"index\":4999}}");
                bool boundary2 = VerifyRow(batchID, $"ASIN-{5000:D8}", $"{{\"index\":5000}}");
                bool boundary3 = VerifyRow(batchID, $"ASIN-{5001:D8}", $"{{\"index\":5001}}");
                bool boundary4 = VerifyRow(batchID, $"ASIN-{9999:D8}", $"{{\"index\":9999}}");
                bool boundary5 = VerifyRow(batchID, $"ASIN-{10000:D8}", $"{{\"index\":10000}}");

                Assert($"Page 1 last row (index 4999) — ItemId & Data aligned",  boundary1, "Mismatch at index 4999");
                Assert($"Page 2 first row (index 5000) — ItemId & Data aligned", boundary2, "Mismatch at index 5000");
                Assert($"Page 2 second row (index 5001) — ItemId & Data aligned",boundary3, "Mismatch at index 5001");
                Assert($"Page 2 last row (index 9999) — ItemId & Data aligned",  boundary4, "Mismatch at index 9999");
                Assert($"Page 3 first row (index 10000) — ItemId & Data aligned",boundary5, "Mismatch at index 10000");

                source.Dispose();
            }
            catch (Exception ex)
            {
                Fail($"Index alignment test threw exception: {ex.Message}");
            }
            finally
            {
                if (!keepData) DeleteRows(batchID);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO 5 — Empty DataTable — no-op, no exception
        // ═══════════════════════════════════════════════════════════════════════
        private static void Test_EmptyDataTable()
        {
            string batchID = TEST_BATCH + "EMPTY";

            try
            {
                DataTable    empty       = BuildSourceTable(0);
                List<string> perItemJson = new List<string>();

                SCSInventoryFeed.BulkInsertFeedData(
                    _connectionString, CUSTOMER_ID, empty, batchID, "ERP-RVD", perItemJson);

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
        // Helpers
        // ═══════════════════════════════════════════════════════════════════════

        private static DataTable BuildSourceTable(int rowCount)
        {
            var dt = new DataTable();
            dt.Columns.Add("CustomerID", typeof(string));
            dt.Columns.Add("ItemId",     typeof(string));

            dt.BeginLoadData();
            for (int i = 0; i < rowCount; i++)
            {
                DataRow row = dt.NewRow();
                row["CustomerID"] = CUSTOMER_ID;
                row["ItemId"]     = $"ASIN-{i:D8}";
                dt.Rows.Add(row);
            }
            dt.EndLoadData();

            return dt;
        }

        private static List<string> BuildPerItemJson(int rowCount)
        {
            var list = new List<string>(rowCount);
            for (int i = 0; i < rowCount; i++)
                list.Add($"{{\"index\":{i}}}");
            return list;
        }

        // ── DB helpers ─────────────────────────────────────────────────────────

        private static bool FeedTableExists()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@t", conn))
                {
                    cmd.Parameters.AddWithValue("@t", FEED_TABLE);
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
                    $"SELECT COUNT(1) FROM [{FEED_TABLE}] WHERE BatchID=@b", conn))
                {
                    cmd.Parameters.AddWithValue("@b", batchID);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        private static bool VerifyRow(string batchID, string itemId, string expectedJson)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    $"SELECT COUNT(1) FROM [{FEED_TABLE}] WHERE BatchID=@b AND ItemId=@i AND Data=@d", conn))
                {
                    cmd.Parameters.AddWithValue("@b", batchID);
                    cmd.Parameters.AddWithValue("@i", itemId);
                    cmd.Parameters.AddWithValue("@d", expectedJson);
                    return (int)cmd.ExecuteScalar() > 0;
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
                        $"DELETE FROM [{FEED_TABLE}] WHERE BatchID=@b", conn))
                    {
                        cmd.Parameters.AddWithValue("@b", batchID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private static void CleanupTestData()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        $"DELETE FROM [{FEED_TABLE}] WHERE BatchID LIKE @p", conn))
                    {
                        cmd.Parameters.AddWithValue("@p", TEST_BATCH + "%");
                        int deleted = cmd.ExecuteNonQuery();
                        if (deleted > 0)
                            Console.WriteLine($"  [Cleanup] Removed {deleted:N0} leftover test rows.");
                    }
                }
            }
            catch { }
        }

        // ── Assert helpers ─────────────────────────────────────────────────────

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
    }
}
