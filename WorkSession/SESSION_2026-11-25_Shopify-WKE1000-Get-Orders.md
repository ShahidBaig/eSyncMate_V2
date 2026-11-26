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

### Step 32: User delete fix committed, Release9 published
- **Commit**: `1b42217` fix(users): add the missing `api/User/deleteUser` endpoint (soft delete). The staged removal of PO_03 / PO_97 was left out of this commit.
- **Command**: `dotnet publish eSyncMate.Processor\eSyncMate.Processor.csproj -c Release -o D:\Deployment\BIZMATE\Release9\api`, then zipped to `D:\Deployment\BIZMATE\Release9\eSyncMate\api.zip`.
- **Checks**: 435 files (same list as Release8); `appsettings.json` (API, RouteWorker, AlertWorker) and `web.config` identical to Release8; `deleteUser` / `DeleteUserModel` present in `eSyncMate.Processor.dll` of `api` and `api\RouteWorker`, absent in Release8; Release8's location activation still present. API only, no UI build needed.
- **Open**: deploy at the client; test by deleting user no 2 (Tanveer) only.

### Step 33: Release9 deployed at the client (user's report)
- **What was done**: the user deployed Release9 at the client on 2026-11-25.
- **Open for 2026-11-26 morning**: test the delete with user no 2 (Tanveer) only, not now because the client is using the site. Check: success message, user gone from the Users list, login refused. Still waiting for the client's approval for the all-items inventory upload.

### Step 34: ASN plan for order 786382 (PO 175205, SO 133177640) - explained, nothing run
- **Asked by the user** from home (no site access): the steps to create the ASN for the SPARS order.
- **Plan given**: (0) SPARS must ship SO 133177640 first; (1) route "Shopify - Fetch ASN from ERP" (type 11, `SCSASNRoute`, SPARS `Get_TrackingInfo`) -> OrderDetail `ASNRVD` + tracking, OrderData `ERPASN-JSON`; (2) route "Shopify - Send ASN Shipment Notification" (type 83) -> `fulfillmentCreate` per tracking number, units `ASNSNT`, order SHIPPED, OrderData `ASN-SNT` / `ASN-RES`. Both in Flow "Shopify - Shipment & ASN", each activated alone for Run Now, then In-Active again.
- **Open questions for the client before step 2** (it writes to the live store): notifyCustomer (default false = no shipment e-mail from Shopify); the order is also open in the NetSuite flow; the Shopify order is assigned to location "1553 W 9000 S"; client's go.
- **Not done**: read-only check / verify scripts for the ASN step (AS_01 / AS_02) are not written yet.

### Step 35: Part A done by the user - ASN received from SPARS for order 786382 (PO 175205)
- **Run**: route "Shopify - Fetch ASN from ERP" via Run Now, by the user on the production site (click steps given by me).
- **SPARS answer** (Order Log "ASN Received from ERP"): Success true, "Tracking Information Provided."; TrackingNo 385463184259, ItemID WEMYA25OS4MB1, OrderLineNo 1, APIOrderLineNo 1, ShippedQty 1, ShippedDate 2026-11-25 08:26, ShippingMethod "FEDEX HOME DELIVERY" (maps to Shopify carrier FedEx).
- **Next**: Part B, route "Shopify - Send ASN Shipment Notification" (writes to the live store): expected one fulfillment, 1 unit, tracking 385463184259, carrier FedEx, no customer e-mail (notifyCustomer default false). Not confirmed: that the fetch route was set back to In-Active.

