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
using Microsoft.SqlServer.Server;
using Nancy;
using static eSyncMate.Processor.Models.SCS_ProductTypeAttributeReponseModel;
using Microsoft.AspNetCore.Mvc;
using static eSyncMate.Processor.Models.SCS_VAPProductCatalogModel;
using static eSyncMate.Processor.Models.SCS_ProductCatalogStatusResponseModel;
using System.Net.Http.Json;
using static eSyncMate.Processor.Models.SCSGetOrderResponseModel;
using Namotion.Reflection;
using static eSyncMate.Processor.Models.PurchaseOrderResponseModel;
using System.Threading;
using static eSyncMate.Processor.Models.WalmartGetOrderResponseModel;
using MySqlX.XDevAPI;
using static Hangfire.Storage.JobStorageFeatures;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Net.Sockets;
using FluentFTP;
using FluentFTP.Exceptions;
using DocumentFormat.OpenXml.Spreadsheet;

namespace eSyncMate.Processor.Managers
{
    public class GetPurchaseOrder850Route
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new DataTable();
            Customers l_Customer = new Customers();
            PurchaseOrders l_Orders = new PurchaseOrders();
            DataTable l_OrderData = new DataTable();
            string jsonString = string.Empty;
            RestResponse sourceResponse = new RestResponse();
            Addresses addresses = new Addresses();
            PurchaseOrderData l_Data = null;
            int lineNo = 0;

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

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
                l_Customer.GetObject("ERPCustomerID", l_SourceConnector.CustomerID);
                l_Orders.UseConnection(l_DestinationConnector.ConnectionString);
             
                List<PurchaseOrders> PurchaseOrders = l_Orders.GetNewPurchaseOrders();

                if (PurchaseOrders == null || !PurchaseOrders.Any()) 
                {
                    return; 
                }

                foreach (PurchaseOrders order in PurchaseOrders)
                {
                    jsonString = JsonConvert.SerializeObject(order);

                    l_Data = new PurchaseOrderData();

                    l_Data.UseConnection(l_DestinationConnector.ConnectionString);
                    l_Data.DeleteWithType(order.Id, "API-JSON");

                    l_Data.Type = "API-JSON";
                    l_Data.Data = jsonString;
                    l_Data.CreatedBy = userNo;
                    l_Data.CreatedDate = DateTime.Now;
                    l_Data.OrderId = order.Id;
                    l_Data.OrderNumber = order.PONumber;

                    l_Data.SaveNew();

                    eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                    string l_TransformationMap = string.Empty;

                    map.UseConnection(l_DestinationConnector.ConnectionString);
                    map.GetObject(route.MapId);

                    l_TransformationMap = map.Map;

                    PurchaseOrders l_PurchaseOrders = new PurchaseOrders();

                    l_PurchaseOrders.UseConnection(l_DestinationConnector.ConnectionString);

                    List<PurchaseOrders> l_PO = l_PurchaseOrders.GetNewPurchaseOrders(order.SupplierID, order.PONumber);

                    foreach (PurchaseOrders item in l_PO)
                    {
                        string jsonData = item.Files.Where(f => f.Type == "API-JSON").ToArray()[0].Data;
                        string jsonTransformation = new JsonTransformer().Transform(l_TransformationMap, jsonData);

                        PurchaseOrderData l_OData = new PurchaseOrderData();

                        l_OData.UseConnection(l_DestinationConnector.ConnectionString);

                        l_OData.DeleteWithType(item.Id, "850-JSON");

                        l_OData.Type = "850-JSON";
                        l_OData.Data = jsonTransformation;
                        l_OData.CreatedBy = userNo;
                        l_OData.CreatedDate = DateTime.Now;
                        l_OData.OrderId = item.Id;
                        l_OData.OrderNumber = item.PONumber;

                        if (l_OData.SaveNew().IsSuccess)
                        {
                            var generator = new _850EDIFileGenerator();
                            string ediFileContent = generator.GenerateEDIFile(jsonTransformation);

                            l_OData = new PurchaseOrderData();

                            l_OData.UseConnection(l_DestinationConnector.ConnectionString);

                            l_OData.DeleteWithType(item.Id, "850-EDI");

                            l_OData.Type = "850-EDI";
                            l_OData.Data = ediFileContent;
                            l_OData.CreatedBy = userNo;
                            l_OData.CreatedDate = DateTime.Now;
                            l_OData.OrderId = item.Id;
                            l_OData.OrderNumber = item.PONumber;

                            l_OData.SaveNew();

                            FtpConnector.Execute(l_SourceConnector.Host, l_SourceConnector.ConsumerKey, l_SourceConnector.ConsumerSecret, l_SourceConnector.Method, false, l_OData.OrderNumber, ediFileContent);
                        }

                        l_Orders.UpdatePoStatus(order.PONumber, "SYNCED", l_DestinationConnector.ConnectionString);
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_data.Dispose();
                l_OrderData.Dispose();
            }
        }
    }
}
