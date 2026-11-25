# Work Session: Shopify WKE1000 - inventory from the ERP (SPARS) into eSyncMate
**Date**: 2026-11-24
**Status**: In Progress

## Objective
The client has opened the inventory for customer `WKE1000` (Walker Edison) in SPARS. Verify, with SQL the user runs
at the client, that the Full / Differential feed brings the inventory into eSyncMate; fix scripts if needed.
The upload to Shopify is the next step and is not part of this one.

## Tasks
- [x] Scripts for check before, verify after, fix, revert (new dated folder)
- [x] Compile / run check on the dev DB
- [ ] User: INV_01 at the client, Test Run of the Full feed, INV_02, paste the output
- [ ] Activate Full + Differential feed from the screen when the Test Run is good
- [ ] Next step: upload to Shopify (needs the walker-edison token, item ids, mapping)

## Progress Log

### Step 1: Scripts written - `eSyncMate.Data/Tables/ShopifyScripts/2026-11-24_Inventory-From-ERP/`
- **Files created**:
  - `INV_01_Check_Before_Turning_On.sql` (read-only): customer, routes, feed connectors (GET, CustomerID, values
    filled), leftover `Temp_SCSInventoryFeed` rows, feed rows now, objects, running lock, mapping, earlier batches.
  - `INV_02_Verify_After_Run.sql` (read-only): batches, route log of route types 7 / 8, feed rows by Status, newest
    batch in `SCSInventoryFeedData_WKE1000` / `SCSInventoryFeed_WKE1000_Log`, temp leftovers; for the next step:
    which items have a Shopify inventory item id, stock per warehouse vs mapping, items waiting for the upload.
  - `INV_03_Fix_If_Needed.sql`: fix A leftover temp rows, fix B batches stuck in Processing -> Error. Dry run by
    default, backup `ZDB_INV03_Temp` / `ZDB_INV03_Batches`, stops while a feed is running (51901) or a backup
    exists (51902).
  - `INV_99_Revert_Fix.sql`: restores from the backup (temp rows only with `@RestoreTempRows = 1`), drops it.
  - `README.md`: order of the steps, what a good run looks like, what each failure means.
- **Why fix A matters** (from `SCSFullInventoryFeedRoute.cs`): the route reads the highest `CurrentPage` of the
  customer's temp rows and starts at the next page, so rows left by a cut-off run make the next run skip pages.
- **Tested on the dev DB** (sqlcmd, password taken from the earlier chat into an environment variable, not
  written anywhere): 01 / 02 run without error; 03 / 99 dry run; 03 apply + 99 revert with one inserted test temp
  row inside a transaction that was rolled back - dev is unchanged (checked after the rollback).
- **Not run**: nothing on the client DB yet. Not committed.
- **Notes**: client route ids are 157 - 167 (the scripts find the routes by customer and type, not by id);
  a finished feed run sends the acknowledgement to SPARS (normal behaviour); earlier answer of SPARS for this
  customer was Code 407 "No items are configured" (dev, 2026-11-11).

### Step 2: INV_01 run at the client by the user - all checks good
- **Customer**: Id 15 `WKE1000`, selected token Id 2 `Shopify - walker-edison.myshopify.com` (new since 2026-11-18:
  Walker Edison has authorized and the token is selected on the customer).
- **Routes 157 - 167**: all In-Active, `JobID` NULL. 157 Full (Daily 06:45), 158 Differential (Minutely 15),
  159 upload (In-Active, stays so).
- **Connectors**: 249 / 251 SPARS feed (Plain, Rest, GET, WKE1000) OK; 250 SQL destination
  (`SP_InsertSCSInventoryFeed`) OK.
- **Temp rows** 0, **feed rows** none, **objects** all 6 present, **lock** none, **earlier batches** none.
- **Mapping**: L55 -> Location 81898176578, L56 -> Location 81898602562.
- **Next**: Test Run route 157 `Shopify - Fetch Full Inventory from ERP` once, then INV_02.

### Step 3: Test Run of route 157 at the client + INV_02 - the feed works
- **Batch** `2cd607e7-6774-4fd8-9d3b-3d5302b62805`: `SCSFullInventoryFeed`, Completed in 1 s, 1 page.
- **Route log**: "1 page(s) with 336 item(s) received from SPARS." No error line.
- **Feed rows**: 336, all `NEW`. History tables: 336 / 336. Temp rows 0.
- **Finding 1 - stock is 0 everywhere**: `Total_ATS` 0 for all 336 items, all 18 warehouses (L55 / L56 included),
  `ETA_Qty` 0. The items are configured in SPARS, the quantities are not there (or really 0). Question for the
  client / ERP team.
