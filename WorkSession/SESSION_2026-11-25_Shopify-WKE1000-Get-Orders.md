# Work Session: Shopify WKE1000 - Get Orders from the live store (walker-edison)
**Date**: 2026-11-25
**Status**: In Progress

## Objective
Next step after the inventory work of 2026-11-24: read orders from the live Shopify store into eSyncMate
(route 160 `Shopify - Get Orders`), later place them in SPARS (route 161, LIVE ERP - not part of this step).

## Tasks
- [x] Scripts: check + baseline, verify, revert
- [ ] User: GO_01 at the client, check connector 252 in the UI, one-order filter, Run Now, GO_02
- [ ] Decide with the client before route 161 is ever run (NetSuite / Celigo fulfils the store's orders today)

## Progress Log

### Step 1: Scripts written - `eSyncMate.Data/Tables/ShopifyScripts/2026-11-25_Get-Orders/`
- **Files created**:
  - `GO_01_Check_And_Baseline.sql`: read-only grids (token, routes type 82 / 9, connectors of the Get Orders
    route, orders now, baseline); with `@Apply = 1` records `ZDB_GO_Baseline` (customer + highest `Orders.Id`).
  - `GO_02_Verify_After_Run.sql` (read-only): route log, saved orders, lines with "item in the feed", routes.
  - `GO_99_Revert_Test_Orders.sql`: deletes the customer's orders above the baseline (dry run by default; stops
    when an order was processed further).
- **Code read** (`ShopifyGetOrdersRoute.cs`): the route only reads from Shopify. Default filter = paid / pending,
  unfulfilled, open, created in the last 10 days; connector Parameters `query` and `days` replace it. Line item =
  feed row by inventory item number, then by SKU, else the SKU itself (store SKU = our ItemId, so it fits).
- **Risks noted**: the live store shows 1,019 open orders - an unfiltered run saves every open order of the last
  10 days; connector 252 is encrypted (Base URL / Parameters only visible in the UI; its Base URL after the
  demo-store test was never confirmed); customer data (name, address) may be refused by Shopify for the app on
  a live store (protected customer data) - not tested; orders of this store are fulfilled by NetSuite / Celigo.
- **Not tested on dev. Not committed.**

### Step 2: GO_01 (check only) at the client - clean
- Token Id 2 (walker-edison) selected; routes 160 / 161 In-Active, no job; connector 252 encrypted, 253 plain;
  WKE1000 has 0 orders; no baseline; highest `Orders.Id` 786358.

### Step 3: Connector 252 checked and changed on the production screen (the user asked me to do the UI part)
- The user: I work on `https://ems.safavieh.com/eSyncMate` through Chrome (already signed in), they run the SQL.
- **Read in Setup > Connectors > Shopify - Get Orders (252)**: type Shopify / Rest, Auth `SHOPIFYGetToken`,
  Base URL `https://walker-edison.myshopify.com/admin/api/2026-10/`, URL `graphql.json`, POST, JSON, Customer ID
  WKE1000. Parameters before: `query` = `(financial_status:paid OR financial_status:pending)
  fulfillment_status:unfulfilled status:open`, `days` = `10`.
- **Order chosen in Shopify admin** (read only): `#175249`, id 7494849527874, paid, unfulfilled, 1 item,
  SKU `WEVIV03KB2NA1`, created 2026-11-24. No open unfulfilled order has `WESAS21704EO1`.
- **Changed and saved** (UI, "updated successfully"): `query` = `name:#175249`; `days` stays `10`.
  The server keeps masked / blank secret fields on save (`MergeConnectorData`, `ConnectorsController.cs:187`).
- **To revert after the test**: `query` back to the value above.
- **Not done yet**: baseline (GO_01 `@Apply = 1`), Run Now of route 160.

### Step 4: Order changed to one whose item SPARS sends
- The user's query: `WEVIV03KB2NA1` (order #175249) is NOT in `SCSInventoryFeed` of WKE1000. The user pasted the
  whole feed: rows are back on `UPDATED` (the `MAJID` test state is gone), `WESAS21704EO1` is `SYNCED`.
- **Order chosen instead** (Shopify admin, read only): `#175223`, id 7494291882050, paid, unfulfilled, 1 unit of
  `WESAS11704CO1` (in the feed, ATS 3765), created 2026-11-24.
- **Connector 252 saved again** (UI): `query` = `name:#175223`, `days` = `10`.
- **Not done yet**: baseline (GO_01 `@Apply = 1`), Run Now of route 160.

