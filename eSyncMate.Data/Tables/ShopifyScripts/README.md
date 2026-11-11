# Task 00006 - Shopify: deployment scripts

Run the scripts **in number order**, one flow step at a time. Every script can be run **as often as needed**:
what is missing is inserted, what exists is updated with the script's values -

- **RouteTypes / customer / warehouse mapping / map / ApplicationSettings**: updated to the script's values.
- **Connectors**: updated, but a secret / connection string left **blank** in CONFIG keeps the stored value.
  An **encrypted** connector (saved from Setup > Connectors) cannot be read by SQL: it is left as it is and listed.
- **Routes**: parties, connectors, map, partner group, group and customer are updated; **Status, schedule and
  JobID are never touched** (Setup > Routes / the Flow own them and the Hangfire job).
- **Live SP branches** (06 `Sp_SCS_GetProductCatalog`, 13 `SP_OrdersData`): the Shopify branch is replaced by the
  script's text; every other branch stays as it is.
- Later scripts **copy** the 03 / 04 connectors: after changing a value in 03 or 04, run the later scripts again.

New routes are created **In-Active**: activate them from the UI (Setup > Routes or a Flow),
never with an UPDATE (that leaves JobID NULL). Deploy **Processor AND RouteWorker** before the first Test Run.

Use the same `@ErpCustomerID` (default `WKE1000`) and `@Prefix` (default `Shopify`) everywhere.

## Secrets are typed in two places only

| Script | CONFIG values |
|---|---|
| `03_Setup_Connectors.sql` | eSyncMate DB connection string, Shopify store domain, API version, app Client ID / **Client secret** |
| `04_Full_Feed.sql` | SPARS BaseUrl / ConsumerKey / **ConsumerSecret** |

Every later script **copies** these connectors (`<P> - WHSW Inventory Get Data`, `<P> - WHSW Inventory Upload`,
`<P> - SPARS Full Inventory Feed`), so the values are not typed again. That only works while those connectors are
still plain JSON: run all the scripts **before** anyone opens and saves a connector in Setup > Connectors
(saving encrypts it). Clear the secrets from your copy of 03 / 04 after running; never commit or e-mail a filled-in copy.

## Run order

| # | Script | Flow step | What it creates |
|---|--------|-----------|-----------------|
| 01 | `01_Setup_RouteType_ConnectorType.sql` | Setup | RouteType 80 + ConnectorType `Shopify` |
| 02 | `02_Setup_Customer.sql` | Setup | CONFIG: customer id, Marketplace, warehouse -> Shopify location. Customer, partner groups, `SCSInventoryFeedData_<id>`, `SCSInventoryFeed_<id>_Log`, view, Warehouse Mapping rows |
| 03 | `03_Setup_Connectors.sql` | Setup | **Secrets.** `<P> - WHSW Inventory Get Data` (eSyncMate SQL) + `<P> - WHSW Inventory Upload` (Shopify) |
| 04 | `04_Full_Feed.sql` | **Full Feed** | **SPARS secret.** `<P> - SPARS Full Inventory Feed`, `<P> - eSyncMate Inventory feed`, route `<P> - Fetch Full Inventory from ERP` (RT 7, Daily) |
| 05 | `05_Differential_Feed.sql` | **Differential** | `<P> - SPARS Differential Inventory Feed`, route `<P> - Fetch Differential Inventory from ERP` (RT 8, Minutely 15) |
| 06 | `06_Upload_Inventory_SP_Branch.sql` | Upload Inventory | Branch `ShopifyInventoryUpload` in the **live** `Sp_SCS_GetProductCatalog` |
| 07 | `07_Upload_Inventory_Route.sql` | Upload Inventory | Route `<P> - Upload Warehouse-Wise Inventory` (RT 80) |
| 08 | `08_Upload_Inventory_FeedSummary.sql` | Upload Inventory | Shopify route type in the Inventory Feed Summary SPs |
| 08b | `08b_Upload_Inventory_FeedSummary_SP_ONLY_IF_NEEDED.sql` | Upload Inventory | Only when the Inventory Feed Summary screen fails with "too many arguments specified" |
| 09 | `09_Upload_Inventory_Concurrency_OPTIONAL.sql` | Upload Inventory | Optional `ShopifyUploadConcurrency` setting (default 3 without it) |
| 10 | `10_Get_Orders.sql` | **Get Orders** | RouteType 82, `<P> - Get Orders` (Shopify) + `<P> - eSyncMate Data Connection`, route `<P> - Get Orders` (Minutely 5) |
| 11 | `11_Place_Order.sql` | **Place Order** | Map `Shopify Order Transformation`, `<P> - eSyncMate Get Orders`, `<P> - SPARS Create Orders`, route `<P> - Place Orders in ERP` (RT 9) |
| 12 | `12_Get_ASN.sql` | **Get ASN** | `<P> - eSyncMate Create ASNs`, `<P> - SPARS Get ASN`, route `<P> - Fetch ASN from ERP` (RT 11) |
| 13 | `13_Create_ASN.sql` | **Create ASN** | RouteType 83, WKE1000 branch in the **live** `SP_OrdersData`, `<P> - Create Fulfillment`, route `<P> - Send ASN Shipment Notification` (RT 83) |
| 14 | `14_Get_Cancellation.sql` | **Get Cancellation** | `<P> - eSyncMate Create CancelOrder`, `<P> - SPARS Get Cancel Order`, route `<P> - Fetch Cancellations from ERP` (RT 24) |
| 15 | `15_Create_Cancellation.sql` | **Create Cancellation** | RouteType 84, `<P> - eSyncMate Cancel Lines` (existing `SP_SyncCancelQty`), `<P> - Cancel Order`, route `<P> - Send Cancellation to Portal` (RT 84) |
| 16 | `16_Nightly_Cleanup_Customer.sql` | Housekeeping | Adds the customer to the list inside `Sp_NightlyCleanup_DeleteCustomerFeedData` / `Sp_NightlyCleanup_DeleteCustomerLogTables` (live SPs), so the nightly cleanup also purges `SCSInventoryFeedData_<id>` and `SCSInventoryFeed_<id>_Log`. Without it the two tables grow for ever. A server without these SPs is reported and left alone |
| 17 | `17_Get_Cancelled_Orders.sql` | **Get Cancelled Orders** | RouteType 85, `<P> - Get Cancelled Orders` (copy of the Shopify connector + Parameter `days`), route `<P> - Get Cancelled Orders` (RT 85, Minutely 30): orders the customer cancelled in Shopify -> CANCELLED / PortalCancel / CancelError. **Needs Task 00005 01 + 02** |
| 18 | `18_Send_Cancellations_To_ERP.sql` | **Send Cancellations to ERP** | `<P> - eSyncMate Orders To Cancel In ERP`, `<P> - SPARS Cancel Order`, route `<P> - Send Cancellations to ERP` (existing RT 79, no Shopify code): PortalCancel -> ERP Cancel_Order. **Needs Task 00005 01 + 02** |
| 19 | `19_Flows.sql` | **Flows** | Run it **last**. Four Flows for the customer on Setup > Flows, like every other customer: `<P> - Inventory Sync` (Full, Differential, Upload), `<P> - Order Processing` (Get Orders, Place Order), `<P> - Shipment & ASN` (Get ASN, Create ASN), `<P> - Cancellation Processing` (the four cancellation routes). Flows and routes are created **In-Active** with the route's schedule; a route that does not exist yet is skipped and listed. Test Run is on the Flow's Route Details and only works for an **Active** route |

