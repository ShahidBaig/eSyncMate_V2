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

namespace eSyncMate.Processor.Managers
{
    public class OrderManager
    {
        public static OrderTransformationResponseModel ParseOrder(Customers p_Customer, EdiTrans p_OrderTrans, string p_TransformationMap, string p_DBFieldsMap)
        {
            OrderTransformationResponseModel l_Response = new OrderTransformationResponseModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                string jsonData = JsonConvert.SerializeObject(p_OrderTrans);
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);
                string jsonDBFields = new JsonTransformer().Transform(p_DBFieldsMap, jsonData);

                jsonTransformation = jsonTransformation.Replace("@@CUSTOMER@@", p_Customer.ERPCustomerID);
                jsonTransformation = jsonTransformation.Replace("@@MARKETPLACE@@", p_Customer.Marketplace);

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.JSON = jsonTransformation;
                l_Response.DBFields = jsonDBFields;
            }
            catch (Exception)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
            }

            return l_Response;
        }

        public static OrderSaveResponseModel SaveOrder(InboundEDI p_EDI, Customers p_Customer, OrderTransformationResponseModel p_OrderData)
        {
            OrderSaveResponseModel l_Response = new OrderSaveResponseModel();

            try
            {
                Orders? l_Order = JsonConvert.DeserializeObject<Orders>(p_OrderData.DBFields);

                l_Response.Code = (int)ResponseCodes.Error;

                if (l_Order == null)
                {
                    return l_Response;
                }

                l_Order.Status = "NEW";
                l_Order.CustomerId = p_Customer.Id;
                l_Order.InboundEDIId = p_EDI.Id;
                l_Order.CreatedBy = p_OrderData.SystemUser;
                l_Order.CreatedDate = DateTime.Now;

                l_Order.UseConnection(Models.CommonUtils.ConnectionString);
                if (l_Order.SaveNew().IsSuccess)
                {

                    foreach (OrderDetail Detaildata in l_Order.Details)
                    {
                        OrderDetail l_OrderDetail = new OrderDetail();

                        l_OrderDetail.UseConnection(string.Empty, l_Order.Connection);

                        for (int i = 1; i <= Convert.ToInt32(Detaildata.LineQty); i++)
                        {
                            l_OrderDetail.OrderId = l_Order.Id;
                            l_OrderDetail.LineNo = Convert.ToInt32(Detaildata.LineNo);
                            l_OrderDetail.LineQty = 1;
                            l_OrderDetail.ItemID = Detaildata.ItemID;
                            l_OrderDetail.UnitPrice = Convert.ToDecimal(Detaildata.UnitPrice);
                            l_OrderDetail.Status = "NEW";
                            l_OrderDetail.order_line_id = Detaildata.order_line_id;
                            l_OrderDetail.CreatedBy = 1;
                            l_OrderDetail.CreatedDate = DateTime.Now;

                            l_OrderDetail.SaveNew();
                        }
                    }

                    OrderData l_Data = new OrderData();

                    l_Data.UseConnection(string.Empty, l_Order.Connection);

                    l_Data.DeleteWithType(l_Order.Id, "850-eSyncMate");

                    l_Data.Type = "850-eSyncMate";
                    l_Data.Data = p_OrderData.EDI;
                    l_Data.CreatedBy = p_OrderData.SystemUser;
                    l_Data.CreatedDate = DateTime.Now;
                    l_Data.OrderId = l_Order.Id;
                    l_Data.OrderNumber = l_Order.OrderNumber;

                    if (l_Data.SaveNew().IsSuccess)
                    {
                        l_Data.DeleteWithType(l_Order.Id, "850-JSON");

                        l_Data.Type = "850-JSON";
                        l_Data.Data = p_OrderData.JSON;
                        l_Data.CreatedBy = p_OrderData.SystemUser;
                        l_Data.CreatedDate = DateTime.Now;
                        l_Data.OrderId = l_Order.Id;
                        l_Data.OrderNumber = l_Order.OrderNumber;

                        if (l_Data.SaveNew().IsSuccess)
                        {
                            l_Data.DeleteWithType(l_Order.Id, "850-Fields");

                            l_Data.Type = "850-Fields";
                            l_Data.Data = p_OrderData.DBFields;
                            l_Data.CreatedBy = p_OrderData.SystemUser;
                            l_Data.CreatedDate = DateTime.Now;
                            l_Data.OrderId = l_Order.Id;
                            l_Data.OrderNumber = l_Order.OrderNumber;

                            if (l_Data.SaveNew().IsSuccess)
                            {
                                l_Response.OrderId = l_Order.Id;
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = "Order processed successfully!";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static async Task<OrderSyncResponseModel> SyncOrder(Customers p_Customer, int p_OrdreId)
        {
            OrderSyncResponseModel l_Response = new OrderSyncResponseModel();
            ConnectorDataModel? l_Data = null;
            Orders l_Order = new Orders();

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                var l_Connector = p_Customer.Connectors.Where(c => c.ConnectorName == "Create Order" && c.ConnectorType == "Orders" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                l_Order.UseConnection(Models.CommonUtils.ConnectionString);
                if (!l_Order.GetObject(p_OrdreId).IsSuccess)
                {
                    return l_Response;
                }

                var l_OrderData = l_Order.Files.Where(f => f.Type == "850-JSON");

                if (l_OrderData == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                request.AddHeader("Content-Length", l_OrderData.FirstOrDefault<OrderData>()?.Data.Length.ToString());

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                request.AddStringBody(l_OrderData.FirstOrDefault<OrderData>()?.Data, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                RestResponse response = await client.ExecuteAsync(request);

                if (response.Content == null)
                    response.Content = "";

                NetSuiteOrderResponseModel l_ResponseData = null;

                try
                {
                    l_ResponseData = JsonConvert.DeserializeObject<NetSuiteOrderResponseModel>(response.Content);
                }
                catch (Exception)
                {
                    l_ResponseData = new NetSuiteOrderResponseModel();
                }

                if (l_ResponseData.status == "OK")
                {
                    if (l_Order.IsStoreOrder)
                    {
                        OrderStores l_OStore = new OrderStores();

                        l_OStore.UseConnection("", l_Order.Connection);
                        l_OStore.DeleteWithOrderId(l_Order.Id);

                        foreach (NetSuiteOrderData l_OData in l_ResponseData.orders)
                        {
                            OrderStores l_Store = new OrderStores();

                            l_Store.UseConnection("", l_Order.Connection);

                            l_Store.OrderId = l_Order.Id;
                            l_Store.CustomerId = p_Customer.Id;
                            l_Store.CustomerPO = l_OData.orderNumber;
                            l_Store.Status = "NEW";
                            l_Store.Data = JsonConvert.SerializeObject(l_OData);
                            l_Store.CreatedBy = l_Order.CreatedBy;
                            l_Store.CreatedDate = DateTime.Now;

                            l_Store.SaveNew();
                        }

                        l_Order.Status = "SPLITED";
                        if (l_Order.Modify().IsSuccess)
                        {
                            OrderData l_OData = new OrderData();

                            l_OData.UseConnection(string.Empty, l_Order.Connection);

                            l_OData.DeleteWithType(l_Order.Id, "850-RESPONSE");

                            l_OData.Type = "850-RESPONSE";
                            l_OData.Data = JsonConvert.SerializeObject(l_ResponseData.orders);
                            l_OData.CreatedBy = l_Order.CreatedBy;
                            l_OData.CreatedDate = DateTime.Now;
                            l_OData.OrderId = l_Order.Id;
                            l_OData.OrderNumber = l_Order.OrderNumber;

                            if (l_OData.SaveNew().IsSuccess)
                            {
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = "Store Order splited successfully";
                            }
                        }
                    }
                    else
                    {
                        l_Order.Status = "SYNCED";
                        if (l_Order.Modify().IsSuccess)
                        {
                            OrderData l_OData = new OrderData();

                            l_OData.UseConnection(string.Empty, l_Order.Connection);

                            l_OData.DeleteWithType(l_Order.Id, "850-RESPONSE");

                            l_OData.Type = "850-RESPONSE";
                            l_OData.Data = JsonConvert.SerializeObject(l_ResponseData.data);
                            l_OData.CreatedBy = l_Order.CreatedBy;
                            l_OData.CreatedDate = DateTime.Now;
                            l_OData.OrderId = l_Order.Id;
                            l_OData.OrderNumber = l_Order.OrderNumber;

                            if (l_OData.SaveNew().IsSuccess)
                            {
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = "Order synced successfully";
                            }
                        }
                    }
                }
                else
                {
                    l_Order.Status = "SYNCERROR";
                    if (l_Order.Modify().IsSuccess)
                    {
                        OrderData l_OData = new OrderData();

                        l_OData.UseConnection(string.Empty, l_Order.Connection);

                        l_OData.DeleteWithType(l_Order.Id, "850-RESPONSE");

                        l_OData.Type = "850-RESPONSE";
                        l_OData.Data = response.Content;
                        l_OData.CreatedBy = l_Order.CreatedBy;
                        l_OData.CreatedDate = DateTime.Now;
                        l_OData.OrderId = l_Order.Id;
                        l_OData.OrderNumber = l_Order.OrderNumber;

                        if (l_OData.SaveNew().IsSuccess)
                        {
                            l_Response.Code = (int)ResponseCodes.Success;
                            l_Response.Message = l_ResponseData.message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static async Task<OrderStoreSyncResponseModel> SyncOrderStore(int p_OrdreStoreId)
        {
            OrderStoreSyncResponseModel l_Response = new OrderStoreSyncResponseModel();
            ConnectorDataModel? l_Data = null;
            OrderStores l_OrderStore = new OrderStores();
            Customers l_Customer = new Customers();

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_OrderStore.UseConnection(Models.CommonUtils.ConnectionString);
                if (!l_OrderStore.GetObject(p_OrdreStoreId).IsSuccess)
                {
                    return l_Response;
                }

                l_Customer.UseConnection("", l_OrderStore.Connection);
                l_Customer.GetObject(l_OrderStore.CustomerId);

                var l_Connector = l_Customer.Connectors.Where(c => c.ConnectorName == "Create Order" && c.ConnectorType == "Orders" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url + "?script=1168&deploy=1", Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                request.AddStringBody(l_OrderStore.Data, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                RestResponse response = await client.ExecuteAsync(request);

                NetSuiteOrderResponseModel l_ResponseData = JsonConvert.DeserializeObject<NetSuiteOrderResponseModel>(response.Content);

                if (l_ResponseData.status == "OK")
                {
                    l_OrderStore.Status = "SYNCED";
                    l_OrderStore.Response = JsonConvert.SerializeObject(l_ResponseData.data);

                    if (l_OrderStore.Modify().IsSuccess)
                    {
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = "Order Store synced successfully";
                    }
                }
                else
                {
                    l_OrderStore.Status = "SYNCERROR";
                    l_OrderStore.Response = response.Content;

                    if (l_OrderStore.Modify().IsSuccess)
                    {
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = l_ResponseData.message;
                    }
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static async Task<OrderSyncResponseModel> SyncOrder(int p_OrdreId)
        {
            OrderSyncResponseModel l_Response = new OrderSyncResponseModel();
            ConnectorDataModel? l_Data = null;
            Orders l_Order = new Orders();
            Customers l_Customer = new Customers();

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order.UseConnection(Models.CommonUtils.ConnectionString);
                if (!l_Order.GetObject(p_OrdreId).IsSuccess)
                {
                    return l_Response;
                }

                l_Customer.UseConnection("", l_Order.Connection);
                l_Customer.GetObject(l_Order.CustomerId);

                var l_Connector = l_Customer.Connectors.Where(c => c.ConnectorName == "Create Order" && c.ConnectorType == "Orders" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                var l_OrderData = l_Order.Files.Where(f => f.Type == "850-JSON");

                if (l_OrderData == null)
                {
                    return l_Response;
                }

                request.AddStringBody(l_OrderData.FirstOrDefault<OrderData>().Data, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                RestResponse response = await client.ExecuteAsync(request);

                NetSuiteOrderResponseModel l_ResponseData = JsonConvert.DeserializeObject<NetSuiteOrderResponseModel>(response.Content);

                if (l_ResponseData.status == "OK")
                {
                    if (l_Order.IsStoreOrder)
                    {
                        OrderStores l_OStore = new OrderStores();

                        l_OStore.UseConnection("", l_Order.Connection);
                        l_OStore.DeleteWithOrderId(l_Order.Id);

                        foreach (NetSuiteOrderData l_OData in l_ResponseData.orders)
                        {
                            OrderStores l_Store = new OrderStores();

                            l_Store.UseConnection("", l_Order.Connection);

                            l_Store.OrderId = l_Order.Id;
                            l_Store.CustomerId = l_Order.CustomerId;
                            l_Store.CustomerPO = l_OData.orderNumber;
                            l_Store.Status = "NEW";
                            l_Store.Data = JsonConvert.SerializeObject(l_OData);
                            l_Store.CreatedBy = l_Order.CreatedBy;
                            l_Store.CreatedDate = DateTime.Now;

                            l_Store.SaveNew();
                        }

                        l_Order.Status = "SPLITED";
                        if (l_Order.Modify().IsSuccess)
                        {
                            OrderData l_OData = new OrderData();

                            l_OData.UseConnection(string.Empty, l_Order.Connection);

                            l_OData.DeleteWithType(l_Order.Id, "850-RESPONSE");

                            l_OData.Type = "850-RESPONSE";
                            l_OData.Data = JsonConvert.SerializeObject(l_ResponseData.orders);
                            l_OData.CreatedBy = l_Order.CreatedBy;
                            l_OData.CreatedDate = DateTime.Now;
                            l_OData.OrderId = l_Order.Id;
                            l_OData.OrderNumber = l_Order.OrderNumber;

                            if (l_OData.SaveNew().IsSuccess)
                            {
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = "Store Order splited successfully";
                            }
                        }
                    }
                    else
                    {
                        l_Order.Status = "SYNCED";
                        if (l_Order.Modify().IsSuccess)
                        {
                            OrderData l_OData = new OrderData();

                            l_OData.UseConnection(string.Empty, l_Order.Connection);

                            l_OData.DeleteWithType(l_Order.Id, "850-RESPONSE");

                            l_OData.Type = "850-RESPONSE";
                            l_OData.Data = response.Content;
                            l_OData.CreatedBy = l_Order.CreatedBy;
                            l_OData.CreatedDate = DateTime.Now;
                            l_OData.OrderId = l_Order.Id;
                            l_OData.OrderNumber = l_Order.OrderNumber;

                            if (l_OData.SaveNew().IsSuccess)
                            {
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = l_ResponseData.message;
                            }
                        }
                    }
                }
                else
                {
                    l_Order.Status = "SYNCERROR";
                    if (l_Order.Modify().IsSuccess)
                    {
                        OrderData l_OData = new OrderData();

                        l_OData.UseConnection(string.Empty, l_Order.Connection);

                        l_OData.DeleteWithType(l_Order.Id, "850-RESPONSE");

                        l_OData.Type = "RESPONSE";
                        l_OData.Data = response.Content;
                        l_OData.CreatedBy = l_Order.CreatedBy;
                        l_OData.CreatedDate = DateTime.Now;
                        l_OData.OrderId = l_Order.Id;
                        l_OData.OrderNumber = l_Order.OrderNumber;

                        if (l_OData.SaveNew().IsSuccess)
                        {
                            l_Response.Code = (int)ResponseCodes.Success;
                            l_Response.Message = l_ResponseData.message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static async Task<OrderResponseModel> Process855(ILogger<OrdersController> logger, int p_OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            ConnectorDataModel? l_Data = null;
            OrderObjectModel l_Order = new OrderObjectModel();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order = await OrderManager.GetOrder(p_OrderId);

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing 855 for Order [{p_OrderId}].");

                Order855InfoResponseModel l_SyncResponse = await OrderManager.GetOrderStatusInfo(l_Order.Customer, l_Order.Order);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                var l_Map = l_Order.Customer.Maps.Where(p => p.MapTypeName == "855 Transformation");

                if (l_Map != null)
                {
                    l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                }

                string l_JSON = JsonConvert.SerializeObject(l_SyncResponse.data);
                string l_TransResponse = OrderManager.Generate855JSON(l_JSON, l_Order.Order, l_TransformationMap);

                if (string.IsNullOrEmpty(l_TransResponse))
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - 855 mapping failed");

                    l_Response.Message = "855 mapping failed";

                    return l_Response;
                }

                Order855TransformationModel l_EDIResponse = OrderManager.Generate855EDI(l_Order.Order, l_Order.Customer, l_TransResponse);

                if (l_EDIResponse.Code != (int)ResponseCodes.Success)
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_EDIResponse.Message}");

                    l_Response.Code = l_EDIResponse.Code;
                    l_Response.Message = l_EDIResponse.Message;

                    return l_Response;
                }

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - 855 processing completed successfully.");

                l_Response.Customer = l_Order.Customer;
                l_Response.Order = l_Order.Order;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "855 processed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");

                throw;
            }

            return l_Response;
        }

        public static async Task<Order855InfoResponseModel> GetOrderStatusInfo(Customers p_Customer, Orders p_Order)
        {
            Order855InfoResponseModel l_Response = new Order855InfoResponseModel();
            ConnectorDataModel? l_Data = null;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                var l_Connector = p_Customer.Connectors.Where(c => c.ConnectorName == "Create 855" && c.ConnectorType == "Order Status" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    if (l_Parameter.Value == "@@CUSTOMERPO@@")
                    {
                        l_Parameter.Value = l_Parameter.Value.Replace("@@CUSTOMERPO@@", p_Order.OrderNumber);
                    }

                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                string l_JSONBODY = @$"{{
                                        'id': '1491',
                                        'customerPO': '{p_Order.OrderNumber}',
                                    }}";

                request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                RestResponse response = await client.ExecuteAsync(request);

                NetSuite855ResponseModel l_ResponseData = JsonConvert.DeserializeObject<NetSuite855ResponseModel>(response.Content);

                if (l_ResponseData.status == "OK")
                {
                    OrderData l_OData = new OrderData();

                    l_OData.UseConnection(string.Empty, p_Order.Connection);

                    l_OData.DeleteWithType(p_Order.Id, "855-NS");

                    l_OData.Type = "855-NS";
                    l_OData.Data = response.Content;
                    l_OData.CreatedBy = p_Order.CreatedBy;
                    l_OData.CreatedDate = DateTime.Now;
                    l_OData.OrderId = p_Order.Id;
                    l_OData.OrderNumber = p_Order.OrderNumber;

                    if (l_OData.SaveNew().IsSuccess)
                    {
                        l_Response.data = l_ResponseData.data[0];
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = l_ResponseData.message;
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = l_ResponseData.message;
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_ResponseData.message;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static string Generate855JSON(string p_JSON, Orders p_Order, string p_TransformationMap)
        {
            string l_Response = string.Empty;

            try
            {
                string jsonData = p_JSON;
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);

                OrderData l_OData = new OrderData();

                l_OData.UseConnection(string.Empty, p_Order.Connection);

                l_OData.DeleteWithType(p_Order.Id, "855-JSON");

                l_OData.Type = "855-JSON";
                l_OData.Data = jsonTransformation;
                l_OData.CreatedBy = p_Order.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.OrderId = p_Order.Id;
                l_OData.OrderNumber = p_Order.OrderNumber;

                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Response = jsonTransformation;
                }
            }
            catch (Exception)
            {
            }

            return l_Response;
        }

        public static Order855TransformationModel Generate855EDI(Orders p_Order, Customers p_Customer, string p_JSON)
        {
            Order855TransformationModel l_Response = new Order855TransformationModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                InboundEDIInfo l_EDIInfo = p_Order.Inbound.Info[0];
                OrderStatus855 l_855 = JsonConvert.DeserializeObject<OrderStatus855>(p_JSON);

                Maps_5010.M_855 map = new Maps_5010.M_855();
                EdiTrans t = new EdiTrans(map);
                int l_HLIndex = 1, l_HL_PIndex = 0;

                OrderManager.Add855Segments(map, t, l_855.StartNodes, "L_N9|L_N1", ref l_HLIndex, l_HL_PIndex);

                foreach (LoopItem l_Item in l_855.Items)
                {
                    OrderManager.Add855Segments(map, t, l_Item.Data, "L_PO1|L_ACK", ref l_HLIndex, l_HL_PIndex);
                }

                OrderManager.Add855Segments(map, t, l_855.EndNodes, "L_CTT", ref l_HLIndex, l_HL_PIndex);

                var g = new EdiGroup("PR");
                g.Transactions.Add(t);

                var i = new EdiInterchange();
                i.Groups.Add(g);

                EdiBatch b = new EdiBatch();
                b.Interchanges.Add(i);

                OutboundEDI l_Outbound = new OutboundEDI();

                l_Outbound.UseConnection(string.Empty, p_Order.Connection);

                l_Outbound.Status = "NEW";
                l_Outbound.Data = string.Empty;
                l_Outbound.CreatedBy = p_Order.CreatedBy;
                l_Outbound.CreatedDate = DateTime.Now;
                l_Outbound.OrderId = p_Order.Id;

                if (l_Outbound.SaveNew().IsSuccess)
                {
                    EdiDataWriterSettings settings = new EdiDataWriterSettings(
                    new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                    new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                    new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                    l_EDIInfo.ISAReceiverQual, l_EDIInfo.ISAReceiverId, l_EDIInfo.ISASenderQual, l_EDIInfo.ISASenderId, l_EDIInfo.GSReceiverId, l_EDIInfo.GSSenderId,
                    l_EDIInfo.ISAEdiVersion, l_EDIInfo.GSEdiVersion, l_EDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, l_EDIInfo.SegmentSeparator, l_EDIInfo.ElementSeparator, "^", "");

                    EdiDataWriter w = new EdiDataWriter(settings);

                    OrderData l_OData = new OrderData();

                    l_OData.UseConnection(string.Empty, p_Order.Connection);

                    l_OData.DeleteWithType(p_Order.Id, "855-eSyncMate");

                    l_OData.Type = "855-eSyncMate";
                    l_OData.Data = w.WriteToString(b);
                    l_OData.CreatedBy = p_Order.CreatedBy;
                    l_OData.CreatedDate = DateTime.Now;
                    l_OData.OrderId = p_Order.Id;
                    l_OData.OrderNumber = p_Order.OrderNumber;

                    if (l_OData.SaveNew().IsSuccess)
                    {
                        l_Outbound.Data = l_OData.Data;
                        if (l_Outbound.Modify().IsSuccess)
                        {
                            p_Order.Status = "ACKNOWLEDGED";
                            if (p_Order.Modify().IsSuccess)
                            {
                                l_Response.EDI = l_OData.Data;
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = "eSyncMate generated successfully!";
                            }
                            else
                            {
                                l_Response.Code = (int)ResponseCodes.Error;
                                l_Response.Message = "Cannot generate 855 eSyncMate";
                            }
                        }
                        else
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = "Cannot generate 855 eSyncMate";
                        }
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = "Cannot generate 855 eSyncMate";
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Cannot generate 855 eSyncMate";
                }
            }
            catch (Exception)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
            }

            return l_Response;
        }

        public static async Task<OrderResponseModel> ProcessASN(ILogger<OrdersController> logger, int p_OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            OrderObjectModel l_Order = new OrderObjectModel();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order = await OrderManager.GetOrder(p_OrderId);

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing ASN for Order [{p_OrderId}].");

                OrderASNInfoResponseModel l_SyncResponse = await OrderManager.GetOrderASNInfo(l_Order.Customer, l_Order.Order);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                var l_Map = l_Order.Customer.Maps.Where(p => p.MapTypeName == "856 Transformation");

                if (l_Map != null)
                {
                    l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                }

                foreach (NetSuiteASNData l_ASN in l_SyncResponse.data)
                {
                    OrderASNTransformationModel l_TransResponse = OrderManager.GenerateASNJSONForEDI(l_ASN, l_Order.Order, l_TransformationMap);

                    if (l_TransResponse.Code != (int)ResponseCodes.Success)
                    {
                        logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                        l_Response.Code = l_TransResponse.Code;
                        l_Response.Message = l_TransResponse.Message;

                        return l_Response;
                    }

                    l_TransResponse = OrderManager.GenerateASNEDI(l_Order.Customer, l_Order.Order, l_TransResponse.JSON, l_ASN.customerPO);

                    if (l_TransResponse.Code != (int)ResponseCodes.Success)
                    {
                        logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                        l_Response.Code = l_TransResponse.Code;
                        l_Response.Message = l_TransResponse.Message;

                        return l_Response;
                    }
                }

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ASN processing completed successfully.");

                l_Response.Customer = l_Order.Customer;
                l_Response.Order = l_Order.Order;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "ASN processed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");

                throw;
            }

            return l_Response;
        }

        public static async Task<OrderASNInfoResponseModel> GetOrderASNInfo(Customers p_Customer, Orders p_Order)
        {
            OrderASNInfoResponseModel l_Response = new OrderASNInfoResponseModel();
            ConnectorDataModel? l_Data = null;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                var l_Connector = p_Customer.Connectors.Where(c => c.ConnectorName == "Create ASN" && c.ConnectorType == "ASNs" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    if (l_Parameter.Value == "@@CUSTOMERPO@@")
                    {
                        l_Parameter.Value = l_Parameter.Value.Replace("@@CUSTOMERPO@@", p_Order.OrderNumber);
                    }

                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                string l_JSONBODY = @$"{{
                                        'id': '1393',
                                        'customerPO': '{p_Order.OrderNumber}',
                                    }}";

                request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                RestResponse response = await client.ExecuteAsync(request);

                NetSuiteASNResponseModel l_ResponseData = JsonConvert.DeserializeObject<NetSuiteASNResponseModel>(response.Content);

                if (l_ResponseData.status == "OK")
                {
                    OrderData l_OData = new OrderData();

                    l_OData.UseConnection(string.Empty, p_Order.Connection);

                    l_OData.DeleteWithType(p_Order.Id, "ASN-NS");

                    l_OData.Type = "ASN-NS";
                    l_OData.Data = response.Content;
                    l_OData.CreatedBy = p_Order.CreatedBy;
                    l_OData.CreatedDate = DateTime.Now;
                    l_OData.OrderId = p_Order.Id;
                    l_OData.OrderNumber = p_Order.OrderNumber;

                    if (l_OData.SaveNew().IsSuccess)
                    {
                        l_Response.data = l_ResponseData.data;
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = l_ResponseData.message;
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = l_ResponseData.message;
                    }
                }
                else
                {
                    OrderData l_OData = new OrderData();

                    l_OData.UseConnection(string.Empty, p_Order.Connection);

                    l_OData.DeleteWithType(p_Order.Id, "ASN-NS-ERROR");

                    l_OData.Type = "ASN-NS-ERROR";
                    l_OData.Data = response.Content;
                    l_OData.CreatedBy = p_Order.CreatedBy;
                    l_OData.CreatedDate = DateTime.Now;
                    l_OData.OrderId = p_Order.Id;
                    l_OData.OrderNumber = p_Order.OrderNumber;

                    l_OData.SaveNew();

                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_ResponseData.message;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static OrderASNTransformationModel GenerateASNJSONForEDI(NetSuiteASNData p_ASN, Orders p_Order, string p_TransformationMap)
        {
            OrderASNTransformationModel l_Response = new OrderASNTransformationModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                string l_FileType = !p_Order.IsStoreOrder ? "ASN-JSON" : "ASN-JSON-" + p_ASN.customerPO;
                string jsonData = JsonConvert.SerializeObject(p_ASN);
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);

                OrderData l_OData = new OrderData();

                l_OData.UseConnection(string.Empty, p_Order.Connection);

                l_OData.DeleteWithType(p_Order.Id, l_FileType);

                l_OData.Type = l_FileType;
                l_OData.Data = jsonTransformation;
                l_OData.CreatedBy = p_Order.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.OrderId = p_Order.Id;
                l_OData.OrderNumber = p_Order.OrderNumber;

                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.JSON = jsonTransformation;
                }
            }
            catch (Exception)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
            }

            return l_Response;
        }

        public static OrderASNTransformationModel GenerateASNEDI(Customers p_Customer, Orders p_Order, string p_JSON, string p_CustomerPO)
        {
            OrderASNTransformationModel l_Response = new OrderASNTransformationModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                string l_FileType = !p_Order.IsStoreOrder ? "ASN-eSyncMate" : "ASN-eSyncMate-" + p_CustomerPO;
                InboundEDIInfo l_EDIInfo = p_Order.Inbound.Info[0];
                ASN l_ASN = JsonConvert.DeserializeObject<ASN>(p_JSON);

                Maps_5010.M_856 map = new Maps_5010.M_856();
                EdiTrans t = new EdiTrans(map);
                int l_HLIndex = 1, l_HL_PIndex = 1;

                OrderManager.AddASNSegments(map, t, l_ASN.StartNodes, "L_HL|L_N1", ref l_HLIndex, l_HL_PIndex);

                if (l_ASN.Orders != null && l_ASN.Orders.Count > 0)
                {
                    l_HLIndex += 1;
                    foreach (LoopOrder l_Order in l_ASN.Orders)
                    {
                        OrderManager.AddASNSegments(map, t, l_Order.Data, "L_HL|L_N1", ref l_HLIndex, l_HL_PIndex);
                        l_HL_PIndex = l_HLIndex;

                        foreach (LoopPackage l_Package in l_Order.Packs)
                        {
                            OrderManager.AddASNSegments(map, t, l_Package.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);
                            l_HL_PIndex = l_HLIndex;

                            foreach (LoopItem l_Item in l_Package.Items)
                            {
                                OrderManager.AddASNSegments(map, t, l_Item.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);
                            }
                        }
                    }
                }
                else
                {
                    l_HLIndex += 2;
                    foreach (LoopPackage l_Package in l_ASN.Packs)
                    {
                        l_HL_PIndex = l_HLIndex;
                        OrderManager.AddASNSegments(map, t, l_Package.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);

                        foreach (LoopItem l_Item in l_Package.Items)
                        {
                            OrderManager.AddASNSegments(map, t, l_Item.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);
                        }
                    }
                }

                OrderManager.AddASNSegments(map, t, l_ASN.EndNodes, string.Empty, ref l_HLIndex, l_HL_PIndex);

                var g = new EdiGroup("SH");
                g.Transactions.Add(t);

                var i = new EdiInterchange();
                i.Groups.Add(g);

                EdiBatch b = new EdiBatch();
                b.Interchanges.Add(i);

                OutboundEDI l_Outbound = new OutboundEDI();

                l_Outbound.UseConnection(string.Empty, p_Order.Connection);

                l_Outbound.Status = "NEW";
                l_Outbound.Data = string.Empty;
                l_Outbound.CreatedBy = p_Order.CreatedBy;
                l_Outbound.CreatedDate = DateTime.Now;
                l_Outbound.OrderId = p_Order.Id;

                if (l_Outbound.SaveNew().IsSuccess)
                {
                    EdiDataWriterSettings settings = new EdiDataWriterSettings(
                    new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                    new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                    new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                    l_EDIInfo.ISAReceiverQual, l_EDIInfo.ISAReceiverId, l_EDIInfo.ISASenderQual, l_EDIInfo.ISASenderId, l_EDIInfo.GSReceiverId, l_EDIInfo.GSSenderId,
                    l_EDIInfo.ISAEdiVersion, l_EDIInfo.GSEdiVersion, l_EDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, l_EDIInfo.SegmentSeparator, l_EDIInfo.ElementSeparator, "^", "");

                    EdiDataWriter w = new EdiDataWriter(settings);

                    OrderData l_OData = new OrderData();

                    l_OData.UseConnection(string.Empty, p_Order.Connection);

                    l_OData.DeleteWithType(p_Order.Id, l_FileType);

                    l_OData.Type = l_FileType;
                    l_OData.Data = w.WriteToString(b);
                    l_OData.CreatedBy = p_Order.CreatedBy;
                    l_OData.CreatedDate = DateTime.Now;
                    l_OData.OrderId = p_Order.Id;
                    l_OData.OrderNumber = p_Order.OrderNumber;

                    if (l_OData.SaveNew().IsSuccess)
                    {
                        l_Outbound.Data = l_OData.Data;
                        if (l_Outbound.Modify().IsSuccess)
                        {
                            p_Order.Status = "ASNGEN";
                            if (p_Order.Modify().IsSuccess)
                            {
                                l_Response.EDI = l_OData.Data;
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = "eSyncMate generated successfully!";
                            }
                            else
                            {
                                l_Response.Code = (int)ResponseCodes.Error;
                                l_Response.Message = "Cannot generate ASN eSyncMate";
                            }
                        }
                        else
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = "Cannot generate ASN eSyncMate";
                        }
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = "Cannot generate ASN eSyncMate";
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Cannot generate ASN eSyncMate";
                }
            }
            catch (Exception)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
            }

            return l_Response;
        }

        public static async Task<ResponseModel> MarkOrderForASN(Customers p_Customer, Orders p_Order)
        {
            ResponseModel l_Response = new ResponseModel();
            ConnectorDataModel? l_Data = null;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                var l_Connector = p_Customer.Connectors.Where(c => c.ConnectorName == "Mark for ASN" && c.ConnectorType == "ASNs" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                OrderData l_OrderData = p_Order.Files.Where(f => f.Type == "ASN-NS").FirstOrDefault<OrderData>();

                if (l_OrderData == null)
                {
                    return l_Response;
                }

                NetSuiteASNResponseModel l_ASN = JsonConvert.DeserializeObject<NetSuiteASNResponseModel>(l_OrderData.Data);

                string responseContent = "";
                string l_JSONSOBODY = "";
                NetSuiteMarkASNResponseModel l_ResponseData = null;

                if (string.IsNullOrEmpty(l_ASN.data[0].fulfillments[0].soId))
                {
                    l_JSONSOBODY = @$"{{
                                        ""id"": ""{l_ASN.data[0].id}"",
                                        ""type"": ""salesorder"",
                                        ""field"": ""custbody5"",
                                        ""value"": ""T""
                                    }},
                                    {{
                                        ""id"": ""{l_ASN.data[0].fulfillments[0].id}"",
                                        ""type"": ""itemfulfillment"",
                                        ""field"": ""custbody5"",
                                        ""value"": ""T""
                                    }}";
                    string l_JSONBODY = @$"[{l_JSONSOBODY}]";

                    request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                    RestResponse response = await client.ExecuteAsync(request);

                    l_ResponseData = JsonConvert.DeserializeObject<NetSuiteMarkASNResponseModel>(response.Content);

                    if (l_ResponseData == null || l_ResponseData.Count == 0)
                    {
                        return l_Response;
                    }

                    responseContent = response.Content;
                }
                else
                {
                    foreach (NetSuiteASNData l_ASNData in l_ASN.data)
                    {
                        foreach (Fulfillment l_Fulfillment in l_ASNData.fulfillments)
                        {
                            l_JSONSOBODY = @$"{{
                                                ""id"": ""{l_Fulfillment.soId}"",
                                                ""type"": ""salesorder"",
                                                ""field"": ""custbody5"",
                                                ""value"": ""T""
                                            }},
                                            {{
                                                ""id"": ""{l_Fulfillment.id}"",
                                                ""type"": ""itemfulfillment"",
                                                ""field"": ""custbody5"",
                                                ""value"": ""T""
                                            }}";
                            string l_JSONBODY = @$"[{l_JSONSOBODY}]";

                            request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                            RestResponse response = await client.ExecuteAsync(request);

                            l_ResponseData = JsonConvert.DeserializeObject<NetSuiteMarkASNResponseModel>(response.Content);

                            if (l_ResponseData == null || l_ResponseData.Count == 0 || l_ResponseData[0].status != "OK")
                            {
                                return l_Response;
                            }

                            responseContent = response.Content;
                        }
                    }
                }

                if (l_ResponseData?[0].status == "OK")
                {
                    p_Order.Status = "ASNMARK";
                    if (p_Order.Modify().IsSuccess)
                    {
                        if (p_Order.IsStoreOrder)
                        {
                            OrderStores l_Stores = new OrderStores();

                            l_Stores.UseConnection("", p_Order.Connection);
                            l_Stores.UpdateOrderStoreStatus(p_Order.Id, "ASNMARK");
                        }

                        OrderData l_OData = new OrderData();

                        l_OData.UseConnection(string.Empty, p_Order.Connection);

                        l_OData.DeleteWithType(p_Order.Id, "ASN-MARK-NS");

                        l_OData.Type = "ASN-MARK-NS";
                        l_OData.Data = responseContent;
                        l_OData.CreatedBy = p_Order.CreatedBy;
                        l_OData.CreatedDate = DateTime.Now;
                        l_OData.OrderId = p_Order.Id;
                        l_OData.OrderNumber = p_Order.OrderNumber;

                        if (l_OData.SaveNew().IsSuccess)
                        {
                            l_Response.Code = (int)ResponseCodes.Success;
                            l_Response.Message = l_ResponseData[0].message;
                        }
                        else
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = l_ResponseData[0].message;
                        }
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = l_ResponseData[0].message;
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_ResponseData[0].message;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static string GenerateInvoiceJSONForNS(string p_ASNJSON, Orders p_Order, string p_TransformationMap)
        {
            string l_Response = string.Empty;

            try
            {
                NetSuiteASNResponseModel l_ASN = JsonConvert.DeserializeObject<NetSuiteASNResponseModel>(p_ASNJSON);
                string jsonData = JsonConvert.SerializeObject(l_ASN.data[0]);
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);

                OrderData l_OData = new OrderData();

                l_OData.UseConnection(string.Empty, p_Order.Connection);

                l_OData.DeleteWithType(p_Order.Id, "INV-JSON");

                l_OData.Type = "INV-JSON";
                l_OData.Data = jsonTransformation;
                l_OData.CreatedBy = p_Order.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.OrderId = p_Order.Id;
                l_OData.OrderNumber = p_Order.OrderNumber;

                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Response = jsonTransformation;
                }
            }
            catch (Exception)
            {
            }

            return l_Response;
        }

        public static async Task<ResponseModel> CreateInvoice(Customers p_Customer, Orders p_Order)
        {
            ResponseModel l_Response = new ResponseModel();
            ConnectorDataModel? l_Data = null;
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                var l_Connector = p_Customer.Connectors.Where(c => c.ConnectorName == "Create Invoice" && c.ConnectorType == "Invoices" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                OrderData l_OrderData = p_Order.Files.Where(f => f.Type == "ASN-NS").FirstOrDefault<OrderData>();

                if (l_OrderData == null)
                {
                    return l_Response;
                }

                var l_Map = p_Customer.Maps.Where(p => p.MapTypeName == "INV Transformation");

                if (l_Map != null)
                {
                    l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                }

                string l_JSONBODY = OrderManager.GenerateInvoiceJSONForNS(l_OrderData.Data, p_Order, l_TransformationMap);

                request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                RestResponse response = await client.ExecuteAsync(request);

                NetSuiteCreateInvoiceResponseModel l_ResponseData = JsonConvert.DeserializeObject<NetSuiteCreateInvoiceResponseModel>(response.Content);

                if (l_ResponseData == null)
                {
                    return l_Response;
                }

                if (l_ResponseData.status == "OK")
                {
                    p_Order.Status = "INVOICED";
                    if (p_Order.Modify().IsSuccess)
                    {
                        OrderData l_OData = new OrderData();

                        l_OData.UseConnection(string.Empty, p_Order.Connection);

                        l_OData.DeleteWithType(p_Order.Id, "810-INV");

                        l_OData.Type = "810-INV";
                        l_OData.Data = response.Content;
                        l_OData.CreatedBy = p_Order.CreatedBy;
                        l_OData.CreatedDate = DateTime.Now;
                        l_OData.OrderId = p_Order.Id;
                        l_OData.OrderNumber = p_Order.OrderNumber;

                        if (l_OData.SaveNew().IsSuccess)
                        {
                            l_Response.Code = (int)ResponseCodes.Success;
                            l_Response.Message = l_ResponseData.message;
                        }
                        else
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = l_ResponseData.message;
                        }
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = l_ResponseData.message;
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_ResponseData.message;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static string GenerateInvoiceJSON(string p_ASNJSON, Orders p_Order, string p_TransformationMap)
        {
            string l_Response = string.Empty;

            try
            {
                NetSuiteASNResponseModel l_ASN = JsonConvert.DeserializeObject<NetSuiteASNResponseModel>(p_ASNJSON);
                string jsonData = JsonConvert.SerializeObject(l_ASN.data[0]);
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);

                OrderData l_OData = new OrderData();

                l_OData.UseConnection(string.Empty, p_Order.Connection);

                l_OData.DeleteWithType(p_Order.Id, "810-JSON");

                l_OData.Type = "810-JSON";
                l_OData.Data = jsonTransformation;
                l_OData.CreatedBy = p_Order.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.OrderId = p_Order.Id;
                l_OData.OrderNumber = p_Order.OrderNumber;

                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Response = jsonTransformation;
                }
            }
            catch (Exception)
            {
            }

            return l_Response;
        }

        public static string GenerateInvoiceJSON(NetSuiteASNData p_ASN, Orders p_Order, string p_TransformationMap)
        {
            string l_Response = string.Empty;

            try
            {
                string jsonData = JsonConvert.SerializeObject(p_ASN);
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);

                OrderData l_OData = new OrderData();

                l_OData.UseConnection(string.Empty, p_Order.Connection);

                l_OData.DeleteWithType(p_Order.Id, "810-JSON");

                l_OData.Type = "810-JSON";
                l_OData.Data = jsonTransformation;
                l_OData.CreatedBy = p_Order.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.OrderId = p_Order.Id;
                l_OData.OrderNumber = p_Order.OrderNumber;

                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Response = jsonTransformation;
                }
            }
            catch (Exception)
            {
            }

            return l_Response;
        }

        public static Order810TransformationModel GenerateInvoiceEDI(Orders p_Order, Customers p_Customer, string p_JSON)
        {
            Order810TransformationModel l_Response = new Order810TransformationModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                InboundEDIInfo l_EDIInfo = p_Order.Inbound.Info[0];
                OrderStatus855 l_810 = JsonConvert.DeserializeObject<OrderStatus855>(p_JSON);

                Maps_5010.M_810 map = new Maps_5010.M_810();
                EdiTrans t = new EdiTrans(map);
                int l_HLIndex = 1, l_HL_PIndex = 0;

                OrderManager.Add810Segments(map, t, l_810.StartNodes, "L_N1", ref l_HLIndex, l_HL_PIndex);

                foreach (LoopItem l_Item in l_810.Items)
                {
                    OrderManager.Add810Segments(map, t, l_Item.Data, "L_IT1", ref l_HLIndex, l_HL_PIndex);
                }

                OrderManager.Add810Segments(map, t, l_810.EndNodes, "L_ISS", ref l_HLIndex, l_HL_PIndex);

                var g = new EdiGroup("IN");
                g.Transactions.Add(t);

                var i = new EdiInterchange();
                i.Groups.Add(g);

                EdiBatch b = new EdiBatch();
                b.Interchanges.Add(i);

                OutboundEDI l_Outbound = new OutboundEDI();

                l_Outbound.UseConnection(string.Empty, p_Order.Connection);

                l_Outbound.Status = "NEW";
                l_Outbound.Data = string.Empty;
                l_Outbound.CreatedBy = p_Order.CreatedBy;
                l_Outbound.CreatedDate = DateTime.Now;
                l_Outbound.OrderId = p_Order.Id;

                if (l_Outbound.SaveNew().IsSuccess)
                {
                    EdiDataWriterSettings settings = new EdiDataWriterSettings(
                    new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                    new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                    new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                    l_EDIInfo.ISAReceiverQual, l_EDIInfo.ISAReceiverId, l_EDIInfo.ISASenderQual, p_Customer.ISA810ReceiverId, l_EDIInfo.GSReceiverId, p_Customer.ISA810ReceiverId,
                    l_EDIInfo.ISAEdiVersion, l_EDIInfo.GSEdiVersion, l_EDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, l_EDIInfo.SegmentSeparator, l_EDIInfo.ElementSeparator, "^", "");

                    EdiDataWriter w = new EdiDataWriter(settings);

                    OrderData l_OData = new OrderData();

                    l_OData.UseConnection(string.Empty, p_Order.Connection);

                    l_OData.DeleteWithType(p_Order.Id, "810-eSyncMate");

                    l_OData.Type = "810-eSyncMate";
                    l_OData.Data = w.WriteToString(b);
                    l_OData.CreatedBy = p_Order.CreatedBy;
                    l_OData.CreatedDate = DateTime.Now;
                    l_OData.OrderId = p_Order.Id;
                    l_OData.OrderNumber = p_Order.OrderNumber;

                    if (l_OData.SaveNew().IsSuccess)
                    {
                        l_Outbound.Data = l_OData.Data;
                        if (l_Outbound.Modify().IsSuccess)
                        {
                            p_Order.Status = "INVEDI";
                            if (p_Order.Modify().IsSuccess)
                            {
                                l_Response.EDI = l_OData.Data;
                                l_Response.Code = (int)ResponseCodes.Success;
                                l_Response.Message = "eSyncMate generated successfully!";
                            }
                            else
                            {
                                l_Response.Code = (int)ResponseCodes.Error;
                                l_Response.Message = "Cannot generate 810 eSyncMate";
                            }
                        }
                        else
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = "Cannot generate 810 eSyncMate";
                        }
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = "Cannot generate 810 eSyncMate";
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Cannot generate 810 eSyncMate";
                }
            }
            catch (Exception)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
            }

            return l_Response;
        }

        public static async Task<OrderResponseModel> Process810(ILogger<OrdersController> logger, int p_OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            ConnectorDataModel? l_Data = null;
            OrderObjectModel l_Order = new OrderObjectModel();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order = await OrderManager.GetOrder(p_OrderId);

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing 810 for Order [{p_OrderId}].");

                OrderData l_OrderData = l_Order.Order.Files.Where(f => f.Type == "ASN-NS").FirstOrDefault<OrderData>();

                if (l_OrderData == null || string.IsNullOrEmpty(l_OrderData?.Data))
                {
                    l_Response = await OrderManager.Process810Special(logger, l_Order);

                    //logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Pre 810 information is not complete for Order [{p_OrderId}].");
                    return l_Response;
                }
                else
                {
                    try
                    {
                        NetSuiteASNResponseModel l_ASN = JsonConvert.DeserializeObject<NetSuiteASNResponseModel>(l_OrderData.Data);

                        if (l_ASN.data[0].fulfillments[0].items == null || l_ASN.data[0].fulfillments[0].items.Count == 0)
                        {
                            l_Response = await OrderManager.Process810Special(logger, l_Order);

                            return l_Response;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                var l_Map = l_Order.Customer.Maps.Where(p => p.MapTypeName == "810 Transformation");

                if (l_Map != null)
                {
                    l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                }

                string l_TransResponse = OrderManager.GenerateInvoiceJSON(l_OrderData.Data, l_Order.Order, l_TransformationMap);

                if (string.IsNullOrEmpty(l_TransResponse))
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - 810 mapping failed");

                    l_Response.Message = "810 mapping failed";

                    return l_Response;
                }

                Order810TransformationModel l_EDIResponse = OrderManager.GenerateInvoiceEDI(l_Order.Order, l_Order.Customer, l_TransResponse);

                if (l_EDIResponse.Code != (int)ResponseCodes.Success)
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_EDIResponse.Message}");

                    l_Response.Code = l_EDIResponse.Code;
                    l_Response.Message = l_EDIResponse.Message;

                    return l_Response;
                }

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - 810 processing completed successfully.");

                l_Response.Customer = l_Order.Customer;
                l_Response.Order = l_Order.Order;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "810 processed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");

                throw;
            }

            return l_Response;
        }

        public static async Task<OrderResponseModel> Process810Special(ILogger<OrdersController> logger, OrderObjectModel p_Order)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            ConnectorDataModel? l_Data = null;
            OrderObjectModel l_Order = p_Order;
            Common l_Common = new Common();
            DataTable l_CustomerItems = new DataTable();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                OrderASNInfoResponseModel l_ASNResponse = await OrderManager.GetOrderASNInfo(l_Order.Customer, l_Order.Order);

                if (l_ASNResponse.Code != (int)ResponseCodes.Success)
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_ASNResponse.Message}");

                    l_Response.Code = l_ASNResponse.Code;
                    l_Response.Message = l_ASNResponse.Message;

                    return l_Response;
                }

                var l_Map = l_Order.Customer.Maps.Where(p => p.MapTypeName == "810 Transformation");

                if (l_Map != null)
                {
                    l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                }

                l_Common.UseConnection(CommonUtils.ConnectionString);
                l_Common.GetList("SELECT VendorStyle, UPC FROM CustomerItems WHERE CustomerId = " + l_Order.Customer.Id, ref l_CustomerItems);

                foreach (Fulfillment l_Fulfillment in l_ASNResponse.data[0].fulfillments)
                {
                    int l_index = 1;
                    foreach (NetsuitItem l_Item in l_Fulfillment.items)
                    {
                        if (string.IsNullOrEmpty(l_Item.ediLineId))
                        {
                            l_Item.ediLineId = l_index.ToString().PadLeft(3, '0');
                        }
                        if (string.IsNullOrEmpty(l_Item.itemSku))
                        {
                            DataRow[] l_Rows = l_CustomerItems.Select("UPC = '" + l_Item.itemUpc + "'");

                            if (l_Rows != null && l_Rows.Length > 0)
                            {
                                l_Item.itemSku = PublicFunctions.ConvertNullAsString(l_Rows[0]["VendorStyle"], string.Empty);
                            }
                        }

                        l_index++;
                    }
                }

                string l_TransResponse = OrderManager.GenerateInvoiceJSON(l_ASNResponse.data[0], l_Order.Order, l_TransformationMap);

                if (string.IsNullOrEmpty(l_TransResponse))
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - 810 mapping failed");

                    l_Response.Message = "810 mapping failed";

                    return l_Response;
                }

                Order810TransformationModel l_EDIResponse = OrderManager.GenerateInvoiceEDI(l_Order.Order, l_Order.Customer, l_TransResponse);

                if (l_EDIResponse.Code != (int)ResponseCodes.Success)
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_EDIResponse.Message}");

                    l_Response.Code = l_EDIResponse.Code;
                    l_Response.Message = l_EDIResponse.Message;

                    return l_Response;
                }

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - 810 processing completed successfully.");

                l_Response.Customer = l_Order.Customer;
                l_Response.Order = l_Order.Order;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "810 processed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");

                throw;
            }
            finally
            {
                l_CustomerItems.Dispose();
            }

            return l_Response;
        }

        public static async Task<OrderObjectModel> GetOrder(int p_OrderId)
        {
            OrderObjectModel l_Response = new OrderObjectModel();
            Orders l_Order = new Orders();
            Customers l_Customer = new Customers();

            l_Order.UseConnection(Models.CommonUtils.ConnectionString);
            if (!l_Order.GetObject(p_OrderId).IsSuccess)
            {
                return l_Response;
            }

            l_Customer.UseConnection("", l_Order.Connection);
            l_Customer.GetObject(l_Order.CustomerId);

            l_Response.Order = l_Order;
            l_Response.Customer = l_Customer;

            return l_Response;
        }

        public static async Task<OrderResponseModel> ProcessASNStore(ILogger<OrdersController> logger, int p_OrderId, DataTable p_Data)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            OrderObjectModel l_Order = new OrderObjectModel();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order = await OrderManager.GetOrder(p_OrderId);

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing ASN for Order [{p_OrderId}].");

                OrderASNInfoResponseModel l_SyncResponse = await OrderManager.GetOrderASNInfo(l_Order.Customer, l_Order.Order);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                List<DCOrders> l_Orders = OrderManager.GenerateStoreASNJSON(l_Order.Order, p_Data);
                if (l_Orders?.Count > 0)
                {
                    var l_Map = l_Order.Customer.Maps.Where(p => p.MapTypeName == "856 Transformation");

                    if (l_Map != null)
                    {
                        l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                    }

                    foreach (DCOrders l_DCOrder in l_Orders)
                    {
                        OrderASNTransformationModel l_TransResponse = OrderManager.GenerateStoreASNJSONForEDI(l_DCOrder, l_Order.Order, l_TransformationMap);

                        if (l_TransResponse.Code != (int)ResponseCodes.Success)
                        {
                            logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                            l_Response.Code = l_TransResponse.Code;
                            l_Response.Message = l_TransResponse.Message;

                            return l_Response;
                        }

                        l_TransResponse = OrderManager.GenerateStoreASNEDI(l_Order.Customer, l_Order.Order, l_TransResponse.JSON, l_DCOrder.CustomerPO + "-" + l_DCOrder.DC);

                        if (l_TransResponse.Code != (int)ResponseCodes.Success)
                        {
                            logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                            l_Response.Code = l_TransResponse.Code;
                            l_Response.Message = l_TransResponse.Message;

                            return l_Response;
                        }
                    }
                }
                else
                {
                    logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Cannot generate JSON for Store ASN");

                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Cannot generate JSON for Store ASN";

                    return l_Response;
                }

                logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ASN processing completed successfully.");

                l_Response.Customer = l_Order.Customer;
                l_Response.Order = l_Order.Order;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Store ASN processed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");

                throw;
            }

            return l_Response;
        }

        public static List<DCOrders> GenerateStoreASNJSON(Orders p_Order, DataTable p_Data)
        {
            bool l_Result = false;
            string l_JSON = string.Empty;
            List<DCOrders> l_Orders = new List<DCOrders>();
            DataTable l_DCData = p_Data.DefaultView.ToTable(true, new string[] { "SONo", "CustomerPO", "DC", "ShipDate", "OrderDate", "TotalCarton", "Carrier", "VendorID", "BOL" });

            foreach (DataRow l_Row in l_DCData.Rows)
            {
                DCOrders l_Order = new DCOrders();

                DBEntity.PopulateObjectFromRow(l_Order, l_DCData, l_Row);

                l_Order.TotalCartonWeight = (Convert.ToInt32(l_Order.TotalCarton) * 1.8).ToString();

                l_Order.Stores = new List<Store>();

                p_Data.DefaultView.RowFilter = "DC = '" + l_Row["DC"] + "'";
                DataTable l_StoreData = p_Data.DefaultView.ToTable(true, new string[] { "StoreNo", "Dept" });
                foreach (DataRow l_SRow in l_StoreData.Rows)
                {
                    Store l_Store = new Store();

                    DBEntity.PopulateObjectFromRow(l_Store, l_StoreData, l_SRow);

                    l_Store.Cartons = new List<StoreCarton>();

                    p_Data.DefaultView.RowFilter = "DC = '" + l_Row["DC"] + "' AND StoreNo = '" + l_SRow["StoreNo"] + "'";
                    DataTable l_CartonData = p_Data.DefaultView.ToTable(true, new string[] { "TrackingNo" });
                    foreach (DataRow l_CRow in l_CartonData.Rows)
                    {
                        StoreCarton l_Carton = new StoreCarton();

                        DBEntity.PopulateObjectFromRow(l_Carton, l_CartonData, l_CRow);

                        l_Carton.Items = new List<CartonItem>();
                        p_Data.DefaultView.RowFilter = "DC = '" + l_Row["DC"] + "' AND StoreNo = '" + l_SRow["StoreNo"] + "' AND TrackingNo = '" + l_CRow["TrackingNo"] + "'";
                        DataTable l_ItemData = p_Data.DefaultView.ToTable(true, new string[] { "VendorStyle", "UPC", "Qty" });
                        foreach (DataRow l_IRow in l_ItemData.Rows)
                        {
                            CartonItem l_Item = new CartonItem();

                            DBEntity.PopulateObjectFromRow(l_Item, l_ItemData, l_IRow);

                            l_Carton.Items.Add(l_Item);
                        }

                        l_ItemData.Dispose();
                        l_Store.Cartons.Add(l_Carton);
                    }

                    l_CartonData.Dispose();

                    l_Store.TotalCarton = l_CartonData.Rows.Count.ToString();
                    l_Store.TotalCartonWeight = (l_CartonData.Rows.Count * 1.8).ToString();

                    l_Order.Stores.Add(l_Store);
                }

                l_StoreData.Dispose();

                l_Orders.Add(l_Order);
            }

            l_DCData.Dispose();

            l_JSON = JsonConvert.SerializeObject(l_Orders);

            OrderData l_OData = new OrderData();

            l_OData.UseConnection(string.Empty, p_Order.Connection);

            l_OData.DeleteWithType(p_Order.Id, "ASN-Store");

            l_OData.Type = "ASN-Store";
            l_OData.Data = l_JSON;
            l_OData.CreatedBy = p_Order.CreatedBy;
            l_OData.CreatedDate = DateTime.Now;
            l_OData.OrderId = p_Order.Id;
            l_OData.OrderNumber = p_Order.OrderNumber;

            l_OData.SaveNew();

            return l_Orders;
        }

        public static OrderASNTransformationModel GenerateStoreASNJSONForEDI(DCOrders p_ASN, Orders p_Order, string p_TransformationMap)
        {
            OrderASNTransformationModel l_Response = new OrderASNTransformationModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                string l_FileType = "ASN-JSON-" + p_ASN.CustomerPO + "-" + p_ASN.DC;
                string jsonData = JsonConvert.SerializeObject(p_ASN);
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);

                OrderData l_OData = new OrderData();

                l_OData.UseConnection(string.Empty, p_Order.Connection);

                l_OData.DeleteWithType(p_Order.Id, l_FileType);

                l_OData.Type = l_FileType;
                l_OData.Data = jsonTransformation;
                l_OData.CreatedBy = p_Order.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.OrderId = p_Order.Id;
                l_OData.OrderNumber = p_Order.OrderNumber;

                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.JSON = jsonTransformation;
                }
            }
            catch (Exception)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
            }

            return l_Response;
        }

        public static OrderASNTransformationModel GenerateStoreASNEDI(Customers p_Customer, Orders p_Order, string p_JSON, string p_CustomerPO)
        {
            OrderASNTransformationModel l_Response = new OrderASNTransformationModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                string l_FileType = "ASN-eSyncMate-" + p_CustomerPO;
                InboundEDIInfo l_EDIInfo = p_Order.Inbound.Info[0];
                ASN l_ASN = JsonConvert.DeserializeObject<ASN>(p_JSON);

                Maps_5010.M_856 map = new Maps_5010.M_856();
                EdiTrans t = new EdiTrans(map);
                int l_HLIndex = 2, l_HL_PIndex = 2, l_HL_Pack_Index = 2;

                OrderManager.AddASNSegments(map, t, l_ASN.StartNodes, "L_HL|L_N1", ref l_HLIndex, l_HL_PIndex);

                foreach (LoopOrder l_Order in l_ASN.Orders)
                {
                    OrderManager.AddASNSegments(map, t, l_Order.Data, "L_HL|L_N1", ref l_HLIndex, l_HL_PIndex);
                    l_HL_Pack_Index = l_HLIndex - 1;

                    foreach (LoopPackage l_Package in l_Order.Packs)
                    {
                        OrderManager.AddASNSegments(map, t, l_Package.Data, "L_HL", ref l_HLIndex, l_HL_Pack_Index);
                        l_HL_PIndex = l_HLIndex - 1;

                        foreach (LoopItem l_Item in l_Package.Items)
                        {
                            OrderManager.AddASNSegments(map, t, l_Item.Data, "L_HL", ref l_HLIndex, l_HL_PIndex);
                        }
                    }
                }

                OrderManager.AddASNSegments(map, t, l_ASN.EndNodes, string.Empty, ref l_HLIndex, l_HL_PIndex);

                var g = new EdiGroup("SH");
                g.Transactions.Add(t);

                var i = new EdiInterchange();
                i.Groups.Add(g);

                EdiBatch b = new EdiBatch();
                b.Interchanges.Add(i);

                OutboundEDI l_Outbound = new OutboundEDI();

                l_Outbound.UseConnection(string.Empty, p_Order.Connection);

                l_Outbound.Status = "NEW";
                l_Outbound.Data = string.Empty;
                l_Outbound.CreatedBy = p_Order.CreatedBy;
                l_Outbound.CreatedDate = DateTime.Now;
                l_Outbound.OrderId = p_Order.Id;

                if (l_Outbound.SaveNew().IsSuccess)
                {
                    EdiDataWriterSettings settings = new EdiDataWriterSettings(
                    new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                    new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                    new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                    l_EDIInfo.ISAReceiverQual, l_EDIInfo.ISAReceiverId, "12", p_Customer.ISA856ReceiverId, l_EDIInfo.GSReceiverId, p_Customer.ISA856ReceiverId,
                    l_EDIInfo.ISAEdiVersion, l_EDIInfo.GSEdiVersion, l_EDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, l_EDIInfo.SegmentSeparator, l_EDIInfo.ElementSeparator, ";", "^");

                    EdiDataWriter w = new EdiDataWriter(settings);

                    OrderData l_OData = new OrderData();

                    l_OData.UseConnection(string.Empty, p_Order.Connection);

                    l_OData.DeleteWithType(p_Order.Id, l_FileType);

                    l_OData.Type = l_FileType;
                    l_OData.Data = w.WriteToString(b);
                    l_OData.CreatedBy = p_Order.CreatedBy;
                    l_OData.CreatedDate = DateTime.Now;
                    l_OData.OrderId = p_Order.Id;
                    l_OData.OrderNumber = p_Order.OrderNumber;

                    if (l_OData.SaveNew().IsSuccess)
                    {
                        l_Outbound.Data = l_OData.Data;
                        if (l_Outbound.Modify().IsSuccess)
                        {
                            l_Response.EDI = l_OData.Data;
                            l_Response.Code = (int)ResponseCodes.Success;
                            l_Response.Message = "eSyncMate generated successfully!";
                        }
                        else
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = "Cannot generate Store ASN eSyncMate";
                        }
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = "Cannot generate Store ASN eSyncMate";
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Cannot generate Store ASN eSyncMate";
                }
            }
            catch (Exception)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
            }

            return l_Response;
        }

        public static async Task<ResponseModel> MarkStoreOrdersForASN(Customers p_Customer, Orders p_Order, string p_CustomerPO)
        {
            ResponseModel l_Response = new ResponseModel();
            ConnectorDataModel? l_Data = null;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                var l_Connector = p_Customer.Connectors.Where(c => c.ConnectorName == "Mark for ASN" && c.ConnectorType == "ASNs" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                OrderData l_OrderData = p_Order.Files.Where(f => f.Type == "ASN-NS").FirstOrDefault<OrderData>();

                if (l_OrderData == null)
                {
                    return l_Response;
                }

                NetSuiteASNResponseModel l_ASN = JsonConvert.DeserializeObject<NetSuiteASNResponseModel>(l_OrderData.Data);
                NetSuiteASNData l_ASNData = l_ASN.data.Where(d => d.customerPO == p_CustomerPO).FirstOrDefault<NetSuiteASNData>();

                if(l_ASNData == null)
                {
                    return l_Response;
                }

                string responseContent = "";
                string l_JSONSOBODY = "";
                NetSuiteMarkASNResponseModel l_ResponseData = null;

                if (string.IsNullOrEmpty(l_ASNData.fulfillments[0].soId))
                {
                    l_JSONSOBODY = @$"{{
                                        ""id"": ""{l_ASNData.id}"",
                                        ""type"": ""salesorder"",
                                        ""field"": ""custbody5"",
                                        ""value"": ""T""
                                    }},
                                    {{
                                        ""id"": ""{l_ASNData.fulfillments[0].id}"",
                                        ""type"": ""itemfulfillment"",
                                        ""field"": ""custbody5"",
                                        ""value"": ""T""
                                    }}";
                    string l_JSONBODY = @$"[{l_JSONSOBODY}]";

                    request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                    RestResponse response = await client.ExecuteAsync(request);

                    l_ResponseData = JsonConvert.DeserializeObject<NetSuiteMarkASNResponseModel>(response.Content);

                    if (l_ResponseData == null || l_ResponseData.Count == 0)
                    {
                        return l_Response;
                    }

                    responseContent = response.Content;
                }
                else
                {
                    foreach (Fulfillment l_Fulfillment in l_ASNData.fulfillments)
                    {
                        l_JSONSOBODY = @$"{{
                                            ""id"": ""{l_Fulfillment.soId}"",
                                            ""type"": ""salesorder"",
                                            ""field"": ""custbody5"",
                                            ""value"": ""T""
                                        }},
                                        {{
                                            ""id"": ""{l_Fulfillment.id}"",
                                            ""type"": ""itemfulfillment"",
                                            ""field"": ""custbody5"",
                                            ""value"": ""T""
                                        }}";
                        string l_JSONBODY = @$"[{l_JSONSOBODY}]";

                        request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                        RestResponse response = await client.ExecuteAsync(request);

                        l_ResponseData = JsonConvert.DeserializeObject<NetSuiteMarkASNResponseModel>(response.Content);

                        if (l_ResponseData == null || l_ResponseData.Count == 0 || l_ResponseData[0].status != "OK")
                        {
                            return l_Response;
                        }

                        responseContent = response.Content;
                    }
                }

                if (l_ResponseData?[0].status == "OK")
                {
                    OrderStores l_Stores = new OrderStores();

                    l_Stores.UseConnection("", p_Order.Connection);
                    l_Stores.Status = "ASNMARK";
                    if (l_Stores.Modify().IsSuccess)
                    {
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = l_ResponseData[0].message;
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = l_ResponseData[0].message;
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_ResponseData[0].message;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static async Task<ResponseModel> CreateStoreOrderInvoice(Customers p_Customer, Orders p_Order, string p_CustomerPO)
        {
            ResponseModel l_Response = new ResponseModel();
            ConnectorDataModel? l_Data = null;
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                var l_Connector = p_Customer.Connectors.Where(c => c.ConnectorName == "Create Invoice" && c.ConnectorType == "Invoices" && c.ConnectorParty == "NetSuite");

                if (l_Connector != null)
                {
                    l_Data = JsonConvert.DeserializeObject<ConnectorDataModel>(l_Connector.FirstOrDefault<CustomerConnectors>()?.ConnectorData);
                }

                if (l_Data == null)
                {
                    return l_Response;
                }

                var l_NSAuth = OAuth1Authenticator.ForAccessToken(
                                consumerKey: l_Data.ConsumerKey,
                                consumerSecret: l_Data.ConsumerSecret,
                                token: l_Data.Token,
                                tokenSecret: l_Data.TokenSecret,
                                OAuthSignatureMethod.HmacSha256);
                l_NSAuth.Realm = l_Data.Realm;
                RestClientOptions options = new RestClientOptions(l_Data.BaseUrl)
                {
                    MaxTimeout = -1,
                    Authenticator = l_NSAuth,
                };
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest(l_Data.Url, Models.CommonUtils.GetRequestMethod(l_Data.Method));

                foreach (eSyncMate.Processor.Models.Parameter l_Parameter in l_Data.Parmeters)
                {
                    request.AddQueryParameter(l_Parameter.Name, l_Parameter.Value);
                }

                foreach (ConnectorHeader l_Header in l_Data.Headers)
                {
                    request.AddHeader(l_Header.Name, l_Header.Value);
                }

                OrderData l_OrderData = p_Order.Files.Where(f => f.Type == "ASN-NS").FirstOrDefault<OrderData>();

                if (l_OrderData == null)
                {
                    return l_Response;
                }

                NetSuiteASNResponseModel l_ASN = JsonConvert.DeserializeObject<NetSuiteASNResponseModel>(l_OrderData.Data);
                NetSuiteASNData l_ASNData = l_ASN.data.Where(d => d.customerPO == p_CustomerPO).FirstOrDefault<NetSuiteASNData>();

                if (l_ASNData == null)
                {
                    return l_Response;
                }

                var l_Map = p_Customer.Maps.Where(p => p.MapTypeName == "INV Transformation");

                if (l_Map != null)
                {
                    l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                }

                string l_JSONBODY = OrderManager.GenerateInvoiceJSONForStoreOrder(l_ASNData, p_Order, l_TransformationMap);

                request.AddStringBody(l_JSONBODY, Models.CommonUtils.GetRequestBodyFormat(l_Data.BodyFormat));

                RestResponse response = await client.ExecuteAsync(request);

                NetSuiteCreateInvoiceResponseModel l_ResponseData = JsonConvert.DeserializeObject<NetSuiteCreateInvoiceResponseModel>(response.Content);

                if (l_ResponseData == null)
                {
                    return l_Response;
                }

                if (l_ResponseData.status == "OK")
                {
                    OrderStores l_Stores = new OrderStores();

                    l_Stores.UseConnection("", p_Order.Connection);
                    l_Stores.GetObject("CustomerPO", p_CustomerPO);
                    l_Stores.Status = "INVOICED";
                    if (l_Stores.Modify().IsSuccess)
                    {
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = l_ResponseData.message;
                    }
                    else
                    {
                        l_Response.Code = (int)ResponseCodes.Error;
                        l_Response.Message = l_ResponseData.message;
                    }
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_ResponseData.message;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        public static string GenerateInvoiceJSONForStoreOrder(NetSuiteASNData p_ASN, Orders p_Order, string p_TransformationMap)
        {
            string l_Response = string.Empty;

            try
            {
                string jsonData = JsonConvert.SerializeObject(p_ASN);
                string jsonTransformation = new JsonTransformer().Transform(p_TransformationMap, jsonData);
                    
                l_Response = jsonTransformation;
            }
            catch (Exception)
            {
            }

            return l_Response;
        }

        public static void ProcessSyncJob(string p_ConnectionString)
        {
            Orders l_Order = new Orders();
            DataTable l_Data = new DataTable();

            CommonUtils.ConnectionString = p_ConnectionString;

            try
            {
                l_Order.UseConnection(CommonUtils.ConnectionString);

                l_Order.GetList("Status = 'SYNCERROR' OR Status = 'NEW'", "Id", ref l_Data);

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    OrderManager.SyncOrder(PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0));
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                l_Data.Dispose();
            }
        }

        public static void ProcessASNJob(ILogger<OrdersController> logger, string p_ConnectionString)
        {
            Orders l_Order = new Orders();
            DataTable l_Data = new DataTable();

            CommonUtils.ConnectionString = p_ConnectionString;

            try
            {
                l_Order.UseConnection(CommonUtils.ConnectionString);

                l_Order.GetList("Status = 'SYNCED'", "Id", ref l_Data);

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    Task<OrderResponseModel> l_Response = OrderManager.ProcessASN(logger, PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0));
                    l_Response.Start();

                    if (l_Response.Result.Code == (int)ResponseCodes.Success)
                    {
                        OrderManager.MarkOrderForASN(l_Response.Result.Customer, l_Response.Result.Order);
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                l_Data.Dispose();
            }
        }

        public static void MarkASNJob(string p_ConnectionString)
        {
            Orders l_Order = new Orders();
            DataTable l_Data = new DataTable();

            CommonUtils.ConnectionString = p_ConnectionString;

            try
            {
                l_Order.UseConnection(CommonUtils.ConnectionString);

                l_Order.GetList("Status = 'ASNGEN'", "Id", ref l_Data);

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    Task<OrderObjectModel> l_OrderObj = OrderManager.GetOrder(PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0));
                    l_OrderObj.Start();

                    OrderManager.MarkOrderForASN(l_OrderObj.Result.Customer, l_OrderObj.Result.Order);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                l_Data.Dispose();
            }
        }

        public static void CreateInvoiceJob(string p_ConnectionString)
        {
            Orders l_Order = new Orders();
            DataTable l_Data = new DataTable();

            CommonUtils.ConnectionString = p_ConnectionString;

            try
            {
                l_Order.UseConnection(CommonUtils.ConnectionString);

                l_Order.GetList("Status = 'ASNMARK'", "Id", ref l_Data);

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    Task<OrderObjectModel> l_OrderObj = OrderManager.GetOrder(PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0));
                    l_OrderObj.Start();

                    OrderManager.CreateInvoice(l_OrderObj.Result.Customer, l_OrderObj.Result.Order);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                l_Data.Dispose();
            }
        }

        public static void Process810Job(ILogger<OrdersController> logger, string p_ConnectionString)
        {
            Orders l_Order = new Orders();
            DataTable l_Data = new DataTable();

            CommonUtils.ConnectionString = p_ConnectionString;

            try
            {
                l_Order.UseConnection(CommonUtils.ConnectionString);

                l_Order.GetList("Status = 'INVOICED'", "Id", ref l_Data);

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    OrderManager.Process810(logger, PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0));
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                l_Data.Dispose();
            }
        }

        private static MapLoop FindMapLoopDef(MapLoop p_Map, string p_ParentNodeName)
        {
            MapLoop l_MapLoopDef = null;

            l_MapLoopDef = (MapLoop)p_Map.Content.First(s => s.Name == p_ParentNodeName);

            return l_MapLoopDef;
        }

        private static void AddASNSegments(Maps_5010.M_856 p_Map, EdiTrans p_Trans, List<SegmentNode> p_Nodes, string p_ParentNodeName, ref int p_HLIndex, int p_HL_PIndex)
        {
            foreach (SegmentNode l_Node in p_Nodes)
            {
                MapSegment l_SegmentDef = null;

                try
                {
                    if (string.IsNullOrEmpty(p_ParentNodeName))
                        l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                    else
                    {
                        string[] l_ParentNodeNames = p_ParentNodeName.Split("|".ToCharArray());

                        try
                        {
                            l_SegmentDef = (MapSegment)((MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0])).Content.First(s => s.Name == l_Node.Name);
                        }
                        catch (InvalidOperationException)
                        {
                            if (l_ParentNodeNames.Length > 1)
                            {
                                int l_index = 1;
                                MapLoop l_MapLoop = (MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0]);
                                while (l_index < l_ParentNodeNames.Length)
                                {
                                    try
                                    {
                                        l_SegmentDef = (MapSegment)(OrderManager.FindMapLoopDef(l_MapLoop, l_ParentNodeNames[l_index])).Content.First(s => s.Name == l_Node.Name);
                                        break;
                                    }
                                    catch (InvalidCastException)
                                    {
                                    }

                                    l_index++;
                                }
                            }
                        }

                        if (l_SegmentDef == null)
                        {
                            l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                }

                if (l_SegmentDef != null)
                {
                    int l_Index = 0;
                    var l_Segment = new EdiSegment(l_SegmentDef);

                    foreach (string l_Value in l_Node.Data)
                    {
                        string l_Val = l_Value;

                        if (l_Val == "@HL_IDX@")
                        {
                            l_Val = p_HLIndex.ToString();
                            p_HLIndex++;
                        }
                        else if (l_Val == "@HL_P_IDX@")
                        {
                            l_Val = p_HL_PIndex.ToString();
                        }
                        else if (l_Value == "@HLCOUNT@")
                        {
                            l_Val = (p_HLIndex - 1).ToString();
                        }

                        l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Val));
                    }

                    p_Trans.Content.Add(l_Segment);
                }
            }
        }

        private static void Add810Segments(Maps_5010.M_810 p_Map, EdiTrans p_Trans, List<SegmentNode> p_Nodes, string p_ParentNodeName, ref int p_HLIndex, int p_HL_PIndex)
        {
            foreach (SegmentNode l_Node in p_Nodes)
            {
                MapSegment l_SegmentDef = null;

                try
                {
                    if (string.IsNullOrEmpty(p_ParentNodeName))
                        l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                    else
                    {
                        string[] l_ParentNodeNames = p_ParentNodeName.Split("|".ToCharArray());

                        try
                        {
                            l_SegmentDef = (MapSegment)((MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0])).Content.First(s => s.Name == l_Node.Name);
                        }
                        catch (InvalidOperationException)
                        {
                            if (l_ParentNodeNames.Length > 1)
                            {
                                int l_index = 1;
                                MapLoop l_MapLoop = (MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0]);
                                while (l_index < l_ParentNodeNames.Length)
                                {
                                    try
                                    {
                                        l_SegmentDef = (MapSegment)(OrderManager.FindMapLoopDef(l_MapLoop, l_ParentNodeNames[l_index])).Content.First(s => s.Name == l_Node.Name);
                                        break;
                                    }
                                    catch (InvalidCastException)
                                    {
                                    }

                                    l_index++;
                                }
                            }
                        }

                        if (l_SegmentDef == null)
                        {
                            l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                }

                if (l_SegmentDef != null)
                {
                    int l_Index = 0;
                    var l_Segment = new EdiSegment(l_SegmentDef);

                    foreach (string l_Value in l_Node.Data)
                    {
                        string l_Val = l_Value;

                        if (l_Val == "@HL_IDX@")
                        {
                            l_Val = p_HLIndex.ToString();
                            p_HLIndex++;
                        }
                        else if (l_Val == "@HL_P_IDX@")
                        {
                            l_Val = p_HL_PIndex.ToString();
                        }
                        else if (l_Value == "@HLCOUNT@")
                        {
                            l_Val = (p_HLIndex - 1).ToString();
                        }

                        l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Val));
                    }

                    p_Trans.Content.Add(l_Segment);
                }
            }
        }

        private static void Add855Segments(Maps_5010.M_855 p_Map, EdiTrans p_Trans, List<SegmentNode> p_Nodes, string p_ParentNodeName, ref int p_HLIndex, int p_HL_PIndex)
        {
            foreach (SegmentNode l_Node in p_Nodes)
            {
                MapSegment l_SegmentDef = null;

                try
                {
                    if (string.IsNullOrEmpty(p_ParentNodeName))
                        l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                    else
                    {
                        string[] l_ParentNodeNames = p_ParentNodeName.Split("|".ToCharArray());

                        try
                        {
                            l_SegmentDef = (MapSegment)((MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0])).Content.First(s => s.Name == l_Node.Name);
                        }
                        catch (InvalidOperationException)
                        {
                            if (l_ParentNodeNames.Length > 1)
                            {
                                int l_index = 1;
                                while (l_index < l_ParentNodeNames.Length)
                                {
                                    try
                                    {
                                        l_SegmentDef = (MapSegment)((MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[l_index])).Content.First(s => s.Name == l_Node.Name);
                                        break;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                    }

                                    l_index++;
                                }

                                if (l_SegmentDef == null)
                                {
                                    l_index = 1;
                                    MapLoop l_MapLoop = (MapLoop)p_Map.Content.First(s => s.Name == l_ParentNodeNames[0]);
                                    while (l_index < l_ParentNodeNames.Length)
                                    {
                                        try
                                        {
                                            l_SegmentDef = (MapSegment)(OrderManager.FindMapLoopDef(l_MapLoop, l_ParentNodeNames[l_index])).Content.First(s => s.Name == l_Node.Name);
                                            break;
                                        }
                                        catch (InvalidOperationException)
                                        {
                                        }

                                        l_index++;
                                    }
                                }
                            }
                        }

                        if (l_SegmentDef == null)
                        {
                            l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    l_SegmentDef = (MapSegment)p_Map.Content.First(s => s.Name == l_Node.Name);
                }

                if (l_SegmentDef != null)
                {
                    int l_Index = 0;
                    var l_Segment = new EdiSegment(l_SegmentDef);

                    foreach (string l_Value in l_Node.Data)
                    {
                        string l_Val = l_Value;

                        if (l_Val == "@HL_IDX@")
                        {
                            l_Val = p_HLIndex.ToString();
                            p_HLIndex++;
                        }
                        else if (l_Val == "@HL_P_IDX@")
                        {
                            l_Val = p_HL_PIndex.ToString();
                        }
                        else if (l_Value == "@HLCOUNT@")
                        {
                            l_Val = (p_HLIndex - 1).ToString();
                        }

                        l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Val));
                    }

                    p_Trans.Content.Add(l_Segment);
                }
            }
        }
    }
}