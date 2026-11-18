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

### Step 24: Published to Release6 (2026-11-18, from commit 1498316)
- Backend: `dotnet publish eSyncMate.Processor\eSyncMate.Processor.csproj -c Release -o D:\Deployment\BIZMATE\Release6\api`
  -> exit 0, 435 files (API + `RouteWorker` + `AlertWorker`).
- UI: `ng build --base-href /eSyncMate` in `UI` (the user's command; the site runs under `/eSyncMate`) -> exit 0,
  copied `UI\dist\ui` to `Release6\ui` (22 files). My first build had no `--base-href` (`<base href="/">`, would not
  load at the client); the user pointed it out, rebuilt and `ui.zip` replaced. Now `<base href="/eSyncMate">`, same
  as Release4.
- Zips in `D:\Deployment\BIZMATE\Release6\eSyncMate`: `api.zip` (75.7 MB), `ui.zip` (2.3 MB), plus a copy of
  `V2_02_Integration_Tokens.sql`.
- Checked: the new code is in `eSyncMate.Processor.dll` of both `api` and `api\RouteWorker`; the three published
  `appsettings.json` point at the client DB (ESYNCMATE / ESYNCMATESCHEDULER); the UI bundle has the Tokens field and
  the client API address `https://ems.safavieh.com:8085/`.
- Not done: deployment at the client (the user does it), running V2_02 on the client DB.

### Step 25: Release6 deployed at the client (2026-11-18, by the user) - checked from outside
- User: API, UI deployed and `V2_02_Integration_Tokens.sql` executed at the client. The `RouteWorker` folder was
  locked again by running workers (no admin rights: wait / Task Manager / client admin).
- Checked from the user's (whitelisted) machine, read-only:
  - `GET https://ems.safavieh.com:8085/api/shopify/oauth/start?shop=devshop-we.myshopify.com` -> HTTP 302 to
    `https://devshop-we.myshopify.com/admin/oauth/authorize` with the app's client_id, the nine scopes and
    `redirect_uri=https://ems.safavieh.com:8085/api/shopify/oauth/callback` (was 404 before the deployment).
  - `api/shopify/status` and `api/Customers/getIntegrationTokens/15` -> HTTP 401 (exist, need a login).
  - UI: `https://ems.safavieh.com/eSyncMate/` serves `main.e1dd7c5938787795.js` = the Release6 build, base href `/eSyncMate`.
- NOT checked: the V2_02 output on the client DB (not reachable from here; user to paste the two grids).
- Next: a store user who may manage apps opens the app (from a whitelisted IP) -> token in the list -> select it in
  Customers > Edit > Tokens (WKE1000) -> Test Run of `Shopify - Get Orders` (read-only) before any writing route.

### Step 26: Lead's objection on the whitelisting e-mail - who calls application_url / the redirect URL
- Lead (voice note transcript): the call to our callback / application_url comes from Shopify's SERVERS, so Shopify's
  IPs must be whitelisted; asked for a standard / known IPs / documentation.
- Answer given: in the authorization code grant both URLs are requested by the user's BROWSER (HTTP 302 redirects).
  The only server-to-server call is ours to Shopify (`POST /admin/oauth/access_token`). Shopify's servers call an app
  only for webhooks, which this app does not have.
  - Proof from our own test (step 16): the demo store was connected with redirect URL `http://localhost:5000/...`.
    Shopify's servers cannot reach localhost on a developer PC; only the browser on that PC can.
  - Check at the client after an authorization: IIS log `c-ip` of the `/api/shopify/oauth/callback` request = the
    user's public IP; or browser DevTools > Network shows the 302 chain.
- Looked up 2026-11-18: Shopify publishes no IP ranges, not even for webhooks (staff answers in the developer
  community; official guidance is HMAC verification). Docs page on the authorization code grant says "Shopify
  redirects to your redirect_uri" (does not spell out "browser").
- Options for the firewall: whitelist the authorizing user's IP / do it from a whitelisted network / open only the two
  `api/shopify/oauth/*` paths on 8085 to the internet (they check HMAC + state). Lead to decide.

### Step 27: Report for the lead (artifact) + his follow-up: the flow starting from the install link
- HTML report published as a private artifact: https://claude.ai/artifact/VfbTUnWNVuEkLAL5M1DEFq (user shares it with
  the lead from the Share menu). Source: session scratchpad, not in the repo.