**Deployed together with Task 00005 (Walmart cancellations):** run
`..\2026-10-22_Task00005_WalmartCancellations\01-04` first. Scripts 17 / 18 use its RouteType 79 and its four
cancellation SPs, which only filter by customer.

## DEV only - never run at the client

`_DEV_ONLY_Rename_Customer.sql` renamed the dev test customer `SHOPIFY01` to `WKE1000` in `eSyncmate_Dev`
(run 2026-11-11). It stops on any other database. It is not a deployment script: the client gets the customer
from scripts 01 - 18.

## Backlog (not part of the deployment until the client asks)

| Script | What it is |
|---|---|
| `backlog/Product_Sync_RouteType_Branch_Route.sql` | Route **Shopify - Sync Products To Catalog Prices** (RouteType 81): stores the Shopify inventory item id per SKU in `CustomerProductCatalogPrices`. Only needed when the ERP does **not** send the bare Shopify inventory item number in `CustomerItemCode`. Needs `..\2026-10-15_TargetProductSync\127` (Sp_InsertMissingTargetCatalogPrices) first. |

## Everything store-specific lives in the connectors / Warehouse Mapping

| Setting | Where |
|---|---|
| Store + API version | Connector **BaseUrl** `https://<store>.myshopify.com/admin/api/2026-10/` |
| App credentials | Connector **ConsumerKey** (Client ID) / **ConsumerSecret** (Client secret) |
| Warehouse -> location | Warehouse Mapping screen, ShipNode = `gid://shopify/Location/<id>` |
| Get Orders filter | `<P> - Get Orders` Parameters `query` / `days` |
| ASN e-mail to the customer | `<P> - Create Fulfillment` Parameter `notifyCustomer` |
| Cancellation settings | `<P> - Cancel Order` Parameters `notifyCustomer` / `restock` / `refund` / `reason` |

## Order life cycle

| Step | Orders.Status | OrderDetail unit status |
|---|---|---|
| Get Orders | New (ERROR when a line has no SKU) | NEW |
| Place Order | SYNCED (ExternalId = SPARS order no.) | NEW |
| Get ASN | SYNCED | ASNRVD + tracking |
| Create ASN | SHIPPED when no unit is NEW / ASNRVD / CANRVD | ASNSNT |
| Get Cancellation | whole order: CANCELLED; lines: SYNCED | CANRVD |
| Create Cancellation | unchanged | CANSNT (sent) / CANERR (Shopify said no; set back to CANRVD to resend) |

A line cancel only removes the line in Shopify (order edit); Shopify then shows "Refund owed" - the refund
itself is done by the store (Manage refund). A whole-order cancel refunds only when `refund` = true.

## History

`_archive_original_134-149/` holds the scripts as they were run on the dev DB (numbers 134-149, before this
re-order). Do not run them again. Several of them still contain the dev DB / SPARS / Shopify secrets - keep the
folder out of git and e-mail.
