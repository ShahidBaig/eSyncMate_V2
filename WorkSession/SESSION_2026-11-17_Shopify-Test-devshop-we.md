# Work Session: Shopify end-to-end test against the client's dev store devshop-we
**Date**: 2026-11-17
**Status**: In Progress - BLOCKED at the token step

## Objective
Test the Shopify integration (Task 00006, customer WKE1000) step by step against the client's dev store
`devshop-we.myshopify.com` using the dev DB, before the live store `walker-edison` is enabled.
Test scripts are kept in `eSyncMate.Data/Tables/ShopifyScripts/Testing-Client-devshop-we`.

## Tasks
- [x] Read devshop-we locations (browser)
- [x] Dev DB: switch WKE1000 to devshop-we (TEST_01)
- [x] Local connection strings -> dev DB
- [ ] BLOCKER: token request refused on the client's stores
- [ ] Test products, inventory upload, orders, ASN, cancellations

## Progress Log

### Step 1: State before
- Dev DB is now `eSyncmate_Dev` on `203.99.180.246,14533` (server DESKTOP-SPQA23Q); the old address 110.93.227.0
  (MCP mssql tool) no longer answers. Queries run with `sqlcmd`, connection read from `eSyncMate.Processor/appsettings.json`.
- WKE1000 = customer Id 14, Marketplace `SHOPIFY ESYNCMATE DEV STORE`, ship nodes L10 / L21 (demo store).
  User's backup: `ZDB_TargetPlusShipNodes_Shopify` (2 rows). Routes 135 - 145 and Flows 51 - 54 all In-Active.
- Existing test data from the demo store: 8 `SCSInventoryFeed` rows, 16 orders for WKE1000 (not touched).
- Dev SPARS connectors (236, 237, 240, 242, 245, 250) point at the LIVE SPARS API `api.safavieh.com`.

### Step 2: devshop-we (browser, read-only)
- Locations: 92849602710 "L55 - YARAGHI INDIANA", 92849635478 "L56 - YARAGHI INDIANA 2",
  55761567894 "2037 South 4130 West" (Salt Lake City).
- Settings > Apps: "eSyncMate Connector - Custom app by eSoftage" is installed.
- `dev.shopify.com/dashboard/1979525/apps` (client organisation): 403 Forbidden for the session that opened.

### Step 3: BLOCKER - token request
- `POST https://<shop>.myshopify.com/admin/oauth/access_token` with `grant_type=client_credentials`, the app's
  Client ID and the secret stored in connector 233 (what `ShopifyConnector` does):
  - esyncmate-dev-store: HTTP 200, token, scopes read_locations, read_products, write_inventory,
    write_merchant_managed_fulfillment_orders, write_order_edits, write_orders.
  - devshop-we: HTTP 400 `shop_not_permitted` "Client credentials cannot be performed on this shop."
  - walker-edison: HTTP 400 `shop_not_permitted`.
- This confirms the risk noted on 2026-11-12: client credentials only work when app and store are in the same
  organisation. Installing the eSoftage app by link is not enough. Affects the live store too.
- Options: (A) an app created in the CLIENT's organisation (Dev Dashboard), its Client ID / secret in the
  connectors - no code change; (B) keep the eSoftage app and add the authorization-code (OAuth install) flow with a
  stored offline token - code change, needs a public redirect URL. Decision is the user's / lead's.

### Step 4: Dev DB switched to devshop-we
- **Files created**: `Testing-Client-devshop-we/TEST_01_Switch_Dev_DB_To_devshop-we.sql`,
  `TEST_99_Back_To_Demo_Store.sql`, `README.md` (made from `v2/V2_01`, plus a guard: only `eSyncmate_Dev`).
- **TEST_01 run on the dev DB**: Marketplace `SHOPIFY DEVSHOP-WE`; L10 / L21 removed, L55 -> 92849602710,
  L56 -> 92849635478 inserted; connectors 233, 234, 243, 248 BaseUrl -> devshop-we (Client ID and secret kept).
  Connector 247 `Shopify - Cancel Order` is encrypted -> NOT changed (to be edited in the UI).
