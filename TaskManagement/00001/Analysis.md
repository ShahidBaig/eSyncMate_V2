# Task 00001 — Analysis

Date: 2026-09-03 · Scope: Amazon customers only

## 1. The mapping already exists — but only at ingestion

`eSyncMate.Processor/Managers/AmazonGetOrdersRoute.cs:290-303`

```csharp
var sku = orderLine?.SellerSKU?.Trim();
DataRow row = p_SCSInventoryFeed.Select($"CustomerItemCode = '{sku}'").FirstOrDefault();
if (row != null)  orderLine.ItemID = row["ItemID"];
else              orderLine.ItemID = sku;      // ← the fallback that creates the error order
```

The feed is loaded once per run at line 131:

```sql
SELECT * FROM SCSInventoryFeed WHERE CustomerID = '{l_SourceConnector.CustomerID}'
```

…from the **destination** connector's connection — the same database the Orders live
in. So the new action needs no cross-database work.

### The mapping table

| `SCSInventoryFeed` column | Meaning |
|---|---|
| `CustomerID` | ERPCustomerID, e.g. `AMA1005` |
| `CustomerItemCode` | the partner's Seller SKU |
| `ItemId` | the ERP item code |

PK is `(CustomerID, ItemId, CustomerItemCode)`.

**The action is a re-run of the ingestion lookup, after the fact** — for orders that
arrived before the SKU existed in the feed.

## 2. Why orders end up with a bad ItemID — four causes

1. **SKU not in the feed yet.** The item was added to `SCSInventoryFeed` after the
   order arrived. This is the case the action fixes, and the common one.
2. **The feed load failed silently.** `AmazonGetOrdersRoute.cs:133-137` wraps
   `GetData` in `catch (Exception) { }` with an empty body. If that query throws,
   `p_SCSInventoryFeedDt` stays empty and **every line in that entire run** falls
   back to the raw SKU, with no log entry at all. If the error orders cluster by
   run, this is the signature.
3. **A SKU containing an apostrophe** breaks `DataTable.Select` — the filter is
   string-concatenated. Same class of bug as the Knot `sf-` prefix issue.
4. **Ambiguous mapping.** The PK permits one `CustomerItemCode` to map to several
   `ItemId` rows. `.FirstOrDefault()` picks one arbitrarily, with no ordering.
   The new action must refuse these rather than repeat the silent guess.

## 3. Where the Seller SKU should come from

| Source | Verdict |
|---|---|
| `OrderDetail.ItemID` itself | Works only for causes 1 and 2, where the fallback left the raw SKU in place. Useless if a *wrong* mapping was applied. |
| `OrderData` row `Type = 'API-JSON'` → `OrderDetail.payload.OrderItems[].SellerSKU` | **Recommended.** Always holds Amazon's original SKU regardless of what was written to OrderDetail. |
| Re-call Amazon `/orders/v0/orders/{id}/orderItems` | Correct but slow, needs OAuth, and pointless — it is already stored. |

Join key: `OrderItems[].OrderItemId` ↔ `OrderDetail.order_line_id`. Exact.
Do **not** join on line position — the Target ASN work already proved positional
assumptions break (`order_line_number` came back as 17, 12, 22, 16, 7, 18).

## 4. What the ERP actually reads — the API-JSON, not OrderDetail

**Correction to an earlier draft of this document.** It claimed updating `OrderDetail.ItemID`
was sufficient. That was read off the *ASN* branches of `SP_OrdersData` and wrongly
generalised. The order-placement path is different:

1. `SP_OrdersData`, first branch (`@l_OrderDataStatus = '@ORDERDATASTATUS@'`, line 40)
   returns **`OD.Data`** — the raw API-JSON blob.
2. `SCSPlaceOrderRoute.cs:257-260` takes that as `Body` and runs it through the JUST
   transformation named by `route.MapId`.
3. Map 6, *Amazon Order Transformation* (route 89, `Amazon - Place Orders in ERP`):

```json
"detail": {
  "#loop($.OrderDetail.payload.OrderItems)": {
    "ItemID": "#currentvalueatpath($.ItemID)",
    "OrderQty": "#currentvalueatpath($.QuantityOrdered)",
    "APIOrderLineNo": "#currentvalueatpath($.LineNo)"
  }
}
```

So the ItemID the ERP receives comes **straight out of
`OrderDetail.payload.OrderItems[].ItemID` inside the API-JSON**. Updating that field is
exactly what makes the order post — and nothing else is needed for it.

### Confirmed JSON shape (OrderData Id 73, order 113-6186437-1324206, AMA1005, ERROR)