- **Finding 2 - item id for Shopify**: all 336 have a 12-digit UPC in `CustomerItemCode` (e.g. `AF30BDBRW` /
  `840145740313`) and no id in `CustomerProductCatalogPrices`. `ShopifyInventoryUploadRoute.cs` (lines 198-203)
  would send `gid://shopify/InventoryItem/<UPC>`, which is wrong. The id must come from `ShopifyProductSyncRoute`
  (RouteType 81); the client's routes 157 - 167 have no route of type 81.
- **Upload route 159**: In-Active, 336 items waiting. Must stay In-Active until finding 2 is solved.
- **Open**: activate 157 + 158 from the screen (user); next step = product sync route at the client, then upload.

### Step 4: Question - item `WESAS21704EO1` has ATS 407 in SPARS, 0 in `SCSInventoryFeed`
- **Row at the client**: all ATS columns 0, Status `UPDATED`, ModifiedDate 2026-11-24 09:12:44 (a second run
  wrote 0 again).
- **Code read**: `SCSFullInventoryFeedRoute.cs` copies `Total_ATS` and 18 fixed `ATS_Lxx` fields of the SPARS
  answer unchanged; `SP_InsertSCSInventoryFeed` copies them unchanged. Nothing sets a quantity to 0.
  A warehouse outside the 18 fixed ones is dropped by the model (`SCSInventoryFeedModel.cs`), but `Total_ATS`
  would still arrive.
- **Limit**: the raw SPARS answer is not saved (`Routes.SaveData` body is commented out); the per-item JSON in
  `SCSInventoryFeedData_WKE1000` is the model written back, so it cannot show fields the model does not know.
- **Conclusion**: the SPARS feed API answered 0 for this customer. Open: which warehouse holds the 407 in SPARS,
  and what the API answers for `WKE1000` (ERP team / Postman).

### Step 5: SPARS quantities arrive; one-item upload test at the client fails on the item number
- **SPARS**: Inventory Inquiry shows `WESAS21704EO1` ATSQ 407 in L56. Batch `5eae7040...` (09:27) brought
  `Total_ATS` 407 / `ATS_L56` 407 (earlier batches 05:09 and 09:12 had 0): the feed side was corrected in SPARS.
- **User's test**: all `WKE1000` rows set to Status `MAJID`, only `WESAS21704EO1` to `UPDATED`; Test Run of
  route 159. Revert: `UPDATE SCSInventoryFeed SET Status = 'UPDATED' WHERE CustomerID = 'WKE1000' AND Status = 'MAJID'`
  (or the next Full feed run).
- **Result** (batch `3a7e6bce...`, 09:43): 1 item, 2 location quantities, 0 SYNCED, 1 ERROR. Sent
  `gid://shopify/InventoryItem/840357748305` (the UPC) for L55 (0) and L56 (407); Shopify:
  `INVALID_INVENTORY_ITEM: The specified inventory item could not be found.` Item Status now `ERROR`.
  Nothing changed in the store. Token, both location ids and the call itself work.
- **Needed**: route `Shopify - Sync Products` (RouteType 81, `ShopifyProductSyncRoute.cs`, in Release6). The client
  has no route of type 81; the script `backlog/Product_Sync_RouteType_Branch_Route.sql` named in the README is
  not in the repo any more.
- **Open risk**: the sync compares the Shopify SKU with `CustomerItemCode` (UPC), `ItemId` only when that is blank
  (`Sp_SCS_GetProductCatalog`, branch `ShopifyProductSync`). If the store has the item code in SKU and the UPC in
  Barcode, nothing matches; then the branch must also offer `ItemId`.
- **File created**: `eSyncMate.Data/Tables/ShopifyScripts/2026-11-24_Product-Sync/PS_01_Check_Before_Setup.sql`
  (read-only: objects, route type 81, SP branches, table type / SP parameters, routes of type 81, connectors of
  route 159, Shopify connectors, catalog ids, compare value). **Not tested on dev** (the session refused reading
  the dev password again). Setup / verify / revert scripts follow from its output.

