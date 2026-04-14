-- ============================================================
-- Test data for merged item-wise view
-- Inserts overlapping items across 4 AMA1005 download batches so we
-- can verify the dedup (ROW_NUMBER per ItemID) returned by the new
-- GetMergedDownloadItemsPaged endpoint works correctly.
--
-- IMPORTANT — based on real schema (verified via SqlMCP):
--   * SCSInventoryFeed PK is (CustomerID, ItemId) — ONE row per item.
--     The Total_ATS / ATS_* values come from this single row.
--   * SCSInventoryFeedData has many rows per item (one per batch),
--     joined by VW_BatchWiseInventory via MAX(Id) per (Cust,Item,Batch).
--   * SCSInventoryFeedData.Type is NOT NULL — using 'ERP-RVD' (received from ERP).
--
-- Batches used (from insert_test_inventory_data.sql):
--   B1 = 27528ce8-a106-47da-88a5-e7714e72c5f0  Full Feed       06:15
--   B2 = a2eba6bf-0ab6-4c38-9d76-b086b223caae  Differential    06:00
--   B3 = 7be1bf13-e289-4f4e-9951-f8415ecd7777  Differential    06:30
--   B4 = 0db9a618-e474-4eeb-9e18-6ff381374431  Differential    07:00
--
-- Items inserted:
--   SKU-MERGE-001 → only in B1                    → 1 raw row
--   SKU-MERGE-002 → in B1 and B3                  → 2 raw rows
--   SKU-MERGE-003 → in B1 and B4                  → 2 raw rows
--   SKU-MERGE-004 → only in B3                    → 1 raw row
--   SKU-MERGE-005 → in B2 and B4                  → 2 raw rows
--
-- Raw rows in VW_BatchWiseInventory: 8
-- After merge (deduped by ItemID): 5 unique items
-- ============================================================

DECLARE @B1 NVARCHAR(50) = '27528ce8-a106-47da-88a5-e7714e72c5f0';  -- Full Feed
DECLARE @B2 NVARCHAR(50) = 'a2eba6bf-0ab6-4c38-9d76-b086b223caae';  -- Diff 06:00
DECLARE @B3 NVARCHAR(50) = '7be1bf13-e289-4f4e-9951-f8415ecd7777';  -- Diff 06:30
DECLARE @B4 NVARCHAR(50) = '0db9a618-e474-4eeb-9e18-6ff381374431';  -- Diff 07:00
DECLARE @CUST VARCHAR(50) = 'AMA1005';

-- ============================================================
-- Cleanup any previous run (idempotent)
-- ============================================================
DELETE FROM SCSInventoryFeedData
WHERE CustomerId = @CUST
  AND ItemId IN ('SKU-MERGE-001','SKU-MERGE-002','SKU-MERGE-003','SKU-MERGE-004','SKU-MERGE-005');

DELETE FROM SCSInventoryFeed
WHERE CustomerID = @CUST
  AND ItemId IN ('SKU-MERGE-001','SKU-MERGE-002','SKU-MERGE-003','SKU-MERGE-004','SKU-MERGE-005');