### Step 36: Part B done by the user - ASN sent to Shopify, order #175205 Fulfilled
- **Run**: route "Shopify - Send ASN Shipment Notification" via Run Now, by the user on the production site.
- **Shopify answer** (Order Log "ASN Created in Customer Portal"): `fulfillmentCreate` -> fulfillment `gid://shopify/Fulfillment/6878608326722`, status SUCCESS, no userErrors.
- **Shopify admin** (user's screenshot, 2026-11-25 8:52 pm): order #175205 Paid / Fulfilled / Archived; fulfillment #175205-F1, Confirmed, November 25, 2026, "FedEx tracking: 385463184259", 1 x WEMYA25OS4MB1 at $134.99, location "1553 W 9000 S".
- **Result**: the whole cycle works on the live store for PO 175205: Get Orders -> SPARS SO 133177640 -> ASN from SPARS -> fulfillment in Shopify.
- **Not confirmed**: order 786382 status Shipped in eSyncMate; both ASN routes back to In-Active. The fulfillment was recorded at location "1553 W 9000 S" (the location Shopify assigned), not L56 - client question on stock stays open.

### Step 37: Client asks to add warehouse L65 (test item ALSD1EIV) - scripts written, nothing run
- **Finding**: no code change needed. The upload route reads the warehouse -> location mapping from `TargetPlusShipNodes` (`ShopifyInventoryUploadRoute.cs:155`), and the source SP branch already returns `ATS_L65`. Adding L65 = one mapping row: L65 -> `gid://shopify/Location/82231951426` ("L65 - Yaraghi Patterson", read in Shopify admin 2026-11-24).
- **Files created** (`eSyncMate.Data/Tables/ShopifyScripts/2026-11-25_Add-L65-Location/`): `L65_01_Add_L65_Mapping.sql` (dry run default; backup `ZDB_L65_ShipNodes`; stops 52010-52014; grids: mapping, plan, test item in the feed, feed L65 totals, upload route), `L65_02_Verify.sql` (read only), `L65_99_Revert.sql`.
- **Risk told to the user**: once L65 is mapped, every uploaded item gets its SPARS L65 quantity written to the Shopify L65 location, also 0. On 2026-11-25 the feed had 0 units at L65 for all 344 items, while Shopify had stock there (PRSB4CCO 938). Check the grid "Feed L65 totals" before any upload.
- **Not checked**: whether ALSD1EIV is in the WKE1000 feed (the dry run shows it). Scripts not run against any database. Not committed.

### Step 38: L65_01 dry run at the client (user) + read-only look in Shopify admin
- **Dry run grids**: mapping now = L55 (row 166) and L56 (row 167); plan = L65 "will be inserted"; test item ALSD1EIV is in the feed (UPC 840357720554, Status UPDATED, Total 547, L55 0, L56 547, L65 0, Shopify id `gid://shopify/InventoryItem/42540949536834`); feed L65 totals = 345 items, 0 with stock at L65, 0 units, 344 waiting for upload; upload route 159 In-Active, JobID NULL.
- **Shopify admin, Inventory, ALSD1EIV (read only, 2026-11-25 evening)**: L65 - Yaraghi Patterson: stocked, Available 0 / On hand 0; L56: Available 548 / On hand 548; L55: not listed (not stocked there).
- **Conclusion**: adding the mapping is safe (route In-Active). A one-item upload of ALSD1EIV would set L56 548 -> 547 and write 0 to L65, where Shopify already shows 0, so the L65 part would show no visible change. SPARS sends 0 at L65 for all 345 items: question for the client / ERP team whether that is right.
- **Not done**: `@Apply = 1` not run yet.

### Step 39: SPARS now sends L65 - second L65_01 dry run (user ran the Full Inventory route, then In-Active)
- **Grids**: mapping unchanged (L55, L56); plan L65 "will be inserted"; ALSD1EIV: Status UPDATED, Total 601, L55 0, L56 547, L65 54, Shopify id present; feed L65 totals: 347 items (2 more than before), 30 with stock at L65, 11,323 units, 347 waiting for upload (the full feed put every row on UPDATED); route 159 In-Active.
- **Plan given**: `@Apply = 1`, `L65_02`, then a one-item upload of ALSD1EIV: other rows to `MAJID` first (user's own method), Run Now of route 159 alone, expected Shopify L56 548 -> 547 and L65 0 -> 54, then rows back to `UPDATED`. All-items upload still waits for the client's approval; the 2 new items need a Sync Products run before it.

### Step 40: L65 mapping applied and proven with ALSD1EIV
- **User (SQL)**: `L65_01` with `@Apply = 1` (row 168: L65 -> `gid://shopify/Location/82231951426`, backup `ZDB_L65_ShipNodes`), `L65_02` verify OK. Feed status then: 345 UPDATED, 2 NEW. Corrected one-item statements given (UPDATED -> `MAJID`, NEW -> `MAJIDN`, because the upload also sends NEW rows).
- **User (site)**: route 159 run alone via Run Now, then back to In-Active (user's report). Route log not yet seen by me.
- **Shopify admin, Inventory, ALSD1EIV (read by me after the run)**: L65 - Yaraghi Patterson Available 54 / On hand 54 (before 0 / 0); L56 Available 547 / On hand 547 (before 548 / 548); L55 On hand 0, Available blank (quantity 0 on a location the product is not stocked at - expected). Matches the SPARS feed (L55 0, L56 547, L65 54).
- **Screenshots for the client**: `D:\Deployment\BIZMATE\L65-Test-ALSD1EIV\` (L65 and L56).
- **Open**: put the hidden rows back (`MAJID` -> UPDATED, `MAJIDN` -> NEW) - not confirmed; all-items upload still waits for the client's approval; Sync Products run for the 2 new items before it. L65 scripts not committed.

### Step 41: L65 test closed (user's report)
- The user set the hidden feed rows back and sent the two Shopify screenshots to the client. ALSD1EIV is a real product of the client; its Shopify stock now equals the SPARS quantities (L56 547, L65 54).
- **Open**: client's confirmation of L65; all-items upload waits for the client's approval (Sync Products run first for the 2 new items); delete-user test with user no 2 (Tanveer) on 2026-11-26 morning; L65 scripts and the PO_03 / PO_97 removal not committed.

### Step 42: Commit + push (user: leave out what causes the rejection, push the rest)
- **Commit**: L65 scripts, removal of PO_03 / PO_97, session file.
- **First push**: denied by the session's safety check (sensitive information). Cause found: 9 files in the 27 unpushed commits carry credentials - the setup scripts 03, 04, 10, 11, 12, 14, 15, 18 (client values put in by old commit `65acf93`) and `Testing-Client-devshop-we/devsql.sh` (old commit `1498316`).
- **What was done**: backup branch `backup/main-before-secret-clean-2026-11-25` (local only, holds the old commits WITH the credentials - never push it); `git filter-branch` over `origin/main..main`: the 8 scripts stay in history as they are on origin, `devsql.sh` is not in history; one commit became empty and was dropped (27 -> 26). The local files were saved first and restored: the working copy is identical to before (the 8 scripts now show as modified, `devsql.sh` as untracked). The rewrite deleted 7 tracked files under `.git-rewrite/` (tracked since an old commit); restored with `git checkout`.
- **Checks**: credential scan of the 26 commits: nothing; Processor builds; trees differ from the backup only in the 9 files.
- **Push**: `f30e096..3134dca main -> main`, 0 commits ahead.
- **Commit ids changed** by the rewrite: user delete fix `1b42217` -> `f698563`, last commit `f1aa587` -> `3134dca`, location activation `9ade7b4` -> `0050656`. Ids of the unpushed commits named earlier in the session files are the old ones.
- **Rule from now**: never `git add` the 8 modified scripts or `devsql.sh`; commit by explicit paths only.

### Step 43: Go-live state and the 74 older orders (user's reports, night of 2026-11-25)
- **Incident**: route 160 Get Orders ran at 12:45 server time with the ORIGINAL filter (cut-off not yet in the connector): 74 orders of the last 10 days saved, and route 161 placed them in SPARS (Status SYNCED, e.g. POs 175258 - 175267). Many may already have been sent to SPARS by the client's NetSuite flow (SPARS accepts duplicate POs). From 12:55 the connector query carries `created_at:>='2026-11-25T18:00:00Z'` and reads 0.
- **Actions**: 161 set In-Active; PO list query given (Shopify PO, SPARS SO = Orders.ExternalId, order date, status; CustomerId 15, CreatedDate >= '2026-11-25 12:40'); the user shares it with the client, who decides which SPARS orders are duplicates. Orders must NOT be deleted in eSyncMate (Get Orders would fetch them again).
- **Route state now**: 157, 158, 159, 160 Active; 161, 162, 163, 164, 165, 166 In-Active; 167 / 168 not shown in the user's list (168 should be Active).
- **Before 161 goes Active**: client confirms their own flow no longer sends Shopify orders to SPARS; all orders queued as New in eSyncMate go to SPARS in its first run. ASN routes 162 / 163 wait for the client's decision on who fulfills the 74 orders in Shopify.

### Step 44: The user decided to mark the 74 older orders DELETED (keep 175273, 175274) - scripts written, not run
- **New orders after the cut-off**: 175273 (Id 786774, New, saved 13:10) and 175274.
- **Files** (`eSyncMate.Data/Tables/ShopifyScripts/2026-11-25_Old-Orders-Deleted/`): `OD_01_Mark_Old_Orders_Deleted.sql` (dry run default; targets CustomerId 15 saved 2026-11-25 12:40 - 12:50 except the keep list; stops unless exactly 74; backup `ZDB_OD01_Orders`; refuses while route 161 is Active), `OD_02_Verify.sql` (read only), `OD_99_Revert.sql`.
- **Checked in code**: Orders screen / dashboard filter `Status <> 'DELETED'` (`OrdersController.cs:695, 736, 824`); Get Orders duplicate check has no status filter, so DELETED orders are not fetched again; ASN branch picks SYNCED only.
- **Open risk found**: `ShopifyGetCancelledOrdersRoute.LoadCandidates` (route 166) excludes only CANCELLED / PortalCancel / CancelError, NOT DELETED: a customer cancellation in Shopify of one of these orders would be sent to SPARS by 167. Fix (add DELETED to the exclusion) needed before 166 / 167 are activated.
- **Not done by the script**: the 74 SPARS sales orders stay in SPARS (client decides there).

### Step 45: The older orders closed (user's report, 2026-11-26)
- OD_01 dry run found 73 (not 74): PO 175267 (Id 786733) was not in the list (assumed already DELETED - not confirmed by the check query); Id 786688 is not a WKE1000 order of that run. Of the 73: 52 SYNCED (had a SPARS sales order, 133181464 - 133181518), 21 ERROR (never created in SPARS).
- User: the client VOIDED the SPARS sales orders, and the orders are marked DELETED in eSyncMate (OD_01 with `@ExpectedCount = 73`, `@Apply = 1`). Backup `ZDB_OD01_Orders` (OD_99 reverts).
- Still to do before routes 166 / 167 go Active: exclude DELETED orders in `ShopifyGetCancelledOrdersRoute.LoadCandidates` (now only CANCELLED / PortalCancel / CancelError).
- Orders 175273 / 175274 (New) wait for route 161 (client's cut-over confirmation).

### Step 46: User delete fix tested on production (2026-11-26)
- On the user's request I deleted user no 2 (Tanveer, SuperAdmin) through the Users screen on the production site (Chrome, already signed in): confirm dialog showed "Tanveer", Delete User pressed.
- **Result**: toast "SUCCESS User deleted successfully!"; the list went from 17 to 16 users and Id 2 is gone. No other user touched. The 404 of `api/User/deleteUser` is fixed (Release9 code; the user called the deployed release "8").
- **Not checked**: the database row (expected `Users.Status = 'DELETED'` for Id 2) and that Tanveer's login is refused.

### Step 47: Delete User dialog - hand cursor on both buttons
- **User's report**: in the Delete User dialog, Delete User and Cancel show no hand cursor on hover (user set Tanveer Active again and retested).
- **Earlier measurement (2026-11-25)**: Delete User showed `pointer` on hover through Material's `.mdc-button:hover` rule; Cancel was not hovered then.
- **Fix**: `UI/src/app/users/delete-user-dialog/delete-user-dialog.component.scss` - `.dialog-actions button { cursor: pointer; }` (always, not only through Material's hover rule).
- **Open**: not built, not committed, not deployed (needs a UI build with `--base-href /eSyncMate`, next release folder Release10).

### Step 48: Hand cursor fixes committed; Release10 UI on hold
- **Fixes**: `delete-user-dialog.component.scss` (`.dialog-actions button, .dialog-actions button *` -> `cursor: pointer !important`) and `view-flow-dialog.component.scss` (`.test-run-btn:not([disabled])` and its children -> `cursor: pointer !important`). The Run Now button already had `cursor: pointer`; a disabled Run Now (route In-Active) gets `pointer-events: none` from Angular Material, so no hand and no tooltip there - expected.
- **Commit**: `bb54de4` fix(ui) (not pushed).
- **Release10 UI**: build started, then STOPPED on the user's request ("do not make release now, more issues to fix first"). No Release10 folder. Build with `npx ng build --base-href /eSyncMate` when the user asks.

### Step 49: Blocker - editing a route on the Routes screen sets its CustomerName (ERP Customer ID) to NULL
- **User's report**: Routes > search 157 > Edit > change Status > Save -> `Routes.CustomerName` becomes NULL (sometimes).
- **Cause**: `RoutesController.UpdateRoute` copies the posted `UpdateRoutesDataModel`, which has NO CustomerName field (the edit screen does not send it), so CustomerName is always empty and is looked up from the parties with `GetExternalCustomerName`, which skips the internal parties eSyncMate and "SPARS Customer". A route between two internal parties (157 / 158 Fetch Inventory from ERP, Place Orders, Fetch ASN, ...) finds nothing -> `Modify()` writes NULL. Routes with an external party (e.g. Upload to Shopify) get the partner's id back, so it happens only "sometimes".
- **Effect of NULL**: route hidden from non-admin users (VW_Routes filter), and per-customer lookups by TypeId + CustomerName fail (e.g. Re-Process -> "No route configured for customer", Re-Transmit ASN).
- **Fix**: `UpdateRoute` reads the old route first and keeps its CustomerName when the lookup finds no external party. Processor builds. Not committed, not published, not tested against a DB.
- **Data repair** (client DB): routes already set to NULL need their CustomerName back (query given to the user).

### Step 50: Routes screen schedule not shown on the Flows screen (route 168: Routes Minutely 10, Flow view "Every 6 hrs")
- **Cause**: two copies of the schedule. The Flows screen reads and saves `FlowDetails` and pushes it into `Routes` through `Sp_LogAndUpdateRoute` (Flow -> Routes only). The Routes screen (`RoutesController.UpdateRoute`) saves only `Routes` and schedules the Hangfire job from it; `FlowDetails` keeps the old values. So route 168 RUNS every 10 min (job from Routes) while the Flow shows 6 h - and the next Flow save that processes this row writes 6 h back into Routes.
- **Fix**: `UpdateRoute` calls new `SyncFlowDetails(route)` after a successful save: parameterised UPDATE of the route's non-deleted FlowDetails row(s) with Status, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime; a failure is logged and does not fail the route save. Processor builds. Not committed / released / tested against a DB.
- **Data now**: route 168's FlowDetails row still says Hourly 6; check query for all mismatches given to the user.
- **Data aligned (user, client DB)**: FlowDetails row of route 168 updated from Routes (Minutely 10); the mismatch check query now returns no rows. The code fix (`SyncFlowDetails`) still waits for Release10.
