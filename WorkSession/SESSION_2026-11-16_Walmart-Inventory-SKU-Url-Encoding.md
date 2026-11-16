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

### Open points (not changed)
- On a failed call the route calls `feed.UpdateItemStatus` (sets `SYNCED`), so a refused item is not picked up
  again by itself. After the deploy this item has to be sent again (status reset or next inventory change).
- `SCSInventoryFeed.UpdateItemStatus / UpdateItemStatusError` build SQL by string concatenation with the ItemID.

## Summary
(fill in at end)