```json
"OrderDetail": { "payload": { "AmazonOrderId": "113-6186437-1324206", "OrderItems": [
  { "ASIN": "B08KTNXPQJ",
    "SellerSKU": "B08KTNXPQJ CY8452-52821-3",
    "QuantityOrdered": 1,
    "OrderItemId": "165200668044921",
    "LineNo": 1,
    "ItemID": "CY8452-52821-3" } ] } }
```

Matching feed row: `CustomerID 'ama1005'`, `CustomerItemCode 'B08KTNXPQJ CY8452-52821-3'`,
`ItemId 'CY8452-52821-3'`. So the Amazon Seller SKU is `ASIN + space + item code`, and
`CustomerItemCode` holds it verbatim.

### But `OrderDetail.ItemID` still matters later

`SP_OrdersData` line 115-120 — the **AMA1005 ASN branch** — selects `MAX(D.ItemID)` from
`OrderDetail`. So an order fixed only in the JSON will post to the ERP correctly and then,
when it ships, send an ASN back to Amazon still carrying the raw Seller SKU as its ItemID.

Updating both in the same call is a small addition. **Not implemented — out of the agreed
scope, needs a decision.**

### Two things the implementation must respect

- **Edit the JSON as a `JObject`, never via the typed model.** `AmazonOrder` does not
  declare `WarehouseCode`, `ShippingCode`, `ShippingAgentCode` or `ShipDate`, all of which
  are present at the root of the stored payload. Deserialise-and-reserialise would drop them.
- **Look the SKUs up in SQL.** Ingestion pulls the whole feed into a `DataTable` — that is
  248,196 rows for AMA1005. For a single order, filter in SQL on the handful of SKUs.

## 5. Amazon-only gate — no view change needed

`VW_Orders` is `SELECT O.*, C.Name CustomerName, C.ERPCustomerID` — there is **no
Marketplace column on the grid row**. But `getERPCustomers`
(`ProductUploadPricesController.cs:380`) returns `CustomerDataModel`, which already
carries `Marketplace`, and `orders.component.ts:503` already loads it into
`customerOptions`. So the UI can gate client-side with no backend or view change.

Server-side authority is the route check, mirroring `ReTransmitASN`
(`OrdersController.cs:1432`):

```sql
SELECT TOP 1 TypeId FROM Routes WHERE CustomerName = @erpCustomerId AND TypeId = 49
```

`RouteTypesEnum.AmazonGetOrders = 49` (`CommonUtils.cs:165`).

Dev state 2026-09-03:

| ERPCustomerID | Marketplace | Has TypeId 49 |
|---|---|---|
| AMA1005 | AMAZON SELLER CENTRAL | yes |
| AMA1006 | AMAZON MASTER WEAVERS OF AMERICA | yes |
| AMA1000 | Amazon Pacific Rugs | **no routes at all** |

AMA1000 is unconfigured, so excluding it is correct — it cannot have Amazon orders.
`Marketplace` is free text and must stay reference-only.

## 6. Decisions

**Settled and implemented in the endpoint:**

1. **The action does not re-process.** Fixing the ItemID leaves the order in ERROR and the
   response says so. Re-Process stays a separate, visible click — a silent chain would hide
   which step failed.
2. **An ambiguous SKU is refused, not guessed.** Where one `CustomerItemCode` maps to more
   than one `ItemId`, that line is reported as `AMBIGUOUS - maps to X, Y` and left untouched.
   Dev has exactly one such SKU across AMA1005's 248,196 rows (248,195 distinct), so this is
   rare — but ingestion's silent `FirstOrDefault` is not repeated.
3. **Single order.** Matches the existing per-row actions.

**Still open:**

4. **Should `OrderDetail.ItemID` be updated too?** Needed for the ASN that goes back to
   Amazon after shipping — see §4. Not in the agreed scope, so not implemented.
5. **Should `SCSInventoryFeed.Status` filter the lookup?** Ingestion ignores it today. If
   inactive rows exist, the action could "fix" an order onto a dead item.
6. **A bulk variant?** If these errors arrive in batches (cause 2), a per-order button means
   a lot of clicking. "Re-map all error orders for this customer" is cheap on the same
   endpoint.

## 7. Worth fixing alongside

The empty `catch (Exception) { }` at `AmazonGetOrdersRoute.cs:133-137`. As written,
a transient failure loading the feed turns an entire run's orders into error orders
with nothing in the log. A `route.SaveLog(LogTypeEnum.Error, ...)` there is a
one-line change, independent of the new action.

## 8. Note on scope

`SCSInventoryFeed` is keyed by `CustomerID`, not by partner — every ingestion route
has this same failure mode. Building the action Amazon-only (as agreed) means
rebuilding it for Walmart or Target later. Keeping the endpoint's core logic
partner-neutral and only the *gate* Amazon-specific costs nothing now and avoids
that.