- Lead (second transcript): the starting point is the INSTALL LINK from the e-mail; step by step, do Shopify's servers
  call us or the browser of the person who clicked?
- Answer added to the report: A. browser opens Shopify's install screen; B. Install -> Shopify installs and grants the
  scopes internally, nothing is sent to eSyncMate; C. Shopify sends the BROWSER to `application_url`; then the four
  requests (start -> authorize -> callback -> our server's token call). Still no call from Shopify's servers.
- Consequence stated: installing alone gives no token; if the installer's browser cannot reach ems.safavieh.com:8085
  (IP whitelist) the app is installed but no token is saved; opening the app again from an allowed IP completes it.
- Corrections to what was said in the call: the redirect URL IS needed (registered in the app, the code arrives
  there) - it is only not called by Shopify's servers.
- NOT verified: step C with a real install link on a client store. Shopify's installation page does not say where the
  user lands after approving; to confirm in the browser Network tab at the first install.

### Step 28: The actual install link examined (user pasted it)
- Link: `https://admin.shopify.com/oauth/install_custom_app?client_id=<app>&no_redirect=true&signature=...`
  Signature payload (base64 JSON, not secret): store `walker-edison.myshopify.com`, purpose `custom_app`,
  `expires_at` 1795108015 = 2026-11-19 17:06 UTC (22:06 PKT). Valid for that one store only.
- `no_redirect=true`: after Install the user stays in Shopify admin; Shopify does not send the browser to
  `application_url` (my reading of the parameter, not found in the docs, not tested). So install and token are two
  separate actions: install = Shopify only, from any network; token = a store user who may manage apps opens the app
  (or `<API>/api/shopify/oauth/start?shop=walker-edison.myshopify.com`) from an IP allowed on ems.safavieh.com:8085.
- Report (artifact) updated accordingly; step C of step 27 corrected.
- Open: the link expires in about 1.5 days - a new link is needed if the client installs later; no link exists for
  `devshop-we` (the app is already installed there).

### Step 29: Client DB has a demo-store token - scripts for an inventory upload test at the client
- User authorized OUR store against the CLIENT API: client `IntegrationTokens` Id 1 = `Shopify -
  esyncmate-dev-store.myshopify.com` (encrypted, non-expiring), selected on customer 15 `WKE1000`
  (Marketplace `SHOPIFY WALKER EDISON`). First proof that start -> authorize -> callback -> token works at the client.
  Note: the saved Scope lists 6 scopes (the write scopes; Shopify leaves out the read scopes they imply).
- User wants to test "Shopify - Upload Warehouse-Wise Inventory" at the client against our store, with apply + revert.
- Read from the demo store in Chrome (account Shahid; Chrome was on majid, switched in the account chooser, no
  password typed):
  - Locations: Shop location = 88131272890, My Custom Location = 88131305658.
  - ESM-SB-ICE: inventory item 52944920346810, stock Shop 50 / Custom 50.
  - ESM-SB-DAWN: inventory item 52944920379578, stock Shop 12 / Custom 3.
- **New folder** `ShopifyScripts/Testing-Client-DB-demo-store/`:
  - `CT_01_Apply_Demo_Store_Inventory_Test.sql`: backup into `ZDB_CT01_ShipNodes` / `ZDB_CT01_Connectors`; mapping
    L55 -> Shop location, L56 -> My Custom Location; ONLY connector `Shopify - WHSW Inventory Upload` BaseUrl store ->
    demo store; two feed rows Status NEW (ICE 41 / 17, DAWN 9 / 4). Stops when: wrong token selected, other items of
    WKE1000 waiting (NEW / UPDATED), connector encrypted, already applied.
  - `CT_01_Verify.sql` (read-only), `CT_99_Revert.sql` (restores from the backup, deletes the two items, drops the
    backup; optional `@ClearTokenSelection`).
- **Decision**: the other Shopify connectors (orders, ASN, cancel) stay on walker-edison. With the demo token
  selected they stop with the store mismatch message, so no demo order can reach the client's ERP (SPARS is live).
- NOT tested: the three scripts were not run anywhere (dev DB connection is not configured locally at the moment).
- Reminder for go-live: run CT_99, select the walker-edison token on the customer.

### Step 30: CT_01 applied on the client DB (user, 2026-11-18) - output checked
- Mapping now: L55 -> 88131272890, L56 -> 88131305658. Backup holds the client values L55 -> 81898176578,
  L56 -> 81898602562 (as expected).
- Connector 248 `Shopify - WHSW Inventory Upload`: `walker-edison` -> `esyncmate-dev-store`, API version 2026-10 kept.
- Feed rows: ESM-SB-ICE 41 / 17 (total 58), ESM-SB-DAWN 9 / 4 (total 13), both NEW.
- Next: CT_01_Verify, Test Run of the upload route only, check the store, CT_01_Verify again, then CT_99_Revert.

### Step 31: CT_01 verified at the client; Get Orders test scripts (CT_02)
- User pasted `CT_01_Verify` output: token OK, mapping OK, only connector 248 on the demo store (252, 258, 262, 263
  on walker-edison), all 11 Shopify routes (157 - 167) In-Active, source SP returns exactly the two test items.
  Told to Test Run route 159 only. Result of that Test Run NOT reported back yet.
- User asked for the Get Orders test with a revert. Added to `Testing-Client-DB-demo-store/`:
  - `CT_02_Apply_Get_Orders_Test.sql`: backup `ZDB_CT02_Connectors` + `ZDB_CT02_Baseline` (highest `Orders.Id`);
    only connector `Shopify - Get Orders` -> demo store. Stops when the demo token is not selected, when
    `Shopify - Place Orders in ERP` is Active, when the connector is encrypted, or when already applied.
  - `CT_02_Verify.sql` (read-only): token, connector stores, route status, the test orders and their lines.
  - `CT_02_Revert.sql`: connector back; deletes Orders / OrderDetail / OrderData of WKE1000 with Id above the
    baseline; stops when a test order was processed further (Status not New / ERROR or ExternalId filled), `@Force`.
- What the route reads (script 10): paid or pending, unfulfilled, open, created in the last 10 days.
- Risk named to the user: route 161 `Shopify - Place Orders in ERP` must stay In-Active; SPARS at the client is live.
- NOT tested: CT_02 scripts were not run anywhere.

### Step 32: CT_02 applied at the client - connector 252 is encrypted
- First run stopped with 51705: connector 252 `Shopify - Get Orders` is encrypted. Cause: the user had changed its
  Base URL by hand in Setup > Connectors (a save from the UI encrypts the connector); it was plain JSON in the
  CT_01_Verify output shortly before.
- `CT_02_Apply_Get_Orders_Test.sql` changed: for an encrypted connector it only makes the backup + baseline
  (flag `@EncryptedAndStillOnClientStore = 1` after checking in the UI that Base URL shows walker-edison); the Base
  URL is then changed in the UI. The revert restores the encrypted value as it is.
- User set the Base URL back to walker-edison in the UI, ran the script with the flag: backup made, baseline
  `MaxOrderId = 775955` (2026-11-18 05:42 server time), WKE1000 had 0 orders before the test.
- Next: Base URL -> `https://esyncmate-dev-store.myshopify.com/admin/api/2026-10/` in the UI, CT_02_Verify,
  Test Run route 160 only. Route 161 stays In-Active.
- Note for the revert: connector 252 stays ENCRYPTED after it (it was encrypted when backed up). Any later script
  that edits it by SQL will skip it; change it in the UI.

### Step 33: Get Orders at the client reaches our store through the customer's token (route log, 2026-11-18)
- User activated route 160 (Minutely 5) instead of a Test Run; 161 and all others In-Active.
- Route log 160 (client DB, server time):
  - 05:28 / 05:30: "Customer [WKE1000] has the token [Shopify - esyncmate-dev-store...] selected, but the connector
    points at [walker-edison.myshopify.com]" -> the store-mismatch check works at the client (connector 252 was
    still on walker-edison; nothing was sent to the client store).
  - 05:33 onwards (after the Base URL was changed in the UI): "0 read in 1 page(s), 0 new saved ..." with the query
    `(financial_status:paid OR financial_status:pending) fulfillment_status:unfulfilled status:open created_at:>=<10 days>`
    -> token taken through the customer, Shopify answered, no matching order in the demo store.