- **Issues**: first version failed to compile (`RAISERROR` with `DB_NAME()` as argument; nothing was changed),
  fixed with a variable. TEST_99 not run.

### Step 5: Local connection strings
- User had already set `eSyncMate.Processor/appsettings.json` to the dev DB and changed the default in
  `Models/CommonUtils.cs` (typo `Server=Server=` fixed).
- `eSyncMate.RouteWorker/appsettings.json` and `eSyncMate.AlertWorker/appsettings.json` set to the same dev
  connection strings (they still pointed at the client DB; `UseExternalProcess` is true, so local routes run there).
- **NOT committed, and must not be published**: a publish copies these files to the client package. Before the next
  client publish: `git checkout -- eSyncMate.Processor/appsettings.json eSyncMate.RouteWorker/appsettings.json
  eSyncMate.AlertWorker/appsettings.json eSyncMate.Processor/Models/CommonUtils.cs`.

### Step 6: Verification script, encrypted connector 247 updated
- **File created**: `Testing-Client-devshop-we/TEST_01_Verify.sql` (read-only, grids with a Check column). Run by me
  and by the user (14:37): identical, all OK; NOTE on the six live SPARS connectors; 246 / 247 shown as encrypted.
- User asked why 247 was not updated. Done with `enc_connector.py` (new, in the test folder with `devsql.sh`): reads
  `ApplicationSettings.EncryptionKey`, AES-CBC as in `EncryptionHelper`, decrypts, changes BaseUrl
  esyncmate-dev-store -> devshop-we, encrypts again, `UPDATE Connectors ... WHERE Id = 247`. Read back: BaseUrl
  devshop-we, Client ID / secret / Url `graphql.json` kept.
- 246 decrypted: SQL connector on the dev DB (`EXEC SP_SyncCancelQty`) - no change needed.
- **Issues**: `sqlcmd` cuts a column at 256 characters (read in 250-character pieces); `python` started WSL bash
  instead of Git Bash (path passed in `GITBASH`).
- Token blocker unchanged; decision (client-organisation app or OAuth flow) still open.

### Step 7: Dev DB password changed - SQL connectors updated by the user in the UI
- The `sa` password of the dev DB was changed by the user's side; the eight Shopify SQL connectors (232, 235, 238,
  239, 241, 244, 246, 249) held the old one. The user saved all eight in Setup > Connectors (now encrypted).
- Checked with the decrypt tool (no secret shown): all eight -> 203.99.180.246,14533 / eSyncmate_Dev, current
  password, commands intact. The five Shopify store connectors (233, 234, 243, 247, 248) -> devshop-we, the six
  SPARS connectors unchanged. 13 SQL connectors of other customers in the dev DB were not checked.

### Step 8: Inventory test plan - two products, feed rows by hand
- User: SPARS routes are not needed for now; the ERP side is simulated by hand. First step: two test products,
  inventory upload to the two locations, then the next step.
- The upload route sends stock by Shopify INVENTORY ITEM id taken from `SCSInventoryFeed.CustomerItemCode`
  (digits) - so the products must exist in devshop-we BEFORE the rows are inserted.
- **File created**: `Testing-Client-devshop-we/TEST_02_Inventory_Test_Data.sql` - SKUs `ESM-TEST-001` (L55 10, L56 5)
  and `ESM-TEST-002` (L55 7, L56 0), Status NEW, only these ItemIds are replaced, old demo rows untouched; ends with
  the source SP call the route makes. Compile-checked on the dev DB: stops at the placeholder guard (51501), 0 rows.
- **Open**: create the two products in devshop-we (tracked, stocked at L55 and L56), read their inventory item ids,
  fill them in, run TEST 02. The upload itself still needs the token blocker solved.

### Step 9: TEST 02 run by the user stopped at the guard; verify script added
- User ran `TEST_02_Inventory_Test_Data.sql` (14:57): error 51501, as designed - the inventory item ids are still
  placeholders. Nothing written (checked).
