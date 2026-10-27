using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using System.Data;
using static eSyncMate.DB.Declarations;
using static eSyncMate.Processor.Models.ShopifyInventoryItemsResponseModel;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Shopify - Sync Products (RouteType 81, Task 00006).
    /// Pulls every inventory item of the store (inventoryItems, 250 per page) and inserts the
    /// ItemId -> inventory item id pairs that CustomerProductCatalogPrices does not have yet, through
    /// Sp_InsertMissingTargetCatalogPrices (insert only, the same SP the Target Product Sync uses).
    /// ShopifyInventoryUploadRoute sends stock with that id (CustomerProductCatalogPrices.ProductID).
    /// Shopify SKU -> ItemId comes from the source SP (Sp_SCS_GetProductCatalog, branch ShopifyProductSync):
    /// SKU = CustomerItemCode, or ItemId when CustomerItemCode is empty - the same rule the upload uses.
    /// A Shopify SKU with no SCSInventoryFeed row is not inserted (no ItemId to store it under); it is picked up
    /// on the first run after the ERP feed brings the item. An existing row is never changed, a different id is only logged.
    /// </summary>
    public class ShopifyProductSyncRoute
    {
        private const int LogListMax = 1000;

        // 250 items = ~252 cost points; on the Basic plan (2,000 bucket, 100/s) ShopifyGraphQL waits ~2.5 s
        // between pages once the bucket is low, so 200,000 items take ~35 minutes and are never throttled
        private const string InventoryItemsQuery = @"query Items($after: String) {
  inventoryItems(first: 250, after: $after) {
    nodes { id sku }
    pageInfo { hasNextPage endCursor }
  }
}";

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_Feed = new DataTable();
            DataTable l_Existing = new DataTable();
            DataTable l_Missing = new DataTable();
            DataTable l_Result = new DataTable();

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
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.ShopifyProductSync));
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                if (l_SourceConnector.CommandType == "SP")
                {
                    l_Connection.GetDataSP(l_SourceConnector.Command, ref l_Feed);
                }

                // Shopify SKU -> our ItemId; one SKU on two items cannot be told apart, so neither is stored
                Dictionary<string, string> l_SkuToItem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> l_AmbiguousSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow l_Row in l_Feed.Rows)
                {
                    string l_ItemId = Convert.ToString(l_Row["ItemId"])?.Trim() ?? string.Empty;
                    string l_Sku = Convert.ToString(l_Row["SKU"])?.Trim() ?? string.Empty;

                    if (string.IsNullOrEmpty(l_ItemId) || string.IsNullOrEmpty(l_Sku))
                        continue;

                    if (l_SkuToItem.TryGetValue(l_Sku, out string l_Other) && !string.Equals(l_Other, l_ItemId, StringComparison.OrdinalIgnoreCase))
                        l_AmbiguousSkus.Add(l_Sku);
                    else
                        l_SkuToItem[l_Sku] = l_ItemId;
                }

                foreach (string l_Sku in l_AmbiguousSkus)
                    l_SkuToItem.Remove(l_Sku);

                if (l_AmbiguousSkus.Count > 0)
                    route.SaveLog(LogTypeEnum.Error, $"{l_AmbiguousSkus.Count} SKU(s) belong to more than one item in the inventory feed of [{l_CustomerId}]. They are skipped.", LogList(l_AmbiguousSkus), userNo);

                // What we already have: ItemId -> ProductID
                CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
                l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);
                l_CustomerProductCatalog.GetCustomerProductCatalogPrices(l_CustomerId, ref l_Existing);

                Dictionary<string, string> l_ExistingItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow l_Row in l_Existing.Rows)
                {
                    string l_ItemId = Convert.ToString(l_Row["ItemId"])?.Trim() ?? string.Empty;

                    if (!string.IsNullOrEmpty(l_ItemId) && !l_ExistingItems.ContainsKey(l_ItemId))
                        l_ExistingItems.Add(l_ItemId, Convert.ToString(l_Row["ProductID"]) ?? string.Empty);
                }

                // Same shape as TargetCatalogPriceType
                l_Missing.Columns.Add("CustomerID", typeof(string));
                l_Missing.Columns.Add("ItemId", typeof(string));
                l_Missing.Columns.Add("ProductID", typeof(string));

                HashSet<string> l_Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> l_Added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<string> l_Mismatches = new List<string>();
                Dictionary<string, string> l_Corrections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                List<string> l_DuplicateSkus = new List<string>();
                List<string> l_NotInFeed = new List<string>();
                int l_ShopifyCount = 0;
                int l_NoSku = 0;
                int l_Pages = 0;
                bool l_Completed = true;
                string l_After = null;

                do
                {
                    ShopifyGraphQLResult<ShopifyInventoryItemsResponseModel> l_Page =
                        ShopifyGraphQL.Execute<ShopifyInventoryItemsResponseModel>(l_DestinationConnector, InventoryItemsQuery, new { after = l_After });

                    InventoryItems l_Items = l_Page.Success ? l_Page.Response.data?.inventoryItems : null;

                    if (l_Items == null)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Shopify inventoryItems failed for [{l_CustomerId}] on page {l_Pages + 1}: {(l_Page.Success ? "no data in the response" : l_Page.Error)}. Stopping pagination.", l_Page.ResponseBody, userNo);
                        l_Completed = false;
                        break;
                    }

                    l_Pages++;

                    foreach (InventoryItem l_Item in l_Items.nodes)
                    {
                        string l_Sku = l_Item?.sku?.Trim() ?? string.Empty;
                        string l_Gid = l_Item?.id?.Trim() ?? string.Empty;

                        if (string.IsNullOrEmpty(l_Gid))
                            continue;

                        l_ShopifyCount++;

                        if (string.IsNullOrEmpty(l_Sku))
                        {
                            l_NoSku++;
                            continue;
                        }

                        // the same SKU on two Shopify variants: the first one is kept, as the upload did
                        if (!l_Seen.Add(l_Sku))
                        {
                            l_DuplicateSkus.Add($"{l_Sku}: {l_Gid}");
                            continue;
                        }

                        if (!l_SkuToItem.TryGetValue(l_Sku, out string l_ItemId))
                        {
                            l_NotInFeed.Add(l_Sku);
                            continue;
                        }

                        if (l_ExistingItems.TryGetValue(l_ItemId, out string l_OurGid))
                        {
                            if (!string.Equals(l_OurGid, l_Gid, StringComparison.OrdinalIgnoreCase))
                            {
                                l_Mismatches.Add($"{l_ItemId} (SKU {l_Sku}): ours={l_OurGid}, shopify={l_Gid}");
                                l_Corrections[l_ItemId] = l_Gid;
                            }

                            continue;
                        }

                        if (l_Added.Add(l_ItemId))
                            l_Missing.Rows.Add(l_CustomerId, l_ItemId, l_Gid);
                    }

                    l_After = l_Items.pageInfo?.hasNextPage == true ? l_Items.pageInfo.endCursor : null;
                }
                while (!string.IsNullOrEmpty(l_After));

                // RouteInfo: SaveLog keeps only Error / Exception / RouteInfo, so Debug/Info/Warning would never reach RouteLog
                route.SaveLog(LogTypeEnum.RouteInfo, $"Shopify items for [{l_CustomerId}]: {l_ShopifyCount} in {l_Pages} page(s){(l_Completed ? "" : " (paging stopped early)")}. Missing in CustomerProductCatalogPrices: {l_Missing.Rows.Count}. Id different in Shopify: {l_Mismatches.Count}. No SKU: {l_NoSku}. Not in the inventory feed: {l_NotInFeed.Count}.", string.Empty, userNo);

                // a product deleted and created again in Shopify gets a new id; the stored one would fail every upload
                if (l_Corrections.Count > 0)
                {
                    int l_Updated = UpdateProductIds(l_Connection, l_CustomerId, l_Corrections);

                    route.SaveLog(LogTypeEnum.RouteInfo, $"{l_Mismatches.Count} item(s) had a different inventory item id in Shopify than in CustomerProductCatalogPrices; {l_Updated} row(s) updated to the Shopify id.", LogList(l_Mismatches), userNo);
                }

                if (l_DuplicateSkus.Count > 0)
                    route.SaveLog(LogTypeEnum.Error, $"{l_DuplicateSkus.Count} Shopify variant(s) repeat a SKU already seen; only the first variant of each SKU is stored.", LogList(l_DuplicateSkus), userNo);

                if (l_NotInFeed.Count > 0)
                    route.SaveLog(LogTypeEnum.RouteInfo, $"{l_NotInFeed.Count} Shopify SKU(s) have no item in the inventory feed of [{l_CustomerId}] yet. They are stored on a later run once the ERP feed has them.", LogList(l_NotInFeed), userNo);

                // Only adds rows, so even a run whose paging stopped early is safe to save.
                if (l_Missing.Rows.Count > 0)
                {
                    l_CustomerProductCatalog.InsertMissingTargetCatalogPrices(l_CustomerId, l_Missing, ref l_Result);

                    string l_List = string.Join(Environment.NewLine, l_Result.AsEnumerable().Select(r => $"{r["Id"]},{r["CustomerID"]},{r["ItemId"]},{r["ProductID"]}"));

                    route.SaveLog(LogTypeEnum.RouteInfo, $"Inserted {l_Result.Rows.Count} item(s) into CustomerProductCatalogPrices for [{l_CustomerId}].", "Id,CustomerID,ItemId,ProductID" + Environment.NewLine + l_List, userNo);
                }

                if (!l_Completed)
                    route.SaveLog(LogTypeEnum.Error, $"Shopify paging did not finish for [{l_CustomerId}], so some missing items may not have been found. They are picked up on the next run.", string.Empty, userNo);

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_Feed.Dispose();
                l_Existing.Dispose();
                l_Missing.Dispose();
                l_Result.Dispose();
            }
        }

        // ItemId -> Shopify inventory item gid, 500 rows per UPDATE; returns the rows changed
        private static int UpdateProductIds(DBConnector p_Connection, string p_CustomerId, Dictionary<string, string> p_Ids)
        {
            int l_Updated = 0;
            DataTable l_Count = new DataTable();

            foreach (List<KeyValuePair<string, string>> l_Batch in p_Ids.Chunk(500).Select(c => c.ToList()))
            {
                string l_Values = string.Join(",", l_Batch.Select(p => $"('{p.Key.Replace("'", "''")}', '{p.Value.Replace("'", "''")}')"));

                l_Count.Clear();
                p_Connection.GetData($@"UPDATE P SET P.ProductID = V.ProductID
                                          FROM CustomerProductCatalogPrices P
                                          JOIN (VALUES {l_Values}) V (ItemId, ProductID) ON V.ItemId = P.ItemId
                                         WHERE P.CustomerID = '{p_CustomerId.Replace("'", "''")}' AND P.ProductID <> V.ProductID;
                                        SELECT @@ROWCOUNT AS Updated;", ref l_Count);

                if (l_Count.Rows.Count > 0)
                    l_Updated += Convert.ToInt32(l_Count.Rows[0]["Updated"]);
            }

            l_Count.Dispose();

            return l_Updated;
        }

        // A full store of 200,000 items would put megabytes into one RouteLog row; the first entries are enough to act on
        private static string LogList(IEnumerable<string> p_Lines)
        {
            List<string> l_Lines = p_Lines.ToList();
            string l_Text = string.Join(Environment.NewLine, l_Lines.Take(LogListMax));

            return l_Lines.Count > LogListMax ? l_Text + Environment.NewLine + $"... and {l_Lines.Count - LogListMax} more" : l_Text;
        }
    }
}