### Step 6: Paused - waiting on the client
- The user: waiting for the client's Shopify rights for products, orders and customers, and for data from the
  client side. Saved in memory (`shopify-wke1000-client-deployment`).
- Resume with: what arrived from the client; SKU / Barcode of one item in the store; `PS_01_Check_Before_Setup.sql`
  at the client; then setup / verify / revert scripts for the Sync Products route.

### Step 7: Item looked up in Shopify admin (the user now has product rights) - read only
- **Product** 7348264370242 "Sasha Contemporary TV Stand and Sideboard ...", variant English Oak:
  SKU `WESAS21704EO1` (= our ItemId), Barcode `840357748305` (= our CustomerItemCode),
  inventory item id `43748597956674`. Second variant `WESAS21704CO1` / `840357748299` / `43748597989442`.
- **Cause 1 (confirmed)**: the upload sent `gid://shopify/InventoryItem/840357748305` (the barcode). The right id
  is `gid://shopify/InventoryItem/43748597956674`. SKU and barcode are not accepted by `inventorySetQuantities`.
- **Open risk of step 5 confirmed**: the store SKU is the ItemId, the UPC is the Barcode. The `ShopifyProductSync`
  branch compares the store SKU with `CustomerItemCode` first, so it would match nothing. The branch must compare
  with `ItemId` for this customer.
- **Cause 2 (expected, not tested)**: the item is stocked at one location only, `26869263` "1553 W 9000 S"
  (55 available). It is not stocked at L55 `81898176578` / L56 `81898602562` (both exist and are active; there is
  also `82231951426` "L65 - Yaraghi Patterson", not mapped). `ShopifyInventoryUploadRoute.cs` treats
  `ITEM_NOT_STOCKED_AT_LOCATION` as an error for a quantity above 0 (hint: turn the location on for the product);
  the route does not activate a location.
- **Question for the client**: should the 55 at "1553 W 9000 S" stay, and who turns L55 / L56 on for the products.

### Step 8: PS_01 output from the client; setup / verify / revert scripts for Sync Products written
- **PS_01 at the client**: objects all present; RouteType 81 MISSING; branch `ShopifyProductSync` MISSING (upload
  branch present); table type = CustomerID, ItemId, ProductID; no route of type 81; upload route 159 = parties
  3 -> 15, partner group 16, RouteGroup Inventory, connectors 247 (SQL, generic `Sp_SCS_GetProductCatalog` command
  with `@ROUTETYPEID@`) / 248 (Rest, `graphql.json`); catalog rows of WKE1000 = 0.
- **Files created** in `eSyncMate.Data/Tables/ShopifyScripts/2026-11-24_Product-Sync/`:
  - `PS_02_Setup_Sync_Products.sql`: RouteType 81, branch `ShopifyProductSync` (pattern of script 06 on the LIVE
    SP; returns ItemId with SKU = ItemId UNION SKU = CustomerItemCode), route `Shopify - Sync Products` In-Active
    reusing connectors 247 / 248 and the parties of route 159. Dry run by default; backup `ZDB_PS02_SpBackup`.
  - `PS_03_Verify_Sync_Products.sql` (read-only), `PS_99_Revert_Sync_Products.sql` (dry run by default; stored
    ids stay unless `@RemoveCatalogIds = 1`).
- **Decision**: no new connectors. The source command takes the route type from the route, and the route code
  only needs a SqlServer source and a Rest destination.
- **Not tested on dev** (local `appsettings.json` is on the client values, `devsql.sh` refuses). Not committed.
- **Still open**: item not stocked at L55 / L56 in the store (step 7).

### Step 9: PS_02 applied at the client; route not in a Flow, so no Run Now button
- **PS_02 at the client** (`@Apply = 1`): route 168 `Shopify - Sync Products`, type 81, In-Active, JobID NULL,
  shown on the Routes screen. Procedure 17894 -> 18404 characters (branch added).
- **Issue**: Run Now exists only inside a Flow; the route was in none. The user asked for a script, not UI steps.
- **File created**: `PS_04_Add_Route_To_Flow.sql` (one In-Active `FlowDetails` row in "Shopify - Inventory Sync",
  pattern of script 19, dry run by default). `PS_99` changed: removes that row with the route, stops only when
  the row is Active. Not tested on dev. Not committed.