-- ============================================================
-- 1. Master rows — SCSInventoryFeed (one per item)
--    Total_ATS / ATS_L* shown here will be what appears in the merged view
--    (latest known state of the item in eSyncMate).
-- ============================================================
INSERT INTO SCSInventoryFeed
    (CustomerID, ItemId, CustomerItemCode, Total_ATS, ATS_L10, ATS_L21, ATS_L28, ATS_L30,
     Status, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
VALUES
    (@CUST, 'SKU-MERGE-001', 'CIC-001', 100,  20, 25, 30, 35, 'SYNCED', '2026-04-13 06:15:00', 1, '2026-04-13 06:15:30', 1),
    (@CUST, 'SKU-MERGE-002', 'CIC-002',  75,  15, 20, 25, 30, 'SYNCED', '2026-04-13 06:15:00', 1, '2026-04-13 06:30:30', 1),
    (@CUST, 'SKU-MERGE-003', 'CIC-003', 180,  40, 45, 50, 55, 'SYNCED', '2026-04-13 06:15:00', 1, '2026-04-13 07:00:30', 1),
    (@CUST, 'SKU-MERGE-004', 'CIC-004',  30,   5, 10, 12, 15, 'SYNCED', '2026-04-13 06:30:00', 1, '2026-04-13 06:30:30', 1),
    (@CUST, 'SKU-MERGE-005', 'CIC-005',  15,   3,  5,  7, 10, 'SYNCED', '2026-04-13 06:00:00', 1, '2026-04-13 07:00:30', 1);

-- ============================================================
-- 2. Detail rows — SCSInventoryFeedData
--    8 raw rows total, distributed across the 4 batches.
--    Same item can appear in multiple batches (the whole point of the merge test).
-- ============================================================
INSERT INTO SCSInventoryFeedData
    (CustomerId, ItemId, [Type], Data, BatchID, CreatedDate, CreatedBy)
VALUES
    -- SKU-MERGE-001 → only in B1 (Full Feed)
    (@CUST, 'SKU-MERGE-001', 'ERP-RVD', '{"ats":100}', @B1, '2026-04-13 06:15:30', 1),

    -- SKU-MERGE-002 → in B1 (06:15) then B3 (06:30) — B3 should win
    (@CUST, 'SKU-MERGE-002', 'ERP-RVD', '{"ats":50}',  @B1, '2026-04-13 06:15:30', 1),
    (@CUST, 'SKU-MERGE-002', 'ERP-RVD', '{"ats":75}',  @B3, '2026-04-13 06:30:30', 1),

    -- SKU-MERGE-003 → in B1 (06:15) then B4 (07:00) — B4 should win
    (@CUST, 'SKU-MERGE-003', 'ERP-RVD', '{"ats":200}', @B1, '2026-04-13 06:15:30', 1),
    (@CUST, 'SKU-MERGE-003', 'ERP-RVD', '{"ats":180}', @B4, '2026-04-13 07:00:30', 1),

    -- SKU-MERGE-004 → only in B3
    (@CUST, 'SKU-MERGE-004', 'ERP-RVD', '{"ats":30}',  @B3, '2026-04-13 06:30:30', 1),

    -- SKU-MERGE-005 → in B2 (06:00) then B4 (07:00) — B4 should win
    (@CUST, 'SKU-MERGE-005', 'ERP-RVD', '{"ats":10}',  @B2, '2026-04-13 06:00:30', 1),
    (@CUST, 'SKU-MERGE-005', 'ERP-RVD', '{"ats":15}',  @B4, '2026-04-13 07:00:30', 1);

PRINT '8 detail rows + 5 master rows inserted for AMA1005 across 4 batches.';
PRINT '';

-- ============================================================
-- VERIFICATION 1: Raw rows in VW_BatchWiseInventory (BEFORE merge)
-- Expect 8 rows — the same items appear in multiple batches.
-- ============================================================
PRINT '--- BEFORE MERGE (raw VW_BatchWiseInventory) — expect 8 rows ---';
SELECT BatchID, ItemId, Total_ATS, ModifiedDate, Id
FROM VW_BatchWiseInventory WITH (NOLOCK)
WHERE CustomerID = @CUST
  AND ItemId LIKE 'SKU-MERGE-%'
ORDER BY ItemId, ModifiedDate, Id;

-- ============================================================
-- VERIFICATION 2: Merged result — EXACT SQL the new endpoint runs
-- Expect 5 unique items (one per ItemID).
-- ============================================================
PRINT '';
PRINT '--- AFTER MERGE (CTE dedup) — expect 5 unique items ---';
WITH MergedItems AS (
    SELECT *,
           ROW_NUMBER() OVER (
               PARTITION BY ItemId
               ORDER BY ISNULL(ModifiedDate, CreatedDate) DESC, Id DESC
           ) AS rn
    FROM VW_BatchWiseInventory WITH (NOLOCK)
    WHERE BatchID IN (@B1, @B2, @B3, @B4)
      AND ItemId LIKE 'SKU-MERGE-%'
)
SELECT BatchID, ItemId, Total_ATS, ModifiedDate, Id
FROM MergedItems
WHERE rn = 1
ORDER BY ItemId;

/*
EXPECTED MERGED RESULT — 5 rows:
+--------------+----------+-----------+
|  ItemId      | Total_ATS|  Notes    |
+--------------+----------+-----------+
| SKU-MERGE-001|   100    | only Full |
| SKU-MERGE-002|    75    | latest=B3 |
| SKU-MERGE-003|   180    | latest=B4 |
| SKU-MERGE-004|    30    | only B3   |
| SKU-MERGE-005|    15    | latest=B4 |
+--------------+----------+-----------+

Note: Total_ATS comes from SCSInventoryFeed (one row per item, latest known state).
The dedup proves the same item won't show twice — that's the user-visible result.
*/

-- ============================================================
-- CLEANUP (run after testing)
-- ============================================================
-- DELETE FROM SCSInventoryFeedData
-- WHERE CustomerId = 'AMA1005' AND ItemId LIKE 'SKU-MERGE-%';
--
-- DELETE FROM SCSInventoryFeed
-- WHERE CustomerID = 'AMA1005' AND ItemId LIKE 'SKU-MERGE-%';
