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


namespace eSyncMate.Processor.Managers
{
    public class Download856FromShipStationRoute
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

                //eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                //string l_TransformationMap = string.Empty;

                //map.UseConnection(l_SourceConnector.ConnectionString);
                //map.GetObject(route.MapId);

                //l_TransformationMap = map.Map;

                //if (string.IsNullOrEmpty(l_TransformationMap))
                //{
                //    route.SaveLog(LogTypeEnum.Error, $"Required map for 855 processing is missing.", string.Empty, userNo);
                //    return;
                //}

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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", "ACKNOWLEDGED");

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
                        //var first = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(item["Data"]));
                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + $"/fulfillments?orderNumber={item["OrderNumber"]}";

                        OrderData l_OrderData = new OrderData();

                        l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                        l_OrderData.DeleteWithType(Convert.ToInt32(item["Id"]), "856-JSON-SNT");

                        l_OrderData.Type = "856-JSON-SNT";
                        l_OrderData.Data = l_DestinationConnector.Url;
                        l_OrderData.CreatedBy = userNo;
                        l_OrderData.CreatedDate = DateTime.Now;
                        l_OrderData.OrderId = Convert.ToInt32(item["Id"]);
                        l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(item["OrderNumber"], string.Empty);

                        l_OrderData.SaveNew();

                        
                        sourceResponse = RestConnector.Execute(l_DestinationConnector,"").GetAwaiter().GetResult();


                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            var model = JsonConvert.DeserializeObject<ShipStation856ResponseModel>(
                                                   JToken.Parse(sourceResponse.Content).Type == JTokenType.String
                                                   ? JValue.Parse(sourceResponse.Content).ToString() // unwrap inner
                                                   : sourceResponse.Content);


                            CustomerShipStationResponseModel l_CustomerShipStationResponseModel = new CustomerShipStationResponseModel();
                            Fullfilment856Packages l_Fullfilment856Packages = new Fullfilment856Packages();


                            l_CustomerShipStationResponseModel.fulfillmentId = model.fulfillments[0].fulfillmentId;
                            l_CustomerShipStationResponseModel.orderId = model.fulfillments[0].orderId;
                            l_CustomerShipStationResponseModel.orderNumber = model.fulfillments[0].orderNumber;
                            l_CustomerShipStationResponseModel.userId = model.fulfillments[0].userId;
                            l_CustomerShipStationResponseModel.customerEmail = model.fulfillments[0].customerEmail;
                            l_CustomerShipStationResponseModel.trackingNumber = model.fulfillments[0].trackingNumber;
                            l_CustomerShipStationResponseModel.createDate = model.fulfillments[0].createDate;
                            l_CustomerShipStationResponseModel.shipDate = model.fulfillments[0].shipDate;
                            l_CustomerShipStationResponseModel.voidDate = model.fulfillments[0].voidDate;
                            l_CustomerShipStationResponseModel.deliveryDate = model.fulfillments[0].deliveryDate;
                            l_CustomerShipStationResponseModel.carrierCode = model.fulfillments[0].carrierCode;
                            l_CustomerShipStationResponseModel.sellerFillProviderId = model.fulfillments[0].sellerFillProviderId;
                            l_CustomerShipStationResponseModel.sellerFillProviderName = model.fulfillments[0].sellerFillProviderName;
                            l_CustomerShipStationResponseModel.fulfillmentProviderCode = model.fulfillments[0].fulfillmentProviderCode;
                            l_CustomerShipStationResponseModel.fulfillmentServiceCode = model.fulfillments[0].fulfillmentServiceCode;
                            l_CustomerShipStationResponseModel.fulfillmentFee = model.fulfillments[0].fulfillmentFee;
                            l_CustomerShipStationResponseModel.voidRequested = model.fulfillments[0].voidRequested;
                            l_CustomerShipStationResponseModel.voided = model.fulfillments[0].voided;
                            l_CustomerShipStationResponseModel.marketplaceNotified = model.fulfillments[0].marketplaceNotified;
                            l_CustomerShipStationResponseModel.notifyErrorMessage = model.fulfillments[0].notifyErrorMessage;
                            l_CustomerShipStationResponseModel.shipTo.name = model.fulfillments[0].shipTo.name;
                            l_CustomerShipStationResponseModel.shipTo.company = model.fulfillments[0].shipTo.company;
                            l_CustomerShipStationResponseModel.shipTo.street1 = model.fulfillments[0].shipTo.street1;
                            l_CustomerShipStationResponseModel.shipTo.street2 = model.fulfillments[0].shipTo.street2;
                            l_CustomerShipStationResponseModel.shipTo.street3 = model.fulfillments[0].shipTo.street3;
                            l_CustomerShipStationResponseModel.shipTo.city = model.fulfillments[0].shipTo.city;
                            l_CustomerShipStationResponseModel.shipTo.state = model.fulfillments[0].shipTo.state;
                            l_CustomerShipStationResponseModel.shipTo.postalCode = model.fulfillments[0].shipTo.postalCode;
                            l_CustomerShipStationResponseModel.shipTo.country = model.fulfillments[0].shipTo.country;
                            l_CustomerShipStationResponseModel.shipTo.phone = model.fulfillments[0].shipTo.phone;
                            l_CustomerShipStationResponseModel.shipTo.residential = model.fulfillments[0].shipTo.residential;
                            l_CustomerShipStationResponseModel.shipTo.addressVerified = model.fulfillments[0].shipTo.addressVerified;
                            l_Fullfilment856Packages.trackingNumber = model.fulfillments[0].trackingNumber;

                            var json = item["Data"]?.ToString();
                            var order = JsonConvert.DeserializeObject<JObject>(json);

                            var items = order["items"] as JArray;

                            if (items != null)
                            {
                                foreach (var i in items)
                                {

                                    Fullfilment856Items l_Fullfilment856Items = new Fullfilment856Items();

                                    l_Fullfilment856Items.sku = i["sku"]?.ToString();
                                    l_Fullfilment856Items.quantity = i["quantity"]?.ToObject<long?>() ?? 0;
                                    l_Fullfilment856Items.unitPrice = i["price"]?.ToObject<string?>() ?? "0";
                                    l_Fullfilment856Items.upc = i["upc"]?.ToString();
                                    l_Fullfilment856Items.lineNo = i["ediLineId"]?.ToObject<string?>() ?? "0";

                                    l_Fullfilment856Packages.items.Add(l_Fullfilment856Items);

                                }
                                l_CustomerShipStationResponseModel.packages.Add(l_Fullfilment856Packages);
                            }

                            l_OrderData = new OrderData();

                            l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                            l_OrderData.DeleteWithType(Convert.ToInt32(item["Id"]), "ASN-RES");

                            l_OrderData.Type = "ASN-RES";
                            l_OrderData.Data = JsonConvert.SerializeObject(l_CustomerShipStationResponseModel); 
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