### Step 10: PS_04 applied; activating the route on the Flows screen failed (my schedule was invalid)
- **PS_04 at the client**: FlowDetails row 166, route 168 in Flow 56, In-Active.
- **Error** (user set the status to Active in Edit Flow and saved): `RecurringJobManager.ValidateCronExpression`
  from `RouteEngine.SetupRouteJob` line 923. **Cause**: PS_02 stored "Minutely, RepeatCount 60"; the code builds
  `*/60 * * * *`, which is not a valid cron text. My mistake in PS_02.
- **Files**: new `PS_05_Fix_Schedule.sql` (read-only "State" grid of all Flow routes; with `@Apply = 1` sets
  route 168 and its Flow row to Hourly / 6). `PS_02` corrected to create the route with Hourly / 6.
- **Open**: whether the failed save left routes 157 / 158 / 159 Active (the "State" grid shows it).
  Run Now does not need an Active route (routes 157 and 159 were test-run In-Active on 2026-11-24).

### Step 11: PS_05 applied, Sync Products run at the client - all 336 items have their Shopify id
- **PS_05**: all 12 Shopify routes In-Active / no job after the failed save; route 168 now Hourly / 6.
  The PS_05 header had a cron example whose star-slash ended the SQL comment (the user patched it by hand);
  header rewritten without cron text.
- **Run Now of route 168** (11:39): 4239 store items in 17 pages; inserted 336; id different 0; no SKU 9;
  not in the feed 3605; 289 store variants repeat a SKU (first variant kept - logged as ERROR by design).
  4239 = 336 + 3605 + 289 + 9.
- **PS_03**: branch returns 672 rows / 336 items, 0 ambiguous; catalog 336 / 336; grid 7 empty;
  `WESAS21704EO1` -> `gid://shopify/InventoryItem/43748597956674` (the id seen in Shopify admin).
- **Open**: whether any of the 336 items is one of the 289 repeated SKUs (then the stored id may be the wrong
  variant); upload test of the one item (expected `ITEM_NOT_STOCKED_AT_LOCATION` for L56).

### Step 12: One-item upload with the right id - Shopify says success, but the stock is not sellable
- **Run Now of route 159** (11:50), batch `0d640ae4-59a0-4d3b-8b80-644aa435d943`: 1 item, 2 quantities, 1 SYNCED,
  0 ERROR. Sent `gid://shopify/InventoryItem/43748597956674`, L55 0 / L56 407. Shopify: `success: true`,
  `available` delta 407 and `on_hand` delta 407 at Location 81898602562. No `ITEM_NOT_STOCKED_AT_LOCATION`
  (my expectation of step 7 was wrong about the error).
- **Checked in Shopify admin (read only)**:
  - Inventory list of location L56, item `WESAS21704EO1`: On hand 407, Available shown as "-".
  - `inventory_levels` of the item: still one level only, location 26869263 with 55.
  - Variant `inventory_quantity`: still 55 (not 462), `updated_at` unchanged (2026-11-21).
  - Adjustment history page for L56: blank.
- **Reading (mine)**: Shopify stored the quantity at L56, but the item is not stocked (active) at L56, so it does
  not count as sellable stock. eSyncMate marks the item SYNCED - a false success. The location must be turned on
  for the product (client in Shopify, or `inventoryActivate` in our upload = code change + Release7).
- **New finding**: stock at "1553 W 9000 S" is written by "Walker Edison: NetSuite API" / "NetSuite Integration
  by Celigo" (adjustments and order fulfilments, last 2026-11-21). A second system manages stock in this store.

### Step 13: End of day - the user continues tomorrow
- Saved in memory (`shopify-wke1000-client-deployment`). Resume with: who activates L55 / L56 for the products
  (client, or `inventoryActivate` in the upload route + Release7); the NetSuite / Celigo question for the client;
  put the `MAJID` test rows back; routes 157, 158, 159, 168 stay In-Active. Nothing committed.

## Summary
2026-11-24: the ERP feed for WKE1000 works (336 items). The upload failed because the UPC was sent as the item
number; the route `Shopify - Sync Products` (168) was set up at the client with scripts PS_01 - PS_05 and stored
the Shopify id of all 336 items. The upload now succeeds technically, but the stock at L56 is not sellable
because the product is not stocked at that location in Shopify. Open: location activation and the second
(NetSuite) stock integration.
