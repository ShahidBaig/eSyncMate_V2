//using eSyncMate.DB;
//using eSyncMate.DB.Entities;
//using eSyncMate.Processor.Connections;
//using eSyncMate.Processor.Models;
//using Intercom.Core;
//using JUST;
//using Newtonsoft.Json;
//using RestSharp;
//using System;
//using System.Data;
//using static eSyncMate.DB.Declarations;
//using static Hangfire.Storage.JobStorageFeatures;

//namespace eSyncMate.Processor.Managers
//{
//    public class GetSCSOrderStatusRoute
//    {
//        public static int OrderId { get; private set; }

//        public static void Execute(IConfiguration config, ILogger logger, Routes route)
//        {
//            int userNo = 1;

//            try
//            {
//                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
//                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
//                string destinationData = string.Empty;
//                string sourceData = string.Empty;
//                string transformedData = string.Empty;

//                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

//                if (l_SourceConnector == null)
//                {
//                    logger.LogError("Source Connector is not setup properly");
//                    route.SaveLog(LogTypeEnum.Info, "Source Connector is not setup properly", string.Empty, userNo);
//                    return;
//                }

//                if (l_DestinationConnector == null)
//                {
//                    logger.LogError("Destination Connector is not setup properly");
//                    route.SaveLog(LogTypeEnum.Info, "Destination Connector is not setup properly", string.Empty, userNo);
//                    return;
//                }

//                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
//                {
//                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
//                    DataTable l_Data = new DataTable();

//                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);

//                    if (l_SourceConnector.CommandType == "SP")
//                        connection.GetDataSP(l_SourceConnector.Command, ref l_Data);
//                    else
//                        connection.GetData(l_SourceConnector.Command, ref l_Data);

//                    route.SaveLog(LogTypeEnum.Info, $"Source connector executed with [{l_Data.Rows.Count}] orders", string.Empty, userNo);

//                    foreach (DataRow l_Row in l_Data.Rows)
//                    {
//                        string orderId = PublicFunctions.ConvertNullAsString(l_Row[l_SourceConnector.KeyFieldName], string.Empty);

//                        try
//                        {
//                            // Fetch data from the API
//                            string apiEndpoint = "https://api.safavieh.com/SPARS.API_UAT/api/service/Get_Orders";
//                            var restClient = new RestClient(apiEndpoint);
//                            var request = new RestRequest(Method.Post.ToString());

//                            request.AddHeader("Content-Type", "application/json");
//                            request.AddParameter("application/json", JsonConvert.SerializeObject(new { CustomerID = "TAR6266P", ExternalID = "0123456789" }), ParameterType.RequestBody);
//                            var response = restClient.Execute(request);

//                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
//                            {
//                                // Save the raw JSON data into your database
//                                string jsonData = response.Content;
//                                SaveRawJsonData(orderId, jsonData, route);

//                                route.SaveLog(LogTypeEnum.Info, $"Processed Order [{orderId}]", string.Empty, userNo);
//                            }
//                            else
//                            {
//                                // Handle the case when API call is unsuccessful
//                                route.SaveLog(LogTypeEnum.Info, $"Error fetching orders from API for Order [{orderId}]", response.ErrorMessage, userNo);
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            route.SaveLog(LogTypeEnum.Info, $"Error processing Order [{orderId}]", ex.Message, userNo);
//                        }
//                    }

//                    l_Data.Dispose();
//                    route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
//                }
//            }
//            catch (Exception ex)
//            {
//                route.SaveLog(LogTypeEnum.Info, $"Error executing the route [{route.Id}]", ex.Message, userNo);
//            }
//        }

//        private static void SaveRawJsonData(string orderId, string jsonData, Routes route)
//        {
//            // Save the raw JSON data into your "ordersdata" table
//            // Replace "YourDatabaseContext" with your actual database context
//            DB.Entities.SCSInventoryFeed l_SCSInventoryFeed = new DB.Entities.SCSInventoryFeed();


//            ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
//            ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
//            l_SCSInventoryFeed.UseConnection(l_DestinationConnector.ConnectionString);

//            DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
//            using (var db = new connection)

//            {
//                var orderData = new OrderData
//                {
//                    OrderId = OrderId,
//                    JsonData = jsonData
//                };
//                db.OrdersData.Add(orderData);
//                db.SaveChanges();
//            }
//        }
//    }
//}


using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Intercom.Core;
using Intercom.Data;
using JUST;
using Nancy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Data;
using static eSyncMate.DB.Declarations;
using static eSyncMate.Processor.Models.SCSInventoryFeedModel;
using static eSyncMate.Processor.Models.SCSPlaceOrderResponseModel;

namespace eSyncMate.Processor.Managers
{
    public class SCSOrderStatusRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
                string transformedData = string.Empty;
                Customers l_Customer = new Customers();
                DataTable l_dataTable = new DataTable();

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    logger.LogError("Source Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Info, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    logger.LogError("Destination Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Info, "Destination Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                //l_Customer.UseConnection(l_DestinationConnector.ConnectionString);
                //l_Customer.GetObject("ERPCustomerID", l_DestinationConnector.CustomerID);
                if (l_SourceConnector.Parmeters != null)
                {
                    foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                    {
                        l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                    }
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                    DataTable l_Data = new DataTable();

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_dataTable);
                    }
                }


                if (l_dataTable.Rows.Count > 0)
                {
                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                    {
                        foreach (DataRow l_Row in l_dataTable.Rows)
                        {
                            string Body = string.Empty;
                            string l_ExternalID = string.Empty;
                            int Cust_Id = 0;
                            int Order_Id = 0;
                            string destinationData = string.Empty;
                            string sourceData = string.Empty;

                            if (l_SourceConnector != null && l_Row["ExternalID"] != DBNull.Value)
                            {
                                Cust_Id = Convert.ToInt32(PublicFunctions.ConvertNull(l_Row["Cust_Id"], 0));
                                Order_Id = Convert.ToInt32(PublicFunctions.ConvertNull(l_Row["Order_Id"], 0));
                                l_ExternalID = Convert.ToString(PublicFunctions.ConvertNull(l_Row["ExternalID"], 0));

                                var data = new
                                {
                                    CustomerID = l_SourceConnector.CustomerID,
                                    ExternalID = l_ExternalID
                                };

                                Body = JsonConvert.SerializeObject(new { Input = data });

                                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                                {
                                    RestResponse sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                    if (sourceResponse.Content == null)
                                        sourceResponse.Content = "";

                                    sourceData = sourceResponse.Content;

                                    route.SaveLog(LogTypeEnum.Info, $"Orders data received!", string.Empty, userNo);


                                    route.SaveData("JSON", Order_Id, sourceData, userNo);

                                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                                    DataTable l_Data = new DataTable();
                                    string Command = string.Empty;

                                    Command = "EXEC SP_UpdateOrderStatus @p_CustomerID =" + l_SourceConnector.CustomerID + ", @p_RouteType = " + RouteTypesEnum.SCSOrderStatus + ", @p_Data = " + sourceData + ", @p_OrderId = " + Order_Id;

                                    connection.Execute(Command);

                                }
                            }

                            route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
                        }
                    }
                    else
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Error: l_SourceConnector or l_ExternalID is null.", string.Empty, userNo);
                    }
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Info, $"Error executing the route [{route.Id}]", ex.Message, userNo);
            }
        }
    }
}
