# eSyncMate_V2 Application Architecture

## Project Path
`D:\eSoftage_Projects\eSyncMate_V2`

---

## Overview
**eSyncMate** is an **EDI (Electronic Data Interchange) processing platform** that integrates with multiple retail partners (Walmart, Amazon, Lowe's, Macy's, Target, Michaels, Knot) and ERP systems. It handles order ingestion, inventory sync, shipment notifications (ASN), invoicing, carrier load tendering, and product catalog management.

---

## Architecture Diagram

```
+--------------+   HTTP/JWT   +---------------------+   ADO.NET/SQL   +------------+
|  Angular 16  | -----------> |  API (.NET 6)       | --------------> | SQL Server |
|  (Port 5015) |              |  User management    |                  | ESYNCMATE  |
+--------------+              +---------------------+                  +------------+
       |                                                                     ^
       |         HTTP/JWT     +---------------------+                        |
       +--------------------> |  Processor (.NET 8) | -----------------------+
                              |  (Port 5000/8085)   |
                              |  + Hangfire Jobs     |
                              +--------+------------+
                                       |
                          +------------+------------+
                          v            v            v
                    +----------+ +----------+ +----------+
                    | Route    | | Alert    | | External |
                    | Worker   | | Worker   | | APIs     |
                    | (.exe)   | | (.exe)   | | (FTP/    |
                    +----------+ +----------+ | SFTP/    |
                                              | REST/    |
                                              | Amazon/  |
                                              | Walmart) |
                                              +----------+
```

---

## Projects

| Project | Path | Tech | Purpose |
|---------|------|------|---------|
| **UI** | `eSyncMate_V2/UI` | Angular 16, Material 15, TypeScript | Frontend — 37 feature modules, 25+ services |
| **API** | `eSyncMate_V2/API/API` | .NET 6, JWT | User management API (login, register, CRUD) |
| **eSyncMate.Processor** | `eSyncMate_V2/eSyncMate.Processor` | .NET 8, Hangfire, JWT | Main EDI processing API — 22 controllers, 80+ route managers |
| **eSyncMateEngine** | `eSyncMate_V2/eSyncMateEngine` | .NET Standard 2.0 | EDI parsing/writing engine (X12 format: ISA/GS/ST segments, loops) |
| **eSyncMate.DB** | `eSyncMate_V2/eSyncMate.DB` | .NET, ADO.NET/SqlClient | Data access layer — DBConnector, DBEntity base class, 44 entity classes |
| **eSyncMate.Data** | `eSyncMate_V2/eSyncMate.Data` | SQL Server (.sqlproj) | Database schema — 80+ tables, 28 stored procedures, 33 views |
| **eSyncMate.RouteWorker** | `eSyncMate_V2/eSyncMate.RouteWorker` | .NET console app | External process for route execution (process isolation) |
| **eSyncMate.AlertWorker** | `eSyncMate_V2/eSyncMate.AlertWorker` | .NET console app | External process for alert processing |

---

## Backend — eSyncMate.Processor (Core Service)

### Program.cs Configuration
- **Hangfire** with SQL Server storage for background job scheduling
- **JWT Bearer authentication** (issuer from `appsettings.json`)
- **Swagger** enabled in both dev and production
- **CORS** — AllowAnyOrigin, AllowAnyHeader, AllowAnyMethod
- **Forwarded Headers** — behind Nginx reverse proxy
- **Static config** bound to `CommonUtils` (ConnectionString, Company, SMTP, etc.)
- **Hangfire Dashboard** at `/dashboard`

### Controllers (22)

| Controller | Purpose |
|-----------|---------|
| `AuthenticationController` | JWT login |
| `OrdersController` | Order CRUD and management |
| `CustomersController` | Customer management |
| `ConnectorsController` | Connector setup (FTP/SFTP/REST/SQL) |
| `MapsController` | EDI field mapping |
| `PartnerGroupsController` | Partner group config |
| `RoutesController` | Route CRUD and scheduling. Auto-populates `CustomerName` on create/update. |
| `RouteTypesController` | Route type definitions |
| `RouteExceptionsController` | Route exception handling |
| `CarrierLoadTenderController` | CLT management |
| `InventoryController` | Inventory tracking |
| `InvFeedFromNDCController` | NDC inventory feed |
| `PurchaseOrderController` | PO management |
| `PurchaseOrdersTrackingController` | PO tracking |
| `ShipmentFromNDCController` | NDC shipment tracking |
| `SalesInvoiceNDCController` | NDC invoice processing |
| `CustomerProductCatalogController` | Product catalog |
| `ProductUploadPricesController` | Price upload |
| `AlertsConfigurationController` | Alert setup |
| `WarehousesController` | Warehouse management |
| `UserController` | User admin |
| `HealthController` | Health check |
| `SSCController` | SSC operations |

### Route Managers (80+)

Organized by retail partner:

**Amazon:**
- `AmazonGetOrdersRoute` — Get orders from Amazon SP-API
- `AmazonUploadInventoryRoute` — Upload inventory (single feed)
- `AmazonUploadWarehouseWiseInventoryRoute` — Upload warehouse-wise inventory (CreateDocument → UploadDocument → SubmitFeed pattern, uses `SharedHttpClientFactory.Amazon`)
- `AmazonASNShipmentNotificationRoute` — ASN shipment
- `AmazonInventoryStatusRoute` — Check inventory feed status

**Walmart:**
- `WalmartGetOrdersRoute`, `WalmartUploadInventoryRoute`, `WalmartASNShipmentNotificationRoute`, `WalmartCancellationLinesRoute`, `WalmartBulkUploadInventoryRoute`, `WalmartInventoryStatusRoute`

**Lowe's (Mirakl):**
- `LowesGetOrderRoute` — Get orders
- `LowesUpdateInventory` — JSON-based inventory upload via `POST /api/offers` (chunks of 10,000, uses `ProcessLowesItemsChunkThread`)
- `LowesBulkItemPricesRoute` — Bulk price upload
- `LowesASNShipmentNotificationRoute` — ASN
- `LowesCancellationRoute` — Order cancellation
- `LowesUploadWarehouseWiseInventoryRoute` — WHSW inventory CSV upload via Mirakl (RouteType 64), uses `SharedHttpClientFactory.Lowes`
- `LowesWHSWInventoryStatusRoute` — WHSW inventory status check via Mirakl (RouteType 65)
- `LowesPriceImportRoute` — Mirakl PRI01 CSV price upload (`POST /api/offers/pricing/imports`, multipart/form-data, semicolon-delimited `offer-sku;price`). Uses TVP (`SyncStatusUpdateType`) for bulk APPROVED→SYNCED status update. Sends ALL prices (APPROVED+SYNCED) in DELETE & REPLACE mode. RouteType 66.
- `LowesPriceImportStatusRoute` — Mirakl PRI02 status polling (`GET /api/offers/pricing/imports?import_id={id}`). Updates `InventoryBatchWiseFeedDetail` with COMPLETE/FAILED/WAITING status. RouteType 67.