### Step 5: Baseline recorded; Run Now is disabled while the route is In-Active
- **GO_01 `@Apply = 1`** (user): baseline CustomerId 15, MaxOrderId 786361, 2026-11-25 02:42:53.
- **Flows > Shopify - Order Processing > View**: both Run Now buttons are disabled. Cause in
  `view-flow-dialog.component.html:88`: `[disabled]` when the detail status is not `Active` (tooltip "Route is
  Inactive"). My click did nothing; no run started. So yesterday's test runs were done with the route set Active.
- **Not done**: I did not activate anything. Activating route 160 in the Flow schedules it every 5 minutes;
  asked the user before doing that on production. Route 161 must never be activated (same dialog, next row;
  the General tab's Active switch would activate both).

### Step 6: Route 160 activated, Run Now, set back to In-Active (production screen, on the user's instruction)
- The user: "first edit then go active then view and test run".
- **Done in Flows > Shopify - Order Processing**: Edit > Route Details > Get Orders = Active (only this row; the
  General status was not touched) > Save ("updated successfully", Flow shown Active, 02:47). View: Run Now on
  Get Orders -> toast "'Shopify - Get Orders' has been triggered successfully". Then Edit > Get Orders =
  In-Active > Save (02:49). View afterwards: both routes In-Active, Flow In-Active, both Run Now disabled.
- Route 161 `Place Orders in ERP` was In-Active the whole time.
- **Code read**: `edit-flow-dialog.component.ts:612` sends the Flow status computed from the details; the General
  status only overwrites the details when its dropdown is changed (`onFlowStatusChange`).
- **Seen, not caused by the test logic**: the Start Date shown for both routes moved by 5 hours on each save
  (11/12/2026 08:00 PM -> 03:00 PM -> 10:00 AM). A time-zone conversion on save in the Flow dialog.
- **Open**: GO_02 by the user; then `query` on connector 252 back to the original value; GO_99 when the test
  order is no longer needed.

### Step 7: GO_02 at the client - Get Orders works on the live store
- **Route log** (02:47:57): query `name:#175223 created_at:>='2026-11-15T07:47:56Z'`, 1 read in 1 page, 1 new
  saved (0 with Status ERROR), 0 known, 0 not saved.
- **Order** Id 786366, OrderNumber 7494291882050, CustomerOrderNo #175223, Status New, ShippingMethod "Free
  Shipping", ship-to name / address / city / state / zip / country filled, phone and email filled, ExternalId
  NULL, 1 unit, 1 data row. **Line**: 1 x `WESAS11704CO1`, 334.99, NEW, item in the feed.
- **Customer data risk did not occur**: Shopify gave name and address to the app on the live store.
- Routes 160 / 161 In-Active, no job.
- **Left as it is on purpose**: connector 252 still has `query` = `name:#175223` (safer than the default while
  testing; original value is in step 3). Baseline `ZDB_GO_Baseline` still open; the order stays for the SPARS step.
- **Next**: Place Orders in ERP (route 161) = a real order in LIVE SPARS; needs the client's go and the
  NetSuite / Celigo answer first. Preview with `CT_03_Preview_Order_For_ERP.sql` before any run.

### Step 8: Order 786366 checked on the Orders screen (production UI, read only)
- **List**: 786366, New, WKE1000, Customer PO `7494291882050`, PO date 11/25/2026, no ERP SO No.
- **View**: ship-to name, address 1, Lincolnshire / IL / 60069 / US filled; company, address 2, warehouse code,
  shipping code, shipping agent code, ship date empty. **Details**: line 1, `WESAS11704CO1`, qty 1, $334.99.
- **Order Logs**: one file "Order Received from Customer" (API-JSON). It holds #175223, PAID / UNFULFILLED,
  total 362.63 USD, shipping line "Free Shipping" (0, no carrier), shipping and billing address, email.
- **Matches Shopify admin**: #175223, $362.63, 1 x `WESAS11704CO1` at $334.99, created 2026-11-24 17:10 store time.
- **Points for the SPARS step** (not errors of Get Orders): the screen's Customer PO is the Shopify id, `#175223`
  is in `CustomerOrderNo`; PO date is UTC (11/25 vs 11/24 in the store); shipping method is the text
  "Free Shipping", not a carrier code; the line shows "Unknown Status" (UI label for any line that is not
  shipped / cancelled, `order-detail.component.ts:166`).

### Step 9: Connector 252 reverted (the user asked for it)
- **Saved in the UI**: `query` back to `(financial_status:paid OR financial_status:pending)
  fulfillment_status:unfulfilled status:open`, `days` = `10`. Read back after the save: same values; Base URL
  `https://walker-edison.myshopify.com/admin/api/2026-10/`, `graphql.json`, POST, JSON, WKE1000 unchanged.
- **Consequence**: a run of route 160 now reads every paid / pending, unfulfilled, open order of the last
  10 days of the live store. Route 160 is In-Active.
- **Still open**: test order 786366 and baseline `ZDB_GO_Baseline` (GO_99 removes both).

### Step 10: The user - the order is NOT a test to remove; next step is creating it in SPARS (production)
- The user: "why we remove? get order is done next step is we will creat order in spars ... we are in production
  now". So order 786366 stays; GO_99 is not to be run. Suggested dropping `ZDB_GO_Baseline` so GO_99 cannot
  delete the order by mistake (user's choice).
- **Code read** (`SCSPlaceOrderRoute.cs`): source SP with `@ORDERSTATUS@` = `New,InProgress`; body = map
  (JUST) over the API-JSON, `@CUSTOMERID@` replaced; status InProgress -> `ERP-SNT` saved -> SPARS `Place_Order`;
  success -> `SP_UpdateOrderStatus` with the SPARS ObjectID, `ERP-JSON` saved; failure -> Status error, `ERP-ERROR`.
  For an InProgress order it first asks SPARS `Get_OrderInfo` by CustomerPO and does not post again when found.
- **Plan**: 1. preview with `Testing-Client-DB-demo-store/CT_03_Preview_Order_For_ERP.sql` (read-only, works on
  the client DB as it is); 2. on the user's go: activate ONLY route 161, Run Now, back to In-Active; 3. verify.

### Step 11: Change of order - the client named Shopify PO #175205
- The user: order #175223 was only our pick; the client sent PO no 175205 ("get this order and make in spars").
  So 786366 is to be deleted (GO_99) and #175205 fetched instead. This replaces step 10's "order stays".
- **Shopify admin (read only)**: `#175205`, id 7490956558402, Monday 2026-11-23 2:58 pm store time, paid,
  unfulfilled, 1 x `WEMYA25OS4MB1` at $134.99, total $134.99. The item is in the feed (UPC 840357754078, ATS 192).
  Note: the admin search box ignores `name:#175205` (lists everything); plain `175205` finds it. The API filter
  `name:#...` worked in the run of step 7.
- **Connector 252 saved** (UI): `query` = `name:#175205`, `days` = `10` (the order is 2 days old).
- **Open**: user runs GO_99 (dry run, then apply) to delete 786366, then GO_01 `@Apply = 1` for a new baseline;
  then I activate route 160 only, Run Now, back to In-Active; user runs GO_02.

### Step 12: #175223 removed, client order #175205 fetched
- **User**: GO_99 dry run + apply -> order 786366 deleted, 0 orders left for WKE1000; GO_01 `@Apply = 1` ->
  baseline CustomerId 15, MaxOrderId 786370, 2026-11-25 03:08:35.
- **Me (production screen)**: Flow "Shopify - Order Processing": Get Orders = Active (only that row), Save (03:10),
  View > Run Now -> "'Shopify - Get Orders' has been triggered successfully"; Get Orders = In-Active, Save (03:11).
  View afterwards: both routes In-Active, Flow In-Active. Route 161 never activated.
- **Orders screen**: new row 786371, New, WKE1000, Customer PO `7490956558402` (= Shopify id of #175205),
  PO date 11/23/2026, created 11/25/2026 03:10 AM.
- **Open**: GO_02 by the user; connector 252 still on `name:#175205`; next = preview of the SPARS body
  (`CT_03_Preview_Order_For_ERP.sql`), then Place Orders in ERP on the user's go.

### Step 13: GO_02 for #175205 clean; connector 252 back on the original filter (the user asked)
- **GO_02**: route log 03:10:54 `name:#175205 ...`: 1 read, 1 new saved, 0 errors. Order 786371, OrderNumber
  7490956558402, #175205, New, OrderDate 2026-11-23 21:58:52 (UTC), "Free Shipping", ship-to name / street /
  Spanish Fork / UT / 84660 / US, phone and email filled, ExternalId NULL, 1 unit, 1 data row.
  Line 1 x `WEMYA25OS4MB1`, 134.99, NEW, item in the feed. Routes 160 / 161 In-Active, no job.
- **Connector 252 saved and read back** (UI): `query` = `(financial_status:paid OR financial_status:pending)
  fulfillment_status:unfulfilled status:open`, `days` = `10`. A run of route 160 now reads all such orders of the
  live store again; set a `name:#...` filter before another single-order run.
- **Open**: `CT_03_Preview_Order_For_ERP.sql` (user), then Place Orders in ERP on the user's go.
  Baseline `ZDB_GO_Baseline` (786370) is open; GO_99 would delete order 786371 - not to be run.

### Step 14: CT_03 preview clean; decision - SPARS gets the Shopify order number as Customer PO
- **CT_03 at the client**: map 12 valid, CustomerID `@CUSTOMERID@`, ShipViaCode `FEDX`, route 161 In-Active.
  Header for 786371: WKE1000, CustomerPO 7490956558402, ship-to complete with phone, dates 11/23/2026; line
  1 x `WEMYA25OS4MB1` at 134.99, item in the feed; nothing sent yet.
- **The user's decision**: the client knows the order as PO 175205, and the number is unique in SPARS and in
  Shopify, so SPARS must get `175205` as Customer PO (not the Shopify id).
- **Design**: map-only change. `order.header.CustomerPO` = `#substring(#valueof($.name),1,#subtract(#length(
  #valueof($.name)),1))`; `ExternalID` stays the Shopify id; `Orders.OrderNumber` stays the Shopify id (the
  Shopify ASN / cancellation routes build `gid://shopify/Order/<OrderNumber>` from it). SPARS shipments are
  matched through `Orders.ExternalId` (the SPARS SO number), not through the PO.
- **Tested locally** with the repo's `JUST.net` (console app in the scratchpad, not in the repo): `#175205` ->
  `175205`, `#1001` -> `1001`, `#175205-A` -> `175205-A`; a name without `#` loses its first character.
- **Known side effect**: `SCSPlaceOrderRoute.cs:344` asks SPARS `Get_OrderInfo` with `Orders.OrderNumber` (the
  Shopify id) before re-posting an InProgress order; with the new PO SPARS will not find it there. The second
  guard still applies: SPARS answers "already exist in SPARS with SO#" and the route marks the order SYNCED.
- **Files created** in `eSyncMate.Data/Tables/ShopifyScripts/2026-11-25_Place-Order/`:
  `PO_01_Map_CustomerPO_Apply.sql` (dry run by default, backup `ZDB_PO01_Map`, checks that only that value
  differs), `PO_02_Preview_And_Verify.sql` (read-only, before and after the run), `PO_99_Revert_Map.sql`.
  Not tested on a database. Not committed.

### Step 15: New requirements from the user - PO number as eSyncMate's order number; SPARS allows duplicate POs
- **The user**: (1) the Orders screen's "Customer PO" (`Orders.OrderNumber`) must be the PO number (175205), so a
  user copies it and searches SPARS and Shopify with it; (2) SPARS can create several orders for the same PO
  number (duplicate PO feature).
- **Consequence of (2)**: my statement of step 14 ("SPARS refuses a duplicate PO") cannot be relied on. The only
  protection against a second SPARS order on a re-process is the route's own `Get_OrderInfo` check, which asks
  with `Orders.OrderNumber`. So OrderNumber and the CustomerPO sent to SPARS must be the same value. A map-only
  change (step 14) would break exactly that check; it is no longer recommended on its own.
- **Code changed (not committed, not deployed; needs Release7)**:
  - new `eSyncMate.Processor/Managers/ShopifyOrderRef.cs`: `PoNumber(name, id)` (name without `#`), and
    `GetShopifyOrderId(connection, orderId, orderNumber)` (reads `legacyResourceId` from the saved API-JSON,
    then `CustomerOrderNo` when all digits, else the order number) - works for orders of both forms.
  - `ShopifyGetOrdersRoute.cs`: `OrderNumber` = PO number, `CustomerOrderNo` = Shopify id; the "already in
    eSyncMate" check is now per customer and matches the PO number or the Shopify id in either column.
  - `ShopifyASNShipmentNotificationRoute.cs`, `ShopifyCancellationRoute.cs`: the Shopify gid comes from
    `ShopifyOrderRef.GetShopifyOrderId`.
  - `ShopifyGetCancelledOrdersRoute.cs` (`LoadCandidates`): finds the orders by the Shopify id in
    `CustomerOrderNo` or `OrderNumber`, keyed by the Shopify id.
  - `dotnet build eSyncMate.Processor` (Debug): Build succeeded. Not run against a database or a store.
- **Scripts added** (`2026-11-25_Place-Order`): `PO_03_Switch_Existing_Orders_To_PO_Number.sql` (order 786371 to
  the new form; only after Release7, `@Release7Deployed = 1`; backup `ZDB_PO03_Orders`), `PO_97_Revert_Switch.sql`.
- **Order of work now**: publish + deploy Release7 -> PO_01 (map: CustomerPO = PO number) -> PO_03 -> PO_02
  preview -> Place Orders in ERP on the user's go -> PO_02 verify.

### Step 16: Committed and published to Release7 (the user asked)
- **Commits on `main`** (not pushed): `7742e37` code (5 files in `eSyncMate.Processor/Managers`), `708b02f`
  scripts of 2026-11-24 / 2026-11-25 and both session files. Left uncommitted as agreed: RouteWorker
  `GlobalUsings.g.cs`, `MOM/`, the connector zip / folder, the `.docx`, the Amazon session file of 2026-11-23.
- **Publish**: `dotnet publish eSyncMate.Processor\eSyncMate.Processor.csproj -c Release -o
  D:\Deployment\BIZMATE\Release7\api` - exit 0, 435 files (same count as Release6).
- **Checked**: `ShopifyOrderRef` is in `eSyncMate.Processor.dll` of `api` and of `api\RouteWorker` (both
  2,009,088 bytes, built 2026-11-25 14:06); `appsettings.json`, `web.config`, `RouteWorker\appsettings.json`,
  `AlertWorker\appsettings.json` are byte-identical to Release6 (client values).
- **Package**: `D:\Deployment\BIZMATE\Release7\eSyncMate\api.zip` (79,383,189 bytes) and
  `Release7\eSyncMate\scripts\` with PO_01, PO_02, PO_03, PO_97, PO_99. No UI build (UI unchanged).
- **Not done**: deployment at the client (user); no test of the new code against a database or the store.

### Step 17: Client question - "you only update On hand, is Available needed or does it follow later?"
- The client's screenshot (2026-11-24): variant English Oak, L56 On hand 407, Available / Committed / Unavailable
  blank; total Available 55, On hand 462.
- **Code**: `ShopifyInventoryUploadRoute` sets the absolute `available` quantity (`inventorySetQuantities`,
  reason `correction`); it does not send On hand. Shopify derives On hand.
- **Shopify admin today (read only)**, item `43748597956674`:
  - Adjustment history of L56: 2026-11-24 9:50 am store time, "Inventory correction" by **eSyncMate Connector**:
    Available +407 -> 407 and On hand +407 -> 407. 2026-11-24 3:48 pm, "Inventory correction" by **Matrixify**:
    +4 -> 411 for both.
  - Variant now: 1553 W 9000 S 0 / 0, L56 Available 411 / On hand 411, L65 0 / 0, total 411 / 411.
  - `inventory_levels` now lists three levels (26869263 = 0, 81898602562 = 411, 82231951426 = 0; the last two
    updated 2026-11-24 15:48 store time). Yesterday before 15:48 it listed only 26869263.
- **Reading**: eSyncMate did send Available. It was not shown (blank) while the product was not stocked at L56;
  it showed after the client's Matrixify import at 3:48 pm, which stocked the product at L56 / L65 and changed
  the quantity to 411. It did not appear by itself with time. This corrects step 12 of the 2026-11-24 session
  ("not sellable"): the cause was the location not being stocked for the product, and the client has since
  done that for this item.
- **Open for the client**: 411 (Matrixify) vs 407 (eSyncMate, SPARS ATS; 4 damaged) - the next eSyncMate upload
  sets 407 again; whether the Matrixify import stocked L55 / L56 / L65 for all products.

### Step 18: Confirmed from Shopify's documentation - Available does not appear by itself
- The user asked for a confirmed answer on the state of 2026-11-24 (On hand 407, Available blank at L56).
- **Shopify docs** (shopify.dev, "Manage inventory quantities and states"): from API 2026-04 a quantity mutation
  on an item that is not stocked at the location creates an INACTIVE inventory level and stores the quantity;
  the item is not available for sale or fulfillment from that location until it is activated there; apps should
  activate it with `inventoryActivate` (or `inventoryBulkToggleActivation`); activating keeps the stored
  quantities. Before 2026-04 the same call answered `ITEM_NOT_STOCKED_AT_LOCATION`. The connector uses 2026-10.
- **So**: eSyncMate sent Available = 407; Shopify stored it on an inactive level, which is why the screen showed
  On hand 407 and Available blank. It does not change with time. It showed after the level was activated
  (Matrixify, 3:48 pm). `ShopifyInventoryUploadRoute` has no activation call.
- **Options**: the client stocks the products at the locations, or the upload activates the level itself
  (code change; Release7 is published but not deployed, so it could still go into it).

### Step 19: Report for the lead published; Release7 is deployed at the client
- **Artifact** (private, the user shares it with the lead): https://claude.ai/artifact/Ugrdqe5emZZVsBrhaFrFRR
  "Shopify Available Quantity" - answer, store history, Shopify documentation, two options, draft reply to the
  client. Source file is in the session scratchpad, not in the repo.
- **The user**: Release7 is deployed at the client; the PO scripts are not executed yet.
- **Consequence**: the location activation (`inventoryActivate`) can no longer go into Release7; it is a
  Release8 item if the lead / client choose it.
- **Next at the client**: PO_01 (dry run, apply) -> PO_03 (dry run, apply with `@Release7Deployed = 1`) ->
  PO_02 preview -> Place Orders in ERP on the user's go -> PO_02 verify.

### Step 20: Location activation added to the inventory upload; Release8 published (the user asked)
- **Shopify docs read first**: `inventoryActivate(inventoryItemId, locationId, available, onHand)` needs
  `write_inventory` and, from 2026-04, an `@idempotent` key; `InventoryLevel.isActive` exists.
- **Code** (commit `9ade7b4`): `ShopifyInventoryUploadRoute.cs` - new `ActivateLocationsAsync`, run after the
  quantities are set and before the results are saved:
  1. only item / location pairs with a quantity above 0 on items without an error;
  2. one query per 100 items (`nodes(ids)` -> `inventoryLevel(locationId) { isActive }` per mapped location)
     finds the pairs with no level or an inactive one;
  3. `inventoryActivate` for each of them with the same Available quantity.
  A failed check is logged and changes nothing; a failed activation puts the item in ERROR with the reason;
  the route log line now also says "N location(s) turned on for a product (F failed)"; the per-item JSON-RVD
  has `locationsTurnedOn`. `ShopifyInventoryItemModel.cs`: new list `Activated`.
- **Build**: `dotnet build` succeeded, 0 errors. **Not tested against a store** (no test store reachable from
  here): the first real proof is an upload run at the client with an item that is not stocked at L55 / L56.
- **Publish**: `dotnet publish ... -o D:\Deployment\BIZMATE\Release8\api` - exit 0, 435 files; both
  `eSyncMate.Processor.dll` (API and RouteWorker) contain `inventoryActivate` and `ShopifyOrderRef`; the four
  config files are identical to Release7. Package `D:\Deployment\BIZMATE\Release8\eSyncMate\api.zip`
  (79,393,666 bytes). Release8 contains Release7. No scripts and no UI build needed for it.

### Step 21: Release8 deployed at the client; today's end-to-end test started
- **The user's plan**: full inventory from SPARS -> upload the same product as yesterday -> check On hand and
  Available -> get PO 175205 -> create it in SPARS. Release8 is deployed (user).
- **Shopify admin (read only)**: 5 of 5 checked items (`WESAS11704CO1`, `WESAS11704EO1`, `PRSB4CCO`,
  `AF46MXETRO`, `CHL18BL`) are stocked at L56 (81898602562) with the SPARS quantities and at L65 (82231951426);
  old location 26869263 is 0. The client's import did this for the products. None has a level at L55
  (81898176578), where SPARS has 0. `PRSB4CCO` has 938 at L65, which is not in our mapping.
  So the new activation step will find nothing to turn on today; it stays untested against the store.
- **Full feed run** (production screen): Flow "Shopify - Inventory Sync", only `Fetch Full Inventory from ERP`
  set Active, Save (04:58), View > Run Now -> "triggered successfully"; set back In-Active, Save (04:59).
  The Flow list shows all four WKE1000 Flows In-Active.
- **Open**: INV_02 by the user; GO_99 to delete order 786371 (fresh fetch chosen); statuses for the one-item
  upload; upload; then Get Orders with the `name:#175205` filter; PO_01; PO_02; Place Orders on the user's go.

### Step 22: Full feed verified, Sync Products, one-item upload - Available and On hand updated in Shopify
- **User**: GO_99 applied (order 786371 deleted). Feed check: 345 items (9 more than 2026-11-24), last change
  04:58:55; statuses set to 1 `UPDATED` (`WESAS21704EO1`: Total 407, L55 0, L56 407) and 344 `MAJID`.
- **Slip**: while opening the upload row the window resized between screenshot and click and the Sync Products
  row opened; cancelled without a change. Then the upload route was set Active and saved (05:04) before the
  user asked "why not sync first?".
- **Changed on the user's wish** (05:05): upload back to In-Active, Sync Products Active -> Run Now ("triggered
  successfully") -> Sync In-Active, upload Active (05:06) -> Run Now ("triggered successfully") -> upload
  In-Active, saved (05:07). Flow list afterwards: all four WKE1000 Flows In-Active.
- **Shopify admin (read only)**, item 43748597956674 at L56: "Just now - Inventory correction - eSyncMate
  Connector": Available -4 -> 407, On hand -4 -> 407 (after Matrixify's 411). `inventory_levels`: 26869263 = 0,
  81898602562 = 407 (updated 2026-11-25 03:07 store time), 82231951426 = 0. Variant sellable quantity 407.
- **Not yet seen**: the route log lines of the sync and the upload (user's SQL); how many of the 9 new items
  got a Shopify id.

### Step 23: Logs confirmed; PO 175205 fetched again with Release8 - saved with the PO number
- **Route log (user)**: sync 05:05:56 - 4239 store items in 17 pages, missing 9, id different 0, no SKU 9, not
  in the feed 3596, "Inserted 9 item(s)" (all 345 feed items have an id now). Upload 05:07:05, batch
  `81a14853-29d8-47d1-a730-72605d202beb`: 1 item, 2 quantities in 2 calls (0 failed), "0 location(s) turned on
  for a product (0 failed)", 1 SYNCED, 0 ERROR - the Release8 line, so the new step ran and found L56 active.
- **Connector 252** (UI): `query` = `name:#175205`, `days` = `10`, saved ("updated successfully").
- **Get Orders** (UI, by element references after the earlier mis-click): Order Processing Flow, only Get
  Orders Active, Save (05:12), View, Run Now on the only enabled button -> "'Shopify - Get Orders' has been
  triggered successfully"; Get Orders In-Active, Save (05:13). All four WKE1000 Flows In-Active.
- **Orders screen**: 786382, New, WKE1000, Customer PO **175205**, PO date 11/23/2026, created 05:12 AM.
  The PO number is now eSyncMate's order number (Release8 code path proven for Get Orders).
- **Open**: connector 252 still on the one-order filter; GO_02 by the user; PO_01 (map) then PO_02 preview;
  PO_03 is not needed (no order in the old form left); Place Orders in ERP on the user's go.

### Step 24: Scripts no longer needed removed (the user asked)
- Removed from the repo (`git rm`, not committed yet) and from `Release7\eSyncMate\scripts`:
  `PO_03_Switch_Existing_Orders_To_PO_Number.sql` and `PO_97_Revert_Switch.sql` - no order in the Release6 form
  is left, the order was fetched fresh with Release8.
- Kept: `PO_01_Map_CustomerPO_Apply.sql`, `PO_02_Preview_And_Verify.sql`, and `PO_99_Revert_Map.sql` (the
  revert of PO_01).

### Step 25: PO_01 applied, preview clean, connector 252 back on the original filter
- **PO_01** (user, dry run + apply): map 12 `order.header.CustomerPO` changed to the substring expression at
  2026-11-25 05:19:08, `ExternalID` unchanged; only route 161 (WKE1000, In-Active) uses the map.
- **PO_02 preview**: header for 786382 - WKE1000, CustomerPO `175205`, ExternalID `7490956558402`, ship-to
  complete (Spanish Fork / UT / 84660 / US, phone filled), ShipViaCode `FEDX`, dates 11/23/2026, check OK;
  line 1 x `WEMYA25OS4MB1` at 134.99, item in the feed; order New, no ERP SO; no ERP-SNT / ERP-JSON / ERP-ERROR
  and no route log line of route 161 yet.
- **Connector 252** (UI, the user asked): `query` back to `(financial_status:paid OR financial_status:pending)
  fulfillment_status:unfulfilled status:open`, `days` = `10`; saved ("updated successfully") and read back.
- **Waiting**: the user's "go" for Place Orders in ERP (route 161, LIVE SPARS).

### Step 26: Order 175205 created in SPARS (the user said "go")
- **Production screen**: Flow "Shopify - Order Processing", only `Place Orders in ERP` (route 161) set Active,
  Save (05:25), View, Run Now on the only enabled button -> "'Shopify - Place Orders in ERP' has been triggered
  successfully"; set back In-Active, Save (05:26). All four WKE1000 Flows In-Active afterwards.
- **Orders screen**: 786382 | **Synced** | WKE1000 | Customer PO 175205 | PO date 11/23/2026 |
  **ERP SO No 133177640** | ERP sync date 11/25/2026 05:25 AM.
- So SPARS accepted the order, including ShipViaCode `FEDX`.
- **Open**: PO_02 by the user (ERP-SNT body and the SPARS answer); the client checks SO 133177640 / PO 175205 in
  SPARS; the NetSuite / Celigo question (the same Shopify order is still open there); next flows are the ASN
  back to Shopify and cancellations.

### Step 27: PO_02 after the run - body sent and SPARS answer confirmed
- **ERP-SNT** (05:25:55): CustomerID WKE1000, CustomerPO `175205`, ship-to complete (Address2 null), OrderTakenBy
  API, ShipViaCode FEDX, Order / Ship / Cancel date 11/23/2026, ExternalID 7490956558402; detail 1 line
  `WEMYA25OS4MB1`, OrderQty 1, UnitPrice 134.99, WHSID empty, APIOrderLineNo 1.
- **ERP-JSON** (05:25:56): `Success: true`, "Sales Order [133177640] has been created successfully.",
  ObjectID 133177640. No ERP-ERROR.
- Order 786382: SYNCED, ERP SO 133177640. Route 161 In-Active, no job. The route log grid is empty because
  `SCSPlaceOrderRoute` writes only Info / Debug lines for a successful order, and those are not saved.

### Step 28: Price validation package for the lead
- The lead wants to validate the price with the JSON and the map. Read from the production site (read only):
  Orders > Order Logs of 786382 (three files) and Setup > Maps (map 12). No browser download was used; the texts
  were read from the page and written locally. Lengths match the screen exactly (order 1647 characters, map 1675).
- **Folder** `D:\Deployment\BIZMATE\Order-175205-Validation\` (outside the repo: file 1 has customer data) and
  `D:\Deployment\BIZMATE\Order-175205-Validation.zip`: `0_Price_Validation.md`,
  `1_Shopify_Order_Received_API-JSON.json`, `2_Order_Sent_To_SPARS_ERP-SNT.json`,
  `3_SPARS_Answer_ERP-JSON.json`, `4_Map_Shopify_Order_Transformation.json`.
- **Price finding**: Shopify line `discountedUnitPriceSet` 134.99 = `originalUnitPriceSet` 134.99, quantity 1,
  shipping 0.00, order total 134.99; map `UnitPrice` = `$.discountedUnitPriceSet.shopMoney.amount`; SPARS got
  `UnitPrice` 134.99. Tax, shipping charge and discount fields are not sent by the map (`TaxAmount`, `Discount`
  fixed empty) - no effect on this order, a gap for orders with tax or paid shipping.

### Step 29: Upload for all items prepared, then stopped by the user before it ran
- The lead asked to upload the inventory for all items. **User (SQL)**: the 344 `MAJID` rows set back to
  `UPDATED` (1 `SYNCED`). Totals of what would be sent: 344 items, 260 with stock at L56, Total_L56 99,081,
  Total_L55 0, Total_L65 0, Total_ATS 99,081, items without a Shopify id 0. (My first totals query had a
  subquery inside `SUM` - Msg 130; corrected with `OUTER APPLY`.)
- I opened the Flows screen and selected WKE1000; the user then said "do not update". **Nothing was changed
  and no route was run.** Checked: all four WKE1000 Flows In-Active, no dialog open.
- **State to remember**: the 344 feed rows are on `UPDATED`, so any run of route 159 (`Upload Warehouse-Wise
  Inventory`) sends all of them to the live store. The route is In-Active.

## Summary
2026-11-25: Get Orders proven on the live store; on the user's requirements the PO number became eSyncMate's
order number (Release7) and the inventory upload activates a location (Release8, deployed, the activation
itself not yet exercised). End-to-end run at the client: full feed (345 items), Sync Products (9 new ids),
one-item upload (Available and On hand 407 at L56 in Shopify), Get Orders for the client's PO 175205 (order
786382), map changed with PO_01, and the order created in SPARS as SO 133177640.
(open)

### Step 30: Lead's question on quantity vs currentQuantity; re-fetch question
- **What was done**: answered from Shopify's LineItem documentation (currentQuantity is correct; unfulfilledQuantity only if partially fulfilled orders are ever fetched). Artifact "Shopify Line Quantity Fields" published (private). Confirmed to the user that an order already in eSyncMate is skipped by Get Orders (`ShopifyGetOrdersRoute.cs:151`); no connector filter needed. Order 786382 must not be deleted while the Shopify order is open and unfulfilled.
- **Open**: all-items inventory upload still waits for the client's approval.

### Step 31: Users screen - delete gives "404" (not Shopify related)
- **Issue**: Users > delete icon > Delete User -> "Http failure response for .../api/User/deleteUser: 404 OK".
- **Cause**: the UI posts to `api/User/deleteUser` (`UI/src/app/services/user.service.ts:18`), but the Processor's `UserController` never had that endpoint (no trace in git history either). The view `VW_Users` already leaves out `Status = 'DELETED'`, so a soft delete was intended.
- **Files changed**: `eSyncMate.Processor/Controllers/UserController.cs` (new `POST api/User/deleteUser`: sets `Users.Status = 'DELETED'`; refuses a missing id, the caller's own user, and a user that does not exist or is already deleted), `eSyncMate.Processor/Models/UserModel.cs` (`DeleteUserModel`).
- **Checks**: `dotnet build` of the Processor succeeded. Not run against a database. On the live Users screen the delete icon and the dialog's Delete User button already show the hand cursor on hover (checked in Chrome; dialog cancelled, no user deleted).
- **Open**: not committed, not published (next folder Release9). After deployment test with user no 2 (Tanveer), as the user asked; no other user is to be deleted.
