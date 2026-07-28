/* ------------------------------------------------------------------
   54 — TargetPlusShipNodes: uniqueness for the Setup > Ship Nodes screen

   Rule: for one customer, a warehouse maps to exactly one ship node,
         and a ship node belongs to exactly one warehouse.

   Verified on eSyncmate_Dev (108 rows): zero duplicates on either pair,
   so both indexes create cleanly. Re-check on production before running.
   ------------------------------------------------------------------ */

-- Pre-flight: both queries must return 0 rows
SELECT CustomerID, WHSID, COUNT(*) AS Cnt
FROM TargetPlusShipNodes
GROUP BY CustomerID, WHSID HAVING COUNT(*) > 1;

SELECT CustomerID, ShipNode, COUNT(*) AS Cnt
FROM TargetPlusShipNodes
GROUP BY CustomerID, ShipNode HAVING COUNT(*) > 1;
GO

-- One warehouse per customer can carry only one ship node
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TargetPlusShipNodes_Customer_WHSID' AND object_id = OBJECT_ID('dbo.TargetPlusShipNodes'))
BEGIN
    CREATE UNIQUE INDEX UX_TargetPlusShipNodes_Customer_WHSID
        ON dbo.TargetPlusShipNodes (CustomerID, WHSID)
        WHERE CustomerID IS NOT NULL AND WHSID IS NOT NULL;
END
GO

-- The same ship node cannot be reused on another warehouse of that customer
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TargetPlusShipNodes_Customer_ShipNode' AND object_id = OBJECT_ID('dbo.TargetPlusShipNodes'))
BEGIN
    CREATE UNIQUE INDEX UX_TargetPlusShipNodes_Customer_ShipNode
        ON dbo.TargetPlusShipNodes (CustomerID, ShipNode)
        WHERE CustomerID IS NOT NULL AND ShipNode IS NOT NULL;
END
GO

-- Lookup index for the list screen (filter by customer, sort by warehouse)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TargetPlusShipNodes_CustomerID' AND object_id = OBJECT_ID('dbo.TargetPlusShipNodes'))
BEGIN
    CREATE INDEX IX_TargetPlusShipNodes_CustomerID
        ON dbo.TargetPlusShipNodes (CustomerID) INCLUDE (WHSID, ShipNode);
END
GO

-- Verify
SELECT i.name AS IndexName, i.is_unique, i.has_filter,
       STUFF((SELECT ', ' + c.name
              FROM sys.index_columns ic
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
              ORDER BY ic.key_ordinal
              FOR XML PATH('')), 1, 2, '') AS KeyColumns
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.TargetPlusShipNodes') AND i.name IS NOT NULL;

/* ---- Rollback ----
DROP INDEX UX_TargetPlusShipNodes_Customer_WHSID ON dbo.TargetPlusShipNodes;
DROP INDEX UX_TargetPlusShipNodes_Customer_ShipNode ON dbo.TargetPlusShipNodes;
DROP INDEX IX_TargetPlusShipNodes_CustomerID ON dbo.TargetPlusShipNodes;
*/
