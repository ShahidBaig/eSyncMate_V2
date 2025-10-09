using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing;
using EdiEngine.Runtime;
using EdiEngine;
using JUST;
using eSyncMate.DB.Entities;
using System.Reflection;
using eSyncMate.Processor.Models;
using eSyncMate.Processor.Managers;
using System.Data;
using eSyncMate.DB;
using Nancy;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using RestSharp;
using System.Security.Claims;
using Intercom.Data;

namespace eSyncMate.Processor.Controllers
{
    [ApiController]
    [Route("EDIProcessor/api/v1/orders")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IConfiguration _config;

        public OrdersController(IConfiguration config, ILogger<OrdersController> logger, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
        {
            _config = config;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
        }

        [HttpPost]
        [Route("process850")]
        public async Task<OrderResponseModel> ProcessEDIFile(IFormFile file)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            string l_TransformationMap = string.Empty;
            string l_DBFieldsMap = string.Empty;
            string l_EDIData = string.Empty;
            int l_SystemUser = 1;

            OrderResponseModel l_Response = new OrderResponseModel();
            Customers l_Customer = new Customers();
            EdiDataReader r = new EdiDataReader();
            InboundEDI l_EDI = new InboundEDI();

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                if (file.Length <= 0)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Please provide a eSyncMate data file.");

                    l_Response.Message = "Please provide a eSyncMate data file";
                    return l_Response;
                }

                using (StreamReader reader = new StreamReader(file.OpenReadStream()))
                {
                    l_EDIData = await reader.ReadToEndAsync();
                }

                l_EDI.UseConnection(CommonUtils.ConnectionString);

                l_EDI.Type = "850";
                l_EDI.Status = "NEW";
                l_EDI.Data = l_EDIData;
                l_EDI.CreatedBy = l_SystemUser;
                l_EDI.CreatedDate = DateTime.Now;

                l_EDI.SaveNew();

                EdiBatch b = r.FromString(l_EDIData);

                l_Customer.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring processing eSyncMate data.");
                foreach (EdiInterchange i in b.Interchanges)
                {
                    if (!l_Customer.GetObject("ISACustomerID", i.ISA.Content[5].Val.ToString().Trim()).IsSuccess)
                    {
                        this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - The party [{i.ISA.Content[5].Val.ToString().Trim()}] is not registered.");

                        l_EDI.Status = "ERROR";
                        l_EDI.Modify();

                        l_Response.Message = $"The party [{i.ISA.Content[5].Val.ToString().Trim()}] is not registered";
                        return l_Response;
                    }

                    var l_Map = l_Customer.Maps.Where(p => p.MapTypeName == "850 Transformation");

                    if (l_Map != null)
                    {
                        l_TransformationMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                    }

                    l_Map = l_Customer.Maps.Where(p => p.MapTypeName == "850 DB Fields");

                    if (l_Map != null)
                    {
                        l_DBFieldsMap = l_Map.FirstOrDefault<CustomerMaps>()?.Map;
                    }

                    if (string.IsNullOrEmpty(l_TransformationMap) || string.IsNullOrEmpty(l_DBFieldsMap))
                    {
                        this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Required maps for 850 processing are missing for [{l_Customer.Name}].");

                        l_EDI.Status = "ERROR";
                        l_EDI.Modify();

                        l_Response.Message = $"Required maps for 850 processing are missing for [{l_Customer.Name}]";
                        return l_Response;
                    }

                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - eSyncMate Customer with required Maps found.");

                    InboundEDIInfo l_EDIInfo = new InboundEDIInfo();

                    l_EDIInfo.InboundEDIId = l_EDI.Id;
                    l_EDIInfo.ISASenderQual = i.ISA.Content[4].Val.ToString().Trim();
                    l_EDIInfo.ISASenderId = i.ISA.Content[5].Val.ToString().Trim();
                    l_EDIInfo.ISAReceiverQual = i.ISA.Content[6].Val.ToString().Trim();
                    l_EDIInfo.ISAReceiverId = i.ISA.Content[7].Val.ToString().Trim();
                    l_EDIInfo.ISAEdiVersion = i.ISA.Content[11].ToString().Trim();
                    l_EDIInfo.ISAUsageIndicator = i.ISA.Content[14].ToString().Trim();
                    l_EDIInfo.ISAControlNumber = i.ISA.Content[12].ToString().Trim();
                    l_EDIInfo.SegmentSeparator = i.SegmentSeparator;
                    l_EDIInfo.ElementSeparator = i.ElementSeparator;
                    l_EDIInfo.CreatedBy = l_SystemUser;
                    l_EDIInfo.CreatedDate = DateTime.Now;

                    l_EDIInfo.UseConnection(CommonUtils.ConnectionString);

                    l_Response.Orders = new List<OrderModel>();

                    foreach (EdiGroup g in i.Groups)
                    {
                        l_EDIInfo.GSSenderId = g.GS.Content[1].ToString().Trim();
                        l_EDIInfo.GSReceiverId = g.GS.Content[2].ToString().Trim();
                        l_EDIInfo.GSControlNumber = g.GS.Content[5].ToString().Trim();
                        l_EDIInfo.GSEdiVersion = g.GS.Content[7].ToString().Trim();

                        l_EDIInfo.SaveNew();

                        foreach (EdiTrans t in g.Transactions)
                        {
                            this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Maps.");
                            OrderTransformationResponseModel l_OrderData = OrderManager.ParseOrder(l_Customer, t, l_TransformationMap, l_DBFieldsMap);

                            if (l_OrderData.Code != (int)ResponseCodes.Success)
                            {
                                this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_OrderData.Message}");

                                l_Response.Code = l_OrderData.Code;
                                l_Response.Message = l_OrderData.Message;

                                return l_Response;
                            }

                            l_OrderData.EDI = l_EDIData;
                            l_OrderData.SystemUser = l_SystemUser;

                            this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Maps executed successfully.");
                            this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing DB Operations.");

                            OrderSaveResponseModel l_OrderResponse = OrderManager.SaveOrder(l_EDI, l_Customer, l_OrderData);

                            if (l_OrderResponse.Code != (int)ResponseCodes.Success)
                            {
                                this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_OrderResponse.Message}");

                                l_Response.Code = l_OrderResponse.Code;
                                l_Response.Message = l_OrderResponse.Message;

                                return l_Response;
                            }

                            this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - DB Operations completed successfully.");
                            this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Third Party Sync.");

                            OrderSyncResponseModel l_SyncResponse = await OrderManager.SyncOrder(l_Customer, l_OrderResponse.OrderId);

                            if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                            {
                                this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");
                            }
                            else
                            {
                                l_Response.Orders.Add(new OrderModel { OrderId = l_OrderResponse.OrderId });
                                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Third Party Sync completed successfully.");
                            }
                        }
                    }
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Order processed successfully!";

