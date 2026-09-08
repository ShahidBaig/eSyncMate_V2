/*==============================================================================
  Task   : 00001 - Re-Map Item IDs action for Amazon error orders
  Script : Verify_AffectedOrders.sql   (READ-ONLY - not a deployment script)
  Date   : 2026-09-03
  Purpose: Sizing and test-data queries. Answers "how many Amazon error orders
           would this action actually fix, and which lines are ambiguous".
           Run these BEFORE and AFTER using the action to confirm the effect.
==============================================================================*/

SET NOCOUNT ON;

/*---- 1. Amazon customers (the only ones this action applies to) ------------
    Gate used by the code: ERPCustomerID is in ApplicationSettings.AmazonCustomerIds.
    Marketplace text is shown for reference only - it is free text and is NOT
    the authority.                                                          */
SELECT  C.Id, C.Name, C.ERPCustomerID, C.Marketplace,
        IsAmazonCustomer =
            CASE WHEN C.ERPCustomerID IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT((SELECT TagValue FROM ApplicationSettings WHERE TagName = 'AmazonCustomerIds'), ','))
                 THEN 1 ELSE 0 END
FROM    Customers C
ORDER BY IsAmazonCustomer DESC, C.ERPCustomerID;

/*---- 2. Amazon error orders, and how many of their lines are unmapped ------
    "Unmapped" = the OrderDetail.ItemID is not an ItemId in this customer's
    SCSInventoryFeed, i.e. the ingestion fallback wrote the raw Seller SKU.   */
SELECT  O.Id AS OrderId, O.OrderNumber, O.Status, C.ERPCustomerID, O.OrderDate,
        TotalLines   = COUNT(DISTINCT D.order_line_id),
        UnmappedLines = COUNT(DISTINCT CASE WHEN F.ItemId IS NULL THEN D.order_line_id END)
FROM    Orders O
        INNER JOIN Customers   C ON C.Id = O.CustomerId
        INNER JOIN OrderDetail D ON D.OrderId = O.Id
        LEFT  JOIN SCSInventoryFeed F
               ON F.CustomerID = C.ERPCustomerID AND F.ItemId = D.ItemID
WHERE   O.Status IN ('ERROR', 'ASNERROR', 'ACKERROR')
  AND   C.ERPCustomerID IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT((SELECT TagValue FROM ApplicationSettings WHERE TagName = 'AmazonCustomerIds'), ','))
GROUP BY O.Id, O.OrderNumber, O.Status, C.ERPCustomerID, O.OrderDate
HAVING  COUNT(DISTINCT CASE WHEN F.ItemId IS NULL THEN D.order_line_id END) > 0
ORDER BY O.OrderDate DESC;

/*---- 3. Would the re-map actually find a match? ----------------------------
    Treats the current ItemID as the Seller SKU (the ingestion fallback case)
    and looks it up the way the action will. Anything with MatchCount = 0 is a
    SKU genuinely missing from the feed - the action cannot fix those.        */
SELECT  C.ERPCustomerID,
        SellerSku   = D.ItemID,
        MatchCount  = COUNT(F.ItemId),
        ResolvedIds = STRING_AGG(F.ItemId, ', '),
        Verdict     = CASE WHEN COUNT(F.ItemId) = 0 THEN 'NOT IN FEED - cannot fix'
                           WHEN COUNT(F.ItemId) = 1 THEN 'OK - single match'
                           ELSE 'AMBIGUOUS - multiple ItemIds' END,
        Orders      = COUNT(DISTINCT D.OrderId)
FROM    Orders O
        INNER JOIN Customers   C ON C.Id = O.CustomerId
        INNER JOIN OrderDetail D ON D.OrderId = O.Id
        LEFT  JOIN SCSInventoryFeed F
               ON F.CustomerID = C.ERPCustomerID AND F.CustomerItemCode = D.ItemID
WHERE   O.Status IN ('ERROR', 'ASNERROR', 'ACKERROR')
  AND   C.ERPCustomerID IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT((SELECT TagValue FROM ApplicationSettings WHERE TagName = 'AmazonCustomerIds'), ','))
GROUP BY C.ERPCustomerID, D.ItemID
ORDER BY MatchCount, Orders DESC;

/*---- 4. Feed rows where one Seller SKU maps to several ERP items -----------
    The PK is (CustomerID, ItemId, CustomerItemCode), so this is ALLOWED. The
    ingestion route silently takes the first row; the new action must refuse
    these instead of guessing.                                                */
SELECT  CustomerID, CustomerItemCode,
        ItemIdCount = COUNT(*),
        ItemIds     = STRING_AGG(ItemId, ', ')
FROM    SCSInventoryFeed
GROUP BY CustomerID, CustomerItemCode
HAVING  COUNT(*) > 1
ORDER BY ItemIdCount DESC;