**Macy's:**
- `MacysGetOrderRoute`, `MacysUpdateInventory`, `MacysBulkItemPricesRoute`, `MacysASNShipmentNotificationRoute`, `MacysCancellationRoute`

**Michaels:**
- `MichealGetOrderRoute`, `MichealUpdateInventory`, `MichealUpdatePrice`, `MichealASNShipmentNotificationRoute`, `MichealCancellationRoute`

**Knot:**
- `KnotGetOrderRoute`, `KnotUpdateInventory`, `KnotBulkItemPricesRoute`, `KnotASNShipmentNotificationRoute`, `KnotCancellationRoute`

**SCS (Safavieh):**
- `SCSGetOrders`, `SCSPlaceOrderRoute`, `SCSASNRoute`, `SCSInvoiceRoute`, `SCSCancelOrderRoute`, `SCSFullInventoryFeedRoute`, `SCSUpdateInventory`, `SCSItemPrices`, `SCSBulkItemPricesRoute`, `SCSOrderStatusRoute`

**Repaint:**
- `RepaintGetOrderRoute`, `RepaintCreateOrderRoute`, `RepaintGenerate855Route`, `GenerateEDI856ForRepaintRoute`, `GenerateEDI810ForRepaintRoute`

**Other:**
- `VeeqoUpdatedProductsQTYRoute`, `VeeqoGetSORoute`, `VeeqoCreateNewProductsRoute`
- `ShipStationUpdateSKUStocklevelsRoute`, `Download856FromShipStationRoute`
- `TargetPlusInventoryFeedWHSWiseRoute`
- `CarrierLoadTenderRoute`, `CarrierLoadTenderAcknowledgmentRoute`, `CarrierLoadTenderResponseRoute`, `CarrierLoadTender214X6Route`
- `GetPurchaseOrder850Route`, `Download855FromFTPRoute`, `Download846FromFTPRoute`, `Download856FromFTPRoute`, `Download810FromFTPRoute`
- `ProductCatalog`, `ProductCatalogStatus`, `ProductTypeAttributes`
- `AlertEngine`, `CustomerWiseAlert`

### Connectors (8 types)

| Connector | File | Purpose |
|-----------|------|---------|
| `IConnector` | `Connections/IConnector.cs` | Interface |
| `RestConnector` | `Connections/RestConnector.cs` | REST API calls via RestSharp — handles Auth1, AmazonGetToken, WALMARTGetToken, SPARSGetToken, RepaintGetToken |
| `FTPConnector` | `Connections/FTPConnector.cs` | FTP file transfer |
| `SFTPConnector` | `Connections/SFTPConnector.cs` | SFTP file transfer |
| `AmazonConnector` | `Connections/AmazonConnector.cs` | Amazon SP-API auth |
| `WalmartConnector` | `Connections/WalmartConnector.cs` | Walmart API auth |
| `SCSConnector` | `Connections/SCSConnector.cs` | SCS/SPARS API auth |
| `RepaintConnector` | `Connections/RepaintConnector.cs` | Repaint API auth (Basic) |

### ConnectorDataModel (Connector Configuration)
```csharp
// File: Models/ConnectorDataModel.cs
public class ConnectorDataModel
{
    public string ConnectivityType { get; set; }    // "SqlServer", "Rest", "SFTP", "FTP"
    public string ConnectionString { get; set; }
    public string CommandType { get; set; }         // "SP" for stored procedures
    public string Command { get; set; }             // SP name with @CUSTOMERID@, @ROUTETYPEID@, @USERNO@ tokens
    public string CustomerID { get; set; }
    public string AuthType { get; set; }            // "Auth1", "AmazonGetToken", "WALMARTGetToken", etc.
    public string BaseUrl { get; set; }
    public string Url { get; set; }
    public string Method { get; set; }              // "GET", "POST", "PUT"
    public string BodyFormat { get; set; }          // "json"
    public string ConsumerKey { get; set; }
    public string ConsumerSecret { get; set; }
    public string Token { get; set; }
    public string TokenSecret { get; set; }
    public string Realm { get; set; }
    public List<ConnectorHeader> Headers { get; set; }
    public List<Parameter> Parmeters { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
```

### RouteTypesEnum (Route Type IDs)
```
InventoryFeed = 1, GetOrders = 2, CreateOrder = 3, GetOrderStatus = 4, ASN = 5,
CreateInvoice = 6, SCSFullInventoryFeed = 7, SCSDifferentialInventoryFeed = 8,
SCSPlaceOrder = 9, SCSOrderStatus = 10, SCSASN = 11, SCSInvoice = 12,
ItemTypesReportRequest = 13, ItemTypesProcessing = 14, ProductTypeAttributes = 15,
ProductCatalog = 16, ProductCatalogStatus = 17, SCSItemPrices = 18,
SCSUpdateInventory = 19, SCSGetOrders = 20,
BulkUploadPrices = 21, BulkUploadOldPrices = 22, ASNShipmentNotification = 23,
SCSCancelOrder = 24, CancellationLines = 25, DeleteCustomerProductCatalog = 26,
WalmartUploadInventory = 27, WalmartGetOrders = 28, WalmartASNShipmentNotification = 29,
WalmartCancellationLines = 30, DownloadItemsData = 31, SCSBulkItemPrices = 32,
MacysInventoryUpload = 33, MacysBulkItemPrices = 34, MacysGetOrders = 35,
MacysASNShipmentNotification = 36, MacysCancellationLines = 37,
TargetPlusInventoryFeedWHSWise = 38,
LowesInventoryUpload = 43, LowesBulkItemPrices = 44, LowesGetOrders = 45,
LowesASNShipmentNotification = 46, LowesCancellationLines = 47,
AmazonInventoryUpload = 48, AmazonGetOrders = 49, AmazonInventoryStatus = 50,
AmazonASNShipmentNotification = 51,
KnotInventoryUpload = 52, KnotBulkItemPrices = 53, KnotGetOrders = 54,
KnotASNShipmentNotification = 55, KnotCancellationLines = 56,
MichealInventoryUpload = 57, MichealBulkItemPrices = 58, MichealGetOrders = 59,
MichealASNShipmentNotification = 60, MichealCancellationLines = 61,
AmazonWHSWInventoryUpload = 62, WalmartInventoryStatus = 63,
LowesWHSWInventoryUpload = 64, LowesWHSWInventoryStatus = 65,
LowesPriceImport = 66, LowesPriceImportStatus = 67,
CarrierLoadTender = 100-105,
GetPurchaseOrder850 = 300, Download855FromFTP = 301, Download846FromFTP = 302,
Download856FromFTP = 303, VeeqoUpdateProductsQTY = 304, VeeqoGetSO = 305,
VeeqoCreateNewProducts = 306, Download810FromFTP = 307,
ShipStationUpdateSKUStocklevels = 308,
RepaintGetOrders = 500-505
```

