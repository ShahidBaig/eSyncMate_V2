# Work Session: Walmart Upload Inventory - SKU with special characters
**Date**: 2026-11-16
**Status**: In Progress

## Objective
Customer WAL4001MP, item `122D100W[5PC]WHS`: Walmart inventory upload fails. Find the cause and fix it.

## Tasks
- [x] Read the sent / received JSON (`D:\MOM\EDI_Emmail\WAL4001MP`)
- [x] Fix the URL building in the route
- [ ] Build, deploy Processor + RouteWorker, resend the item (user)

## Progress Log

### Step 1: Cause
- **Received**: `{"fault":{"faultstring":"Invalid path PUT /marketplace-apis/v3/inventories/122D100W[5PC]WHS HTTP/1.1","detail":{"errorcode":"protocol.http.InvalidPath"}}}`
- The sent body is fine (15 ship nodes, 23 units on node 913517333036257281). The request is refused at Walmart's
  gateway because the SKU is put into the URL path as it is, and `[` `]` are not allowed there.
- `WalmartUploadInventoryRoute.cs` built the URL as `BaseUrl + "inventories/" + ItemId` with no encoding.
  `RestConnector` passes the string to RestSharp 110.2.0 as the resource, which does not encode it.

### Step 2: Fix
- **Files changed**: `eSyncMate.Processor/Managers/WalmartUploadInventoryRoute.cs` (ProcessItem)
- **What was done**: `Uri.EscapeDataString(itemId)` on the SKU path segment -> `.../inventories/122D100W%5B5PC%5DWHS`.
  SKUs with only letters, digits, `-`, `_`, `.`, `~` produce the same URL as before.
- **Not done**: not built, not run against Walmart.

### Step 3: Source of the report, commit
- The issue was reported by the client by e-mail; the two JSON files were taken from the inventory Files popup
  (Sent / Received). The e-mail itself is not in the folder and was not read.
- A reply to the client was drafted in the chat (finding, impact, fix in the next deployment); not sent.
- Fix and this session file committed on `main` (not pushed).

### Step 4: Same fix for every route that puts a data value into a URL (user request)
- **New file**: `eSyncMate.Processor/Managers/UrlValue.cs` - `UrlValue.Escape(object)` =
  `Uri.EscapeDataString(Convert.ToString(value) ?? "")`. Ordinary ids (letters, digits, `-` `_` `.` `~`) give the
  same URL as before.
- **Files changed (28, 61 lines)**: every live line in `eSyncMate.Processor/Managers` where an order number,
  product id, line number or SKU from a data row / API order is put into a URL path or query string:
  ASNShipmentNotificationRoute, AmazonASNShipmentNotificationRoute, AmazonGetOrdersRoute, BulkUploadOldPricesRoute,
  BulkUploadPricesRoute, CancellationLinesRoute, Download856FromShipStationRoute, Knot / Lowes / Macys / Micheal
  ASN + GetOrder routes, ProductCatalogRoute, SCSASNRoute, SCSBulkItemPricesRoute, SCSCancelOrderRoute,
  SCSGetOrdersRoute, SCSItemPricesRoute, SCSUpdateInventoryRoute, TargetPlusInventoryFeedWHSWiseRoute,
  VeeqoUpdatedProductsQTYRoute (`query={itemID}` = SKU), WalmartASNShipmentNotificationRoute,
  WalmartCancellationLinesRoute, WalmartGetOrdersRoute, WalmartUploadInventoryRoute (now uses the helper).
- **Left as they are on purpose**: connector config values (BaseUrl, Url, Realm), dates and paging numbers in
  query strings, ids returned by the marketplace inside the same call (Amazon feed / document ids, Mirakl
  import ids, Target report / status ids, Veeqo numeric ids), commented-out lines, `RouteTestApp`.
  Routes that send the SKU in the body or a file (Lowes, Macys, Knot, Michaels, Amazon, Shopify, Walmart bulk)
  had nothing to change. `ProductCatalogStatusRoute` already escaped the item id.
- **Issue**: the first `sed -i` run rewrote line endings in every file of the folder; reverted with
  `git checkout -- eSyncMate.Processor/Managers` and re-run with `sed -b` on the matching files only.
- **Check**: `dotnet build eSyncMate.Processor -c Debug` -> Build succeeded, 0 errors. Not run against any
  marketplace. Committed on `main` (not pushed).

