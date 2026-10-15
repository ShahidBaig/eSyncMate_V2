using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
using static eSyncMate.DB.Declarations;
using static eSyncMate.Processor.Models.ProductCatalogModel;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Pulls every product of the seller from Target (products_catalog, after_id paging) and inserts the
    /// ItemID -> ProductID pairs that CustomerProductCatalogPrices does not have yet, through
    /// Sp_InsertMissingTargetCatalogPrices. Insert only: an existing row is never changed, a different
    /// ProductID on Target is only logged. The inserted Ids are logged so a run can be undone.
    /// </summary>
    public class TargetProductSyncRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_Existing = new DataTable();
            DataTable l_Missing = new DataTable();
            DataTable l_Result = new DataTable();

            try
            {
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                string l_CustomerID = l_SourceConnector.CustomerID;

                if (!ProductCatalog.IsTargetAuthReady(route, l_DestinationConnector, userNo))
                {
                    return;
                }

                // What we already have: ItemId -> ProductID
                CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
                l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);
                l_CustomerProductCatalog.GetCustomerProductCatalogPrices(l_CustomerID, ref l_Existing);

                Dictionary<string, string> l_ExistingItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in l_Existing.Rows)
                {
                    string l_ItemId = Convert.ToString(row["ItemId"])?.Trim() ?? string.Empty;

                    if (!string.IsNullOrEmpty(l_ItemId) && !l_ExistingItems.ContainsKey(l_ItemId))
                    {
                        l_ExistingItems.Add(l_ItemId, Convert.ToString(row["ProductID"]) ?? string.Empty);
                    }
                }

                // RouteInfo: SaveLog keeps only Error / Exception / RouteInfo, so Debug/Info/Warning would never reach RouteLog
                route.SaveLog(LogTypeEnum.RouteInfo, $"Items already in CustomerProductCatalogPrices for [{l_CustomerID}]: {l_ExistingItems.Count}.", string.Empty, userNo);

                // Same shape as TargetCatalogPriceType
                l_Missing.Columns.Add("CustomerID", typeof(string));
                l_Missing.Columns.Add("ItemId", typeof(string));
                l_Missing.Columns.Add("ProductID", typeof(string));

                HashSet<string> l_Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<string> l_Mismatches = new List<string>();
                int l_TargetCount = 0;
                int l_Pages = 0;
                bool l_Completed = true;
                string after_id = string.Empty;

                do
                {
                    string? json = GetItems(l_DestinationConnector, after_id, route, userNo);

                    if (json == null)
                    {
                        // one retry for the same page
                        json = GetItems(l_DestinationConnector, after_id, route, userNo);
                    }

                    if (json == null)
                    {
                        l_Completed = false;
                        break;
                    }

                    TargetItems[]? items = JsonConvert.DeserializeObject<TargetItems[]>(json);

                    if (items == null)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Items response could not be parsed for customer [{l_CustomerID}] on page {l_Pages + 1}. Stopping pagination.", json, userNo);
                        l_Completed = false;
                        break;
                    }

                    l_Pages++;

                    foreach (TargetItems item in items)
                    {
                        string l_ItemId = item.external_id?.Trim() ?? string.Empty;
                        string l_ProductID = item.id?.Trim() ?? string.Empty;

                        if (string.IsNullOrEmpty(l_ItemId) || string.IsNullOrEmpty(l_ProductID) || !l_Seen.Add(l_ItemId))
                        {
                            continue;
                        }

                        l_TargetCount++;

                        if (l_ExistingItems.TryGetValue(l_ItemId, out string? l_OurProductID))
                        {
                            if (!string.Equals(l_OurProductID, l_ProductID, StringComparison.OrdinalIgnoreCase))
                            {
                                l_Mismatches.Add($"{l_ItemId}: ours={l_OurProductID}, target={l_ProductID}");
                            }

                            continue;
                        }

                        l_Missing.Rows.Add(l_CustomerID, l_ItemId, l_ProductID);
                    }

                    string l_NextAfterId = items.Length > 0 ? items[items.Length - 1].id : string.Empty;

                    // Target handing back the same cursor would loop for ever
                    if (!string.IsNullOrEmpty(l_NextAfterId) && l_NextAfterId == after_id)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Target returned the same after_id [{after_id}] twice for [{l_CustomerID}]. Stopping pagination.", string.Empty, userNo);
                        l_Completed = false;
                        break;
                    }

                    after_id = l_NextAfterId;
                }
                while (!string.IsNullOrEmpty(after_id));

                route.SaveLog(LogTypeEnum.RouteInfo, $"Target items for [{l_CustomerID}]: {l_TargetCount} in {l_Pages} page(s){(l_Completed ? "" : " (paging stopped early)")}. Missing in CustomerProductCatalogPrices: {l_Missing.Rows.Count}. ProductID different on Target: {l_Mismatches.Count}.", string.Empty, userNo);

                if (l_Mismatches.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.RouteInfo, $"{l_Mismatches.Count} item(s) have a different ProductID on Target than in CustomerProductCatalogPrices. They are not changed.", string.Join(Environment.NewLine, l_Mismatches), userNo);
                }

                // Only adds rows, so even a run whose paging stopped early is safe to save.
                if (l_Missing.Rows.Count > 0)
                {
                    l_CustomerProductCatalog.InsertMissingTargetCatalogPrices(l_CustomerID, l_Missing, ref l_Result);

                    string l_List = string.Join(Environment.NewLine, l_Result.AsEnumerable().Select(r => $"{r["Id"]},{r["CustomerID"]},{r["ItemId"]},{r["ProductID"]}"));

                    route.SaveLog(LogTypeEnum.RouteInfo, $"Inserted {l_Result.Rows.Count} item(s) into CustomerProductCatalogPrices for [{l_CustomerID}].", "Id,CustomerID,ItemId,ProductID" + Environment.NewLine + l_List, userNo);
                }

                if (!l_Completed)
                {
                    route.SaveLog(LogTypeEnum.Error, $"Target paging did not finish for [{l_CustomerID}], so some missing items may not have been found. They are picked up on the next run.", string.Empty, userNo);
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_Existing.Dispose();
                l_Missing.Dispose();
                l_Result.Dispose();
            }
        }

        /// <summary>
        /// One page of products_catalog. Null when Target did not answer 200, so the caller can retry or stop.
        /// </summary>
        static string? GetItems(ConnectorDataModel connector, string after_id, Routes route, int userNo)
        {
            // Deep-clone the connector for per-page parameter changes.
            ConnectorDataModel l_Connector = JsonConvert.DeserializeObject<ConnectorDataModel>(JsonConvert.SerializeObject(connector))!;

            if (l_Connector.Parmeters == null)
                l_Connector.Parmeters = new List<eSyncMate.Processor.Models.Parameter>();

            // Only id + external_id are needed; expand=fields makes every page many times bigger.
            // per_page is fixed here so the page size does not depend on the connector setup.
            l_Connector.Parmeters.RemoveAll(p => string.Equals(p.Name, "expand", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(p.Name, "per_page", StringComparison.OrdinalIgnoreCase)
                                              || p.Name == "after_id");

            // The same two may also be written straight into the connector Url
            l_Connector.Url = RemoveQueryKeys(l_Connector.Url, "expand", "per_page", "after_id");

            l_Connector.Parmeters.Add(new eSyncMate.Processor.Models.Parameter { Name = "per_page", Value = "1000" });

            if (!string.IsNullOrEmpty(after_id))
                l_Connector.Parmeters.Add(new eSyncMate.Processor.Models.Parameter { Name = "after_id", Value = after_id });

            RestResponse response = RestConnector.Execute(l_Connector, "").GetAwaiter().GetResult();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.Content ?? string.Empty;
            }

            route.SaveLog(LogTypeEnum.Error, $"Target products_catalog failed (after_id [{after_id}]). HTTP {(int)response.StatusCode} {response.StatusCode}.", response.Content ?? response.ErrorMessage ?? string.Empty, userNo);

            return null;
        }

        static string RemoveQueryKeys(string p_Url, params string[] p_Keys)
        {
            if (string.IsNullOrEmpty(p_Url) || !p_Url.Contains('?'))
                return p_Url;

            int l_Index = p_Url.IndexOf('?');
            string l_Path = p_Url.Substring(0, l_Index);

            List<string> l_Kept = p_Url.Substring(l_Index + 1)
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !p_Keys.Any(key => part.Split('=')[0].Equals(key, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return l_Kept.Count > 0 ? l_Path + "?" + string.Join("&", l_Kept) : l_Path;
        }
    }
}