### RouteEngine — Job Dispatcher
- **File:** `Managers/RouteEngine.cs`
- Dispatches route execution by `route.TypeId` matching `RouteTypesEnum`
- Uses `ConcurrentDictionary<int, Routes>` to prevent duplicate execution
- Supports in-process and external process execution (`UseExternalProcess` config flag)
- **Hangfire scheduling:** Minutely, Hourly, Daily (with execution times), Weekly (with weekdays), Monthly (with days)

### SharedHttpClientFactory
- **File:** `Models/CommonUtils.cs`
- Thread-safe `ConcurrentDictionary<string, HttpClient>` per service key
- Pre-defined: `Default`, `Amazon`, `Walmart`, `Veeqo`, `ShipStation`, `Lowes`
- `SocketsHttpHandler` with connection pooling (lifetime 10min, idle 5min, max 50/server)

### Route Execution Pattern (Standard)
```csharp
public static void Execute(IConfiguration config, Routes route)
{
    // 1. Deserialize Source & Destination ConnectorDataModel from route.SourceConnectorObject.Data
    // 2. Validate connectors
    // 3. Execute source SP (replace @CUSTOMERID@, @ROUTETYPEID@, @USERNO@ tokens)
    // 4. Initialize InventoryBatchWise tracking
    // 5. Process data in chunks:
    //    a. Build request (JSON/CSV/XML)
    //    b. Call destination API (RestConnector or HttpClient)
    //    c. Bulk insert sent/response data to SCSInventoryFeedData
    //    d. Update item status to SYNCED
    // 6. Update InventoryBatchWise to Completed/Error
    // 7. Log throughout: route.SaveLog(LogTypeEnum, message, data, userNo)
}
```

### Logging Pattern
```csharp
route.SaveLog(LogTypeEnum.Info, "message", "data", userNo);     // Info
route.SaveLog(LogTypeEnum.Debug, "message", "data", userNo);    // Debug
route.SaveLog(LogTypeEnum.Error, "message", "data", userNo);    // Error
route.SaveLog(LogTypeEnum.Exception, "message", ex.ToString(), userNo); // Exception
route.SaveData("JSON-SNT", 0, body, userNo);                    // Sent data
route.SaveData("JSON-RVD", 0, response, userNo);                // Received data
```

---

## Data Access Layer — eSyncMate.DB

### DBConnector
- **File:** `eSyncMate.DB/DBConnector.cs`
- ADO.NET wrapper with `System.Data.SqlClient`
- Methods: `GetData()`, `GetDataSP()`, `Execute()`, `BeginTransaction()`, `CommitTransaction()`, `RollbackTransaction()`

### DBEntity Base Class
- **File:** `eSyncMate.DB/DBEntity.cs`
- Base class for all entities
- Methods: `PrepareInsertQuery()`, `PrepareUpdateQuery()`, `PrepareDeleteQuery()`, `PrepareGetObjectQuery()`, `PopulateObject()`
- All entities use `UseConnection(connectionString)` pattern

### Key Entities (44 total)

| Entity | Table | Purpose |
|--------|-------|---------|
| `Routes` | Routes | Route configuration + logging. **Note:** `CustomerName` column (ERPCustomerID of external partner) is auto-populated during create/update via `GetExternalCustomerName()`. Required for non-admin user filtering in `VW_Routes`. |
| `RouteData` | RouteData | Route execution data (JSON-SNT/JSON-RVD) |
| `RouteLog` | RouteLog | Route execution logs |
| `RouteTypes` | RouteTypes | Route type definitions |
| `SCSInventoryFeed` | SCSInventoryFeed | Inventory feed data + batch tracking |
| `SCSInventoryFeedData` | SCSInventoryFeedData | Per-item feed data (sent/received) |
| `InventoryBatchWise` | InventoryBatchWise | Batch execution tracking (BatchID, Status, StartDate, FinishDate) |
| `InventoryBatchWiseFeedDetail` | InventoryBatchWiseFeedDetail | Feed-level tracking (FeedDocumentID/import_id) |
| `Orders` | Orders | Sales orders |
| `OrderData` | OrderData | Order raw data |
| `OrderDetail` | OrderDetail | Order line items |
| `Customers` | Customers | Customer master |
| `Connectors` | Connectors | Connector configurations |
| `CustomerConnectors` | CustomerConnectors | Customer-connector mappings |
| `Maps` | Maps | EDI field mappings |
| `CustomerMaps` | CustomerMaps | Customer-map mappings |
| `PartnerGroups` | PartnerGroups | Partner group definitions |
| `Inventory` | Inventory | Inventory tracking |
| `PurchaseOrders` | PurchaseOrders | Purchase orders |
| `CarrierLoadTender` | CarrierLoadTender | CLT management |
| `Users` | Users | User accounts |
| `Warehouses` | Warehouses | Warehouse master |
| `AlertsConfiguration` | AlertsConfiguration | Alert definitions |
| `CustomerProductCatalog` | SCS_CustomerProductCatalog | Product catalog |

### SCSInventoryFeed Key Methods
```csharp
InsertInventoryBatchWise(InventoryBatchWise)      // Start batch tracking
UpdateInventoryBatchWise(InventoryBatchWise)      // Update batch status (Completed/Error)
InsertInventoryBatchWiseFeedDetail(batchID, status, feedDocumentID, customerID) // Track feed/import ID
UpdateInventoryBatchWiseFeedDetail(BatchID, FeedDocumentID, Status, Data) // Update feed detail status
BulkNewInsertData(connectionString, tableName, dataTable)  // Bulk insert to SCSInventoryFeedData
BulkLowesFeedData(connectionString, tableName, dataTable)  // Bulk insert to SCSLowesFeedData (BatchID, ItemID, CustomerID, ImportId, Data)
BulkUpdateItemStatus(connectionString, items)     // Bulk update items to SYNCED
UpdateItemStatus(itemId, customerId)              // Single item update to SYNCED
UpdateSCSLowesFeedData(batchID, importId, guid)   // Update SCSLowesFeedData ImportId
LowesUpdateStatusSCSInventoryFeed(CustomerID, BatchID, ImportId) // Call Sp_Lowes_UpdateStatusSCSInventoryFeed
GetLowesStockImportHeader()                       // Get CSV header row from Sp_Lowes_GetStockImportHeader
```

---

## Database — SQL Server (ESYNCMATE)

### Connection Strings (from appsettings.json)
- **Main DB:** `Server=192.168.0.44,7100;Database=ESYNCMATE`
- **Hangfire DB:** `Server=192.168.0.44,7100;Database=ESYNCMATESCHEDULER`
- **MySQL (legacy):** `Server=162.241.63.30;Database=geckote1_edi`

### Key Tables (80+)
Orders, OrderData, OrderDetail, OrderStores, Customers, CustomerConnectors, CustomerMaps, CustomerItems,
Connectors, ConnectorTypes, Maps, MapTypes, Routes, RouteTypes, RouteData, RouteLog,
PartnerGroups, CarrierLoadTender, CarrierLoadTenderData, SCSInventoryFeed, SCSInventoryFeedData,
InventoryBatchWise, InventoryBatchWiseFeedDetail, SCSAmazonFeedData,
PurchaseOrders, PurchaseOrderData, PurchaseOrderDetail, InboundEDI, OutboundEDI,
Users, Warehouses, SCS_CustomerProductCatalog, SCS_CustomerProductCatalogData,
SCS_ItemsType, SCS_ItemTypeAttribute, ProductUploadPrices, InvFeedFromNDC,
WalmartShipNodes, TargetPlusShipNodes, ApplicationSettings, etc.

