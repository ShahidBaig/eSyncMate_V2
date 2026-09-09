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
        [Route("getDashboardStats")]
        public Task<object> GetDashboardStats([FromQuery] string fromDate = "", [FromQuery] string toDate = "", [FromQuery] string erpCustomerID = "")
        {
            try
            {
                var claimsIdentity = User.Identity as ClaimsIdentity;
                if (claimsIdentity?.Claims == null)
                    return Task.FromResult<object>(new { code = 401, message = "Not Authorized" });

                var userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

                DBConnector l_Conn = new DBConnector(CommonUtils.ConnectionString);

                // All three breakdowns come from Sp_GetDashboardStats in a single round trip.
                // The SQL lives in the proc so the status-to-event mapping can be tuned on the
                // server without a Processor redeploy — see
                // scripts/eSyncmateScripts/2026-07-30/81_Create_Sp_GetDashboardStats.sql.
                //
                // Each status now reports the date of the event it actually means (ASN sent /
                // created in ERP / cancelled) instead of the ingestion date, which was the same
                // value on every tile. Counts are still scoped to Orders.CreatedDate so they keep
                // matching the drilldown; only the date comes from the event window.
                // Analysis: TaskManagement/Dashboard-StatusWise-Dates-Analysis.html
                string l_AllowedCustomers = "";
                if (!userData.IsSuperAdmin && !string.IsNullOrEmpty(userData.Customers))
                    l_AllowedCustomers = userData.Customers.Replace("'", "");   // the claim arrives pre-quoted

                string l_Param = string.Empty;
                string l_Query = "EXEC [dbo].[Sp_GetDashboardStats] ";

                PublicFunctions.FieldToParam(fromDate ?? string.Empty, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " @p_FromDate = " + l_Param;

                PublicFunctions.FieldToParam(toDate ?? string.Empty, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", @p_ToDate = " + l_Param;

                PublicFunctions.FieldToParam(l_AllowedCustomers, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", @p_AllowedCustomers = " + l_Param;

                PublicFunctions.FieldToParam(erpCustomerID ?? string.Empty, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", @p_ERPCustomerIDs = " + l_Param;

                DataSet l_DS = new DataSet();
                l_Conn.GetDataSP(l_Query, ref l_DS);

                // 'EVENT' when the date is the status's own event timestamp, 'CREATED' when the
                // proc fell back to Orders.CreatedDate. The UI needs this so a tile label cannot
                // claim "last shipment" over what is really an ingestion date. Guarded by column
                // presence so an older proc version does not break the endpoint.
                Func<DataRow, string> l_Basis = row =>
                    row.Table.Columns.Contains("LastOrderDateBasis") ? row["LastOrderDateBasis"]?.ToString() : "";

                // Result set 1 — customer-wise counts
                var customerWise = new List<object>();
                int totalOrders = 0;
                DateTime? totalLastOrderDate = null;
                if (l_DS.Tables.Count > 0)
                {
                    foreach (DataRow row in l_DS.Tables[0].Rows)
                    {
                        int count = Convert.ToInt32(row["OrderCount"]);
                        totalOrders += count;
                        DateTime? l_Lod = row["LastOrderDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["LastOrderDate"]);
                        if (l_Lod.HasValue && (!totalLastOrderDate.HasValue || l_Lod > totalLastOrderDate)) totalLastOrderDate = l_Lod;
                        customerWise.Add(new { customerName = row["CustomerName"]?.ToString(), erpCustomerID = row["ERPCustomerID"]?.ToString(), orderCount = count, lastOrderDate = l_Lod, lastOrderDateBasis = l_Basis(row) });
                    }
                }

                // Result set 2 — status-wise counts, grouped by the effective (display) status so
                // 'Partially Shipped'/'Partially Cancelled' count correctly and match the drilldown
                var statusWise = new List<object>();
                if (l_DS.Tables.Count > 1)
                {
                    foreach (DataRow row in l_DS.Tables[1].Rows)
                    {
                        statusWise.Add(new { status = row["Status"]?.ToString(), statusCount = Convert.ToInt32(row["StatusCount"]), lastOrderDate = row["LastOrderDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["LastOrderDate"]), lastOrderDateBasis = l_Basis(row) });
                    }
                }

                // Result set 3 — partner + status breakdown (for expandable detail)
                var partnerStatusWise = new Dictionary<string, List<object>>();
                if (l_DS.Tables.Count > 2)
                {
                    foreach (DataRow row in l_DS.Tables[2].Rows)
                    {
                        string custId = row["ERPCustomerID"]?.ToString() ?? "";
                        if (!partnerStatusWise.ContainsKey(custId))
                            partnerStatusWise[custId] = new List<object>();
                        partnerStatusWise[custId].Add(new { status = row["Status"]?.ToString(), statusCount = Convert.ToInt32(row["StatusCount"]), lastOrderDate = row["LastOrderDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["LastOrderDate"]), lastOrderDateBasis = l_Basis(row) });
                    }
                }

                l_DS.Dispose();

                return Task.FromResult<object>(new { code = 200, message = "Success", customerWise, statusWise, totalOrders, totalLastOrderDate, partnerStatusWise });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new { code = 500, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("getPartnerStatusBreakdown")]
        public Task<object> GetPartnerStatusBreakdown([FromQuery] string erpCustomerID = "", [FromQuery] string fromDate = "", [FromQuery] string toDate = "")
        {
            try
            {
                var claimsIdentity = User.Identity as ClaimsIdentity;
                if (claimsIdentity?.Claims == null)
                    return Task.FromResult<object>(new { code = 401, message = "Not Authorized" });

                DBConnector l_Conn = new DBConnector(CommonUtils.ConnectionString);

                string l_DateFilter;
                if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                    l_DateFilter = $" CreatedDate >= '{fromDate}' AND CreatedDate < DATEADD(DAY, 1, CAST('{toDate}' AS DATE))";
                else
                    l_DateFilter = " CreatedDate >= DATEADD(HOUR, -24, GETDATE())";

                string l_Query = $@"SELECT ISNULL(Status,'') as Status, COUNT(*) as StatusCount
                    FROM VW_Orders
                    WHERE {l_DateFilter} AND Status <> 'DELETED' AND ERPCustomerID = '{erpCustomerID}'
                    GROUP BY Status ORDER BY StatusCount DESC";

                DataTable l_DT = new DataTable();
                l_Conn.GetData(l_Query, ref l_DT);
                var statuses = new List<object>();
                foreach (DataRow row in l_DT.Rows)
                {
                    statuses.Add(new { status = row["Status"]?.ToString(), statusCount = Convert.ToInt32(row["StatusCount"]) });
                }

                return Task.FromResult<object>(new { code = 200, statuses });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new { code = 500, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("getDashboardOrders")]
        public async Task<GetOrdersResponseModel> GetDashboardOrders([FromQuery] string status = "", [FromQuery] string erpCustomerID = "", [FromQuery] string fromDate = "", [FromQuery] string toDate = "", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            GetOrdersResponseModel l_Response = new GetOrdersResponseModel();
            DataTable l_Data = new DataTable();

            var claimsIdentity = User.Identity as ClaimsIdentity;
            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Not Authorized";
                return l_Response;
            }

            var userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

            try
            {
                Orders l_Order = new Orders();
                l_Order.UseConnection(CommonUtils.ConnectionString);

                string l_Criteria = "Status <> 'DELETED'";

                // Customer filter (user-assigned + optional specific customer)
                string l_CustomerFilter = "";
                if (!userData.IsSuperAdmin && !string.IsNullOrEmpty(userData.Customers))
                    l_CustomerFilter = $" AND ERPCustomerID IN ({userData.Customers})";

                if (!string.IsNullOrEmpty(erpCustomerID))
                    l_CustomerFilter += $" AND ERPCustomerID = '{erpCustomerID}'";

                l_Criteria += l_CustomerFilter;

                // Date filter on CreatedDate (matches dashboard stats).
                // A date-only bound means "the whole of that day", so it moves to the next
                // midnight. A bound carrying a time is an exact instant and is used as-is —
                // that is what makes the 24H preset mean 24 hours here too, so the drilldown
                // returns the same population as the tile it was opened from.
                if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                    l_Criteria += $" AND CreatedDate >= CAST('{fromDate}' AS DATETIME)"
                                + $" AND CreatedDate < CASE WHEN CAST('{toDate}' AS DATETIME) = CAST(CAST('{toDate}' AS DATETIME) AS DATE)"
                                + $" THEN DATEADD(DAY, 1, CAST('{toDate}' AS DATETIME)) ELSE CAST('{toDate}' AS DATETIME) END";

                // Status filter — match the effective (display) status, consistent with the dashboard tiles/breakdown
                if (!string.IsNullOrEmpty(status))
                    l_Criteria += $" AND ISNULL(NULLIF(DisplayStatus,''), Status) = '{status}'";

                int totalCount = 0;
                l_Order.GetViewListPagedWithErpInfo(l_Criteria, ref l_Data, "CreatedDate DESC", pageNumber, pageSize, out totalCount);

                l_Response.OrdersData = l_Data;
                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Dashboard orders fetched successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getOrders/{OrderId}/{FromDate}/{ToDate}/{OrderNumber}/{Status}/{ExternalId}/{CustomerId}")]
        public async Task<GetOrdersResponseModel> GetOrders(int? OrderId = 0, string FromDate = "", string ToDate = "", string OrderNumber = "", string Status = "", string ExternalId = "", string CustomerId = "", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
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
                CustomerId = CustomerId == "EMPTY" ? ((!string.IsNullOrEmpty(userData.Customers) && !userData.IsSuperAdmin ? userData.Customers : string.Empty)) : CustomerId;

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
                    l_Criteria += $" AND (Status = '{Status}' OR DisplayStatus = '{Status}')";
                }

                // Filter on CreatedDate — the date the order reached eSyncMate — so this screen
                // agrees with the dashboard tiles and the drilldown, which both use CreatedDate.
                // It used to filter on OrderDate (the partner's PO date), so the same range could
                // return a different set of orders here than on the dashboard.
                // Written as a half-open range rather than CONVERT(DATE, ...) >= / <= so the
                // predicate stays SARGable: a function on the column blocks any index use.
                if (!string.IsNullOrEmpty(FromDate))
                {
                    l_Criteria += $" AND CreatedDate >= CAST('{FromDate}' AS DATE)";
                }

                if (!string.IsNullOrEmpty(ToDate))
                {
                    l_Criteria += $" AND CreatedDate < DATEADD(DAY, 1, CAST('{ToDate}' AS DATE))";
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

                int totalCount = 0;
                l_Order.GetViewListPagedWithErpInfo(l_Criteria, ref l_Data, "Id DESC", pageNumber, pageSize, out totalCount);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Orders searched {{{l_Data.Rows.Count}}} of {totalCount} total.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating orders.");


                l_Response.OrdersData = l_Data;
                l_Response.TotalCount = totalCount;
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

                l_OrderData.GetViewList(l_Criteria, string.Empty, ref l_Data, "TypeSortOrder, Id");

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

                        l_Result = order.UpdateShippingInfo(orderModel.Id, orderModel.ShipToAddress1, orderModel.ShipToAddress2, orderModel.ShipToCity, orderModel.ShipToState, orderModel.ShipToZip, orderModel.ShipToCountry, orderModel.ShipToName, orderModel.ShipToCompanyName);

                        if (l_Result.IsSuccess)
                        {
                            l_Result = null;

                            l_Result = order.UpdateShippingAddress(orderModel.Id, orderModel.ShipToAddress1, orderModel.ShipToAddress2, orderModel.ShipToCity, orderModel.ShipToState, orderModel.ShipToZip, orderModel.ShipToCountry, orderModel.ShipToName, orderModel.ShipToCompanyName);

                        }

                        // Warehouse / shipping instructions are kept only on the stored payload —
                        // the same copy the transformation map reads, so there is nothing to keep in sync.
                        if (l_Result != null && l_Result.IsSuccess)
                        {
                            l_Result = order.UpdateShippingFieldsInfo(orderModel.Id, orderModel.WarehouseCode, orderModel.ShippingCode, orderModel.ShippingAgentCode, orderModel.ShipDate);
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

            Result? l_Result = l_order.SetOrderStatus(OrderId, "InProgress");

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

        // Resubmit an already-SYNCED order to the ERP. Like Reprocess, the ERP is asked first
        // (Get_OrderInfo): the order is posted again ONLY if it does not exist in SPARS, or exists with a
        // Cancelled/Void status. Differences from Reprocess: previous OrderData logs are kept (not replaced),
        // and the order stays SYNCED until it is actually posted (Reprocess flips it to InProgress up front).
        [HttpPost]
        [Route("resubmitOrder")]
        public IActionResult ResubmitOrder(int OrderId, string CustomerName)
        {
            if (OrderId <= 0)
            {
                return BadRequest(new { code = 400, message = "Invalid orderId" });
            }

            // The order is left SYNCED here on purpose — the ERP is asked first, and the status is only
            // moved to InProgress inside the route at the moment the order is actually posted.
            string result = SCSPlaceOrderRoute.ExecuteSingle(_config, OrderId, CustomerName, true, out string l_InfoMessage);

            if (string.IsNullOrEmpty(result))
            {
                // The ERP already had the order with a live status — it was not posted again.
                if (!string.IsNullOrEmpty(l_InfoMessage))
                {
                    return Ok(new { code = 200, message = $"Order {OrderId} was not resubmitted. {l_InfoMessage}" });
                }

                return Ok(new { code = 200, message = $"Order {OrderId} resubmitted to ERP successfully." });
            }

            return BadRequest(new { code = 400, message = $"Order {OrderId} failed to resubmit: {result}" });
        }

        // Re-Transmit the ASN for a SHIPPED order: (1) fetch the ASN from the ERP (SPARS) again for
        // this order, (2) re-send it to the customer's marketplace. Order status is left unchanged.
        [HttpPost]
        [Route("reTransmitASN")]
        public IActionResult ReTransmitASN(int OrderId, string CustomerName)
        {
            if (OrderId <= 0)
            {
                return BadRequest(new { code = 400, message = "Invalid orderId" });
            }

            // Step 1 — get the ASN from the ERP again (refreshes the stored ERPASN-JSON).
            string getResult = SCSASNRoute.ExecuteSingle(_config, OrderId, CustomerName);
            if (!string.IsNullOrEmpty(getResult))
            {
                return BadRequest(new { code = 400, message = $"ASN fetch from ERP failed: {getResult}" });
            }

            // Find which marketplace ASN route this customer uses.
            string l_AsnTypeList = string.Join(",", new[]
            {
                (int)RouteTypesEnum.ASNShipmentNotification,
                (int)RouteTypesEnum.WalmartASNShipmentNotification,
                (int)RouteTypesEnum.MacysASNShipmentNotification,
                (int)RouteTypesEnum.LowesASNShipmentNotification,
                (int)RouteTypesEnum.AmazonASNShipmentNotification,
                (int)RouteTypesEnum.KnotASNShipmentNotification,
                (int)RouteTypesEnum.MichealASNShipmentNotification
            });

            int l_AsnType = 0;
            DBConnector l_Conn = new DBConnector(CommonUtils.ConnectionString);
            DataTable l_Dt = new DataTable();
            l_Conn.GetData($"SELECT TOP 1 TypeId FROM Routes WHERE CustomerName = '{CustomerName}' AND TypeId IN ({l_AsnTypeList})", ref l_Dt);
            if (l_Dt.Rows.Count > 0)
            {
                l_AsnType = Convert.ToInt32(l_Dt.Rows[0]["TypeId"]);
            }
            l_Dt.Dispose();

            // Step 2 — re-send the ASN to the marketplace via the matching partner route.
            string sendResult;
            switch (l_AsnType)
            {
                case (int)RouteTypesEnum.WalmartASNShipmentNotification:
                    sendResult = WalmartASNShipmentNotificationRoute.ExecuteSingle(_config, OrderId, CustomerName); break;
                case (int)RouteTypesEnum.MacysASNShipmentNotification:
                    sendResult = MacysASNShipmentNotificationRoute.ExecuteSingle(_config, OrderId, CustomerName); break;
                case (int)RouteTypesEnum.LowesASNShipmentNotification:
                    sendResult = LowesASNShipmentNotificationRoute.ExecuteSingle(_config, OrderId, CustomerName); break;
                case (int)RouteTypesEnum.AmazonASNShipmentNotification:
                    sendResult = AmazonASNShipmentNotificationRoute.ExecuteSingle(_config, OrderId, CustomerName); break;
                case (int)RouteTypesEnum.KnotASNShipmentNotification:
                    sendResult = KnotASNShipmentNotificationRoute.ExecuteSingle(_config, OrderId, CustomerName); break;
                case (int)RouteTypesEnum.MichealASNShipmentNotification:
                    sendResult = MichealASNShipmentNotificationRoute.ExecuteSingle(_config, OrderId, CustomerName); break;
                case (int)RouteTypesEnum.ASNShipmentNotification:
                    sendResult = ASNShipmentNotificationRoute.ExecuteSingle(_config, OrderId, CustomerName); break;
                default:
                    return BadRequest(new { code = 400, message = $"No marketplace ASN route configured for customer {CustomerName}." });
            }

            if (string.IsNullOrEmpty(sendResult))
            {
                return Ok(new { code = 200, message = $"Order {OrderId} ASN re-transmitted successfully." });
            }

            return BadRequest(new { code = 400, message = $"Order {OrderId} ASN re-transmit failed: {sendResult}" });
        }

        // The ERPCustomerIDs the Re-Map Item IDs action applies to. Read live from
        // ApplicationSettings (not CommonUtils, which is bound once at startup) so a new Amazon
        // customer can be added without restarting the Processor.
        internal static HashSet<string> GetAmazonCustomerIds()
        {
            HashSet<string> l_Ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                DataTable l_Data = new DataTable();
                new DBConnector(CommonUtils.ConnectionString)
                    .GetData("SELECT TagValue FROM ApplicationSettings WHERE TagName = 'AmazonCustomerIds'", ref l_Data);

                if (l_Data.Rows.Count > 0)
                {
                    foreach (string l_Id in Convert.ToString(l_Data.Rows[0]["TagValue"]).Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!string.IsNullOrWhiteSpace(l_Id))
                            l_Ids.Add(l_Id.Trim());
                    }
                }
            }
            catch (Exception)
            {
                // An unreadable setting leaves the set empty, which closes the action rather than
                // opening it to every customer.
            }

            return l_Ids;
        }

        // Re-Map Item IDs (Amazon only). An Amazon order whose Seller SKU was not yet in
        // SCSInventoryFeed when it arrived gets the raw SKU written into the API-JSON as its
        // ItemID (AmazonGetOrdersRoute), and the ERP rejects it. This re-runs that same lookup
        // now: SellerSKU -> SCSInventoryFeed.CustomerItemCode -> ItemId, and writes the result
        // back into the ItemID field of every line in the stored API-JSON.
        //
        // The API-JSON is what the ERP actually receives: SP_OrdersData hands OD.Data to the
        // JUST map, whose detail loop reads $.OrderDetail.payload.OrderItems[].ItemID. Nothing
        // else needs to change for the order to post.
        //
        // The order is NOT re-processed here — that stays a separate, visible step.
        [HttpPost]
        [Route("remapItemIds")]
        public IActionResult RemapItemIds(int OrderId, string CustomerName)
        {
            if (OrderId <= 0)
            {
                return BadRequest(new { code = 400, message = "Invalid orderId" });
            }

            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                return BadRequest(new { code = 400, message = "Customer is required." });
            }

            try
            {
                DBConnector l_Conn = new DBConnector(CommonUtils.ConnectionString);

                // Amazon only, by explicit ERPCustomerID list. Not derived from Customers.Marketplace
                // (free text) and not from the Amazon GetOrders route either — AMA1000 has no routes
                // configured yet but is still an Amazon customer. The list lives in
                // ApplicationSettings so onboarding another Amazon customer is a one-row UPDATE.
                if (!GetAmazonCustomerIds().Contains(CustomerName.Trim()))
                {
                    return BadRequest(new { code = 400, message = $"Re-Map Item IDs is available for Amazon customers only. {CustomerName} is not in AmazonCustomerIds." });
                }

                // The API-JSON is looked up by OrderNumber, not OrderId: some OrderData rows are
                // written before the OrderId is known (see UpdateOrderDataOrderID).
                DataTable l_OrderRow = new DataTable();
                l_Conn.GetData($"SELECT TOP 1 O.OrderNumber, C.ERPCustomerID FROM Orders O WITH (NOLOCK) INNER JOIN Customers C WITH (NOLOCK) ON C.Id = O.CustomerId WHERE O.Id = {OrderId}", ref l_OrderRow);

                if (l_OrderRow.Rows.Count == 0)
                {
                    return BadRequest(new { code = 400, message = $"Order {OrderId} not found." });
                }

                string l_OrderNumber = Convert.ToString(l_OrderRow.Rows[0]["OrderNumber"]);
                string l_ErpCustomerId = Convert.ToString(l_OrderRow.Rows[0]["ERPCustomerID"]);

                OrderData l_OrderData = new OrderData();
                l_OrderData.UseConnection(CommonUtils.ConnectionString);

                Result l_Found = l_OrderData.GetObjectFromQuery($"SELECT TOP 1 * FROM OrderData WITH (NOLOCK) WHERE OrderNumber = '{l_OrderNumber.Replace("'", "''")}' AND Type = 'API-JSON' ORDER BY Id DESC", true);

                if (!l_Found.IsSuccess || string.IsNullOrWhiteSpace(l_OrderData.Data))
                {
                    return BadRequest(new { code = 400, message = $"No API-JSON found for order {l_OrderNumber}." });
                }

                // These are error orders, so the payload may be one of the ones carrying an
                // unescaped quote in a product Title — JObject.Parse would throw before any
                // re-mapping happened. Repair it first, exactly as SCSPlaceOrderRoute does.
                string l_Json = l_OrderData.Data;
                bool l_PayloadRepaired = OrderPayloadRepair.TryRepair(l_Json, out l_Json);

                if (!OrderPayloadRepair.IsValid(l_Json))
                {
                    return BadRequest(new { code = 400, message = $"Order {l_OrderNumber} has an API-JSON payload that cannot be parsed or repaired. Fix OrderData.Data for this order first." });
                }

                // Edited as a JObject rather than through the typed model: re-serialising the model
                // would drop any field it does not declare (WarehouseCode, ShippingCode, ShipDate
                // and anything a later change adds) and silently rewrite the rest of the payload.
                JObject l_Payload = JObject.Parse(l_Json);
                JToken l_Items = l_Payload.SelectToken("OrderDetail.payload.OrderItems");

                if (l_Items == null || l_Items.Type != JTokenType.Array || !l_Items.Any())
                {
                    return BadRequest(new { code = 400, message = $"Order {l_OrderNumber} has no order lines in its API-JSON." });
                }

                // Every line is handled the same way, so a single-line and a multi-line order need
                // no special casing.
                List<string> l_Skus = l_Items
                    .Select(i => Convert.ToString(i["SellerSKU"]))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!l_Skus.Any())
                {
                    return BadRequest(new { code = 400, message = $"Order {l_OrderNumber} has no Seller SKU on any line." });
                }

                // One lookup for the whole order. The feed holds ~250k rows per Amazon customer, so
                // this is filtered in SQL rather than pulled into memory the way ingestion does.
                string l_SkuList = string.Join(",", l_Skus.Select(s => $"'{s.Replace("'", "''")}'"));

                DataTable l_Feed = new DataTable();
                l_Conn.GetData($"SELECT CustomerItemCode, ItemId FROM SCSInventoryFeed WITH (NOLOCK) WHERE CustomerID = '{l_ErpCustomerId.Replace("'", "''")}' AND CustomerItemCode IN ({l_SkuList})", ref l_Feed);

                // A Seller SKU may legitimately carry several ItemIds (the PK is CustomerID +
                // ItemId + CustomerItemCode). Those are reported, never guessed at.
                Dictionary<string, List<string>> l_Map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow l_Row in l_Feed.Rows)
                {
                    string l_Code = Convert.ToString(l_Row["CustomerItemCode"]).Trim();
                    string l_ItemId = Convert.ToString(l_Row["ItemId"]).Trim();

                    if (string.IsNullOrWhiteSpace(l_Code) || string.IsNullOrWhiteSpace(l_ItemId))
                        continue;

                    if (!l_Map.ContainsKey(l_Code))
                        l_Map[l_Code] = new List<string>();

                    if (!l_Map[l_Code].Contains(l_ItemId, StringComparer.OrdinalIgnoreCase))
                        l_Map[l_Code].Add(l_ItemId);
                }

                List<object> l_Lines = new List<object>();
                int l_Updated = 0, l_Unchanged = 0, l_NotFound = 0, l_Ambiguous = 0;

                foreach (JToken l_Item in l_Items)
                {
                    string l_Sku = Convert.ToString(l_Item["SellerSKU"])?.Trim() ?? string.Empty;
                    string l_Current = Convert.ToString(l_Item["ItemID"])?.Trim() ?? string.Empty;
                    string l_LineNo = Convert.ToString(l_Item["LineNo"]);

                    if (string.IsNullOrWhiteSpace(l_Sku))
                    {
                        l_NotFound++;
                        l_Lines.Add(new { lineNo = l_LineNo, sellerSku = l_Sku, currentItemId = l_Current, newItemId = (string)null, result = "NO SELLER SKU ON LINE" });
                        continue;
                    }

                    if (!l_Map.ContainsKey(l_Sku))
                    {
                        l_NotFound++;
                        l_Lines.Add(new { lineNo = l_LineNo, sellerSku = l_Sku, currentItemId = l_Current, newItemId = (string)null, result = "NOT IN INVENTORY FEED" });
                        continue;
                    }

                    if (l_Map[l_Sku].Count > 1)
                    {
                        l_Ambiguous++;
                        l_Lines.Add(new { lineNo = l_LineNo, sellerSku = l_Sku, currentItemId = l_Current, newItemId = (string)null, result = $"AMBIGUOUS - maps to {string.Join(", ", l_Map[l_Sku])}" });
                        continue;
                    }

                    string l_New = l_Map[l_Sku][0];

                    if (string.Equals(l_Current, l_New, StringComparison.OrdinalIgnoreCase))
                    {
                        l_Unchanged++;
                        l_Lines.Add(new { lineNo = l_LineNo, sellerSku = l_Sku, currentItemId = l_Current, newItemId = l_New, result = "ALREADY CORRECT" });
                        continue;
                    }

                    l_Item["ItemID"] = l_New;
                    l_Updated++;
                    l_Lines.Add(new { lineNo = l_LineNo, sellerSku = l_Sku, currentItemId = l_Current, newItemId = l_New, result = "UPDATED" });
                }

                // Nothing re-mapped, but a payload that had to be repaired to be read at all is
                // still worth saving — otherwise the order stays unreadable for the ERP.
                if (l_Updated == 0 && !l_PayloadRepaired)
                {
                    return Ok(new
                    {
                        code = 200,
                        message = $"Order {l_OrderNumber}: nothing to re-map ({l_Unchanged} already correct, {l_NotFound} not in the feed, {l_Ambiguous} ambiguous).",
                        orderNumber = l_OrderNumber,
                        updated = 0,
                        unchanged = l_Unchanged,
                        notFound = l_NotFound,
                        ambiguous = l_Ambiguous,
                        lines = l_Lines
                    });
                }

                // Formatting.None matches how the route stored it in the first place.
                if (!l_OrderData.UpdateData(l_OrderData.Id, l_Payload.ToString(Formatting.None)))
                {
                    return BadRequest(new { code = 400, message = $"Order {l_OrderNumber}: failed to save the updated API-JSON." });
                }

                this._logger.LogInformation($"[RemapItemIds] Order {l_OrderNumber} ({l_ErpCustomerId}) - {l_Updated} line(s) re-mapped, {l_Unchanged} already correct, {l_NotFound} not in feed, {l_Ambiguous} ambiguous.");

                return Ok(new
                {
                    code = 200,
                    message = l_PayloadRepaired
                        ? $"Order {l_OrderNumber}: unreadable payload repaired, {l_Updated} line(s) re-mapped. Re-Process the order to send it to the ERP."
                        : $"Order {l_OrderNumber}: {l_Updated} line(s) re-mapped. Re-Process the order to send it to the ERP.",
                    orderNumber = l_OrderNumber,
                    updated = l_Updated,
                    unchanged = l_Unchanged,
                    notFound = l_NotFound,
                    ambiguous = l_Ambiguous,
                    lines = l_Lines
                });
            }
            catch (Exception ex)
            {
                this._logger.LogCritical($"[RemapItemIds] Order {OrderId} - {ex}");

                return BadRequest(new { code = 400, message = $"Order {OrderId} re-map failed: {ex.Message}" });
            }
        }
    }
}
