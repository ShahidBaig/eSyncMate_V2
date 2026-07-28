using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Auto-retries error orders that failed to place in SPARS due to Period / Timeout /
    /// "No Response from SPARS". The source SP returns eligible orders (Status = ERROR, no
    /// ExternalId, RetryCount &lt; 3, error text contains one of those terms). Each order is
    /// counted (RetryCount++), flipped to InProgress and handed to the SAME single-order
    /// reprocess as the manual button (SCSPlaceOrderRoute.ExecuteSingle) — which itself checks
    /// SPARS first (Get_OrderInfo) and skips / re-places accordingly. One route covers every
    /// customer; the per-order CustomerName comes from the source SP.
    /// </summary>
    public class ErrorOrderRetryRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new();

            try
            {
                route.SaveLog(LogTypeEnum.RouteInfo, "[ErrorOrderRetry] Started", string.Empty, userNo);

                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);

                if (l_SourceConnector == null)
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    DBConnector connection = new(l_SourceConnector.ConnectionString);

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }
                }

                route.SaveLog(LogTypeEnum.RouteInfo, $"[ErrorOrderRetry] {l_data.Rows.Count} error order(s) to retry", string.Empty, userNo);

                DBConnector updateConn = new(CommonUtils.ConnectionString);

                foreach (DataRow l_Row in l_data.Rows)
                {
                    int l_OrderId = PublicFunctions.ConvertNullAsInteger(l_Row["OrderId"], 0);
                    string l_CustomerName = PublicFunctions.ConvertNullAsString(l_Row["ERPCustomerID"], string.Empty);

                    if (l_OrderId == 0 || string.IsNullOrEmpty(l_CustomerName))
                    {
                        continue;
                    }

                    try
                    {
                        // Count this attempt and flip ERROR -> InProgress, exactly as the manual reprocess does.
                        updateConn.Execute($"UPDATE Orders SET RetryCount = ISNULL(RetryCount, 0) + 1, Status = 'InProgress' WHERE Id = {l_OrderId}");

                        string l_Result = SCSPlaceOrderRoute.ExecuteSingle(config, l_OrderId, l_CustomerName);

                        if (string.IsNullOrEmpty(l_Result))
                        {
                            route.SaveLog(LogTypeEnum.RouteInfo, $"[ErrorOrderRetry] Order {l_OrderId} ({l_CustomerName}) reprocessed successfully", string.Empty, userNo);
                        }
                        else
                        {
                            route.SaveLog(LogTypeEnum.Error, $"[ErrorOrderRetry] Order {l_OrderId} ({l_CustomerName}) still failing: {l_Result}", string.Empty, userNo);
                        }
                    }
                    catch (Exception exOrder)
                    {
                        route.SaveLog(LogTypeEnum.Exception, $"[ErrorOrderRetry] Order {l_OrderId} error", exOrder.ToString(), userNo);
                    }
                }

                route.SaveLog(LogTypeEnum.RouteInfo, "[ErrorOrderRetry] Completed", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, "[ErrorOrderRetry] Error", ex.ToString(), userNo);
            }
        }
    }
}