### Views (33, all prefixed VW_)
VW_Orders, VW_OrderData, VW_OrderDetail, VW_Customers, VW_Connectors, VW_Maps, VW_Routes,
VW_RouteTypes, VW_RouteLog, VW_RouteData, VW_PartnerGroups, VW_Inventory, VW_BatchWiseInventory,
VW_CarrierLoadTender, VW_PurchaseOrders, VW_Users, etc.

### Stored Procedures (28)
SP_OrdersData, SP_InsertSCSInventoryFeed, SP_UpdateOrderStatus, SP_SCSInvoice, SP_GETSCSASN,
SP_GETSCSOrderStatus, SP_InsertSCSOrderStatus, SP_SyncCancelQty, SP_CLTUpdateAddress,
SP_CustomerProductCatalog, Sp_SaveCustomerProductCatalog, Sp_ProductPrices, sp_GetSCSOrder, etc.

---

## Frontend — Angular 16

### Tech Stack
- Angular 16.2.4, TypeScript 4.9.4
- Angular Material 15.2.9
- @auth0/angular-jwt 5.1.2
- @ngx-translate/core 15.0.0 (English + Spanish)
- RxJS 7.8, moment.js, xlsx
- SCSS styling, Roboto/Poppins fonts
- Dev server: port 5015, Build output: `dist/ui`

### Environment URLs
- **Production:** `https://ems.safavieh.com:8085/`
- **Development:** `http://localhost:5000/`

### Authentication
- JWT stored in `localStorage` as `access_token`
- `AuthInterceptor` adds Bearer token to all requests
- Session timeout: 2 hours of inactivity
- Guards: `AuthenticationGuard` (logged in check), `AuthorizationGuard` (role-based)

### Routes (25+)
```
/login, /register, /change-password
/edi/all-orders, /edi/process850, /edi/carrier
/edi/customers, /edi/maps, /edi/connectors
/edi/partnergroups, /edi/routes, /edi/routeTypes, /edi/routeExceptions
/edi/inventory, /edi/invFeedFromNDC, /edi/productPrices, /edi/productuploadprices
/edi/purchaseOrder, /edi/purchaseOrdersTracking
/edi/sipmentFromNdc, /edi/salesInvoiceNdc
/edi/customerProductCatalog, /edi/ediFileCounter
/edi/alertConfiguration, /edi/users
/users/list, /users/profile
```

### Company-Based Routing
- GeckoTech -> `/edi/carrier`
- eSyncMate/RepaintStudios -> `/edi/dashboard`
- Surgimac -> `/edi/purchaseOrder`

### Notifications
- Bell icon in navbar with unread badge, dropdown panel
- `NotificationService` polls every 10 seconds via `BehaviorSubject`
- Per-user notifications (userId from JWT claims)
- Used for Test Run status tracking (Flow View dialog)
- Backend: `Notifications.cs` static methods with `DateTime.UtcNow` timestamps
- Endpoints: `GET notifications`, `POST notifications/markRead/{id}`, `POST notifications/markAllRead`

### Services (25+)
api, AuthInterceptor, language, InactivityService, connectors, customers, maps,
partnerGroups, routes, routelog, routedata, route-types, customerProductCatalogDialog,
inventory, invFeed, invFeedFromNDC, purchaseOrder, purchaseOrdersTracking,
carrierLoadTender, ProductUploadPrices, shipmentFromNDC, warehouse, user,
alertsConfiguration, csv-export-service

### Key Models (models/models.ts)
User, Order, Customer, Connector, Map, PartnerGroup, Routes, RouteType, RouteLog,
CustomerProductCatalog, Inventory, BatchWiseInventory, InvFeedFromNDC, PurchaseOrder,
CarrierLoadTender, ShipmentFromNDC, SalesInvoiceNDC, PurchaseOrdersTracking, AlertConfiguration

---

## Multi-Company Support
The application serves multiple companies via configuration:
- **GeckoTech** — Original EDI processor
- **eSyncMate** — Main product (Safavieh)
- **RepaintStudios** — Repaint operations
- **Surgimac** — Medical supplies

Company is set via `CompanyName` in appsettings.json and `CommonUtils.Company`.

---

## Key Patterns

1. **Route-based EDI processing** — Each partner/operation is a "Route" scheduled via Hangfire
2. **External process isolation** — RouteWorker/AlertWorker run as separate .exe processes
3. **Entity-based data access** — `DBEntity` base class with `GetObject()`, `SaveNew()`, `Modify()`, `Delete()`
4. **EDI X12 engine** — Custom parser/writer for ISA/GS/ST/SE/GE/IEA envelope structure
5. **JSON transformation maps** — `Transformations.cs` + JUST.net for data mapping
6. **Shared HttpClient factory** — Prevents socket exhaustion across 100+ concurrent routes
7. **Chunked processing** — Large datasets split into chunks (10K-100K per batch)
8. **Batch tracking** — `InventoryBatchWise` + `InventoryBatchWiseFeedDetail` for audit trail

---

## Task Management

### Task Documents Path
`D:\eSoftage_Projects\TaskManagement`

### Active Development Plans

| Document | Description | Status |
|----------|-------------|--------|
| `SL-11751-Development-Plan.html` | Klaviyo Marketplace OAuth Integration (SocialLadder project, not eSyncMate) | Reference document |
| `Lowes-Mirakl-Stock-Import-Development-Plan.html` | Lowes Mirakl STO01/STO02/STO03 CSV stock import — 8 phases, ~7 days | **PLANNED** |

### Lowes WHSW Inventory Upload (RouteType 64-65) — IMPLEMENTED

**Routes:**
- **LowesWHSWInventoryUpload (64)** — Warehouse-wise inventory CSV upload to Mirakl
- **LowesWHSWInventoryStatus (65)** — Status check for WHSW inventory uploads

**Connectors (from DB):**
- Source: `LOWES WHSW Inventory Get Data` (SqlServer SP) / `LOWES WHSW Get Status Data`
- Destination: `Lowes WHSW Inventory Upload` (REST POST) / `Lowes WHSW Get Report` (REST GET)
- BaseUrl: `https://lowesus-prod.mirakl.net`

**Files:**
- `eSyncMate.Processor/Managers/LowesUploadWarehouseWiseInventoryRoute.cs`
- `eSyncMate.Processor/Managers/LowesWHSWInventoryStatusRoute.cs`

### Lowes Price Import PRI01/PRI02 (RouteType 66-67) — IMPLEMENTED

**Mirakl APIs:**
- **PRI01** `POST /api/offers/pricing/imports` — Upload CSV file (`offer-sku;price`, semicolon-delimited, multipart/form-data) -> returns `import_id`
- **PRI02** `GET /api/offers/pricing/imports?import_id={id}` — Poll import status (WAITING/RUNNING/COMPLETE/FAILED)