- **File created**: `Testing-Client-devshop-we/TEST_02_Verify.sql` (read-only). Run on the dev DB 14:59: database OK,
  both test rows MISSING, locations L55 / L56 OK, the 8 old demo rows SYNCED / ERROR (not sent), no catalog rows,
  nothing waiting for the upload route.
- Chrome is now signed in as the eSoftage account (shahid): it has no access to devshop-we, so the store's products
  cannot be read or created until the client staff session (majid) is open again.

### Step 10: What the client staff account (majid) can do on devshop-we (browser, read-only)
- User asked to create the test products with Postman. Not possible: an API call needs an app token, the store
  refuses the token of our app, and the app has `read_products` only.
- Products page: "You need permission to view this feature - Your account majid at esoftage.com doesn't have access".
- Settings > Apps > App development: "Custom apps are now built in the Dev Dashboard. You can't create new apps
  here. Existing apps keep working." One legacy app exists: "devshop-we: Boomi Integration". So the client's hint
  ("Develop apps" in the store settings) can no longer create an app.
- Dev Dashboard of the client organisation (`dev.shopify.com/dashboard/1979525/apps`): 403 Forbidden.
- Needed from the client: Dev Dashboard access for the account (to create the app in their organisation) or the
  client creates the app and sends Client ID + secret; and the Products permission or two test products.
  A message to the client was drafted in the chat; not sent.

### Step 11: Postman collection
- "Go to your organizations" on the Dev Dashboard 403 page leads back to 403 (also `dev.shopify.com/dashboard`):
  the client staff account has no Dev Dashboard access at all.
- User does not want a new app or store. Proposed instead: keep the installed eSoftage app and get a permanent
  token with the authorization-code grant (authorize link -> code -> exchange), plus a small code change so the
  connector can use a stored token. Not started; needs the user's OK and a redirect URL in the app's settings.
- User asked for a Postman collection and a recording.
  **Files created** in `Testing-Client-devshop-we`: `eSyncMate_Shopify_devshop-we.postman_collection.json`
  (9 requests in 3 folders, no secret inside) and `Postman_Shopify_collection_import_walkthrough.gif`.
  Imported into the user's Postman web workspace "My Workspace" (file upload in the Import dialog): collection
  "eSyncMate - Shopify (devshop-we test)". No request was sent. Postman's "Agent Mode" terms prompt was left alone.

### Step 12: Postman results, authorization-code attempt on devshop-we
- User typed the Client secret into the collection. 1.1 on devshop-we in Postman: `shop_not_permitted` (same as curl).
- "Couldn't resolve host": the variable NAME `shop` had been overwritten with `esyncmate-dev-store`; restored
  (name `shop`). 1.1 on esyncmate-dev-store: 200 OK, token, scopes read_locations, read_products, write_inventory,
  write_merchant_managed_fulfillment_orders, write_order_edits, write_orders.
- Dev Dashboard (Shahid), app version 1.0.0 (active, released 2026-10-23): application_url
  `https://shopify.dev/apps/default-app-home`, redirect_urls `https://shopify.dev/apps/default-app-home/api/auth`,
  embedded false, scopes as known (no `write_products`). A redirect URL exists, so NO app change is needed for the
  authorization-code grant. Collection variable `redirect_uri` set to it; `shop` set back to `devshop-we`.
- Authorize link opened for devshop-we
  (`/admin/oauth/authorize?client_id=...&redirect_uri=https://shopify.dev/apps/default-app-home/api/auth`):
  page "forbidden - Unauthorized Access". The signed-in account may not authorize the app on that store.
  So the code has to come from a devshop-we user with app permissions (the client), or the client gives the staff
  account that permission. No token for devshop-we yet.
- Test products through the API are not possible with this app on any store: it has `read_products` only.

### Step 13: App "v1 code" and the lead's point about install = token
- User added `ShopifyScripts/e-sync-mate-connector/` (+ `.zip`, `MOM/StepsToImplenet.mp4`), untracked. It is the
  Shopify CLI project of the app: only `shopify.app.toml` matters (client_id, application_url and redirect_urls =
  Shopify placeholders `https://shopify.dev/apps/default-app-home[/api/auth]`, scopes, embedded false, no webhooks).
  The rest is the unchanged "extension only" template. There is NO server code, so nothing receives a token.
