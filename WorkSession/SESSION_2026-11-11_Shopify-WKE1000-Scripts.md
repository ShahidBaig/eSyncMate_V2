# Work Session: Shopify deployment scripts - client customer WKE1000
**Date**: 2026-11-11 to 2026-11-12
**Status**: In Progress - client DB scripts run 2026-11-12; backend / UI not deployed at the client yet (see "RESUME HERE")

## RESUME HERE (state on 2026-11-12, afternoon - client DB done, api.zip / ui.zip ready, not deployed)

### Done
- Scripts in `eSyncMate.Data/Tables/ShopifyScripts` are ready for the client: customer `WKE1000`, client DB connection
  in 03 / 04 / 10 / 11 / 12 / 14 / 15 / 18, SPARS address + key + secret in 04, demo Shopify store values in 02 / 03
  (temporary), feed connectors GET, new scripts 16 (nightly cleanup) and 19 (Flows).
- Code: UI connector dialog fix; `SCSFullInventoryFeedRoute.cs` retry limit, SPARS answer logged, code 407 = Error.
- Backend published to `D:\Deployment\BIZMATE\Release4\api` (2026-11-12 13:28): Processor + RouteWorker + AlertWorker,
  latest code inside, all three appsettings.json point to the client DB (192.168.0.44,7100). Verified from the files;
  Visual Studio froze after each publish and never showed "succeeded". NOT started / tested yet.
- Dev DB: Shopify setup renamed SHOPIFY01 -> WKE1000, Flows 51 - 54 created, all 11 routes In-Active.

- After the Windows restart (boot 13:42, no pending-reboot flags): user published again from Visual Studio. Output
  written 13:48 - 13:49:40 (435 files, Processor + RouteWorker + AlertWorker, all appsettings -> 192.168.0.44,7100).
  Visual Studio again went "Not Responding" on the Publish page after the last file (one CPU core busy, no more files
  written) - so the publish itself completes, only the VS window hangs. Cause of the hang NOT verified. Restart did
  not fix it. Suggested command line publish instead:
  `dotnet publish eSyncMate.Processor\eSyncMate.Processor.csproj -c Release -p:PublishProfile=FolderProfile`

- 13:52 user ran the command line publish: "Build succeeded with 246 warning(s)". Output went to
  `eSyncMate.Processor\bin\Release\net8.0\publish` (435 files), NOT to the profile's PublishUrl; and
  `D:\Deployment\BIZMATE\Release4\api` was left EMPTY (emptied 13:52:02, cause not verified - profile has
  DeleteExistingFiles=true). 13:56 I copied the publish folder to `D:\Deployment\BIZMATE\Release4\api` with robocopy
  (435 files, 0 failed; appsettings of all three -> 192.168.0.44,7100). For next time use an explicit output folder:
  `dotnet publish eSyncMate.Processor\eSyncMate.Processor.csproj -c Release -o D:\Deployment\BIZMATE\Release4\api`

- 14:04 Angular UI built: `npx ng build --configuration production` in `UI` (Node 22.23.4), exit 0, warnings only
  (CommonJS dependencies). Output `UI\dist\ui`, 22 files, 6.4 MB; API address in the bundle is
  `https://ems.safavieh.com:8085/` (environment.prod.ts), no `localhost:5000`. Copied to the new folder
  `D:\Deployment\BIZMATE\Release4\ui`. User has uncommitted change in `UI/src/environments/environment.ts`
  (also set to ems.safavieh.com:8085 - not used by the production build, and it changes the local `ng serve` target).
  User zipped the backend to `D:\Deployment\BIZMATE\Release4\eSyncMate\api.zip` (13:59).

- 14:08 UI rebuilt the way the team builds it: `npx ng build --base-href /eSyncMate` (production is the default
  configuration), exit 0. index.html has `<base href="/eSyncMate">` (no trailing slash - same style as the team's
  earlier BizMate builds, `<base href="/BizMate">` in Release5 / Release13 ui.zip). `D:\Deployment\BIZMATE\Release4\ui`
  replaced with this build (22 files). The 14:04 build without base-href is gone.

- 14:1x `D:\Deployment\BIZMATE\Release4\eSyncMate\ui.zip` created from `Release4\ui` (22 entries, files at the zip
  root like the earlier ui.zip files, base href /eSyncMate). Sits beside the user's `api.zip`.

