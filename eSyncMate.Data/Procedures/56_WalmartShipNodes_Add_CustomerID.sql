/* ------------------------------------------------------------------
   56 — WalmartShipNodes: add CustomerID so both ship-node tables share one shape

   TargetPlusShipNodes : ID, WHSID, ShipNode, CustomerID
   WalmartShipNodes    : ID, WHSID, ShipNode, APIName   -> + CustomerID

   APIName stays untouched (the Walmart inventory routes still read it).
   Walmart trades under a single ERP customer: WAL4001MP.

   Run BEFORE deploying the Setup > Ship Nodes screen.
   ------------------------------------------------------------------ */

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.WalmartShipNodes') AND name = 'CustomerID')
BEGIN
    ALTER TABLE dbo.WalmartShipNodes ADD CustomerID VARCHAR(500) NULL;
END
GO

-- All existing rows belong to the Walmart marketplace customer
UPDATE dbo.WalmartShipNodes
SET CustomerID = 'WAL4001MP'
WHERE ISNULL(CustomerID, '') = '';
GO

-- Same uniqueness rules as TargetPlusShipNodes: one warehouse -> one node, per customer
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_WalmartShipNodes_Customer_WHSID' AND object_id = OBJECT_ID('dbo.WalmartShipNodes'))
BEGIN
    CREATE UNIQUE INDEX UX_WalmartShipNodes_Customer_WHSID
        ON dbo.WalmartShipNodes (CustomerID, WHSID)
        WHERE CustomerID IS NOT NULL AND WHSID IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_WalmartShipNodes_Customer_ShipNode' AND object_id = OBJECT_ID('dbo.WalmartShipNodes'))
BEGIN
    CREATE UNIQUE INDEX UX_WalmartShipNodes_Customer_ShipNode
        ON dbo.WalmartShipNodes (CustomerID, ShipNode)
        WHERE CustomerID IS NOT NULL AND ShipNode IS NOT NULL;
END
GO

-- Verify
SELECT ID, WHSID, ShipNode, APIName, CustomerID
FROM dbo.WalmartShipNodes
ORDER BY CustomerID, WHSID;

/* ---- Rollback ----
DROP INDEX UX_WalmartShipNodes_Customer_WHSID ON dbo.WalmartShipNodes;
DROP INDEX UX_WalmartShipNodes_Customer_ShipNode ON dbo.WalmartShipNodes;
ALTER TABLE dbo.WalmartShipNodes DROP COLUMN CustomerID;
*/