**Key Design Decisions:**
- CSV format: `offer-sku;price` (semicolon-delimited, DELETE & REPLACE mode — sends ALL prices APPROVED+SYNCED)
- Uses `HttpClient` directly (not `RestConnector`) for multipart/form-data upload via `SharedHttpClientFactory.Lowes`
- TVP approach (`SyncStatusUpdateType` with ItemID+CustomerID) for bulk APPROVED→SYNCED status update via `Sp_UpdateLowesPriceSyncStatus`
- `BeginLoadData()`/`EndLoadData()` for fast DataTable population (100K rows)
- Fire & Forget pattern: PRI01 upload → immediate SYNCED, separate PRI02 status check route
- PRI02 updates `InventoryBatchWiseFeedDetail` + `InventoryBatchWise` on COMPLETE/FAILED

**Connectors (from DB):**
- Source: `Lowes Price Import - Source` (SqlServer SP) / `Lowes Price Import Status - Source`
- Destination: `Lowes Price Import - Destination` (REST POST multipart) / `Lowes Price Import Status - Destination` (REST GET)
- BaseUrl: `https://lowesus-prod.mirakl.net`

**New Files (4):**
- `eSyncMate.Processor/Managers/LowesPriceImportRoute.cs` — PRI01 CSV upload + TVP bulk update
- `eSyncMate.Processor/Managers/LowesPriceImportStatusRoute.cs` — PRI02 status polling
- `eSyncMate.Processor/Models/LowesPriceImportResponseModel.cs` — PRI01 response (`import_id`)
- `eSyncMate.Processor/Models/LowesPriceImportStatusResponseModel.cs` — PRI02 response (status, lines_in_success, lines_in_error, has_error_report)

**Modified Files:**
- `CommonUtils.cs` — Add `LowesPriceImport = 66`, `LowesPriceImportStatus = 67`, `LowesWHSWInventoryUpload = 64`, `LowesWHSWInventoryStatus = 65`, `SharedHttpClientFactory.Lowes`
- `RouteEngine.cs` + `RouteWorker/Program.cs` — Add dispatch entries for route types 64-67
- `SCSInventoryFeed.cs` — Add `BulkLowesFeedData`, `UpdateSCSLowesFeedData`, `LowesUpdateStatusSCSInventoryFeed`, `GetLowesStockImportHeader`, `UpdateInventoryBatchWiseFeedDetail`
- `CustomerProductCatalog.cs` — Add `UpdateInventoryBatchWiseStatus`

**Database Objects (via deployment script 20):**
- `SyncStatusUpdateType` — TVP type (ItemID NVARCHAR(100), CustomerID NVARCHAR(50))
- `Sp_UpdateLowesPriceSyncStatus` — Bulk update APPROVED→SYNCED via TVP
- `Sp_GetLowesPendingPriceImports` — Get pending import IDs for PRI02

---

## Development Conventions

- **Route creation** (`RoutesController.createRoute/updateRoute`): Auto-populates `CustomerName` from external partner (skips 'eSyncMate' and 'SPARS Customer' internal parties). Error responses properly return `l_Response.Message` (not `l_Result.Description`).
- **VW_Routes** uses INNER JOINs on Customers, Connectors, PartnerGroups, RouteTypes — routes with invalid FK values won't appear in the list.
- Routes follow static `Execute(IConfiguration config, Routes route)` pattern
- Deep-clone route/feed objects for thread-safe chunk processing: `JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj))`
- Source connector SP tokens: `@CUSTOMERID@`, `@ROUTETYPEID@`, `@USERNO@`
- Bulk insert via `SqlBulkCopy` for large datasets
- Log types: `CSV-SNT` (CSV sent), `CSV-RVD` (CSV received), `CSV-ERR` (CSV errors), `JSON-SNT`, `JSON-RVD`
- HttpClient reuse via `SharedHttpClientFactory` — NEVER create `new HttpClient()` per request
- Connector auth configured in `ConnectorDataModel.Headers` JSON array

---

## 5. Flow Interface

### Overview
The **Flow Interface** groups multiple EDI Routes into a single manageable unit (Flow) per retail partner, with centralized scheduling and Hangfire job management.

### Database Tables

| Table | Purpose |
|-------|---------|
| `Flows` | Parent record — Id, CustomerID, Title, Description, Status, audit fields |
| `FlowDetails` | Child records — FlowId (FK), RouteId (FK), scheduling fields (FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime) |
| `FlowHistory` | Audit trail — logs every flow-route change (activations, deactivations, removals) |

### Stored Procedures

| SP | Purpose |
|----|---------|
| `Sp_LogAndUpdateRoute` | Called after FlowDetail save/update. Logs to FlowHistory. When Active: updates Routes with scheduling + JobID. When In-Active: sets Routes to In-Active, clears JobID. |
| `Sp_GetAutofillByRouteId` | Returns scheduling autofill data when user selects a route in the Flow Detail dialog. |

### API Endpoints (`api/flows`)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `getFlows` | List flows filtered by customer (role-based) |
| POST | `createFlow` | Create flow + details, schedule Hangfire jobs |
| POST | `updateFlow` | Update flow, manage job transitions. **Note:** Uses POST not PUT (Kong proxy blocks PUT). |
| POST | `deleteFlow/{id}` | Soft-delete flow + details, cleanup Hangfire jobs. **Note:** Uses POST not DELETE (Kong proxy blocks DELETE). |
| GET | `getConfiguredRouteIds` | Get route IDs already in use (prevent duplicates) |
| GET | `GetByRouteId` | Autofill scheduling data for route selection |
| POST | `testRun/{routeId}` | Manual test run — fires Hangfire job + creates notification |
| GET | `notifications` | Get per-user notifications (from JWT claims) |
| POST | `notifications/markRead/{id}` | Mark single notification as read |
| POST | `notifications/markAllRead` | Mark all user notifications as read |

### Hangfire Job Lifecycle

Two types of Hangfire jobs are used:
1. **BackgroundJob** (one-time wait job) — created by `ScheduleWaitJob()`, stored in `Routes.JobID`
2. **RecurringJob** — created by `Schedule()` → `SetupRouteJob()`, named like `Route [123] at [09:00]`

When activating: `ScheduleWaitJob()` → creates BackgroundJob → on trigger → `Schedule()` → creates RecurringJob(s)
When deactivating/removing: `BackgroundJob.Delete(jobId)` + `RemoveRouteJob(route)` → clears both job types

### Status Behavior

| Action | Flow Status | Detail Status | Routes Table | Hangfire |
|--------|:-----------:|:-------------:|:------------:|:--------:|
| General tab → Active | Active | All Active | All Active + JobID set | Jobs scheduled |
| General tab → In-Active | In-Active | All In-Active | All In-Active + JobID NULL | All jobs removed |
| Individual detail → Active | Recalculated | That detail Active | That route Active | Job scheduled |
| Individual detail → In-Active | Recalculated | That detail In-Active | That route In-Active | Job removed |
| Remove detail row | Recalculated | DELETED | Route In-Active + JobID NULL | Both job types removed |
| Delete entire flow | DELETED | All DELETED | All routes In-Active | All jobs removed |

