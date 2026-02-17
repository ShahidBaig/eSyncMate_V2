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

namespace eSyncMate.Processor.Managers
{
    public class GetOrdersRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
                string destinationData = string.Empty;
                string sourceData = string.Empty;
                string transformedData = string.Empty;
                Customers l_Customer = new Customers();

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

                l_Customer.UseConnection(l_DestinationConnector.ConnectionString);
                l_Customer.GetObject("ERPCustomerID", l_DestinationConnector.CustomerID);

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                {
                    RestResponse sourceResponse = RestConnector.Execute(l_SourceConnector, "").GetAwaiter().GetResult();

                    if (sourceResponse.Content == null)
                        sourceResponse.Content = "";

                    sourceData = sourceResponse.Content;

                    route.SaveLog(LogTypeEnum.Debug, $"Orders data received!", string.Empty, userNo);
                }

                dynamic data = JObject.Parse(sourceData);

                route.SaveData("SRC", 0, sourceData, userNo);

                foreach (dynamic order in data[l_SourceConnector.JsonDataCollectionName])
                {
                    route.SaveLog(LogTypeEnum.Info, $"Processing Order [{order.CustomerPO}]", string.Empty, userNo);

                    string jsonString = JsonConvert.SerializeObject(order);
                    string dbFields = new JsonTransformer().Transform(route.MapObject.Map, jsonString);

                    Orders? l_Order = JsonConvert.DeserializeObject<Orders>(dbFields);
                    
                    if (l_Order == null)
                    {
                        route.SaveLog(LogTypeEnum.Info, $"Error Processing Order [{order.CustomerPO}], it cannot be deserialized", "", userNo);
                    }
                    else
                    {
                        l_Order.Status = "NEW";
                        l_Order.CustomerId = l_Customer.Id;
                        l_Order.CreatedBy = userNo;
                        l_Order.CreatedDate = DateTime.Now;

                        l_Order.UseConnection(l_DestinationConnector.ConnectionString);
                        if (l_Order.SaveNew().IsSuccess)
                        {
                            OrderData l_Data = new OrderData();

                            l_Data.UseConnection(string.Empty, l_Order.Connection);
                            l_Data.DeleteWithType(l_Order.Id, "SRC");

                            l_Data.Type = "SRC";
                            l_Data.Data = jsonString;
                            l_Data.CreatedBy = userNo;
                            l_Data.CreatedDate = DateTime.Now;
                            l_Data.OrderId = l_Order.Id;
                            l_Data.OrderNumber = l_Order.OrderNumber;

                            if (l_Data.SaveNew().IsSuccess)
                            {
                                route.SaveData("DST", l_Order.Id, dbFields, userNo);
                            }
                        }

                        route.SaveLog(LogTypeEnum.Info, $"Processed Order [{order.CustomerPO}]", string.Empty, userNo);
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Info, $"Error executing the route [{route.Id}]", ex.Message, userNo);
            }
        }
    }
}