### Step 5: Backend published (2026-11-17)
- Commit `f79a806` (sweep) on top of `b3f4c9f` (Walmart fix).
- **Command** (run in `D:\Projects\eSyncMate_V2`; one command publishes Processor + RouteWorker + AlertWorker):
  `dotnet publish eSyncMate.Processor\eSyncMate.Processor.csproj -c Release -o D:\Deployment\BIZMATE\Release5\api`
- **Output**: `D:\Deployment\BIZMATE\Release5\api`, 435 files, written 2026-11-17 12:28. `UrlValue` is inside
  `eSyncMate.Processor.dll` of the API and of `RouteWorker`. The three `appsettings.json` and `web.config` are
  byte-identical to `Release4\api` (client DB). `Release4` was not touched.
- **Zip**: `D:\Deployment\BIZMATE\Release5\eSyncMate\api.zip`.
- UI not rebuilt (no UI change). Not deployed, not started, not tested.

### Step 6: Memory saved, session committed (2026-11-17)
- Session file with the publish step committed as `5def375`.
- Two Claude memories written (outside the repo): the backend publish procedure for the client, and the open
  follow-up of this issue (client deploy of Release5, resend of the refused SKUs, task number for the commits).
- Still open: deploy at the client, run the two queries, decide on "failed call is marked SYNCED".

### Step 7: Deployment at the client - RouteWorker folder locked (2026-11-17)
- Client API folder: `C:\inetpub\wwwroot\eSyncMateAPI_SSL` (subfolder `RouteWorker`).
- **Issue**: with the IIS pool stopped the `RouteWorker` folder could not be deleted: "file is open in
  eSyncMate.RouteWorker.exe". `tasklist | findstr /i "eSyncMate"` showed four `eSyncMate.RouteWorker.exe`
  (PIDs 37704, 27492, 14740, 22696, session Services, about 300 MB each). `taskkill /F` gave "Access is denied":
  the user's server account (`s.baig`) has no admin rights and the workers run under the app pool account.
- **Advice given**: wait until the workers finish (pool stopped = no new ones), or Task Manager > Details > End task,
  or ask the client's server admin; do not work around the access control.
- **Result (reported by the user)**: the folder is deleted. Which way it was freed was not said.
- Not confirmed yet: new files copied, pool started again, Walmart upload inventory re-run.
- For next time: stop the pool, then wait for `tasklist | findstr /i "eSyncMate"` to print nothing before
  replacing files; the server commands must be cmd (not PowerShell) unless a PowerShell window is opened.

### Step 8: Deployed at the client, resend of the item started (2026-11-17, server time = USA, about 9-10 h behind PKT)
- User deployed Release5 at the client (`C:\inetpub\wwwroot\eSyncMateAPI_SSL`) around 03:04 server time.
- Route `Walmart - Upload Inventory` is Active, every 30 minutes. Source SP takes `SCSInventoryFeed.Status IN
  ('UPDATED','NEW')`. The nightly ERP feed (about 01:00) marks the item for upload even with an unchanged
  quantity (23 on 16 and 17 Nov), so my earlier statement "only on a quantity change" was wrong.
- Item `122D100W[5PC]WHS`: last `InvalidPath` 03:03:02 (batch started 01:30 with the old code, cut off by the
  deployment, stays "Processing"). User set the item to `UPDATED` at 03:28 (1 row) and pressed Run Now.
- Batch `2049b4b4-...` started 03:05:06 (after the deployment) is still running: 21,006 `UPDATED` items at about
  03:39, 19,518 at 03:41. The item is not in it (it was `SYNCED` at 03:05). Run Now started no second batch.
- 404 "No item found" lines in RouteLog are items not listed at Walmart; unrelated.
- **Waiting**: batch to finish (estimate 04:15 - 04:20), next run 04:30 picks the item. Check after 04:35:
  new `JSON-RVD` row for the item dated after 03:28 with ship nodes = fix confirmed. NOT confirmed yet.
- `D:\Deployment\BIZMATE\Release5pi` is no longer on the dev machine (only `Release5\eSyncMatepi.zip`).

### Open points (not changed)
- On a failed call the route calls `feed.UpdateItemStatus` (sets `SYNCED`), so a refused item is not picked up
  again by itself. After the deploy this item has to be sent again (status reset or next inventory change).
- `SCSInventoryFeed.UpdateItemStatus / UpdateItemStatusError` build SQL by string concatenation with the ItemID.

## Summary
(fill in at end)
