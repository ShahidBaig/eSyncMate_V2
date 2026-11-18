# Shopify (Task 00006) - end-to-end test against the client's DEV store `devshop-we`

DEV database only (`eSyncmate_Dev` on 203.99.180.246,14533). Every script here stops on any other database.
Nothing in this folder is run at the client. The client DB uses `..\v2\`.

Store: `devshop-we.myshopify.com` (Walker Edison organisation, Shopify Plus dev shop). Customer `WKE1000`.

## Scripts

| Script | What it does | State |
|---|---|---|
| `TEST_01_Switch_Dev_DB_To_devshop-we.sql` | Marketplace, warehouse -> location mapping (L55, L56) and BaseUrl of the Shopify connectors -> devshop-we | Run on the dev DB 2026-11-17 |
| `TEST_01_Verify.sql` | Read-only check of the dev DB after TEST 01 (grids with a Check column) | Run 2026-11-17: all OK |
| `TEST_02_Inventory_Test_Data.sql` | Two test products (`ESM-TEST-001`, `ESM-TEST-002`) as NEW rows in `SCSInventoryFeed`, stock for L55 / L56 - stands in for the ERP full feed | Written; stops until the two Shopify inventory item ids are filled in |
| `TEST_02_Verify.sql` | Read-only check of the inventory test data: the two test rows, locations, all rows of the customer, catalog ids, last sent / received data, and what the upload route reads now | Run 2026-11-17: test rows missing (TEST 02 not run successfully yet) |
| `TEST_03_Verify_Token_Link.sql` | Read-only: customer -> token selected on it (Customers > Edit > Tokens) -> store of the customer's Shopify connectors -> warehouse mapping. Works on the dev and the client DB | Run 2026-11-17 on the demo store setup: all OK |
| `TEST_99_Back_To_Demo_Store.sql` | The same, back to `esyncmate-dev-store` (L10, L21) | Not run |

Old mapping backup (made by the user 2026-11-17): table `ZDB_TargetPlusShipNodes_Shopify`.

## devshop-we locations (Settings > Locations, read 2026-11-17)

| Location id | Name | Mapped to |
|---|---|---|
| 92849602710 | L55 - YARAGHI INDIANA | ERP warehouse L55 |
| 92849635478 | L56 - YARAGHI INDIANA 2 | ERP warehouse L56 |
| 55761567894 | 2037 South 4130 West (Salt Lake City UT) | not mapped |

## BLOCKER (2026-11-17): the store refuses the app's token request

App `eSyncMate Connector` (custom app by eSoftage) IS installed on devshop-we. But the connector's token request
(`POST /admin/oauth/access_token`, `grant_type=client_credentials`, AuthType `SHOPIFYGetToken`) answers:

| Store | Result |
|---|---|
| esyncmate-dev-store (eSoftage organisation) | HTTP 200, token issued |
| devshop-we (client organisation) | HTTP 400 `shop_not_permitted` - "Client credentials cannot be performed on this shop." |
| walker-edison (client organisation, live) | HTTP 400 `shop_not_permitted` |

So no Shopify route can work on a client store with these credentials. Nothing below can be tested until this is
solved (see the work session for the options).

## Test plan (after the blocker is cleared)

1. Token + locations: Test Run `Shopify - Get Orders` (read-only).
2. Two test products in devshop-we with SKUs, stocked at L55 / L56; note their inventory item ids.
3. Test rows for those SKUs in `SCSInventoryFeed` (WKE1000, ATS_L55 / ATS_L56) -> `Shopify - Upload Warehouse-Wise Inventory`;
   check the quantities in the store.
4. Test order in devshop-we -> `Shopify - Get Orders`.
5. `Shopify - Place Orders in ERP`: the dev SPARS connectors point at the LIVE SPARS API (api.safavieh.com) - decide
   first whether a test order may be placed there.
6. ASN -> `Shopify - Send ASN Shipment Notification` (fulfilment in the store).
7. Line cancel / order cancel -> `Shopify - Send Cancellation to Portal`, `Shopify - Get Cancelled Orders`.

## Postman

`eSyncMate_Shopify_devshop-we.postman_collection.json` - imported into the user's Postman workspace on 2026-11-17
as "eSyncMate - Shopify (devshop-we test)". `Postman_Shopify_collection_import_walkthrough.gif` shows the import and
the folders.

| Folder | Requests |
|---|---|
| 1. Token | 1.1 client credentials (what eSyncMate does today; refused on devshop-we), 1.2 authorize link, 1.3 exchange the code |
| 2. Read (changes nothing) | 2.1 shop + locations, 2.2 token scopes, 2.3 product by SKU -> inventory item id, 2.4 stock per location, 2.5 latest orders |
| 3. Write (CHANGES the store) | 3.1 set stock at L55 and L56 (the mutation of the upload route) |

Collection variables: `shop`, `api_version`, `client_id`, `client_secret` (blank - type it as a CURRENT value),
`access_token` (filled by 1.1 / 1.3), `redirect_uri`, `code`, `sku`, `inventory_item_id`, `location_L55`, `location_L56`.
To check another store, change `shop` (for example `esyncmate-dev-store`, where 1.1 works).

## Encrypted connectors (saved from the UI, SQL cannot read them)

`enc_connector.py` decrypts them with the app's own key (`ApplicationSettings.EncryptionKey`, AES as in
`EncryptionHelper`), shows the non-secret fields, and with the argument `apply` sets the BaseUrl of connector 247
`Shopify - Cancel Order` to devshop-we and encrypts it again. It needs `devsql.sh` beside it (runs `sqlcmd` with the
connection string from `eSyncMate.Processor/appsettings.json`, dev DB only) and Git Bash:
`GITBASH=$(cygpath -w "$(which bash)") python enc_connector.py [apply]`. No secret is stored in either file.

- 247 `Shopify - Cancel Order`: BaseUrl changed to devshop-we on 2026-11-17 (Client ID, secret, Url, Parameters kept).
- 246 `Shopify - eSyncMate Cancel Lines`: SQL connector on the dev DB, `EXEC SP_SyncCancelQty` - nothing to change.

`TEST_01_Verify.sql` still shows both as "encrypted": SQL cannot look inside them.
