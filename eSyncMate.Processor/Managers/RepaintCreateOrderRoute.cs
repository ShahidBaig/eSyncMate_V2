using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Intercom.Core;
using Intercom.Data;
using JUST;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators;
using System.Data;
using static eSyncMate.DB.Declarations;
using static eSyncMate.Processor.Models.SCSPlaceOrderResponseModel;
using Nancy;
using static eSyncMate.Processor.Models.MacysAsnRequestModel;
using EdiEngine.Runtime;
using EdiEngine;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using EdiEngine.Runtime;
using JUST;
using Namotion.Reflection;
using Nancy;
using Nancy.Responses;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators;
using System.Reflection.PortableExecutable;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;
using Maps_4010VICS = EdiEngine.Standards.X12_004010VICS.Maps;
using Maps_5010 = EdiEngine.Standards.X12_005010.Maps;
using EdiEngine.Common.Definitions;
using EdiEngine;
using SegmentDefinitions = EdiEngine.Standards.X12_005010.Segments;
using Nancy.Diagnostics;
using System.Linq;
using System.Data;
using eSyncMate.DB;
using System.Reflection;
using eSyncMate.Processor.Controllers;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.Xml;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using static eSyncMate.Processor.Models.RepaintTransformJsonModel;


namespace eSyncMate.Processor.Managers
{
    public class RepaintCreateOrderRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_dataTable = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            SCSPlaceOrderResponse l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();
            OrderObjectModel? l_Order = null;

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    logger.LogError("Source Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    logger.LogError("Destination Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                string l_TransformationMap = string.Empty;

                map.UseConnection(l_SourceConnector.ConnectionString);
                map.GetObject(route.MapId);

                l_TransformationMap = map.Map;

                if (string.IsNullOrEmpty(l_TransformationMap))
                {
                    route.SaveLog(LogTypeEnum.Error, $"Required map for 850 processing is missing.", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    if (l_SourceConnector.Parmeters != null)
                    {
                        foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                        {
                            l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                        }
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@DATATYPE@", "850-JSON");
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", "NEW");


                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_dataTable);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_dataTable.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start...", string.Empty, userNo);

                    foreach (DataRow item in l_dataTable.Rows)
                    {
                        var first = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(item["Data"]));

                        string jsonData;
                        if (first is string inner && (inner.TrimStart().StartsWith("{") || inner.TrimStart().StartsWith("[")))
                        {
                            jsonData = JToken.Parse(inner).ToString(Formatting.None);
                        }
                        else
                        {
                            jsonData = JToken.FromObject(first).ToString(Formatting.None);
                        }

                        string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, jsonData);

                        //RepaintTransformJsonModel l_RepaintTransformJsonModel = JsonConvert.DeserializeObject<RepaintTransformJsonModel>(jsonTransformation);

                        //static string Q(string? s) => JsonConvert.ToString(s ?? "");

                        //var lineItems = l_RepaintTransformJsonModel.data.line_items ?? new List<Line_Items>();
                        //var lineItemsGql = string.Join(",\n", lineItems.Select(x => $@"{{
                        //                  sku: {Q(x.sku)}
                        //                  partner_line_item_id: {Q(l_RepaintTransformJsonModel.data.order_number +"-"+x.partner_line_item_id)}
                        //                  quantity: {x.quantity}
                        //                  price: {Q(x.price.ToString())}   
                        //                  product_name: {Q(x.product_name)}
                        //                  fulfillment_status: {Q(x.fulfillment_status)}
                        //                  quantity_pending_fulfillment: {x.quantity}
                        //                }}"));

                        //var query = $@"
                        //                mutation {{
                        //                    order_create(
                        //                    data: {{
                        //                        order_number: {Q(l_RepaintTransformJsonModel.data.order_number + "_1")}
                        //                        shop_name: ""repaint-studios""
                        //                        fulfillment_status: ""pending""
                        //                        order_date: {Q(l_RepaintTransformJsonModel.data.order_date)}

                        //                        shipping_lines: {{
                        //                        title: {Q(l_RepaintTransformJsonModel.data.shipping_lines.title)}
                        //                        price: ""0""
                        //                        carrier: {Q(l_RepaintTransformJsonModel.data.shipping_lines.carrier)}
                        //                        method: """"
                        //                        }}

                        //                        shipping_address: {{
                        //                        first_name: {Q(l_RepaintTransformJsonModel.data.shipping_address.first_name)}
                        //                        last_name: """"
                        //                        company: {Q(l_RepaintTransformJsonModel.data.shipping_address.company)}
                        //                        address1: {Q(l_RepaintTransformJsonModel.data.shipping_address.address1)}
                        //                        address2: {Q(l_RepaintTransformJsonModel.data.shipping_address.address2)}
                        //                        city: {Q(l_RepaintTransformJsonModel.data.shipping_address.city)}
                        //                        state: {Q(l_RepaintTransformJsonModel.data.shipping_address.state)}
                        //                        state_code: {Q(l_RepaintTransformJsonModel.data.shipping_address.state_code)}
                        //                        zip: {Q(l_RepaintTransformJsonModel.data.shipping_address.zip)}
                        //                        country: {Q(l_RepaintTransformJsonModel.data.shipping_address.country)}
                        //                        country_code: {Q(l_RepaintTransformJsonModel.data.shipping_address.country_code)}
                        //                        email: {Q(l_RepaintTransformJsonModel.data.shipping_address.email)}
                        //                        phone: {Q(l_RepaintTransformJsonModel.data.shipping_address.phone)}
                        //                        }}

                        //                        billing_address: {{
                        //                        first_name: {Q(l_RepaintTransformJsonModel.data.billing_address.first_name)}
                        //                        last_name: {Q(l_RepaintTransformJsonModel.data.billing_address.last_name)}
                        //                        company: {Q(l_RepaintTransformJsonModel.data.billing_address.company)}
                        //                        address1: {Q(l_RepaintTransformJsonModel.data.billing_address.address1)}
                        //                        address2: {Q(l_RepaintTransformJsonModel.data.billing_address.address2)}
                        //                        city: {Q(l_RepaintTransformJsonModel.data.billing_address.city)}
                        //                        state: {Q(l_RepaintTransformJsonModel.data.billing_address.state)}
                        //                        state_code: {Q(l_RepaintTransformJsonModel.data.billing_address.state_code)}
                        //                        zip: {Q(l_RepaintTransformJsonModel.data.billing_address.zip)}
                        //                        country: {Q(l_RepaintTransformJsonModel.data.billing_address.country)}
                        //                        country_code: {Q(l_RepaintTransformJsonModel.data.billing_address.country_code)}
                        //                        email: {Q(l_RepaintTransformJsonModel.data.billing_address.email)}
                        //                        phone: {Q(l_RepaintTransformJsonModel.data.billing_address.phone)}
                        //                        }}

                        //                        line_items: [
                        //                                        {lineItemsGql}
                        //                                    ]

                        //                        required_ship_date: {Q(l_RepaintTransformJsonModel.data.required_ship_date)}
                        //                    }}
                        //                    ) {{
                        //                    request_id
                        //                    complexity
                        //                    order {{
                        //                        id
                        //                        order_number
                        //                        shop_name
                        //                        fulfillment_status
                        //                        order_date
                        //                        required_ship_date
                        //                        shipping_address {{ first_name address1 city state_code zip country_code phone }}
                        //                        line_items(first: 1) {{
                        //                        edges {{ node {{ id sku quantity product_name fulfillment_status quantity_pending_fulfillment }} }}
                        //                        }}
                        //                    }}
                        //                    }}
                        //                }}";
                        
                        route.SaveData("JSONCreateOrder-SNT", 0, jsonTransformation, userNo);

                        OrderData l_OrderData = new OrderData();

                        l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                        l_OrderData.DeleteWithType(Convert.ToInt32(item["Id"]), "JSON-SNT");

                        l_OrderData.Type = "JSON-SNT";
                        l_OrderData.Data = jsonTransformation;
                        l_OrderData.CreatedBy = userNo;
                        l_OrderData.CreatedDate = DateTime.Now;
                        l_OrderData.OrderId = Convert.ToInt32(item["Id"]);
                        l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(item["OrderNumber"], string.Empty);

                        l_OrderData.SaveNew();

                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "/orders/createorder";
                        //var gqlJson = JsonConvert.SerializeObject(new { query = query });

                        sourceResponse = RestConnector.Execute(l_DestinationConnector, jsonTransformation).GetAwaiter().GetResult();

                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                            string Command = string.Empty;


                            Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + l_SourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.RepaintCreateOrder + "', @p_OrderId = '" + Convert.ToInt32(item["Id"]) + "'";

                            connection.Execute(Command);


                            l_OrderData = new OrderData();

                            l_OrderData.UseConnection(l_SourceConnector.ConnectionString);
                            l_OrderData.DeleteWithType(Convert.ToInt32(item["Id"]), "ERP-SNT");

                            l_OrderData.Type = "ERP-SNT";
                            l_OrderData.Data = sourceResponse.Content;
                            l_OrderData.CreatedBy = userNo;
                            l_OrderData.CreatedDate = DateTime.Now;
                            l_OrderData.OrderId = Convert.ToInt32(item["Id"]);
                            l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(item["OrderNumber"], string.Empty);

                            l_OrderData.SaveNew();

                        }
                        else
                        {
                            DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                            string Command = string.Empty;

                            Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + l_SourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.RepaintCreateOrder + "Error', @p_OrderId = '" + Convert.ToInt32(item["Id"]) + "'";

                            connection.Execute(Command);

                            l_OrderData = new OrderData();

                            l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                            l_OrderData.Type = "ERP-ERROR";
                            l_OrderData.Data = sourceResponse.Content;
                            l_OrderData.CreatedBy = userNo;
                            l_OrderData.CreatedDate = DateTime.Now;
                            l_OrderData.OrderId = Convert.ToInt32(item["Id"]);
                            l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(item["OrderNumber"], string.Empty);

                            l_OrderData.SaveNew();
                        }
                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
                    }

                    route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_dataTable.Dispose();
            }
        }

    }
}