- PROVEN at the client: token via `Customers.IntegrationTokenId` in the RouteWorker, read call to the store.
- Not yet shown: an order actually saved. Needs one order in the demo store: paid or pending, unfulfilled, open,
  created now, with a shipping address.
- Still not reported by the user: result of the inventory upload Test Run (route 159).

### Step 34: Test order created in the demo store (Chrome, on the user's request)
- `esyncmate-dev-store`, account Shahid: Orders > Create order -> The Complete Snowboard / Ice (`ESM-SB-ICE`) x 1,
  existing sample customer "Test Buyer" (Salt Lake City UT, example.com address), shipping Standard $0.00,
  Collect payment > Mark as paid > Create order.
- Result: order **#1018**, Shopify order id **8771273621690**, Paid, Unfulfilled, $699.95, location Shop location.
  Shopify sent its confirmation e-mail to the sample customer's example.com address (not a real mailbox).
- Side observation in the product picker: Ice shows 41 available and Dawn 9 = the quantities of CT_01 for L55
  (Shop location). So the inventory upload (route 159) reached the store. Custom location (17 / 4) not looked at.
- Expected at the client: route 160 (every 5 min) saves OrderNumber 8771273621690, CustomerOrderNo #1018,
  Status New, 1 line. User to confirm with the route log / CT_02_Verify.

