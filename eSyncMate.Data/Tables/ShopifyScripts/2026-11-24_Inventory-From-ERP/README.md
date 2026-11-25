# 2026-11-24 - WKE1000 (Walker Edison): inventory from the ERP into eSyncMate

The client has opened the inventory for customer `WKE1000` in SPARS. This folder checks that the inventory
arrives in eSyncMate (`SCSInventoryFeed`). It does **not** send anything to Shopify: that is the next step.

Run the scripts on the **client** database in SSMS. Scripts 01 and 02 only read.

## Steps

| # | What | Where |
|---|------|-------|
| 1 | Run `INV_01_Check_Before_Turning_On.sql`. Every `[Check]` must say OK or NOTE. | SSMS |
| 2 | Test Run **Shopify - Fetch Full Inventory from ERP** once. Do not activate it yet. | eSyncMate, Setup > Flows (or Routes) |
| 3 | Run `INV_02_Verify_After_Run.sql`. Compare with "A good run" below. | SSMS |
| 4 | Only if step 3 shows leftover temp rows or a stuck batch: `INV_03_Fix_If_Needed.sql` (dry run first), then step 2 again. | SSMS |
| 5 | When step 3 is good: activate the Full feed (Daily 06:45) and **Shopify - Fetch Differential Inventory from ERP** (every 15 min) from the screen, never with an UPDATE (that leaves `JobID` empty). | eSyncMate |
| 6 | After the first Differential run: `INV_02_Verify_After_Run.sql` again. | SSMS |

Keep **Shopify - Upload Warehouse-Wise Inventory** In-Active during all of this.

## A good run (INV_02)

- Grid 1: the newest batch is `Completed` with a page count.
- Grid 2: a line `ERP inventory for [WKE1000] batch [...]: N page(s) with M item(s) received from SPARS.` and no
  ERROR / EXCEPTION line.
- Grid 3: `M` items; `NEW` after the first run, `UPDATED` after later runs.
- Grid 4: the same `M` in both history tables.
- Grid 5: 0 leftover temp rows.

## When it is not good

| What INV_02 shows | Meaning | What to do |
|---|---|---|
| Grid 2 empty, no new batch | The route did not run | Start the Test Run again; check the RouteWorker / Scheduler View |
| `SPARS has no inventory feed for [WKE1000]: Code 407` | SPARS still has no items for the customer | ERP team: the items are not configured yet. Nothing to fix in eSyncMate |
| `SPARS ... gave no inventory page ... HTTP 401 / 403` | SPARS key or secret refused | Setup > Connectors > `Shopify - SPARS Full Inventory Feed`: compare with another customer's SPARS connector |
| `HTTP 405` | Connector method is POST | Set Method to GET on the SPARS feed connector |
| Batch `Completed`, 0 items, line "answered ... with Success ..., 0 item(s)" | SPARS answered without items | Send the logged SPARS message to the ERP team |
| Batch stays `Processing`, temp rows left | The run was cut off | `INV_03_Fix_If_Needed.sql`, then Test Run again |
| Grid 6: "cannot be sent yet" | Not a feed problem. The item has no Shopify inventory item number yet | Next step: product sync / the number in `CustomerItemCode` |
| Grid 7: stock in a warehouse that is not mapped | Not a feed problem. That stock is not sent to Shopify | Next step: Warehouse Mapping screen, if the warehouse should be sent |

## Good to know

- A finished run sends the acknowledgement `inventory-feed-acknowledgement` to SPARS for the customer. This is the
  normal feed behaviour for every customer. A run that ends in an error sends none.
- The Full feed and the Differential feed of one customer never run at the same time: the second one skips.
- `INV_03` changes only the temp table and the status of stuck batches. It makes a backup
  (`ZDB_INV03_Temp`, `ZDB_INV03_Batches`); `INV_99_Revert_Fix.sql` puts it back and drops the backup.

## Tested

2026-11-24 on the dev database: 01 and 02 run without error; 03 and 99 run in dry-run mode, and once with
`@Apply = 1` (fix, then revert) inside a transaction that was rolled back. Not run on the client database yet.