                l_EDI.Status = "PROCESSED";
                l_EDI.Modify();

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - eSyncMate data processing completed successfully.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;

                l_EDI.Status = "EXCEPTION";
                l_EDI.Modify();

                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("syncOrder")]
        public async Task<OrderResponseModel> SyncOrder(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            Orders l_Order = new Orders();

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Third Party Sync.");

                OrderSyncResponseModel l_SyncResponse = await OrderManager.SyncOrder(OrderId);

                if (l_Response.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Third Party Sync completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Order processed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("syncOrderStore")]
        public async Task<OrderStoreResponseModel> SyncOrderStore(int OrderStoreId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderStoreResponseModel l_Response = new OrderStoreResponseModel();
            OrderStores l_OrderStore = new OrderStores();

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_OrderStore.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Third Party Sync.");

                OrderStoreSyncResponseModel l_SyncResponse = await OrderManager.SyncOrderStore(OrderStoreId);

                if (l_Response.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                //l_OrderStore.GetObject(OrderStoreId);
                //l_OrderStore.UpdateOrderStatus(l_OrderStore.OrderId);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Third Party Sync completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Order processed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getOrderStores")]
        public async Task<GetOrderStoresResponseModel> GetOrderStores(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetOrderStoresResponseModel l_Response = new GetOrderStoresResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                OrderStores l_Order = new OrderStores();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Order.UseConnection(CommonUtils.ConnectionString);

                if (OrderId > 0)
                {
                    l_Criteria = $"OrderId = {OrderId}";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring order stores search.");

                l_Order.GetList(l_Criteria, string.Empty, ref l_Data, "Id ASC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Order Stores searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating order stores.");

                l_Response.OrderStores = new List<OrderStoreDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    OrderStoreDataModel l_OrderRow = new OrderStoreDataModel();

                    DBEntity.PopulateObjectFromRow(l_OrderRow, l_Data, l_Row);

                    l_Response.OrderStores.Add(l_OrderRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Order Stores fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Order Stores are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpPost]
        [Route("process855")]
        public async Task<OrderResponseModel> Generate855EDI(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            OrderObjectModel l_Order = new OrderObjectModel();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing 855 eSyncMate for Order [{OrderId}].");

                ResponseModel l_SyncResponse = await OrderManager.Process855(this._logger, OrderId);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Generate 855 eSyncMate completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "855 eSyncMate completed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("process856")]
        public async Task<OrderResponseModel> Process856ForOrder(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Response = await OrderManager.ProcessASN(this._logger, OrderId);
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("markFor856")]
        public async Task<OrderResponseModel> MarkForASN(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            OrderObjectModel? l_Order = null;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order = await OrderManager.GetOrder(OrderId);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Mark for ASN for Order [{OrderId}].");

                ResponseModel l_SyncResponse = await OrderManager.MarkOrderForASN(l_Order.Customer, l_Order.Order);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Mark for ASN completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Order marked for ASN successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("createInvoice")]
        public async Task<OrderResponseModel> CreateInvoice(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            OrderObjectModel l_Order = new OrderObjectModel();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_Order = await OrderManager.GetOrder(OrderId);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Create Invoice for Order [{OrderId}].");

                ResponseModel l_SyncResponse = await OrderManager.CreateInvoice(l_Order.Customer, l_Order.Order);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Create Invoice completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Invoice created successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("process810")]
        public async Task<OrderResponseModel> Generate810EDI(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            OrderObjectModel l_Order = new OrderObjectModel();
            string l_TransformationMap = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing 810 eSyncMate for Order [{OrderId}].");

                ResponseModel l_SyncResponse = await OrderManager.Process810(this._logger, OrderId);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Generate 810 eSyncMate completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "810 eSyncMate completed successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getOrders/{OrderId}/{FromDate}/{ToDate}/{OrderNumber}/{Status}/{ExternalId}/{CustomerId}")]
        public async Task<GetOrdersResponseModel> GetOrders(int? OrderId = 0, string FromDate = "", string ToDate = "", string OrderNumber = "", string Status = "", string ExternalId = "", string CustomerId = "")
        {
            GetOrdersResponseModel l_Response = new GetOrdersResponseModel();
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();

            UsersClaimData userData = new UsersClaimData();

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Invalid token: Not Authorized";

                return l_Response;
            }

            userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

            try
            {
                string l_Criteria = string.Empty;
                Orders l_Order = new Orders();

                // Handle default values
                FromDate = FromDate == "1999-01-01" ? string.Empty : FromDate;
                ToDate = ToDate == "1999-01-01" ? string.Empty : ToDate;
                OrderNumber = OrderNumber == "EMPTY" ? string.Empty : OrderNumber;
                Status = Status == "EMPTY" ? string.Empty : Status;
                ExternalId = ExternalId == "EMPTY" ? string.Empty : ExternalId;
                CustomerId = CustomerId == "EMPTY" ? ((!string.IsNullOrEmpty(userData.Customers) && userData.UserType?.ToUpper() != "ADMIN" ? userData.Customers : string.Empty)) : CustomerId;

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Order.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"Status <> 'DELETED'";

                if (OrderId > 0)
                {
                    l_Criteria += $" AND Id = {OrderId.Value}";
                }

                if (!string.IsNullOrEmpty(OrderNumber))
                {
                    l_Criteria += $" AND OrderNumber LIKE '%{OrderNumber}%'";
                }

                if (!string.IsNullOrEmpty(Status))
                {
                    l_Criteria += $" AND Status LIKE '%{Status}%'";
                }

                if (!string.IsNullOrEmpty(FromDate))
                {
                    l_Criteria += $" AND CONVERT(DATE, OrderDate) >= '{FromDate}'";
                }

                if (!string.IsNullOrEmpty(ToDate))
                {
                    l_Criteria += $" AND CONVERT(DATE, OrderDate) <= '{ToDate}'";
                }

                if (!string.IsNullOrEmpty(ExternalId))
                {
                    l_Criteria += $" AND ExternalId LIKE '%{ExternalId}%'";
                }

                if (!string.IsNullOrEmpty(CustomerId))
                {
                    CustomerId = !CustomerId.StartsWith("'") ? $"'{CustomerId}'" : CustomerId;
                    l_Criteria += $" AND ERPCustomerID IN ({CustomerId})";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring order search.");

                l_Order.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Orders searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating orders.");


                l_Response.OrdersData = l_Data;
                //l_Response.Orders = new List<OrderDataModel>();
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    OrderDataModel l_OrderRow = new OrderDataModel();

                //    DBEntity.PopulateObjectFromRow(l_OrderRow, l_Data, l_Row);

                //    l_Response.Orders.Add(l_OrderRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Orders fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Orders are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getOrderFiles/{OrderId}")]
        public async Task<GetOrderFilesResponseModel> GetOrderFiles(int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetOrderFilesResponseModel l_Response = new GetOrderFilesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                OrderData l_OrderData = new OrderData();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_OrderData.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"OrderId = {OrderId}";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring order files search.");

                l_OrderData.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Order files searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating order files.");

                l_Response.Files = new List<OrderFileModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    OrderFileModel l_OrderFile = new OrderFileModel();

                    DBEntity.PopulateObjectFromRow(l_OrderFile, l_Data, l_Row);

                    l_Response.Files.Add(l_OrderFile);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Order Files fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Order Files are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpPost]
        [Route("process856Store")]
        public async Task<OrderResponseModel> Process856ForStoreOrders(IFormFile file, int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();
            string l_CSVData = string.Empty;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                if (file.Length <= 0)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Please provide a csv data file.");

                    l_Response.Message = "Please provide a csv data file";
                    return l_Response;
                }

                using (StreamReader reader = new StreamReader(file.OpenReadStream()))
                {
                    l_CSVData = await reader.ReadToEndAsync();
                }

                string[] columnNames = { "SONo", "CustomerPO", "Dept", "VendorStyle", "UPC", "StoreNo", "Qty", "DC", "TrackingNo", "ShipDate", "OrderDate", "TotalCarton", "Carrier", "VendorID", "BOL" };
                DataTable l_Data = CommonUtils.ConvertCSVToDataTable(l_CSVData, columnNames);

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    string l_TrackingNo = PublicFunctions.ConvertNullAsString(l_Row["TrackingNo"], string.Empty);
                    string l_DC = PublicFunctions.ConvertNullAsString(l_Row["DC"], string.Empty);
                    string l_StoreNo = PublicFunctions.ConvertNullAsString(l_Row["StoreNo"], string.Empty);
                    string l_Dept = PublicFunctions.ConvertNullAsString(l_Row["Dept"], string.Empty);

                    l_DC = l_DC.PadLeft(4, '0');
                    l_StoreNo = l_StoreNo.PadLeft(4, '0');
                    l_Dept = l_Dept.PadLeft(4, '0');
                    l_TrackingNo = l_TrackingNo.Replace(" ", "").Replace("(", "").Replace(")", "");

                    l_Row["TrackingNo"] = l_TrackingNo;
                    l_Row["DC"] = l_DC;
                    l_Row["StoreNo"] = l_StoreNo;
                    l_Row["Dept"] = l_Dept;
                }

                l_Data.AcceptChanges();

                l_Response = await OrderManager.ProcessASNStore(this._logger, OrderId, l_Data);

                l_Data.Dispose();
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("process810Store")]
        public async Task<OrderResponseModel> Process810ForStoreOrders(IFormFile file, int OrderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderResponseModel l_Response = new OrderResponseModel();

            l_Response.Code = (int)ResponseCodes.Error;

            //try
            //{
            //    l_Response = await OrderManager.ProcessASN(this._logger, OrderId);
            //}
            //catch (Exception ex)
            //{
            //    l_Response.Code = (int)ResponseCodes.Exception;
            //    this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            //}

            return l_Response;
        }

        [HttpPost]
        [Route("markSendOrderStore")]
        public async Task<OrderStoreResponseModel> MarkStoreOrders(int OrderStoreId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderStoreResponseModel l_Response = new OrderStoreResponseModel();
            OrderStores l_OrderStore = new OrderStores();
            OrderObjectModel? l_Order = null;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_OrderStore.UseConnection(CommonUtils.ConnectionString);
                l_OrderStore.GetObject(OrderStoreId);

                l_Order = await OrderManager.GetOrder(l_OrderStore.OrderId);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Mark for ASN for Store Order [{l_OrderStore.Id}].");

                ResponseModel l_SyncResponse = await OrderManager.MarkStoreOrdersForASN(l_Order.Customer, l_Order.Order, l_OrderStore.CustomerPO);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Mark for ASN completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Store Order marked for ASN successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("createInvoiceOrderStore")]
        public async Task<OrderStoreResponseModel> CreateInvoiceStoreOrder(int OrderStoreId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            OrderStoreResponseModel l_Response = new OrderStoreResponseModel();
            OrderStores l_OrderStore = new OrderStores();
            OrderObjectModel? l_Order = null;

            l_Response.Code = (int)ResponseCodes.Error;

            try
            {
                l_OrderStore.UseConnection(CommonUtils.ConnectionString);
                l_OrderStore.GetObject(OrderStoreId);

                l_Order = await OrderManager.GetOrder(l_OrderStore.OrderId);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Create Invoice for Store Order [{l_OrderStore.Id}].");

                ResponseModel l_SyncResponse = await OrderManager.CreateStoreOrderInvoice(l_Order.Customer, l_Order.Order, l_OrderStore.CustomerPO);

                if (l_SyncResponse.Code != (int)ResponseCodes.Success)
                {
                    this._logger.LogError($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {l_SyncResponse.Message}");

                    l_Response.Code = l_SyncResponse.Code;
                    l_Response.Message = l_SyncResponse.Message;

                    return l_Response;
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Create Invoice completed successfully.");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Store Order Invoice created successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("setOrderStatus")]
        public async Task<ResponseModel> SetOrderStatus(int OrderId, string Status)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ResponseModel l_Response = new ResponseModel();

            try
            {
                Orders order = new Orders();
                string l_Criteria = string.Empty;

                l_Response.Code = (int)ResponseCodes.Error;

                order.UseConnection(CommonUtils.ConnectionString);

                Result l_Result = order.SetOrderStatus(OrderId, Status);

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "Order status updated successfully!";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("processOrderForShipment")]
        public async Task<ResponseModel> ProcessOrderForShipment(string OrderNumber)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ResponseModel l_Response = new ResponseModel();

            try
            {
                return ASNShipmentNotificationRoute.ExecuteShipment(OrderNumber);
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getOrderDetail")]
        public async Task<OrderDetailResponseModel> GetOrderDetail(int OrderID)
        {
            {
                OrderDetailResponseModel l_Response = new OrderDetailResponseModel();
                MethodBase l_Me = MethodBase.GetCurrentMethod();
                DataTable l_Data = new DataTable();

                try
                {
                    string l_Criteria = $" OrderId = {OrderID}";
                    Orders l_Order = new Orders();

                    // Handle default values
                    l_Response.Code = (int)ResponseCodes.Error;
                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                    l_Order.UseConnection(CommonUtils.ConnectionString);

                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring order search.");

                    l_Order.GetDetailData(l_Criteria, ref l_Data);

                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Order Details searched {{{l_Data.Rows.Count}}}.");
                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating orders.");


                    l_Response.DetailData = l_Data;

                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "Order Detail fetched successfully!";

                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Order Details are ready.");
                }
                catch (Exception ex)
                {
                    l_Response.Code = (int)ResponseCodes.Exception;
                    this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
                }
                finally
                {
                    l_Data.Dispose();
                }

                return l_Response;
            }
        }

        [HttpPost]
        [Route("updateSalesOrder")]
        public async Task<ResponseModel> UpdateSalesOrder([FromBody] OrderStatusModel orderModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ResponseModel l_Response = new ResponseModel();
            Result? l_Result = new Result();

            try
            {
                Orders order = new Orders();
                string l_Criteria = string.Empty;

                l_Response.Code = (int)ResponseCodes.Error;

                order.UseConnection(CommonUtils.ConnectionString);

                l_Result = order.SetOrderStatus(orderModel.Id, orderModel.Status);

                if (l_Result.IsSuccess)
                {
                    if (!string.IsNullOrEmpty(orderModel.Status) && (orderModel.Status.ToUpper() == "NEW" || orderModel.Status.ToUpper() == "ERROR"))
                    {
                        l_Result = null;

                        l_Result = order.UpdateShippingInfo(orderModel.Id, orderModel.ShipToAddress1, orderModel.ShipToAddress2, orderModel.ShipToCity, orderModel.ShipToState, orderModel.ShipToZip, orderModel.ShipToCountry, orderModel.ShipToName);

                        if (l_Result.IsSuccess)
                        {
                            l_Result = null;

                            l_Result = order.UpdateShippingAddress(orderModel.Id, orderModel.ShipToAddress1, orderModel.ShipToAddress2, orderModel.ShipToCity, orderModel.ShipToState, orderModel.ShipToZip, orderModel.ShipToCountry, orderModel.ShipToName);
                        }
                    }

                    l_Response.Code = l_Result.IsSuccess ? (int)ResponseCodes.Success : (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.IsSuccess ? "Order status updated successfully!" : l_Result.Description;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message.ToString();
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("reprocessOrder")]
        public IActionResult ReprocessOrder(int OrderId, string CustomerName, string Status, string OrderNumber, int ASNError, int isASNError = 0)
        {
            string result = string.Empty;
            Orders l_order = new Orders();
            l_order.UseConnection(CommonUtils.ConnectionString);

            if (isASNError == 1)
            {
                return BadRequest(new { code = 400, message = $"Failed to set order status for OrderId {OrderId}" });
            }

            if (OrderId <= 0)
                return BadRequest(new { code = 400, message = "Invalid orderId" });

            if (Status == "ACKERROR")
            {
                if (CustomerName == "WAL4001MP")
                {
                    result = WalmartGetOrdersRoute.ExecuteSingle(_config, OrderId, CustomerName, OrderNumber);
                }

                result = SCSGetOrders.ExecuteSingle(_config, OrderId, CustomerName, OrderNumber);

                if (string.IsNullOrEmpty(result))
                {
                    return Ok(new { code = 200, message = $"Order {OrderId} reprocessed successfully." });
                }
                else
                {
                    return BadRequest(new { code = 400, message = $"Order {OrderId} failed to process: {result}" });
                }
            }
            else if (Status == "ASNERROR")
            {
                Result? l_ResultSYNCED = l_order.SetOrderStatusSync(OrderId, "SYNCED");

                if (l_ResultSYNCED.IsSuccess)
                {
                    return Ok(new { code = 200, message = $"Order {OrderId} reprocessed successfully." });
                }
                else
                {
                    return BadRequest(new { code = 400, message = $"Order {OrderId} failed to process: {result}" });
                }
            }

            Result? l_Result = l_order.SetOrderStatus(OrderId, "NEW");

            if (l_Result.IsSuccess)
            {
                result = SCSPlaceOrderRoute.ExecuteSingle(_config, OrderId, CustomerName);

                if (string.IsNullOrEmpty(result))
                {
                    return Ok(new { code = 200, message = $"Order {OrderId} reprocessed successfully." });
                }
                else
                {
                    return BadRequest(new { code = 400, message = $"Order {OrderId} failed to process: {result}" });
                }
            }

            return BadRequest(new { code = 400, message = $"Failed to set order status for OrderId {OrderId}: {l_Result?.Description ?? "Unknown error"}" });
        }
    }
}
