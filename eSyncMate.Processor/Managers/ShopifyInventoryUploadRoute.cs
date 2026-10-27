using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using System.Data;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Shopify - Upload Warehouse-Wise Inventory (RouteType 80, Task 00006).
    /// Source Sp_SCS_GetProductCatalog (branch ShopifyInventoryUpload) returns the customer's UPDATED/NEW SCSInventoryFeed rows
    /// (filled by the ERP Full / Differential feed; one row per item, ATS_&lt;WHSID&gt; columns, SKU, ProductId).
    /// Shopify stock calls take the inventory item id, not the SKU: CustomerProductCatalogPrices.ProductID, filled by
    /// ShopifyProductSyncRoute (RouteType 81), otherwise CustomerItemCode when it is the bare number
    /// (gid://shopify/InventoryItem/ is added here). No Shopify lookup is made here.
    /// The warehouse -> Shopify location mapping is TargetPlusShipNodes (Warehouse Mapping screen): ShipNode = gid://shopify/Location/...
    /// Each item/location pair is one quantity; 250 quantities per inventorySetQuantities call (Shopify's array limit),
    /// absolute "available" quantity, changeFromQuantity null (the ERP is the source of truth), idempotency key per call.
    /// Items whose every quantity landed become SYNCED; no inventory item id yet or a rejected quantity makes the item ERROR,
    /// with the reason in SCSInventoryFeedData_&lt;CustomerID&gt;. A failed call (token, network, HTTP, throttle, a rejection
    /// that names no row) leaves its items UPDATED / NEW, so the next run sends them again. A zero rejected with
    /// ITEM_NOT_STOCKED_AT_LOCATION is not an error; every other rejection is.
    /// </summary>
    public class ShopifyInventoryUploadRoute
    {
        private const int ChunkSize = 250;
        private const int StatusBatchSize = 1000;
        private const string InventoryItemGidPrefix = "gid://shopify/InventoryItem/";
        // A zero for a location the item is not stocked at is already right: the only rejected zero that is not an error.
        // API 2026-10 (the connector's version) accepts such a zero without an error; older versions answer this code.
        // Any other code (INVALID_LOCATION, INVALID_INVENTORY_ITEM, ...) is a real problem and puts the item in ERROR.
        private const string NotStockedCode = "ITEM_NOT_STOCKED_AT_LOCATION";

        private const string SetQuantitiesMutation = @"mutation SetStock($input: InventorySetQuantitiesInput!, $key: String!) {
  inventorySetQuantities(input: $input) @idempotent(key: $key) {
    inventoryAdjustmentGroup {
      changes { name delta quantityAfterChange item { id } location { id } }
    }
    userErrors { code field message }
  }
}";

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_Data = new DataTable();
            DataTable l_ShipNodes = new DataTable();
            InventoryBatchWise l_InventoryBatchWise = new InventoryBatchWise();
            SCSInventoryFeed l_SCSInventoryFeed = new SCSInventoryFeed();

            l_InventoryBatchWise.BatchID = Guid.NewGuid().ToString();

            try
            {
                ConnectorDataModel l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null || l_SourceConnector.ConnectivityType != ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly (eSyncMate SQL Server connector expected)", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null || l_DestinationConnector.ConnectivityType != ConnectorTypesEnum.Rest.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly (Shopify REST connector expected)", string.Empty, userNo);
                    return;
                }

                string l_CustomerId = l_SourceConnector.CustomerID;
                DBConnector l_Connection = new DBConnector(l_SourceConnector.ConnectionString);

                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_CustomerId);
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.ShopifyInventoryUpload));
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                if (l_SourceConnector.CommandType == "SP")
                {
                    l_Connection.GetDataSP(l_SourceConnector.Command, ref l_Data);
                }

                l_SCSInventoryFeed.UseConnection(l_SourceConnector.ConnectionString);

                if (l_Data.Rows.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]: no updated items", string.Empty, userNo);
                    return;
                }

                List<(string WHSID, string LocationId)> l_Locations = GetLocations(l_SCSInventoryFeed, l_CustomerId, l_Data, route, userNo, ref l_ShipNodes);

                if (l_Locations.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Error, $"No Shopify location is mapped for [{l_CustomerId}] in Warehouse Mapping. Nothing was sent.", string.Empty, userNo);
                    return;
                }

                l_InventoryBatchWise.StartDate = DateTime.Now;
                l_InventoryBatchWise.Status = "Processing";
                l_InventoryBatchWise.RouteType = RouteTypesEnum.ShopifyInventoryUpload.ToString();
                l_InventoryBatchWise.CustomerID = l_CustomerId;
                l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                Dictionary<string, ShopifyInventoryItemModel> l_Items = new Dictionary<string, ShopifyInventoryItemModel>();
                List<ShopifyInventoryQuantityModel> l_Quantities = BuildQuantities(l_Data, l_Locations, l_Items);

                // Zeros go in calls of their own, after the rest: if Shopify ever rejects a zero for a location the item
                // is not stocked at (the dev store accepts it), only those calls are sent twice
                List<List<ShopifyInventoryQuantityModel>> l_Chunks = new List<List<ShopifyInventoryQuantityModel>>();

                foreach (List<ShopifyInventoryQuantityModel> l_Group in new[] { l_Quantities.Where(q => q.Quantity > 0).ToList(), l_Quantities.Where(q => q.Quantity == 0).ToList() })
                {
                    for (int l_Start = 0; l_Start < l_Group.Count; l_Start += ChunkSize)
                        l_Chunks.Add(l_Group.GetRange(l_Start, Math.Min(ChunkSize, l_Group.Count - l_Start)));
                }

                int l_Calls = l_Chunks.Count;
                int l_CallsFailed = SendAllAsync(l_Chunks, l_Items, l_DestinationConnector, route, l_InventoryBatchWise.BatchID, userNo).GetAwaiter().GetResult();

                SaveResults(l_Items, l_Data, l_SCSInventoryFeed, l_SourceConnector.ConnectionString, l_CustomerId, l_InventoryBatchWise.BatchID, out int l_Synced, out int l_Failed, out int l_Retry);

                l_InventoryBatchWise.Status = l_CallsFailed > 0 ? "Error" : "Completed";
                l_InventoryBatchWise.FinishDate = DateTime.Now;
                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                route.SaveLog(LogTypeEnum.RouteInfo, $"Shopify inventory for [{l_CustomerId}] batch [{l_InventoryBatchWise.BatchID}]: {l_Items.Count} item(s), {l_Quantities.Count} location quantities in {l_Calls} call(s) ({l_CallsFailed} failed), {l_Synced} SYNCED, {l_Failed} ERROR, {l_Retry} left for next run.", string.Empty, userNo);
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                l_InventoryBatchWise.FinishDate = DateTime.Now;
                l_InventoryBatchWise.Status = "Error";

                if (!string.IsNullOrEmpty(l_InventoryBatchWise.CustomerID))
                    l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_Data.Dispose();
                l_ShipNodes.Dispose();
            }
        }

        // Warehouse -> Shopify location; anything that is not a Location gid is a mapping mistake
        private static List<(string WHSID, string LocationId)> GetLocations(SCSInventoryFeed p_Feed, string p_CustomerId, DataTable p_Data, Routes p_Route, int p_UserNo, ref DataTable p_ShipNodes)
        {
            List<(string WHSID, string LocationId)> l_Locations = new List<(string WHSID, string LocationId)>();

            p_Feed.TargetPlusShipNode(p_CustomerId, ref p_ShipNodes);

            foreach (DataRow l_Node in p_ShipNodes.Rows)
            {
                string l_WhsId = Convert.ToString(l_Node["WHSID"])?.Trim() ?? string.Empty;
                string l_Gid = Convert.ToString(l_Node["ShipNode"])?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(l_WhsId))
                    continue;

                if (!l_Gid.StartsWith("gid://shopify/Location/", StringComparison.OrdinalIgnoreCase))
                {
                    p_Route.SaveLog(LogTypeEnum.Error, $"Warehouse Mapping for [{p_CustomerId}] warehouse [{l_WhsId}] is not a Shopify location id (expected gid://shopify/Location/...): [{l_Gid}]. That warehouse is skipped.", string.Empty, p_UserNo);
                    continue;
                }

                if (!p_Data.Columns.Contains($"ATS_{l_WhsId}"))
                {
                    p_Route.SaveLog(LogTypeEnum.Error, $"Warehouse Mapping for [{p_CustomerId}] has warehouse [{l_WhsId}] but the inventory feed has no ATS_{l_WhsId} column. That warehouse is skipped.", string.Empty, p_UserNo);
                    continue;
                }

                l_Locations.Add((l_WhsId, l_Gid));
            }

            return l_Locations;
        }

        private static List<ShopifyInventoryQuantityModel> BuildQuantities(DataTable p_Data, List<(string WHSID, string LocationId)> p_Locations,
                                                                          Dictionary<string, ShopifyInventoryItemModel> p_Items)
        {
            List<ShopifyInventoryQuantityModel> l_Quantities = new List<ShopifyInventoryQuantityModel>();

            foreach (DataRow l_Row in p_Data.Rows)
            {
                ShopifyInventoryItemModel l_Item = new ShopifyInventoryItemModel
                {
                    ItemId = Convert.ToString(l_Row["ItemId"]) ?? string.Empty,
                    Sku = (Convert.ToString(l_Row["SKU"]) ?? string.Empty).Trim()
                };

                p_Items[l_Item.ItemId] = l_Item;

                // The id the Sync Products route stored in CustomerProductCatalogPrices comes first (it was read from
                // Shopify); without it, an all-digit CustomerItemCode is taken as the Shopify inventory item number
                // (e.g. 52944921100474). A UPC or numeric SKU there only matters when the catalog has no id.
                string l_ItemCode = (Convert.ToString(l_Row["CustomerItemCode"]) ?? string.Empty).Trim();
                string l_CatalogId = (Convert.ToString(l_Row["ProductId"]) ?? string.Empty).Trim();
                string l_InventoryItemId = l_CatalogId.StartsWith(InventoryItemGidPrefix, StringComparison.OrdinalIgnoreCase) ? l_CatalogId
                    : l_ItemCode.Length > 0 && l_ItemCode.All(char.IsAsciiDigit) ? InventoryItemGidPrefix + l_ItemCode
                    : l_CatalogId;

                if (!l_InventoryItemId.StartsWith(InventoryItemGidPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    l_Item.Errors.Add(string.IsNullOrEmpty(l_InventoryItemId)
                        ? $"SKU [{l_Item.Sku}] has no Shopify inventory item id: CustomerItemCode is not the inventory item number and CustomerProductCatalogPrices has none yet. Send the number in CustomerItemCode, or check the SKU exists in Shopify (the Shopify - Sync Products route stores the id)."
                        : $"SKU [{l_Item.Sku}] has [{l_InventoryItemId}] in CustomerProductCatalogPrices, which is not a Shopify inventory item id ({InventoryItemGidPrefix}...).");
                    continue;
                }

                foreach ((string WHSID, string LocationId) l_Location in p_Locations)
                {
                    object l_Value = l_Row[$"ATS_{l_Location.WHSID}"];
                    int l_Qty = l_Value == DBNull.Value ? 0 : Math.Max(0, Convert.ToInt32(l_Value));

                    ShopifyInventoryQuantityModel l_Quantity = new ShopifyInventoryQuantityModel
                    {
                        ItemId = l_Item.ItemId,
                        InventoryItemId = l_InventoryItemId,
                        LocationId = l_Location.LocationId,
                        WHSID = l_Location.WHSID,
                        Quantity = l_Qty
                    };

                    l_Item.Sent.Add(l_Quantity);
                    l_Quantities.Add(l_Quantity);
                }
            }

            return l_Quantities;
        }

        // Shopify applies a call all-or-nothing: when some quantities are rejected, the rest is sent again once
        // without them, so one bad item does not block the other 249.
        // ShopifyUploadConcurrency calls in flight at once (ApplicationSettings, default 3). Every call costs ~10 points of
        // the store's bucket (Basic plan: 2,000, 100 restored per second), and ShopifyGraphQL waits when it runs low.
        // Returns the number of failed calls.
        private static async Task<int> SendAllAsync(List<List<ShopifyInventoryQuantityModel>> p_Chunks, Dictionary<string, ShopifyInventoryItemModel> p_Items,
                                                    ConnectorDataModel p_Connector, Routes p_Route, string p_BatchId, int p_UserNo)
        {
            int l_Failed = 0;

            using SemaphoreSlim l_Gate = new SemaphoreSlim(Math.Max(1, CommonUtils.ShopifyUploadConcurrency));

            IEnumerable<Task> l_Tasks = p_Chunks.Select(async (l_Chunk, l_Index) =>
            {
                await l_Gate.WaitAsync();

                try
                {
                    if (!await SendChunkAsync(l_Chunk, p_Items, p_Connector, p_Route, p_BatchId, l_Index + 1, p_UserNo))
                        Interlocked.Increment(ref l_Failed);
                }
                catch (Exception ex)
                {
                    // a call that throws is a failed call: its items go again next run, the other calls carry on
                    foreach (ShopifyInventoryQuantityModel l_Row in l_Chunk)
                        AddCallError(p_Items[l_Row.ItemId], $"Shopify call failed: {ex.Message}");

                    Interlocked.Increment(ref l_Failed);
                    SaveLog(p_Route, LogTypeEnum.Exception, $"Shopify inventorySetQuantities call {l_Index + 1} threw. Its {l_Chunk.Count} quantities are sent again on the next run.", ex.ToString(), p_UserNo);
                }
                finally
                {
                    l_Gate.Release();
                }
            }).ToList();

            await Task.WhenAll(l_Tasks);

            return l_Failed;
        }

        // Routes.SaveLog shares the route's DB connection, so calls running side by side take turns
        private static readonly object LogLock = new object();

        private static void SaveLog(Routes p_Route, LogTypeEnum p_Type, string p_Message, string p_Details, int p_UserNo)
        {
            lock (LogLock)
                p_Route.SaveLog(p_Type, p_Message, p_Details, p_UserNo);
        }

        private static void SaveData(Routes p_Route, string p_Type, string p_Data, int p_UserNo)
        {
            lock (LogLock)
                p_Route.SaveData(p_Type, 0, LogJson.Ensure(p_Data), p_UserNo);
        }

        // An item can sit in two calls at once (a stock call and a zero call), so its lists are changed under its own lock
        private static void AddError(ShopifyInventoryItemModel p_Item, string p_Error) { lock (p_Item) p_Item.Errors.Add(p_Error); }
        private static void AddCallError(ShopifyInventoryItemModel p_Item, string p_Error) { lock (p_Item) p_Item.CallErrors.Add(p_Error); }

        private static async Task<bool> SendChunkAsync(List<ShopifyInventoryQuantityModel> p_Chunk, Dictionary<string, ShopifyInventoryItemModel> p_Items, ConnectorDataModel p_Connector,
                                      Routes p_Route, string p_BatchId, int p_CallNo, int p_UserNo)
        {
            List<ShopifyInventoryQuantityModel> l_ToSend = p_Chunk;

            for (int l_Attempt = 0; l_Attempt < 2 && l_ToSend.Count > 0; l_Attempt++)
            {
                ShopifyInventorySetQuantitiesRequestModel l_Request = new ShopifyInventorySetQuantitiesRequestModel();

                l_Request.key = $"{p_BatchId}-{p_CallNo}" + (l_Attempt > 0 ? "-r" : string.Empty);
                l_Request.input.referenceDocumentUri = $"esyncmate://inventory-batch/{p_BatchId}";
                l_Request.input.quantities = l_ToSend.Select(q => new ShopifyInventorySetQuantitiesRequestModel.Quantity
                {
                    inventoryItemId = q.InventoryItemId,
                    locationId = q.LocationId,
                    quantity = q.Quantity,
                    changeFromQuantity = null
                }).ToList();

                ShopifyGraphQLResult<ShopifyInventorySetQuantitiesResponseModel> l_Result =
                    await ShopifyGraphQL.ExecuteAsync<ShopifyInventorySetQuantitiesResponseModel>(p_Connector, SetQuantitiesMutation, l_Request);

                ShopifyInventorySetQuantitiesResponseModel.InventorySetQuantities l_Payload = l_Result.Success ? l_Result.Response.data?.inventorySetQuantities : null;
                List<ShopifyGraphQLResponseModel.UserError> l_UserErrors = l_Payload?.userErrors ?? new List<ShopifyGraphQLResponseModel.UserError>();

                // Every item already has its own JSON-SNT / JSON-RVD row; the whole call goes to RouteData only when something went wrong
                if (!l_Result.Success || l_UserErrors.Count > 0)
                {
                    SaveData(p_Route, "JSON-SNT", l_Result.RequestBody, p_UserNo);
                    SaveData(p_Route, "JSON-RVD", l_Result.ResponseBody, p_UserNo);
                }

                if (!l_Result.Success)
                {
                    // token / network / HTTP / throttle: not the items' fault, they keep their status and go again next run
                    foreach (ShopifyInventoryQuantityModel l_Row in l_ToSend)
                        AddCallError(p_Items[l_Row.ItemId], $"Shopify call failed: {l_Result.Error}");

                    SaveLog(p_Route, LogTypeEnum.Error, $"Shopify inventorySetQuantities call {p_CallNo} failed: {l_Result.Error}. Its {l_ToSend.Count} quantities are sent again on the next run.", l_Result.ResponseBody, p_UserNo);
                    return false;
                }

                if (l_UserErrors.Count == 0)
                {
                    // one inventory item id per item: match the changes inside this call only
                    Dictionary<string, ShopifyInventoryItemModel> l_ByInventoryItem = new Dictionary<string, ShopifyInventoryItemModel>(StringComparer.OrdinalIgnoreCase);

                    foreach (ShopifyInventoryQuantityModel l_Row in l_ToSend)
                        l_ByInventoryItem[l_Row.InventoryItemId] = p_Items[l_Row.ItemId];

                    foreach (ShopifyInventorySetQuantitiesResponseModel.Change l_Change in l_Payload?.inventoryAdjustmentGroup?.changes ?? new List<ShopifyInventorySetQuantitiesResponseModel.Change>())
                    {
                        if (l_Change?.item?.id != null && l_ByInventoryItem.TryGetValue(l_Change.item.id, out ShopifyInventoryItemModel l_Item))
                            lock (l_Item) l_Item.Changes.Add(l_Change);
                    }

                    return true;
                }

                List<ShopifyInventoryQuantityModel> l_Rejected = new List<ShopifyInventoryQuantityModel>();

                foreach (ShopifyGraphQLResponseModel.UserError l_Error in l_UserErrors)
                {
                    int l_Index = ShopifyGraphQL.GetInputIndex(l_Error.field, "quantities");
                    string l_Message = $"{l_Error.code}: {l_Error.message}";

                    if (l_Index < 0 || l_Index >= l_ToSend.Count)
                    {
                        // an error that names no row (idempotency, reason, ...): nothing in this call was applied,
                        // and no item is to blame, so they all go again next run
                        foreach (ShopifyInventoryQuantityModel l_Row in l_ToSend)
                            AddCallError(p_Items[l_Row.ItemId], $"Shopify rejected the call: {l_Message}");

                        SaveLog(p_Route, LogTypeEnum.Error, $"Shopify inventorySetQuantities call {p_CallNo} rejected: {l_Message}. Its {l_ToSend.Count} quantities are sent again on the next run.", l_Result.ResponseBody, p_UserNo);
                        return false;
                    }

                    ShopifyInventoryQuantityModel l_Bad = l_ToSend[l_Index];
                    ShopifyInventoryItemModel l_BadItem = p_Items[l_Bad.ItemId];

                    l_Rejected.Add(l_Bad);

                    if (l_Bad.Quantity == 0 && string.Equals(l_Error.code, NotStockedCode, StringComparison.OrdinalIgnoreCase))
                    {
                        lock (l_BadItem) l_BadItem.Sent.Remove(l_Bad);
                        continue;
                    }

                    // what to do depends on the code: a wrong location is a Warehouse Mapping mistake, not a product setting
                    string l_Hint = string.Equals(l_Error.code, "INVALID_LOCATION", StringComparison.OrdinalIgnoreCase)
                        ? $"Check the Warehouse Mapping of warehouse {l_Bad.WHSID}: that Shopify location does not exist."
                        : string.Equals(l_Error.code, NotStockedCode, StringComparison.OrdinalIgnoreCase)
                            ? "Turn the location on for this product in Shopify (product > Inventory > locations)."
                            : string.Empty;

                    AddError(l_BadItem, $"Warehouse {l_Bad.WHSID} ({l_Bad.LocationId}): {l_Message.TrimEnd('.')}.{(l_Hint.Length > 0 ? " " + l_Hint : string.Empty)}");
                }

                HashSet<ShopifyInventoryQuantityModel> l_RejectedSet = new HashSet<ShopifyInventoryQuantityModel>(l_Rejected);
                l_ToSend = l_ToSend.Where(q => !l_RejectedSet.Contains(q)).ToList();
            }

            if (l_ToSend.Count > 0)
            {
                // the resend was rejected too, so its rows were not applied either; they go again next run
                foreach (ShopifyInventoryQuantityModel l_Row in l_ToSend)
                    AddCallError(p_Items[l_Row.ItemId], "Shopify rejected the call twice; not applied.");

                return false;
            }

            return true;
        }

        private static void SaveResults(Dictionary<string, ShopifyInventoryItemModel> p_Items, DataTable p_Data, SCSInventoryFeed p_Feed, string p_ConnectionString,
                                        string p_CustomerId, string p_BatchId, out int p_Synced, out int p_Failed, out int p_Retry)
        {
            DataTable l_FeedData = ProcessLowesStockImportChunkThread.CreateBulkInsertDataTable();

            DataTable l_SyncedItems = new DataTable();
            l_SyncedItems.Columns.Add("ItemId", typeof(string));
            l_SyncedItems.Columns.Add("CustomerId", typeof(string));

            DataTable l_ErrorItems = l_SyncedItems.Clone();

            DataTable l_SyncedRows = p_Data.Clone();
            DataTable l_FailedRows = p_Data.Clone();

            p_Synced = 0;
            p_Failed = 0;
            p_Retry = 0;

            HashSet<string> l_Done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow l_Row in p_Data.Rows)
            {
                ShopifyInventoryItemModel l_Item = p_Items[Convert.ToString(l_Row["ItemId"]) ?? string.Empty];

                // the same ItemId twice in the feed: counted and saved once
                if (!l_Done.Add(l_Item.ItemId))
                    continue;

                l_FeedData.Rows.Add(p_CustomerId, l_Item.ItemId, "JSON-SNT",
                    JsonConvert.SerializeObject(new { sku = l_Item.Sku, quantities = l_Item.Sent.Select(s => new { warehouse = s.WHSID, locationId = s.LocationId, inventoryItemId = s.InventoryItemId, quantity = s.Quantity }) }),
                    DateTime.Now, 1, p_BatchId);

                if (l_Item.CallErrors.Count > 0)
                {
                    // status left as it is (UPDATED / NEW), so the next run sends the item again
                    p_Retry++;
                    l_FeedData.Rows.Add(p_CustomerId, l_Item.ItemId, "JSON-ERR", JsonConvert.SerializeObject(new { success = false, retryNextRun = true, errors = l_Item.CallErrors.Concat(l_Item.Errors) }), DateTime.Now, 1, p_BatchId);
                }
                else if (l_Item.Errors.Count == 0)
                {
                    p_Synced++;
                    l_SyncedItems.Rows.Add(l_Item.ItemId, p_CustomerId);
                    l_SyncedRows.ImportRow(l_Row);
                    l_FeedData.Rows.Add(p_CustomerId, l_Item.ItemId, "JSON-RVD", JsonConvert.SerializeObject(new { success = true, changes = l_Item.Changes }), DateTime.Now, 1, p_BatchId);
                }
                else
                {
                    p_Failed++;
                    l_ErrorItems.Rows.Add(l_Item.ItemId, p_CustomerId);
                    l_FailedRows.ImportRow(l_Row);
                    l_FeedData.Rows.Add(p_CustomerId, l_Item.ItemId, "JSON-ERR", JsonConvert.SerializeObject(new { success = false, errors = l_Item.Errors }), DateTime.Now, 1, p_BatchId);
                }
            }

            UpdateStatusInBatches(p_Feed, p_ConnectionString, l_SyncedItems, "SYNCED");
            UpdateStatusInBatches(p_Feed, p_ConnectionString, l_ErrorItems, "ERROR");
            p_Feed.BulkNewInsertData(p_ConnectionString, "SCSInventoryFeedData_" + p_CustomerId, l_FeedData);

            string l_LogTable = SCSInventoryFeed.GetLogTableName(p_ConnectionString, p_CustomerId);

            if (!string.IsNullOrEmpty(l_LogTable))
            {
                string[] l_LogCols = SCSInventoryFeed.GetLogTableColumns(p_ConnectionString, l_LogTable);

                SCSInventoryFeed.BulkInsertToLogTable(p_ConnectionString, l_LogTable, l_SyncedRows, p_BatchId, "UPLOAD", l_LogCols, "Synced");
                SCSInventoryFeed.BulkInsertToLogTable(p_ConnectionString, l_LogTable, l_FailedRows, p_BatchId, "UPLOAD", l_LogCols, "Error");
            }
        }

        // BulkUpdateItemStatus puts one (ItemID AND CustomerID) condition per row in a single WHERE;
        // a full feed of 200,000 items is sent in pieces so the statement stays small
        private static void UpdateStatusInBatches(SCSInventoryFeed p_Feed, string p_ConnectionString, DataTable p_Items, string p_Status)
        {
            for (int l_Start = 0; l_Start < p_Items.Rows.Count; l_Start += StatusBatchSize)
            {
                DataTable l_Batch = p_Items.Clone();

                for (int i = l_Start; i < Math.Min(l_Start + StatusBatchSize, p_Items.Rows.Count); i++)
                    l_Batch.ImportRow(p_Items.Rows[i]);

                p_Feed.BulkUpdateItemStatus(p_ConnectionString, l_Batch, p_Status);
            }
        }
    }
}
