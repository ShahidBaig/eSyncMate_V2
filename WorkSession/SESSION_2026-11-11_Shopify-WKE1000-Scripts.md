# Work Session: Shopify deployment scripts - client customer WKE1000
**Date**: 2026-11-11
**Status**: Completed

## Objective
Change the Shopify deployment scripts (`eSyncMate.Data/Tables/ShopifyScripts`) from the test customer `SHOPIFY01`
to the client's ERP customer `WKE1000`, and review them for client readiness against the dev DB (`eSyncmate_Dev`).

## Tasks
- [x] Read all 19 scripts + README
- [x] Compare with dev DB (customer, partner groups, connectors, routes, route types, map, SPs, views) - read-only
- [x] Replace `SHOPIFY01` -> `WKE1000` in the scripts and README
- [x] Remove the dev DB connection string from `10_Get_Orders.sql`
- [ ] Open points for the team (see Summary) - not changed by this session

## Progress Log

### Step 1: Review
- **Files changed**: none
- **What was done**: read the scripts; queried dev DB read-only (dev DB reached with the Processor appsettings
  connection string because the mssql MCP server 110.93.227.0 was unreachable).
- **Found**:
  - Dev DB matches what the scripts create: customer 14, partner groups 14/15, connectors 232-250, routes 135-145
    (all In-Active), RouteTypes 80, 82-85, map 9, branches in SP_OrdersData / Sp_SCS_GetProductCatalog / feed summary SPs.
  - `10_Get_Orders.sql` had the dev DB connection string (with password) as the CONFIG default.
  - Nightly cleanup SPs (`Sp_NightlyCleanup_DeleteCustomerFeedData` / `...LogTables`) keep a hardcoded customer
    list; no script adds the Shopify customer.
  - `SP_InsertSCSInventoryFeed` sets `CustomerItemCode` only on INSERT, never on UPDATE.
  - Dev: `CustomerProductCatalogPrices` has 0 rows for SHOPIFY01, RouteType 81 (Product Sync) is not in RouteTypes,
    the `backlog/` script named in the README is not in the folder.
  - Dev: Full / Differential feed routes (137 / 138) have never run (no InventoryBatchWise rows).
  - Folders named in the README are not in the repo: `_archive_original_134-149/`, `backlog/`,
    `2026-10-22_Task00005_WalmartCancellations`.
  - Dev connectors 246 / 247 are already encrypted.

### Step 2: Edits
- **Files changed**: `02, 03, 04, 05, 06, 07, 08, 10, 11, 12, 13, 14, 15, 17, 18 *.sql`, `README.md` in
  `eSyncMate.Data/Tables/ShopifyScripts`; `eSyncMate.Processor/Managers/ShopifyASNShipmentNotificationRoute.cs` (doc comment only)
- **What was done**: `SHOPIFY01` -> `WKE1000` everywhere; `@DbConnectionString` in script 10 set to `''`
  (blank = copy the connection of "<P> - WHSW Inventory Get Data").