- Lead: installing = consent, Shopify records it and hands the app an offline token. Correct, but Shopify only hands
  the token over when the app runs the authorization-code grant (or token exchange for embedded apps). Our app
  never does, and its redirect URL is a placeholder.
- Shopify docs checked 2026-11-17: client credentials "only works when the app and the store belong to the same
  Shopify organization ... can't reach a store outside your organization, including a client's store"; for an app
  outside the admin use custom distribution + the authorization code grant (authorize -> callback with code, hmac,
  shop, state, timestamp -> POST access_token with client_id, client_secret, code, optional expiring=1).
- Proper fix proposed (not started): OAuth install + callback endpoints in eSyncMate.Processor, token stored per
  store, Shopify connector uses the stored token, `shopify.app.toml` application_url / redirect_urls -> the public
  eSyncMate API, new app version, client opens the app once per store. An OAuth flow of this kind already exists
  for Target (`CustomersController`, `TargetConnector`, Customers OAuth columns) and can be the model.

### Step 14: Proper fix implemented - authorization code grant in eSyncMate (user: go-ahead for code + new app version)
- Public addresses given by the user: UI `https://ems.safavieh.com/eSyncMate/`, API `https://ems.safavieh.com:8085/`.
- **Files created**
  - `eSyncMate.Processor/Connections/ShopifyTokenStore.cs` - table `ShopifyStoreTokens` (get / save, tokens
    encrypted with `EncryptionHelper` + `CommonUtils.EncryptionKey`; a DB without the table = no stored token).
  - `eSyncMate.Processor/Controllers/ShopifyController.cs` - `GET api/shopify/install?shop=` (anonymous; redirects to
    the store's authorize page, state nonce 10 min), `GET api/shopify/callback` (anonymous; state + HMAC + shop
    checks, code -> token, saved), `POST api/shopify/exchangeCode` (JWT; code or redirected URL copied by hand),
    `GET api/shopify/status?shop=` (JWT; never returns the token). Client ID / secret come from the Shopify
    connector whose BaseUrl host is the store. Optional config: `Shopify:PublicApiBaseUrl`, `Shopify:Scopes`,
    `Shopify:ExpiringTokens`.
  - `eSyncMate.Data/Tables/ShopifyScripts/v2/V2_02_Shopify_Store_Tokens_Table.sql` (+ row in the scripts README).
- **Files changed**
  - `eSyncMate.Processor/Connections/ShopifyConnector.cs` - `GetAccessToken` uses the stored token of the store
    first (re-read from the DB every 10 minutes, refresh-token grant when it is an expiring one), then the client
    credentials grant as before; `shop_not_permitted` now carries a hint. AuthType stays `SHOPIFYGetToken`, no
    connector change needed.
  - `ShopifyScripts/e-sync-mate-connector/e-sync-mate-connector/shopify.app.toml` (untracked folder) - redirect_urls:
    `https://ems.safavieh.com:8085/api/shopify/callback`, `http://localhost:5000/api/shopify/callback`, placeholder
    kept; application_url still the placeholder, the new value is in a comment (switch after the client deploy).
- **Checks**: `dotnet build eSyncMate.Processor -c Debug` -> 0 errors. `V2_02` run on the dev DB -> table created,
  0 rows. The OAuth flow itself has NOT been run end to end.
- **Not done**: new app version not released. Shopify CLI (4.9.4, in the app folder) is not logged in (it opens a
  device-code login); the Dev Dashboard in Chrome answers 403 again (the Shopify session is no longer Shahid).
  Needs the user: `npx shopify app deploy` in the app folder, signed in as Shahid.
- Nothing committed.

### Step 15: App version 1.1.0 released
- `shopify app deploy --allow-updates --version 1.1.0 --message "#00006: redirect URLs ..."` run from the app folder
  (Shopify CLI 4.9.4). Device-code login approved by the user in the browser. Output: "Logged in", Org eSoftage,
  App eSyncMate Connector, "New version released to users. 1.1.0".
  Version page: `https://dev.shopify.com/dashboard/195768317/apps/439087824897/versions/1231279849473`.