### Frontend Components

| Component | Path | Purpose |
|-----------|------|---------|
| FlowsComponent | `UI/src/app/flows/flows.component.*` | Main listing with partner dropdown (searchable) |
| AddFlowDialog | `UI/src/app/flows/add-flow-dialog/` | Create flow — General + Route Details tabs |
| AddFlowDetailDialog | `UI/src/app/flows/add-flow-detail-dialog/` | Configure single route schedule |
| EditFlowDialog | `UI/src/app/flows/edit-flow-dialog/` | Edit existing flow (customer locked) |
| ViewFlowDialog | `UI/src/app/flows/view-flow-dialog/` | Read-only view with 3 tabs: Route Details, Flow Diagram (Pipeline), Hub & Spoke |
| ConfirmDeleteDialog | `UI/src/app/flows/confirm-delete-dialog/` | Delete confirmation |

### Flow Diagrams (View Mode)

The View Flow dialog has three tabs:

1. **Route Details** — Card-based view showing each route's frequency, status, and next recurrence
2. **Flow Diagram (Sequential Pipeline)** — Horizontal step-by-step diagram: Source → Route Action → Destination for each route, connected by arrows
3. **Hub & Spoke** — eSyncMate at center (circle), ERP on left, Trading Partner on right, routes as spokes

#### Diagram Features
- **Dynamic route direction** based on route name keywords (fetch/download = inbound, upload/send = outbound) with API fallback using `sourceParty`/`destinationParty`
- **PDF download** via `html2canvas` + `jsPDF`
- **Maximize popup** — full screen view with download
- **Color-coded nodes**: Green (ERP/SPARS), Orange (eSyncMate), Indigo (Trading Partner)
- **Status indicators**: Green border = Active, Red border = Inactive
- **Tooltips** on all elements showing full details

#### Route Direction Logic (`getFlowDirection`)
| Route Name Contains | Direction |
|---|---|
| fetch full/differential inventory, fetch asn, fetch cancel | ERP → eSyncMate |
| upload inventory/warehouse, upload prices, send asn, send cancel, create/delete product | eSyncMate → Partner |
| get order | Partner → eSyncMate |
| request/download item type, get item type attribute, download items data | eSyncMate → Partner |
| place order | eSyncMate → ERP |
| Generic: fetch/download/get/pull/import | Inbound (check for 'erp' keyword) |
| Generic: upload/send/push/create/submit/export | Outbound (check for 'erp' keyword) |
| Fallback | Uses `routeInfo.sourceParty` / `routeInfo.destinationParty` from API |

### Backend Files

| File | Purpose |
|------|---------|
| `FlowsController.cs` | 6 API endpoints under /api/flows |
| `FlowsManager.cs` | ProcessUpdateDetail — handles detail update/insert with Hangfire job management |
| `RouteEngine.cs` | ScheduleWaitJob, Schedule, Execute, RemoveRouteJob |
| `Flows.cs` (DB Entity) | Entity ORM — GetList, SaveNew, Modify, ResolveIdByTitle |
| `FlowDetails.cs` (DB Entity) | Entity ORM — SaveWithRoute, UpdateWithRoute → calls Sp_LogAndUpdateRoute |
| `CustomerModel.cs` | Request/Response models (SaveFlowDataModel, EditFlowDataModel, FlowsResponseModel, ConfiguredRouteIdsResponse) |

### Deployment Scripts

Located at `D:\eSoftage_Projects\scripts\eSyncmateScripts\`:

| # | Script | Purpose |
|---|--------|---------|
| 01 | `01_Create_Flows_Table.sql` | Flows table |
| 02 | `02_Create_FlowDetails_Table.sql` | FlowDetails table (FK to Flows + Routes) |
| 03 | `03_Create_FlowHistory_Table.sql` | FlowHistory audit table |
| 04 | `04_Create_Sp_LogAndUpdateRoute.sql` | SP: log history + update routes (Active AND In-Active handling) |
| 05 | `05_Create_Sp_GetAutofillByRouteId.sql` | SP: autofill scheduling data |
| 06 | `06_Insert_RouteTypes_Knot_Micheal.sql` | RouteTypes IDs 52-63 |
| 07 | `07_Add_Indexes_FlowDetails.sql` | Performance indexes |
| 08 | `08_Update_Routes_Names.sql` | Route names → "[Partner] - [Description]" convention |
| 09 | `09_Insert_Flows_Data_Target.sql` | Flows data for TAR6266P + TAR6266PAH (7 flows each) |
| 10 | `10_Insert_Flows_Data_Other_Partners.sql` | Flows data for Walmart, Macys, Lowes, Amazon, Knot, Michaels |
| 11 | `11_Split_Product_Catalog_Flows.sql` | Split "Product Catalog Management" into "Products Download" + "Products Creation" for TAR6266P & TAR6266PAH |
| 12 | `12_Create_Notifications_Table.sql` | Notifications table for Test Run and system notifications |
| 13 | `13_Lowes_PriceImport_Complete_Deployment.sql` | Combined script: RouteTypes 66-67, SyncStatusUpdateType TVP, SPs, Connectors for PRI01+PRI02 |
| 17 | `17_Insert_RouteType_LowesPriceImportStatus.sql` | RouteType 67 (Lowes Price Import Status PRI02) |
| 18 | `18_Create_Sp_GetLowesPendingPriceImports.sql` | SP: Get pending import IDs for PRI02 |
| 19 | `19_Create_LowesPriceImportStatus_Route.sql` | Source + Destination Connectors for PRI02 |
| 20 | `20_Lowes_Complete_Deployment_AllRoutes.sql` | **Master deployment**: RouteTypes 64-67, TVP, SPs, all 8 connectors (WHSW+Price), fix NULL CustomerName |

---

## 6. Sidebar Menu Structure

```
Dashboard (ESYNCMATE/REPAINTSTUDIOS only)
  └── Orders KPI strip + partner cards + status grid (auto-refresh 15min)

Order Management (ESYNCMATE/REPAINTSTUDIOS only)
  └── All Orders

Purchases (SURGIMAC only)
  └── Purchase Orders

Carrier Load Management (GECKOTECH only)
  └── Carrier Load Tender
  └── EDI File Counter

Product Management
  └── Customer Product Catalog, Prices, Inventory, etc.

Alerts
  └── Alert Configuration

Setup (Admin/SetupMenu users only)
  └── Customers
  └── Connectors
  └── Maps
  └── Partner Groups
  └── Routes
  └── Route Types
  └── Flows
  └── Scheduler View (Hangfire Dashboard) — last position

Exceptions
  └── Route Exceptions

Admin
  └── User Management
```

---

## 7. Alert Engine Email System

### Email Sending Flow
```
AlertEngine.Execute(alertId)
  → CustomerWiseAlert.Execute()
    → Execute SP query → resolve email body → beautify HTML → send via Microsoft Graph API