- Client DB pre-check (user ran it in SSMS on EMSBD / ESYNCMATE; this machine cannot reach 192.168.0.44:7100 over the
  SonicWall VPN - connection refused): no customer WKE1000 / Shopify, 0 Shopify connectors / routes / partner groups,
  RouteTypes 80, 82-85 free (highest is 79). RouteTypes 78 + 79 exist since 2026-10-27, so Task 00005 was already
  deployed at the client. Its four SPs (Sp_SetWalmartPortalCancel, Sp_SaveWalmartCancelMessage,
  Sp_GetWalmartOrdersToCancelInERP, Sp_SetWalmartERPCancelResult) NOT checked yet - scripts 17 / 18 stop with an
  error if they are missing. Note: the folder `2026-10-22_Task00005_WalmartCancellations` named in the README is
  NOT in this repo (only `ShopifyScripts` is under `eSyncMate.Data\Tables`).

- Client DB: all four Task 00005 SPs present (user's check, all OK). Pre-checks done - client DB is ready for
  scripts 01 - 19 (skip 08b and _DEV_ONLY). Scripts not run yet.

- Client DB backup taken by the user before the scripts: `G:\SQL Server\ESYNCMATE_before_Shopify_20261112.bak`
  (COPY_ONLY, COMPRESSION, CHECKSUM), 19,012,662 pages (~145 GB of data) in 144 s, finished 2026-11-12 04:44:25
  server time (UTC-5). DB: ~148 GB data in 2 files (B: mdf, G: ndf), log 120 GB allocated / 3 GB used; a backup tool
  also takes full backups every 4 hours (last 04:00 server time). Delete the .bak when the deployment is confirmed.

- **CLIENT DB DEPLOYED 2026-11-12 (user ran 01 - 19 in SSMS, no errors; output reviewed):** ConnectorType 12
  Shopify, RouteTypes 80, 82 - 85, customer Id 15 `WKE1000` (Marketplace still "SHOPIFY ESYNCMATE DEV STORE"),
  partner groups 16 / 17, connectors 247 - 265 (all plain JSON, none encrypted), map 12, routes 157 - 167 (all
  In-Active), Flows 56 - 59 (In-Active, details 155 - 165), tables SCSInventoryFeed_WKE1000_Log /
  SCSInventoryFeedData_WKE1000, ApplicationSettings ShopifyUploadConcurrency = 3 (script 09 was run), Shopify
  branches present in Sp_SCS_GetProductCatalog / SP_OrdersData / feed summary SPs / nightly cleanup SPs.
  Still demo store: BaseUrl esyncmate-dev-store, locations L10 -> 88131272890, L21 -> 88131305658; Shopify Client
  secret blank on every Shopify connector (248, 252, 258, 262, 263). Backend / UI not deployed at the client yet.

### Not done / next steps, in order
**2026-11-12, end of day - user reports: api.zip, ui.zip and the scripts are all deployed at the client (not checked
by me). Everything Shopify stays In-Active. ON HOLD until (a) the ERP team opens the customer's inventory for
WKE1000 and (b) the client gives its Shopify account; the user will then ask for the client store setup
(steps 4 - 5 below). Step 2 below is done, step 3 not confirmed to me.**

Done on 2026-11-12: Windows restart, backend publish, UI build, client DB pre-checks, DB backup, scripts 01 - 19.
1. Push: local `main` is ahead of origin (65acf93, 160eb7b + the session-file commit). 65acf93 / 160eb7b hold the
   client DB password and the SPARS secret; my push is blocked by the safety check - the user pushes with
   `! git push origin main`.
2. Deploy at the client server (user asked how on 2026-11-12; general IIS steps given, server layout not known to
   me): `D:\Deployment\BIZMATE\Release4\eSyncMate\api.zip` and `ui.zip`. Back up both folders -> compare
   appsettings.json (zip points to 192.168.0.44,7100) -> stop the app pool, wait for RouteWorker / AlertWorker
   processes to end -> extract api.zip over the folder (keep `logs`) -> start pool -> check `/api/health` and
   `/dashboard`. UI: keep the server's web.config (the build has none), replace the hashed files, site must be served
   under `/eSyncMate`. NOTE: api.zip was made 13:59 from the 13:56 copy of the command line publish.
3. After the deploy: Setup > Flows / Routes show the 4 Shopify Flows + 11 routes In-Active; an existing customer's
   route still runs.
4. While the client DB holds the DEMO store: do not activate "Place Orders in ERP" / ERP cancellation routes; do not
   save a Shopify connector (247 - 265) on the Connectors screen (saving encrypts it, scripts then cannot update it).
5. When the client sends its store domain, Client ID, secret and locations: fill 02 + 03, re-run 02, 03, 10, 13, 15, 17.
6. When the deployment is confirmed: delete `G:\SQL Server\ESYNCMATE_before_Shopify_20261112.bak` on the client server.
7. Uncommitted, left alone: `UI/src/environments/environment.ts` (user pointed local dev at ems.safavieh.com:8085)
   and the generated `eSyncMate.RouteWorker/obj/.../GlobalUsings.g.cs`.

### Waiting on others
- ERP team: configure items for `WKE1000` (SPARS answers Code 407 "No items are configured against provided
  Customer ID"), with `CustomerItemCode` = Shopify inventory item number.
- Client: Shopify app in their organization (scopes as the demo app, API 2026-10), installed on their store.

### Open points (found, not fixed)
- Shopify upload type 80 is missing from `InventoryRouteHelper` (no coordination with the feeds inside a Flow).
- Flows screen moves a route's StartDate back 5 hours on every save.
- Test Run notification always says "Route completed successfully".
- Upload to a location where the item is not stocked: Shopify shows On hand but blank Available (ESM-SB-ICE / DAWN
  at My Custom Location on the demo store); cause not verified.
- Amazon MWA SPARS feed connectors 198 / 199 are POST (should be GET).
- `SP_InsertSCSInventoryFeed` never updates `CustomerItemCode` of an existing row.

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

### Step 16: Commit / push, then demo values in scripts 02 / 03 (user's decision)
- Pushed to origin/main: 2bfd27b (UI connector dialog), 654f187 (ShopifyScripts + view + session log),
  fdc2836 (SCSFullInventoryFeedRoute.cs). `appsettings.json` / `CommonUtils.cs` (dev connection strings) NOT committed.
- User decided to run the scripts at the client now with the DEMO store values and replace them when the client
  sends its own. User set `@CustomerName = 'WKE1000'`, `@Marketplace = 'SHOPIFY ESYNCMATE DEV STORE'` in 02.
  Added: 02 `@Locations` = L10 -> 88131272890, L21 -> 88131305658 (demo store); 03 `@ShopDomain` =
  esyncmate-dev-store.myshopify.com, `@ClientId` = 2878ddf0df5aeb8e4839b6392e701d02. Marked TEMPORARY in both files.
  Secrets (Client secret, SPARS secret, DB password) still blank. These two edits are NOT committed yet.
- **To do when the client's values arrive**: 02 + 03 with the real values, re-run 02, 03, then 10, 13, 15, 17.

### Step 17: Client DB connection string in the scripts (user's instruction; scripts only, dev DB untouched)
- Pushed f30e096 (demo store values in 02 / 03).
- User gave the client eSyncMate DB connection (192.168.0.44,7100 / ESYNCMATE) and wanted it in every script that
  makes a DB connector. `@DbConnectionString` is now set in 03 and 10 (existing setting) and was ADDED to
  04, 11, 12, 14, 15, 18: when filled it overrides the connection copied from "<P> - WHSW Inventory Get Data"
  (blank keeps the copied one). Scripts 05, 13, 17 make no DB connector of their own.
- Checked: all scripts compile (SET NOEXEC ON - nothing executed); no dev Shopify connector points at the client server.
- First left script 10 blank on my own judgement ("blank = copy from 03") - the user corrected that.
- NOT committed (the scripts now hold the client DB password; it is already in 6 committed files of the repo).

### Step: Client Shopify account checked in the browser (2026-11-12, evening) - BLOCKED on permissions
- Client's answers: store `walker-edison.myshopify.com`, plan Plus, developer access = yes. Client says the staff
  account "should have all permissions needed for API/App development" (Settings > Apps > Develop apps).
- Checked with the user's logged-in Chrome session (staff account of the user at esoftage.com), read-only:
  - Organisation "Walker Edison Furniture Co", 4 stores. The browser opens `devshop-we` first
    (`devshop-we.myshopify.com`, password-protected "Opening soon" storefront); `walker-edison` (walkeredison.com) also opens.
  - On BOTH stores every page says "You need permission to view this feature": Home, Orders, Products > Inventory,
    Settings > Apps, Settings > Apps > Development, Settings > Locations, Settings > Domains.
  - Dev Dashboard (dev.shopify.com/dashboard/1979525) opens: Apps = none, Stores = "No stores yet". "Create app" is offered.
- So there is NO Client ID / Client secret yet, and the location ids cannot be read.
- **Files changed**: `03_Setup_Connectors.sql` - `@ShopDomain` = `walker-edison.myshopify.com`, `@ClientId` = '' (the
  script throws 51373 until it is filled, so the demo app cannot be paired with the client store). NOT committed.
  `02_Setup_Customer.sql` unchanged (still the demo locations / Marketplace) - waiting for the location ids.
- **Open question for the client**: which store to connect - `walker-edison` (live) or `devshop-we` (their dev shop)?
- **Next**: client grants the permissions (apps development, view locations, products / inventory, orders) -> create the
  app in the Dev Dashboard (scopes for inventory, orders, fulfillment, order edits, locations), install it on the store,
  fill `@ClientId` / secret in 03 and `@Locations` / `@Marketplace` in 02, user re-runs 02, 03, 10, 13, 15, 17.

### Step: V2 independent script for the store switch (2026-11-12, user's request)
- User: 01 - 19 were executed at the client as they were (demo values); from now on wants independent "v2" scripts.
- **Files created**: `eSyncMate.Data/Tables/ShopifyScripts/v2/V2_01_Switch_To_Client_Store.sql`. README: new "V2" section.
- **What it does**: for `WKE1000` only, one transaction - Marketplace, TargetPlusShipNodes = exactly `@Locations`
  (others deleted), BaseUrl / ConsumerKey / ConsumerSecret on every Shopify-type connector named "Shopify - ...".
  Replaces the earlier plan "fill 02 + 03, re-run 02, 03, 10, 13, 15, 17".
- **Filled**: `@ShopDomain` = walker-edison.myshopify.com, `@Marketplace` = 'SHOPIFY WALKER EDISON' (my wording - confirm).
  **Still empty**: `@ClientId`, `@ClientSecret`, `@Locations` (script throws until filled; refuses demo values).
- **NOT verified**: could not compile-check it - dev DB (MCP, 110.93.227.0:1433) unreachable. Note: the repo's
  `eSyncMate.Processor/appsettings.json` connection string currently points at the CLIENT DB (192.168.0.44,7100);
  my NOEXEC check tried that by mistake, the connection timed out, nothing ran.
- Screenshots of the "You need permission" pages (Apps > Develop apps, Locations) given to the user for the client.
- Script 03 was left as committed in 02499c2 (client domain, empty Client ID) - no longer the way to switch stores.

### Step: Lead's decision - client installs the eSoftage app with a custom install link (2026-11-12, late)
- Lead generated a custom install link from the eSoftage app "eSyncMate Connector" (Client ID 2878ddf0...1d02).
  Decoded from the link: store `walker-edison.myshopify.com`, client organisation 1979525, expires ~2026-11-19.
  The eSoftage app is now tied to the client's organisation (as far as I know: permanent).
- Client e-mail with the link + steps written as a doc: https://claude.ai/artifact/Qu8Ftmkf7wsUuJcpVKiYYk
- **V2 script changed**: `@ClientId` = the eSoftage app's id (same as demo), guard that refused that id removed
  (the demo STORE is still refused). V2 is still needed: BaseUrl on the 5 Shopify connectors is the demo store's,
  the locations are the demo store's, and the Client secret is blank in the client DB.
- **Still missing**: Client secret (from the eSoftage Dev Dashboard, typed at run time, never committed) and the
  client store's location ids (majid account has no access to Settings > Locations).
- **RISK, not tested**: the connector uses the client-credentials token (SHOPIFYGetToken). I believe Shopify allows
  that only when app and store are in the same organisation; here they are not. First check after the install:
  Test Run of one route. If the token call fails, the app must be created in the client's organisation instead.
- Side work in the browser (user's request): personal trial store `yuvnir-tf.myshopify.com` created under the user's
  own Gmail Shopify account (name "My Store", no card entered). The lead's link does not work on it.
- Shopify accounts: majid at esoftage.com = the client's staff account; shahid = eSoftage's own (demo store + app).

## Summary
Scripts now default to `WKE1000` and hold no dev connection string. Not verified: the client DB itself (no access).
Open points for the team: nightly cleanup list, CustomerItemCode from the ERP (inventory item number), Task 00005
scripts must be at the client before 17 / 18, pre-check that `WKE1000` is not already a customer at the client,
ERP feed never tested end to end on dev, ship-via code and carrier fallback to confirm with the client.