- Only change against 1.0.0: `[auth] redirect_urls` (+ `https://ems.safavieh.com:8085/api/shopify/callback`,
  + `http://localhost:5000/api/shopify/callback`, placeholder kept). application_url and scopes unchanged.
- **Next**: local end-to-end test on esyncmate-dev-store (API on `http://localhost:5000`, open
  `/api/shopify/install?shop=esyncmate-dev-store.myshopify.com` as the store owner), then devshop-we (needs a client
  user who may manage apps), then client deployment + `V2_02` on the client DB + application_url switch (1.2.0).

### Step 16: Authorization code flow PROVEN on esyncmate-dev-store (local API)
- `V2_02` run by the user on the dev DB: table there, empty.
- Local API on `http://localhost:5000` (started by the user). `GET api/shopify/install?shop=devshop-we.myshopify.com`
  -> 302 to the store's authorize page with client_id, the nine scopes, redirect_uri
  `http://localhost:5000/api/shopify/callback`, state. In the browser: Shopify "Unauthorized Access".
- Same on esyncmate-dev-store after the user ran `TEST_99` -> also "Unauthorized Access". Cause found: Chrome's
  Shopify session was majid (client staff account), which has no access to our own store ("Your account majid ...
  doesn't have permission to view this page"). After switching to Shahid:
  **"Connected - eSyncMate is now connected to esyncmate-dev-store.myshopify.com"**.
- Checked: `ShopifyStoreTokens` has 1 row (esyncmate-dev-store, token encrypted, AccessTokenExpiryUtc NULL, no
  refresh token = a token that does not expire). The stored token (prefix `shpca_`) answers a read-only GraphQL
  call: HTTP 200, shop "eSyncMate Dev Store", locations 88131305658 / 88131272890, 10 access scopes.
  So install -> callback (state, HMAC) -> exchange -> encrypted save all work.
- "Unauthorized Access" = the signed-in account may not approve the app on that store (seen for majid on both
  stores). For devshop-we a client user who may manage apps is needed.
- Dev DB is currently on the DEMO store (TEST_99 run by the user); run TEST_01 again before testing devshop-we.

### Step 17: Lead's design - general token list, token selected on the customer
- Lead (transcript given by the user): a token arrives without a customer; keep tokens in one list with metadata
  (other platforms later, e.g. TikTok); add a "Tokens" dropdown on the customer; the selected token's Id is saved on
  the customer; all routes of the customer take the access token through that Id. One customer (WKE1000) can have a
  demo, UAT and production store. Lead will check himself whether a changed redirect URL needs a reinstall.
- **Reworked** (the first version, table `ShopifyStoreTokens` keyed by store, is replaced):
  - `v2/V2_02_Integration_Tokens.sql` (replaces `V2_02_Shopify_Store_Tokens_Table.sql`): table `IntegrationTokens`
    (Id, Platform, Name, AccountKey, ClientId, tokens encrypted, scope, expiry), column `Customers.IntegrationTokenId`,
    copies the rows of `ShopifyStoreTokens` and drops it. Run on the dev DB: table + column created, 1 row copied
    (Id 1, "Shopify - esyncmate-dev-store.myshopify.com"), old table dropped.
  - `Connections/IntegrationTokenStore.cs` (replaces `ShopifyTokenStore.cs`): GetForCustomer, GetByAccount, Save
    (same store + app keeps its Id), List (no token values, with linked customers), Get/SetCustomerToken.
  - `Connections/ShopifyConnector.cs`: `GetAccessToken(baseUrl, clientId, secret, customerId)` - 1. the token
    selected on the customer (error when it is the token of another store than the connector BaseUrl), 2. client
    credentials when the customer has none. No lookup by store without a selection: the link is the customer.
    `RestConnector` passes `connector.CustomerID`.
  - `Controllers/ShopifyController.cs`: saves into `IntegrationTokens`; app credentials = the connector of the store,
    else the one app all Shopify connectors share (so a store without a connector can authorize); success page tells
    the user to select the token on the customer.
  - `Controllers/CustomersController.cs`: `GET api/Customers/getIntegrationTokens/{customerId}`,
    `POST api/Customers/setIntegrationToken?customerId=&tokenId=` (0 = none).
  - UI: `customers/edit-customer-popup` - "Tokens" dropdown (None + the list, linked customers shown), saved after
    the customer update; `services/customers.service.ts`; translations `tokens`, `noToken`, `tokensHint` (en, es).
- **Checks**: `dotnet build eSyncMate.Processor` 0 errors; `npx tsc --noEmit -p UI/tsconfig.app.json` exit 0.
  NOT yet tested in the running application (API restart + UI needed): dropdown, selection, route with the token.

### Step 18: Tokens dropdown tested by the user (local UI + API)
- Local UI `http://localhost:5015`, API `http://localhost:5000` (new build). Customers > Edit WKE1000: the Tokens
  field is there (the user first did not see it - an old copy of the page), entry
  "Shopify - esyncmate-dev-store.myshopify.com" selected, Update.
- `V2_02_Integration_Tokens.sql` run again by the user: grid 1 = token Id 1 (encrypted, no expiry); grid 2 = customer
  14 WKE1000, IntegrationTokenId 1, Check OK. So list -> select -> saved on the customer works.
- Not tested yet: a route run that takes the token through the customer. WKE1000's connectors point at devshop-we
  and the selected token is the demo store's, so a run now stops with the mismatch message (by design).

### Step 19: Dev DB back on the demo store, link verified
- User ran `TEST_99_Back_To_Demo_Store.sql`. **File created**: `Testing-Client-devshop-we/TEST_03_Verify_Token_Link.sql`
  (read-only: customer -> selected token -> connector store -> warehouse mapping).
- Result on the dev DB: WKE1000 (Id 14), Marketplace SHOPIFY ESYNCMATE DEV STORE, IntegrationTokenId 1 =
  "Shopify - esyncmate-dev-store.myshopify.com"; connectors 233, 234, 243, 248 on esyncmate-dev-store = token store
  (OK); mapping L10 / L21 (demo store locations).
- Connector 247 `Shopify - Cancel Order` is encrypted and still on devshop-we (TEST 99 cannot change it): only the
  route `Shopify - Send Cancellation to Portal` is affected; it would stop with the mismatch message.
- Next: Test Run of `Shopify - Get Orders` (read-only) to prove a route takes the token through the customer.

### Step 20: Lead's points on the app settings - application_url, endpoint names
- Lead: change application_url too (not only the redirect URL), so opening / installing the app starts the flow by
  itself; endpoint checks the HMAC and redirects to authorize; callback saves the offline token; keep managed
  installation (no use_legacy_install_flow); no write_products unless product writes are in scope.
- **Changed**: `ShopifyController` - endpoints now `api/shopify/oauth/start` and `api/shopify/oauth/callback` (the
  lead's names); the first names `api/shopify/install` / `api/shopify/callback` stay as second routes, so links and
  the redirect URL of app version 1.1.0 keep working. The redirect_uri sent is now `.../api/shopify/oauth/callback`.
  Comments / README / V2_02 header updated to the new names.
- **Changed**: `shopify.app.toml` - application_url `https://ems.safavieh.com:8085/api/shopify/oauth/start`;
  redirect_urls = oauth/callback for ems.safavieh.com:8085 and localhost:5000, plus the two 1.1.0 names; the
  Shopify placeholder URLs are removed; scopes unchanged; managed installation kept. Flow described in the header.
- Already as the lead describes: HMAC check on the start request when Shopify sends one; state + HMAC + shop
  checks on the callback; code -> offline token -> saved encrypted.
- **Check**: `dotnet build eSyncMate.Processor` 0 errors. Not re-tested in the browser after the rename.
- **NOT released**: app version 1.2.0 with this toml. Reason: application_url points at the client's API, where
  the new build is not deployed yet; released now, opening the app at the client (or on our demo store) would
  show an error page. Order: deploy at the client + V2_02 on the client DB -> `shopify app deploy` -> the client
  opens the app once -> select the token on the customer.
- Note: with 1.2.0 the placeholder redirect is gone, so the Postman requests 1.2 / 1.3 (code copied by hand) need
  one of the new redirect URLs; the app also serves ONE eSyncMate installation (application_url is one address).

### Step 21: App version 1.2.0 released (user: needed because the App URL changed)
- Client addresses checked from the dev machine: UI `https://ems.safavieh.com/eSyncMate/login` 200; API
  `https://ems.safavieh.com:8085/swagger/index.html` 200, `/api/Customers/getCustomers` 401 (reachable from the
  internet); `/` and `/eSyncMate/` on 8085 give 404 (normal, the API has no pages);
  `/api/shopify/oauth/start` 404 = new build not deployed at the client yet.
- `shopify app deploy --allow-updates --version 1.2.0` -> "New version released to users. 1.2.0".
  `https://dev.shopify.com/dashboard/195768317/apps/439087824897/versions/1231778938881`.
  Contents: application_url `https://ems.safavieh.com:8085/api/shopify/oauth/start`, redirect_urls oauth/callback
  (ems.safavieh.com:8085, localhost:5000) + the two 1.1.0 names; scopes unchanged.
- **Consequence until the client deployment**: opening the app in a store's admin lands on the client API's 404.
  Local testing still works by opening `http://localhost:5000/api/shopify/oauth/start?shop=...` by hand.
- Tomorrow: restore the three appsettings.json, publish, deploy at the client, `V2_02` on the client DB, check
  `https://ems.safavieh.com:8085/api/shopify/oauth/start?shop=devshop-we.myshopify.com` redirects to Shopify, a
  client user who may manage apps opens the app once, select the token on the customer.

### Step 22: Firewall whitelisting e-mail for the client's security team
- Lead asked which URLs / IPs / ports Shopify uses towards our application. Answer prepared and sent by the user
  (signed Shahid):
  - Outbound, eSyncMate server -> Shopify, HTTPS 443: `walker-edison.myshopify.com`, `devshop-we.myshopify.com`
    (paths `/admin/oauth/access_token`, `/admin/api/2026-10/graphql.json`); whitelist by domain name.
  - Inbound, HTTPS 8085: `https://ems.safavieh.com:8085/api/shopify/oauth/start` and `.../oauth/callback`, opened by
    the browser of the user who authorizes the app, not by Shopify's servers.
  - No webhooks, no other callbacks, no Shopify IP ranges (checked: no webhook / bulk-operation code in the Shopify files).
- **Correction**: `ems.safavieh.com` is open to whitelisted IPs only (user). My earlier check of port 8085 was from
  the user's whitelisted machine, so "reachable from the internet" was wrong. Consequence: the person who authorizes
  the app must be on a whitelisted IP, or the client whitelists that person's public IP (one-time step per store).
- Not verified today: "Shopify does not provide static IP addresses for its stores" (from knowledge, no lookup).
- Open: the client's answer; the authorization can only be done after the new API build is deployed at the client.

### Step 23: Commit (2026-11-18)
- Committed on `main` as `#00006:` - token list + token on the customer (API, RouteWorker code, UI), OAuth endpoints,
  `v2/V2_02_Integration_Tokens.sql`, the `Testing-Client-devshop-we` folder, README, both session files.
- Checked before the commit: no secret, token or password value in any committed file (Postman collection has a
  blank `client_secret`; test scripts have a blank `@ClientSecret`).
- The three `appsettings.json` and `CommonUtils.cs` show no local change any more (back on the committed client
  values). Local testing against the dev DB needs them changed again; a client publish can be done as they are.
- NOT committed, left as they are: the Shopify app project `ShopifyScripts/e-sync-mate-connector/` (it is its own git
  repository; `shopify.app.toml` 1.2.0 lives there), `e-sync-mate-connector.zip`, `ShopifyScripts/MOM/`, the `.docx`
  in `v2/`, and the build output `eSyncMate.RouteWorker/obj/...GlobalUsings.g.cs`.
- Not pushed: the user pushes with `git push origin main`.

## Summary
(fill in at end)