```

### Email Body Priority
1. **SP Result Columns**: AlertReason, BodyData, or Data column
2. **Template**: CustomerAlerts.EmailBody with `[ColumnName]` / `@ColumnName` placeholders
3. **Auto-generate**: HTML table from full DataTable result

### Microsoft 365 / Outlook HTML Compatibility Rules
- **NO `<style>` blocks** — Outlook strips them. All CSS must be inline.
- **NO `linear-gradient`** — Use `bgcolor` attribute + `background-color` CSS (solid color only).
- **NO `border-radius`** — Outlook ignores it.
- **NO `max-width`** — Use `width` attribute on `<table>` tag.
- **`bgcolor` HTML attribute** required alongside `background-color` CSS — Outlook reads `bgcolor`, not CSS.
- **`font-family`** must be on every `<td>`, `<th>`, `<h1>` — Outlook resets fonts per cell.

### Email Template Structure (Outlook-safe)
```html
<table width='800' style='background-color:#ffffff;border:1px solid #e0e0e0;'>
  <tr><td bgcolor='#1e3a8a' style='background-color:#1e3a8a;color:#ffffff;padding:30px;'>
    <h1>[Alert Name]</h1>
  </td></tr>
  <tr><td style='padding:30px 40px;'>
    [Content with inline-styled tables]
  </td></tr>
  <tr><td bgcolor='#f8fafc' style='background-color:#f8fafc;'>
    Footer
  </td></tr>
</table>
```

### Configuration (appsettings.json)
```json
"MicrosoftGraph": {
  "TenantId": "...",
  "ClientId": "...",
  "ClientSecret": "...",
  "SenderEmail": "alerts@SAFAVIEH.COM"
}
```

---

## 8. eSyncMate API Monitor (Separate Project)

**Path:** `D:\eSoftage_Projects\eSyncmateApiMonitor`

### Overview
Windows Service (.NET 8 Worker) that monitors API health, Hangfire jobs, and database connectivity. Sends email alerts via Microsoft Graph API when unhealthy.

### Architecture
- **ApiHealthWorker** — Checks HTTP endpoints every 120s. Sends email when consecutive failures meet threshold.
- **HangfireMonitorWorker** — Checks failed jobs, stuck jobs, server heartbeats every 300s.
- **DatabaseMonitorWorker** — Checks SQL Server connectivity every 300s.

### Alert Logic (Throttled)
- **First failure** → Record, wait for threshold (default: 2 consecutive failures)
- **Threshold met** → Send ONE alert email
- **Continued failure** → Re-alert only after `ReminderIntervalMinutes` (default: 30 min)
- **Recovery** → Reset state; next failure triggers fresh alert
- **MonitorStateService** tracks: `ShouldSendAlert(key, reminderMinutes)`, `MarkAlertSent(key)`, `RecordSuccess(key)` clears alert history

### MSI Installer
- **WiX Toolset v6** — `eSyncmateApiMonitor.Installer\Package.wxs`
- Installs to `C:\Program Files\eSoftage\eSyncmateApiMonitor\`
- Registers Windows Service (auto-start, auto-restart on failure)
- `<Files Include="$(var.eSyncmateApiMonitor.TargetDir)**" />` — harvests ALL build output
- EXE component has `ServiceInstall` directly (must be same component as EXE)
- Build: `dotnet build eSyncmateApiMonitor.Installer\eSyncmateApiMonitor.Installer.wixproj -c Release`

### Key Files
| File | Purpose |
|------|---------|
| `Program.cs` | Minimal host setup — no Serilog, no logging |
| `Workers/ApiHealthWorker.cs` | API endpoint monitoring |
| `Workers/HangfireMonitorWorker.cs` | Hangfire job monitoring |
| `Workers/DatabaseMonitorWorker.cs` | SQL Server connectivity monitoring |
| `Services/EmailService.cs` | Microsoft Graph email with Outlook-safe inline HTML |
| `Services/MonitorStateService.cs` | Failure counter, downSince tracking, alert throttling (ShouldSendAlert/MarkAlertSent) |
| `appsettings.json` | All configuration (endpoints, intervals, thresholds, email) |

---

## 9. Routes Naming Convention

All routes follow: **`[Partner Name] - [Action Description]`**

Examples: `Target - Get Orders`, `Amazon - Upload Inventory`, `Michaels - Send ASN Shipment Notification`

### Partners & Route Type IDs

| Partner | CustomerID | Route Type IDs |
|---------|-----------|---------------|
| Target | TAR6266P | Shared types (7-32, 38) |
| Target SEI | TAR6266PAH | Shared types (7-32, 38) |
| Walmart | WAL4001MP | 27-30 |
| Macy's | MAC0149M | 33-37 |
| Lowe's | LOW2221MP | 43-47, 64-67 |
| Amazon | AMA1005 | 48-51, 62 |
| Knot | KNO8068 | 52-56 |
| Michaels | MIC1300MP | 57-61 |

---

## 10. UI Improvements

### Page Titles
Every interface displays a page title with an icon at the top. Style defined in global `styles.scss` as `.page-title`. Uses eSyncMate logo orange color (`#E8834A`) for text, no background.

### Searchable Dropdowns
Multiple interfaces use searchable `mat-select` dropdowns with a sticky search input inside the dropdown panel. Pattern:
```html
<mat-select (openedChange)="onSelectOpened($event)">
  <div class="select-search-container">
    <input class="select-search-input" [(ngModel)]="searchText" (input)="filterOptions()" (keydown)="$event.stopPropagation()">
  </div>
  <mat-option value="">All Items</mat-option>
  <mat-option *ngFor="let item of filteredOptions" [value]="item.id">{{ item.name }}</mat-option>
</mat-select>
```

Implemented in: Orders (Status, Customer), Inventory (Customer, Route Type), Logs (Route Name), Flows (Partner).

### Status Badges
All status badges across interfaces use `display: inline-block` (not `-webkit-inline-box`) with `[ngClass]` on a `<span>` inside `<td>` (never on `<td>` directly — breaks table row layout). Dark text on light background with colored border for readability.

### Orders Interface
- Single "Actions" column consolidating all action buttons (View, Files, Re-Process, ASN, ACK, Invoice, etc.)
- Company-level conditions (`isCompany !== 'esyncmate'`) handled via `<ng-container>` wrapper
- Original `statusOptions` array preserved; `'Select Status'` excluded from searchable list via `getFilterableStatuses()`

### Inventory Interface
- Batch-wise popup: indigo header with batch status chip, SyncDate column (modifiedDate or createdDate), dynamic column header (Received Date for Full/Differential inventory, Sent Date for others)
- Fixed dialog dimensions: `95vw x 85vh` with `panelClass: 'batch-wise-dialog-panel'`
- Route type badge styling in table

### Logs Interface
- Searchable route name dropdown with `min-width: 320px`
- Flex layout search form
- Status on `<span>` not `<td>`

---

## 11. Knot ASN Split Shipment Support