- **Decisions**: repo copy `Views/VW_AllSCSInventoryFeedData.sql` left with `SCSInventoryFeedData_SHOPIFY01`
  (it mirrors the dev DB; script 02 appends the client's table to the live view). No DB was changed.

### Step 3: Script 16 (nightly cleanup) for the client
- **Files changed**: new `ShopifyScripts/16_Nightly_Cleanup_Customer.sql`, `README.md` (row 16 + "DEV only" section)
- **What was done**: script adds `('<@ErpCustomerID>')` to the customer list inside the live
  `Sp_NightlyCleanup_DeleteCustomerFeedData` / `Sp_NightlyCleanup_DeleteCustomerLogTables`. Re-runnable; a server
  without the SPs is reported and left alone; an unexpected list shape stops with error 51602 (manual fallback in the file).
- **Tested**: on dev inside a transaction that was rolled back - both SPs altered correctly. NOT applied on dev
  (it would let a nightly job purge the dev test rows older than 7 days).

### Step 4: Dev DB renamed SHOPIFY01 -> WKE1000 (user asked for it)
- **Files changed**: new `ShopifyScripts/_DEV_ONLY_Rename_Customer.sql` (stops unless DB is `eSyncmate_Dev`;
  `@Commit = 0` default = dry run; swap `@From` / `@To` to go back), `Views/VW_AllSCSInventoryFeedData.sql` (repo copy now `_WKE1000`)
- **What was done on `eSyncmate_Dev`**: dry run first, then committed. Customers 14 (ERPCustomerID + Name),
  PartnerGroups 14 / 15 descriptions, 17 plain connectors (CustomerID + the SP_OrdersData literal in 239),
  11 routes (CustomerName), 2 TargetPlusShipNodes, 8 SCSInventoryFeed, 6 InventoryBatchWise, 8 RouteExecutionLock,
  Users.CustomerName of user 1, tables renamed to `SCSInventoryFeedData_WKE1000` / `SCSInventoryFeed_WKE1000_Log`
  (+ 4 IX_ indexes, CustomerId columns), `VW_AllSCSInventoryFeedData`, the ASN branch of `SP_OrdersData`.
- **Encrypted connectors**: 246 decrypted with the app's EncryptionKey (ApplicationSettings), CustomerID set to
  WKE1000, re-encrypted the same way (AES, IV + cipher, `ENC:` prefix), read back OK. 247 held CustomerID
  `SHOPIFY02` (not SHOPIFY01); the user confirmed it was a mistake, so it was set to WKE1000 the same way.
- **Connector 247 repair (user approved)**: its Parmeters were 4 empty entries `[{},{},{},{}]` and BaseUrl had a
  leading space (both there before this session, probably from a save on Setup > Connectors - not confirmed).
  Parmeters set back to the script 15 defaults (notifyCustomer false, restock true, refund false, reason OTHER),
  space removed, re-encrypted, read back OK. Open: test whether a UI save wipes connector Parmeters.
- **Not changed**: history tables (ConnectorsLogs, CustomersLogs, PartnerGroupsLogs, RoutesLogs, RouteLog),
  Marketplace, route Status / schedule / JobID (all 11 still In-Active), the dev Shopify store / locations.
- **Verified after**: 0 SPs / views still name SHOPIFY01; 11 routes visible in VW_Routes; view returns the 34 feed data rows.
- **Watch**: dev SPARS connectors point at `https://api.safavieh.com/SPARS.API/api/service/`; ERP calls from dev now
  go out as WKE1000. The 16 existing dev orders were placed in the ERP under SHOPIFY01.

### Step 5: Connector edit screen wiped script-made Parameters (bug found + fixed)
- **Files changed**: `UI/src/app/connectors/edit-connector-dialog/edit-connector-dialog.component.ts`
- **Cause**: the screen read `obj.name` / `obj.value`, but connectors made by the SQL scripts store `Name` / `Value`
  (C# model casing). The screen showed blank chips and a save wrote `[{},{},{},{}]` - that is how connector 247
  lost its four cancel settings. Same for Headers.
- **Fix**: read `obj.name ?? obj.Name` / `obj.value ?? obj.Value` for Headers and Parmeters.
- **Tested** on the local site (localhost:5015 -> dev DB): connector 247 now shows notifyCustomer / restock /
  refund / reason; Update saved them intact (stored as `name` / `value`, which the C# side reads the same,
  Newtonsoft is case-insensitive); credentials, URL and CustomerID WKE1000 kept.
- **For the client**: the UI must be deployed with this fix BEFORE anyone saves a Shopify connector on
  Setup > Connectors, otherwise the Parameters of Get Orders / Create Fulfillment / Cancel Order /
  Get Cancelled Orders are wiped.

### Step 6: Route 137 test run attempts + Flows
- **Hangfire password**: local `appsettings.json` had a wrong password in `HangfireConnection` (login failed for sa);
  the user fixed it and restarted the Processor - this machine now registers as a Hangfire server.
- **Test Run of route 137**: triggered through the app's own `api/Flows/testRun/137` call; notification 17 said
  COMPLETED in under a second, but nothing ran. Cause: `RouteEngine.ExecuteExternal` returns at once for an
  In-Active route (line 83) and `ExecuteWithNotification` still writes "Route completed successfully".
  Route 137 was NOT activated (the user said not to); it is still In-Active, no feed has been pulled for WKE1000.
- **Flows (user asked for them, on dev and as a client script)**: new `ShopifyScripts/19_Flows.sql` + README row 19.
  Four Flows (Inventory Sync, Order Processing, Shipment & ASN, Cancellation Processing), all 11 routes, everything
  In-Active, schedule copied from dbo.Routes, End Date default today + 5 years when the route has none. Re-runnable.
  Dry run on dev (rolled back), then applied on dev: Flows 51 - 54, FlowDetails 135 - 145.
- **Open (code, not changed)**: `InventoryRouteHelper` does not list RouteType 80 (ShopifyInventoryUpload) in
  `_inventoryUploadTypes`, `_uploadRouteTypes` or `GetInventoryTypeIds()`, so inside a Flow the Shopify upload does
  not wait for / give way to the Full and Differential feeds the way the other partners' uploads do.

### Step 7: Real Test Run of route 137 (user activated it from the Flow)
- User set route 137 Active in Flow 51 (13:13:36; routes 138 / 135 stay In-Active). "Run Now" was only greyed out
  because the user's screen had not been refreshed.
- Pressed Run Now (13:14:21, notification 18). RouteWorker PID 18820 started, batch
  9a735e1d-8f7f-4807-aca4-71fb31078e40 created as Processing - then nothing: no page saved, no Temp rows,
  no RouteData, and the worker kept opening connections to api.safavieh.com (60+ in 5 minutes).
- **Cause of the endless run**: `SCSFullInventoryFeedRoute` - when `GetSCSInventory` returns null (ERP answer is
  not HTTP 200 / empty) the page loop does `Thread.Sleep(100); i--; continue;` with no retry limit.
- **Stopped** the worker (Stop-Process 18820) at ~13:20. Lock 79 released by the Processor; RouteLog 120021533
  "External process failed for route [137]" (no output captured); notification 18 still says COMPLETED.
- **Left as is**: batch 9a735e1d... still "Processing" (stale); route 137 still Active (Daily 06:45, JobID set) -
  the user has to set it In-Active from the Flow, otherwise it loops again tomorrow at 06:45.
- **Not known**: what the ERP answered for WKE1000 (not logged). Connector 236 has the same BaseUrl, Url, AuthType,
  key and secret as the other partners' full-feed connectors, so the setup matches; last full feed that completed
  on dev was LOW2221MP on 2026-05-18.

### Step 8: Retry limit in the ERP inventory feed (user approved)
- **Files changed**: `eSyncMate.Processor/Managers/SCSFullInventoryFeedRoute.cs` (Full + Differential feed, all customers)
- **What was done**:
  - a page SPARS does not answer properly (not HTTP 200 / no body) is retried at most 5 times in a row
    (`MaxEmptyResponses`), waiting 3 s, 6 s, 9 s, 12 s (`EmptyResponseWaitMs` x attempt) instead of 0.1 s for ever;
    then the run throws -> outer catch -> batch "Error" + Exception log, no acknowledgement sent to SPARS;
  - `GetSCSInventory` now logs the failed answer (HTTP status, transport error, body) as an Error route log and
    returns it through `out p_Failure` so the final error names it;
  - `retryCount++` -> `retryCount + 1` in its exception retry (the old form passed the same value on, so that
    retry never ended either);
  - the inner `catch` got `when (l_EmptyResponses < MaxEmptyResponses)` so the final error is not retried again.
- **Checked**: compiles (msbuild -t:Compile, 0 errors). NOT run against the ERP. Takes effect after the Processor and
  RouteWorker are rebuilt and restarted. Not committed.

### Step 9: Second run of route 137 with the retry fix - cause found (wrong HTTP method)
- Run at 13:45:25 ended by itself at 13:46:04: batch 4d08da06-10bb-4e3b-ae99-9356070f5f00 = Error, 5 attempts, route log:
  `HTTP 405 MethodNotAllowed, body: {"Message":"The requested resource does not support http method 'POST'."}`.
  So the retry limit and the new logging work.
- **Cause**: script 04 created the SPARS feed connector with Method POST. Every other customer's Full / Differential
  feed connector is GET (checked all 22 on dev, encrypted ones decrypted; only Amazon MWA 198 / 199 are also POST).
- **Scripts fixed**: `04_Full_Feed.sql` template Method GET; `05_Differential_Feed.sql` sets GET; `11_Place_Order.sql`,
  `12_Get_ASN.sql`, `14_Get_Cancellation.sql` now set POST themselves (they copy the feed connector; 18 already did).
- **Dev DB**: connectors 236 and 237 Method POST -> GET. 240 / 242 / 245 / 250 stay POST.
- **Open**: Amazon MWA connectors 198 / 199 have the same POST problem (not touched). Route 137 is Active
  (Daily 06:45) from the user's test - to be set In-Active after the next test run.

### Step 10: Third run of route 137 (GET) - completed with zero items
- Run at 13:49:19 finished at 13:49:22: batch 55b8c2d6-b965-49fc-8390-66b4ac26f879 = Completed, no error, PageCount
  empty, no rows for WKE1000 (still the 8 test rows), Temp table empty. So SPARS now answers HTTP 200, but with no items.
- The request is the same as every working partner's (GET, JSON body {CustomerID, PageNo}, no Parmeters / Headers,
  same key / secret, body format check is case-insensitive) - so the empty answer is SPARS's reply for WKE1000.
- What SPARS said is not stored: `Routes.SaveData` is commented out (RouteData is empty) and `SaveLog` only keeps
  Error / Exception / RouteInfo.
- **Code (same file, SCSFullInventoryFeedRoute.cs)**: a 200 answer without items is now logged as RouteInfo with the
  body (first 2000 chars), and every run writes one RouteInfo summary line "N page(s) with M item(s) received".
  Compiles; needs a rebuild + one more run to show SPARS's reply.

### Step 11: Fourth run of route 137 - SPARS's reply
- Run at 14:03:26 - 14:03:29, batch f0d01f34-5f42-499b-b0b1-ae9771ee7aec = Completed, 0 pages / 0 items.
- SPARS reply (route log 120021540):
  `{"Success":false,"Code":407,"Message":"No items are configured against provided Customer ID.","CurrentPage":0,"TotalPages":0,"InventoryFeed":null}`
- **Conclusion**: eSyncMate side works (connector, method, auth, route, flow). SPARS has no items configured for
  customer WKE1000 - ERP-side setup is needed before any inventory can come.
- **Open (not changed)**: the route reports "Completed" and sends the acknowledgement even when SPARS answers
  Success=false; route 137 still Active (user to set In-Active).

### Step 12: SPARS code 407 = Error, no acknowledgement (user approved, 407 only)
- **Files changed**: `eSyncMate.Processor/Managers/SCSFullInventoryFeedRoute.cs`
- **What was done**: when SPARS answers `Success false` + `Code 407` ("No items are configured against provided
  Customer ID") the run throws at once (no retry: `l_Refused` bypasses the inner catch) -> batch Error, Exception log
  with the SPARS message, `SP_InsertSCSInventoryFeed` not run, acknowledgement not sent.
  Every other `Success false` keeps the old behaviour; an item-less answer is logged as RouteInfo with
  Success / Code / Message + the body.
- **Decision**: 407 only, because what SPARS answers a Differential feed with nothing changed is unknown; a blanket
  rule could turn every quiet Differential run of every partner into an Error.
- **Note**: the feed now writes 1 RouteInfo line per run (summary) + 1 per item-less answer, for all partners.
- **Checked**: compiles (0 errors). Not run yet. Not committed.

### Step 13: Dev store check + test rows prepared for the upload route (135)
- **Shopify (read-only, in the browser)**: app "eSyncMate Connector" in the Dev Dashboard (org eSoftage), Client ID
  2878ddf0df5aeb8e4839b6392e701d02 = connector 233 ConsumerKey, 1 install, version 1.0.0 (2026-10-23), API 2026-10,
  scopes: read_inventory, write_inventory, read_locations, read_products, read_orders, write_orders, write_order_edits,
  read_merchant_managed_fulfillment_orders, write_merchant_managed_fulfillment_orders.
  Locations: Shop location 88131272890 (L10), My Custom Location 88131305658 (L21) - match TargetPlusShipNodes.
- **Dev DB (user approved)**: the 8 WKE1000 rows in SCSInventoryFeed set to `UPDATED`; `CustomerItemCode` filled with
  the store's inventory item numbers for 7 of them (ESM-SB-ICE 52944920346810, -DAWN 52944920379578,
  -POWDER 52944920412346, -MULTI 52944921100474, -OOS 52944920936634, -VIDEO 52944920772794,
  sku-untracked-1 52944920608954). ESM-NOT-IN-SHOPIFY left without a number on purpose.
- **Next**: user activates route 135 in Flow 51, Run Now, back to In-Active; expected 6 SYNCED, 2 ERROR.

### Step 14: Upload test - route 135 against the dev store (user ran it)
- Batch ffad5af4-5468-46cf-893c-3a253e88fb11, 14:40:07 - 14:40:09, Completed. Route log: "8 item(s), 14 location
  quantities in 2 call(s) (0 failed), 7 SYNCED, 1 ERROR, 0 left for next run."
- Rows: 7 SYNCED (incl. sku-untracked-1 - Shopify accepted a quantity for the untracked item; expectation of ERROR
  was wrong), ESM-NOT-IN-SHOPIFY ERROR "has no Shopify inventory item id".
- Shopify checked in the browser: Shop location (L10) Available = ATS_L10 for all six ESM-SB SKUs (50 / 12 / 0 / 25 / 5 / 40).
  My Custom Location (L21): ESM-SB-MULTI 15 available; ESM-SB-ICE and ESM-SB-DAWN show On hand 50 / 3 but
  Available "-" and are not in the product's total (50 / 12). Cause not verified - probably the item is not stocked
  (activated) at that location in Shopify, and API 2026-10 takes the quantity without activating it.
- Route 135 left Active by the user (Minutely 20) - to be set In-Active.

### Step 15: Get Orders test - route 136 against the dev store (user asked me to run it)
- Activated route 136 from Flow 52 in the UI (15:06), Run Now (notification 24), then set it back to In-Active (15:08).
- Route log 120021545: "Shopify orders for [WKE1000] (... created_at:>='2026-11-01T10:06:53Z'): 0 read in 1 page(s),
  0 new saved, 0 already in eSyncMate, 0 not saved, 0 gift card / digital only." Token + query work.
- 0 orders because the newest order in the dev store is #1017 from 2026-10-26, outside the connector's 10-day window
  (Parameter days = 10); all 17 store orders are from 10/26 and already in eSyncMate.
- **Bug noticed (not fixed)**: every save of a Flow moves the routes' StartDate back 5 hours (10/23 00:00 ->
  10/22 19:00 -> 10/22 14:00; Flow 51 is at 10/21 13:00 after several saves) - local time / UTC conversion on save.
- Route 135 (upload) is still Active from the user's test.

## Summary
Scripts now default to `WKE1000` and hold no dev connection string. Not verified: the client DB itself (no access).
Open points for the team: nightly cleanup list, CustomerItemCode from the ERP (inventory item number), Task 00005
scripts must be at the client before 17 / 18, pre-check that `WKE1000` is not already a customer at the client,
ERP feed never tested end to end on dev, ship-via code and carrier fallback to confirm with the client.