### Step 35: Tests done (user) - revert order given, optional history cleanup script
- User: inventory upload and Get Orders tested at the client against the demo store, both done. Wants to revert
  everything and delete the demo products and orders.
- Order given: 1. route 160 In-Active from the UI (it was activated, runs every 5 min); 2. `CT_02_Revert.sql`
  (connector 252 back, deletes orders above Id 775955 = #1018 / 8771273621690); 3. `CT_99_Revert.sql`
  (mapping + connector 248 back, deletes ESM-SB-ICE / ESM-SB-DAWN from SCSInventoryFeed), recommended with
  `@ClearTokenSelection = 1`; 4. optional `CT_98_Cleanup_Test_History_OPTIONAL.sql` (new): feed data rows of the
  two items in `SCSInventoryFeedData_WKE1000`, upload batches in `InventoryBatchWise`, route logs only with a flag;
  dry run by default.
- Left as it is unless the user asks: the demo token row in the client `IntegrationTokens`; order #1018 and the
  test stock (41 / 17, 9 / 4) in our demo store.
- NOT tested: none of the revert / cleanup scripts has been run yet.

### Step 36: Both reverts run at the client (user, 2026-11-18) - output checked
- `CT_99_Revert`: mapping L55 -> 81898176578, L56 -> 81898602562 (client locations); connector 248 back on
  walker-edison; 0 feed rows left for WKE1000. Run with `@ClearTokenSelection = 0`: the DEMO token (Id 1) is still
  selected on WKE1000.
- `CT_02_Revert`: connectors 248 / 258 / 262 / 263 on walker-edison, 252 encrypted (restored from the backup taken
  while it showed walker-edison; not readable by SQL); 0 orders left for WKE1000 (test order #1018 deleted).
- Still open at the client: confirm route 160 is In-Active; look at connector 252's Base URL in the UI; the demo
  token is still selected on WKE1000 (clear it or leave it until the walker-edison token exists); optional CT_98.
- In our demo store: order #1018 and the test stock remain.
- RESULT of the day at the client: authorization, token via the customer, inventory upload and Get Orders all
  worked against the demo store; client setup restored.

### Step 37: Demo token taken off the customer, kept in the list (user's decision)
- `CT_98` dry run at the client: 4 feed data rows, 1 upload batch, 14 route log rows, 0 route data rows.
- User ran only `UPDATE Customers SET IntegrationTokenId = NULL WHERE ERPCustomerID = 'WKE1000'`. The demo store
  token row (Id 1) STAYS in the client `IntegrationTokens` on purpose, for the next test.
- Next test at the client against the demo store: Customers > Edit > Tokens -> select
  "Shopify - esyncmate-dev-store.myshopify.com", then CT_01 / CT_02 as today, reverts after. No new authorization
  needed as long as the app stays installed on the demo store.
- Before go-live: WKE1000 must get the walker-edison token (not this one).

### Step 38: "Is our map to Safavieh (SPARS) OK, how to check?" - preview script
- The ERP body is built by map "Shopify Order Transformation" (script 11) from the API-JSON Get Orders saves; route
  161 `Shopify - Place Orders in ERP` (RouteType 9) posts it to SPARS `Place_Order`. There is no map test screen /
  endpoint in the application (MapsController: get / create / update only).
- New `Testing-Client-DB-demo-store/CT_03_Preview_Order_For_ERP.sql` (read-only): map + route status, header and
  lines exactly with the map's JSON paths, [Check] per row, and any ERP-JSON / ERP-ERROR already stored.
  It mirrors the map in SQL; it does not run the JUST engine. Not run anywhere yet.
- The client has no Shopify order at the moment (CT_02_Revert deleted #1018): to preview, select the demo token,
  apply CT_02, let Get Orders read #1018 again, run CT_03, revert.
- Told the user what a preview cannot prove: that SPARS accepts the body (ItemID known in the ERP for WKE1000,
  ShipViaCode FEDX still "client to confirm"). Real proof = one agreed test order with the ERP team.

### Step 39: CT_03 at the client; lead: keep the scripts, commit, pull, push
- `CT_03` run at the client: map 12 `Shopify Order Transformation` valid, ShipViaCode FEDX, route 161 In-Active;
  no order of WKE1000 in eSyncMate (deleted by CT_02_Revert), so no header / lines to preview.
- User / lead decision: no further Get Orders test for now; the map check against a saved order is NOT done.
  Scripts stay in `Testing-Client-DB-demo-store` for later.
- Commit of the folder (CT_01, CT_01_Verify, CT_02 apply / verify / revert, CT_03, CT_98, CT_99) + this session file.

### Step 40: End of 2026-11-18 - state for the next session
- Committed `da67304` (test scripts + session file); `git pull --ff-only`: already up to date; push NOT done by me
  (denied by the session's permission settings) - the user runs `git push origin main`. `main` is ahead of origin;
  two old commits (65acf93, 160eb7b) hold the client DB password and the SPARS secret, GitHub may reject the push.
- Told the user `CT_98` can be run at the client with `@Delete = 1` (route logs kept). Result not reported yet.
- Memory files updated (client deployment state, dev DB access notes).
- Open, in order:
  1. Push `main`.
  2. Client: confirm route 160 In-Active and connector 252 Base URL = walker-edison (UI, it is encrypted).
  3. Walker Edison authorizes the app (install link expires 2026-11-19 17:06 UTC; the browser that opens the app
     must be on an IP allowed on ems.safavieh.com:8085), then select the walker-edison token on WKE1000.
  4. Map check (order -> SPARS body) against a saved order with `CT_03`; then one agreed test order with the ERP team.
  5. Walmart: confirm the resent SKU `122D100W[5PC]WHS`, list / reset the other refused SKUs.

## Summary
(fill in at end)