### Overview
Knot (Mirakl) marketplace requires split shipments — when a seller ships items on different days, each shipment must be sent separately via `POST /api/shipments` (ST01) with a `shipped` flag indicating whether this is the final shipment.

### Changes Made
- **`SP_OrdersData.sql`** — Added `KNO8068` section with `Shipped` BIT column. Uses `CROSS APPLY` to compute remaining qty: `SUM(LineQty) - SUM(ASNQty) - SUM(CancelQty)`. If remaining = 0 → `Shipped = 1` (last shipment), else `Shipped = 0`.
- **`KnotAsnRequestModel.cs`** — Added `shipped` bool property to `KnotShipment` class (defaults to `true`).
- **`KnotASNShipmentNotificationRoute.cs`** — Reads `Shipped` column from SP result if available (backward compatible via `Columns.Contains("Shipped")`).

### Shipped Flag Logic
| Scenario | Remaining Qty | shipped |
|----------|--------------|---------|
| 2 lines, first shipped | 1 | false |
| 2 lines, both shipped | 0 | true |
| 1 line, shipped | 0 | true |
| 3 lines, 1 cancelled, 2 shipped | 0 | true |

---

## 12. Help Guide System

### Overview
Each interface has a Help Guide dialog accessible via a help icon button. All help dialogs share common styles from `shared-help-dialog.scss` and include a **Download** button that exports the help content as a standalone HTML file.

### Help Dialog Components
| Interface | Component | Path |
|-----------|-----------|------|
| Inventory Feed Summary | `InventoryHelpDialogComponent` | `inventory/inventory-help-dialog/` |
| Orders | `OrderHelpDialogComponent` | `orders/order-help-dialog/` |
| Product Prices | `ProductPricesHelpDialogComponent` | `product-prices/product-prices-help-dialog/` |
| Product Promotions | `UploadPricesHelpDialogComponent` | `product-upload-prices/upload-prices-help-dialog/` |
| Product Catalog | `ProductCatalogHelpDialogComponent` | `customer-product-catalog/product-catalog-help-dialog/` |
| Customers | `CustomersHelpDialogComponent` | `customers/customers-help-dialog/` |
| Connectors | `ConnectorsHelpDialogComponent` | `connectors/connectors-help-dialog/` |
| Maps | `MapsHelpDialogComponent` | `maps/maps-help-dialog/` |
| Routes | `RoutesHelpDialogComponent` | `routes/routes-help-dialog/` |
| Route Types | `RouteTypesHelpDialogComponent` | `route-types/route-types-help-dialog/` |
| Partner Groups | `PartnerGroupsHelpDialogComponent` | `partnergroups/partnergroups-help-dialog/` |
| Alert Configuration | `AlertConfigHelpDialogComponent` | `alert-configuration/alert-config-help-dialog/` |
| Flows | `FlowsHelpDialogComponent` | `flows/flows-help-dialog/` |
| Users | `UsersHelpDialogComponent` | `users/users-help-dialog/` |
| Role Management | `RoleHelpDialogComponent` | `role-management/role-help-dialog/` |

### Download Feature
Each help dialog has a download button that captures all CSS from document stylesheets, wraps the content in a standalone HTML file with Google Fonts (Poppins + Material Icons), and triggers a browser download.

---

## 13. Connector Edit Dialog — Null Data Fix

When a connector's `Data` field is null or empty, the Edit Connector dialog would crash because `JSON.parse(null)` throws an error. Fixed with try/catch and default empty object. When connectivity type is empty, form initializes with empty fields so user can fill in and save.

**File:** `UI/src/app/connectors/edit-connector-dialog/edit-connector-dialog.component.ts`

---

## 14. Date Format Standardization

All date displays across the application use `MM/dd/yyyy hh:mm a` (12-hour with AM/PM) format, **except** `orderDate` in the Orders interface which uses `MM/dd/yyyy` (date only).

### Previous formats replaced:
- `MMM-dd-YYYY` → `MM/dd/yyyy hh:mm a`
- `MMM-dd-YYYY HH:mm:ss` → `MM/dd/yyyy hh:mm a`
- `dd-MMM-YYYY` → `MM/dd/yyyy hh:mm a`
- `MM/dd/yyyy HH:mm` (24-hour) → `MM/dd/yyyy hh:mm a`
- `MM/dd/yyyy` (date only) → `MM/dd/yyyy hh:mm a`

---

## 15. Dashboard — Shipped Status Color Fix

The Shipped status color in the Partner Order Distribution section was `#33691e` (dark green) while the main KPI tile used `#3f51b5` (indigo). Updated `statusConfig` in `dashboard.component.ts` to use `#3f51b5` with matching background `#e8eaf6`.

---

## 16. Route Exceptions Interface Fixes

- **View File blinking fix**: `viewFile()` was overwriting `this.listOfRouteExceptions` (table data source) with the route log API response. Changed to use local variable `routeLogData`.
- **Column rename**: "Route Name" → "Routes" in both table header and dropdown label.
- **Action button positioning**: Fixed with absolute positioning for spinner inside button, fixed column width.

---

## 17. RouteTestApp — Amazon Missing Orders Report

### Purpose
Console app that compares Amazon Shipped orders (from SP-API) against eSyncMate database orders to identify missing orders and their reasons.

### Location
`D:\eSoftage_Projects\eSyncMate_V2\RouteTestApp\Program.cs` — `FindMissingAmazonOrders()` function

### How It Works
1. Gets Amazon OAuth token via SP-API credentials
2. Fetches Shipped orders by CreatedDate AND LastUpdatedDate (covers orders shipped on a date even if created earlier)
3. Merges and deduplicates both sets
4. Queries eSyncMate database for the same date range
5. Compares and identifies missing orders with reasons (FBA/AFN, Already Shipped MFN, etc.)
6. Exports Excel report: Sheet 1 = Summary, Sheet 2 = Missing Order Details

### Configuration
Hardcoded credentials in the function — update `TODO_` values before running:
- Amazon: BaseUrl, ClientId, ClientSecret, RefreshToken, ApplicationId, MarketplaceId
- Database: ConnectionString, ERPCustomerID
- Date range: CreatedAfter, CreatedBefore

### Rate Limiting
Handles Amazon `429 TooManyRequests` and `QuotaExceeded` with exponential backoff (10s → 20s → 40s → 60s) and automatic retry. 2-second delay between requests.

---

## 18. Build & Deployment

### Deployment Scripts
Located at `D:\eSoftage_Projects\scripts\eSyncmateScripts\`:
- Script 28: `28_Add_MFA_Support.sql` — 2FA columns (MFAEnabled, MFASecret) + VW_Users update (ready but not deployed)

### API Monitor MSI Build
```bash
cd D:\eSoftage_Projects\eSyncmateApiMonitor
build.bat
```
Steps: Publish → Build MSI → Copy MSI to `publish/` folder. Deploy by copying MSI to server and double-click.

### Secrets Management
- `ClientSecret` stored as `<<SET_IN_PRODUCTION>>` in repo
- Set actual value on production server's `appsettings.json` after deployment
- GitHub push protection blocks secrets in commits
