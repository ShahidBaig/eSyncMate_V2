using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;
using System.Threading;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly ILogger<PurchaseOrderController> _logger;

        public PurchaseOrderController(ILogger<PurchaseOrderController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getPurchaseorders")]
        public async Task<GetPurchaseOrderResponseModel> GetPurchaseorders([FromQuery] PurchaseOrderSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            DataTable l_Data = new DataTable();
            string dateRange = string.Empty;
            string[] dateValues = new string[0];
            string startDate = string.Empty;
            string endDate = string.Empty;
            DataTable l_DetailData = new DataTable();

            if (searchModel.SearchOption == "Purchase Order Date")
            {
                dateRange = searchModel.SearchValue;
                dateValues = dateRange.Split('/');
                startDate = dateValues[0].Trim() + " 00:00:00.000";
                endDate = dateValues[1].Trim() + " 23:59:59.999";
            }

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.PurchaseOrders l_PurchaseOrders = new DB.Entities.PurchaseOrders();
                DB.Entities.PurchaseOrderDetail l_PurchaseOrderDetail = new DB.Entities.PurchaseOrderDetail();


                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_PurchaseOrders.UseConnection(CommonUtils.ConnectionString);
                l_PurchaseOrderDetail.UseConnection(CommonUtils.ConnectionString);

                if (searchModel.SearchOption == "Purchase Order Date")
                {
                    l_Criteria += $" CONVERT(DATE,OrderDate) >= '{startDate}'";
                }

                if (searchModel.SearchOption == "Purchase Order Date")
                {
                    l_Criteria += $" AND CONVERT(DATE,OrderDate) <= '{endDate}'";
                }

                if (searchModel.SearchOption == "Purchase Order No")
                {
                    l_Criteria = $" PONumber LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "Status")
                {
                    l_Criteria = $" Status = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "ItemID")
                {
                    l_Criteria = $" ItemID LIKE '%{searchModel.SearchValue}%'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring PurchaseOrder search.");

                l_PurchaseOrders.GetViewList(l_Criteria, "VW_Temp_PurchaseOrders", ref l_Data, "Id DESC");
                l_PurchaseOrderDetail.GetViewList("", "", ref l_DetailData, "");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - PurchaseOrder searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating PurchaseOrder.");

                l_Response.PurchaseOrders = l_Data;
                l_Response.DetailData = l_DetailData;
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    InvFeedFromNDCDataModel l_InvRow = new InvFeedFromNDCDataModel();

                //    DBEntity.PopulateObjectFromRow(l_InvRow, l_Data, l_Row);

                //    l_Response.Inv.Add(l_InvRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "PurchaseOrder fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - PurchaseOrder are ready.");
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
        [Route("createPurchaseorders")]
        public async Task<GetPurchaseOrderResponseModel> CreatePurchaseorders([FromBody] SavePurchaseOrderModel orderModel)
        {
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            Result l_Result = new Result();
            string l_JobID = string.Empty;
            PurchaseOrderDetail l_PurchaseOrderDetail = new PurchaseOrderDetail();

            try
            {
                DB.Entities.PurchaseOrders l_PurchaseOrders = new DB.Entities.PurchaseOrders();
                l_PurchaseOrders.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(orderModel, l_PurchaseOrders);

                l_PurchaseOrders.Status = "INPROGRESS";
                l_PurchaseOrders.CreatedBy = l_PurchaseOrders.CreatedBy;
                l_PurchaseOrders.CreatedDate = DateTime.Now;

                if (l_PurchaseOrders.GetObject("PONumber", l_PurchaseOrders.PONumber).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"This Purchase Order Number [ {l_PurchaseOrders.PONumber} ] is already Exists!";

                    return l_Response;
                }


                l_Result = l_PurchaseOrders.SaveNew();

                if (l_Result.IsSuccess == true)
                {
                    foreach (var item in orderModel.Details)
                    {

                        l_PurchaseOrderDetail = new PurchaseOrderDetail();

                        l_PurchaseOrderDetail.OrderId = l_PurchaseOrders.Id;
                        l_PurchaseOrderDetail.LineNo = item.LineNo;
                        l_PurchaseOrderDetail.ItemID = item.ItemID;
                        l_PurchaseOrderDetail.UPC = item.UPC;
                        l_PurchaseOrderDetail.Description = item.Description;
                        l_PurchaseOrderDetail.OrderQty = item.OrderQty;
                        l_PurchaseOrderDetail.UnitPrice = item.UnitPrice;
                        l_PurchaseOrderDetail.ManufacturerName = item.ManufacturerName;
                        l_PurchaseOrderDetail.NDCItemID = item.NDCItemID;
                        l_PurchaseOrderDetail.PrimaryCategoryName = item.PrimaryCategoryName;
                        l_PurchaseOrderDetail.SecondaryCategoryName = item.SecondaryCategoryName;
                        l_PurchaseOrderDetail.ProductName = item.ProductName;
                        l_PurchaseOrderDetail.ExtendedPrice = item.ExtendedPrice;
                        l_PurchaseOrderDetail.UOM = item.UOM;
                        l_PurchaseOrderDetail.Status = "NEW";
                        l_PurchaseOrderDetail.UseConnection(CommonUtils.ConnectionString);
                        l_PurchaseOrderDetail.SaveNew();
                        //l_PurchaseOrderDetail.OrderQuantityUpdate(l_PurchaseOrderDetail.OrderQty, l_PurchaseOrderDetail.ItemID);
                    }
                }

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Purchase Order [ {l_PurchaseOrders.Id} ] has been created successfully!";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }

            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
            }
            finally
            {

            }

            return l_Response;
        }

        [HttpPost]
        [Route("updatePurchaseorders")]
        public async Task<GetPurchaseOrderResponseModel> UpdatePurchaseorders([FromBody] UpdatePurchaseOrderModel orderModel)
        {
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            Result l_Result = new Result();
            string l_JobID = string.Empty;
            PurchaseOrderDetail l_PurchaseOrderDetail = new PurchaseOrderDetail();

            try
            {
                DB.Entities.PurchaseOrders l_PurchaseOrders = new DB.Entities.PurchaseOrders();
                l_PurchaseOrders.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(orderModel, l_PurchaseOrders);

                l_PurchaseOrders.Status = "INPROGRESS";

                l_Result = l_PurchaseOrders.Modify();

                if (l_Result.IsSuccess == true)
                {
                    foreach (var item in orderModel.Details)
                    {
                        l_PurchaseOrderDetail = new PurchaseOrderDetail();

                        l_PurchaseOrderDetail.OrderId = l_PurchaseOrders.Id;
                        l_PurchaseOrderDetail.LineNo = item.LineNo;
                        l_PurchaseOrderDetail.ItemID = item.ItemID;
                        l_PurchaseOrderDetail.UPC = item.UPC;
                        l_PurchaseOrderDetail.Description = item.Description;
                        l_PurchaseOrderDetail.OrderQty = item.OrderQty;
                        l_PurchaseOrderDetail.UnitPrice = item.UnitPrice;
                        l_PurchaseOrderDetail.ManufacturerName = item.ManufacturerName;
                        l_PurchaseOrderDetail.NDCItemID = item.NDCItemID;
                        l_PurchaseOrderDetail.PrimaryCategoryName = item.PrimaryCategoryName;
                        l_PurchaseOrderDetail.SecondaryCategoryName = item.SecondaryCategoryName;
                        l_PurchaseOrderDetail.ProductName = item.ProductName;
                        l_PurchaseOrderDetail.ExtendedPrice = item.ExtendedPrice;
                        l_PurchaseOrderDetail.UOM = item.UOM;
                        l_PurchaseOrderDetail.UseConnection(CommonUtils.ConnectionString);

                        if (item.isNew)
                        {
                            l_PurchaseOrderDetail.Status = "NEW";
                            l_PurchaseOrderDetail.SaveNew();
                        }
                        else if (item.Status == "DELETE")
                        {
                            l_PurchaseOrderDetail.DeleteItems(l_PurchaseOrderDetail.OrderId, l_PurchaseOrderDetail.LineNo);
                        }
                        else
                        {
                            l_PurchaseOrderDetail.Modify();
                        }
                    }
                }

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Purchase Order [ {l_PurchaseOrders.Id} ] has been updated successfully!";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }

            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getSuppliers")]
        public async Task<GetPurchaseOrderResponseModel> GetSuppliers()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                PurchaseOrders l_PurchaseOrder = new PurchaseOrders();

                l_Response.Code = (int)ResponseCodes.Error;

                l_PurchaseOrder.UseConnection(CommonUtils.ConnectionString);

                l_PurchaseOrder.GetSuppliers(CommonUtils.ConnectionString, ref l_Data);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Suppliers searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Suppliers.");

                l_Response.SupplierData = l_Data;
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    CustomerDataModel l_CustomerRow = new CustomerDataModel();

                //    DBEntity.PopulateObjectFromRow(l_CustomerRow, l_Data, l_Row);

                //    l_Response.Customers.Add(l_CustomerRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Suppliers fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Suppliers are ready.");
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
        [Route("getItems")]
        public async Task<GetPurchaseOrderResponseModel> GetItems()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                PurchaseOrders l_PurchaseOrder = new PurchaseOrders();

                l_Response.Code = (int)ResponseCodes.Error;

                l_PurchaseOrder.GetItems(CommonUtils.ConnectionString, ref l_Data);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Items searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Items.");

                l_Response.ItemsData = l_Data;
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    CustomerDataModel l_CustomerRow = new CustomerDataModel();

                //    DBEntity.PopulateObjectFromRow(l_CustomerRow, l_Data, l_Row);

                //    l_Response.Customers.Add(l_CustomerRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Items fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Items are ready.");
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
        [Route("getSuppliersItems")]
        public async Task<GetPurchaseOrderResponseModel> GetSuppliersItems(string SupplierName)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                PurchaseOrders l_PurchaseOrder = new PurchaseOrders();

                l_Response.Code = (int)ResponseCodes.Error;

                l_PurchaseOrder.GetSuppliersItems(SupplierName,CommonUtils.ConnectionString, ref l_Data);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Suppliers Items searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Suppliers Items.");

                l_Response.SuppliersItemsData = l_Data;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Items fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Items are ready.");
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
        [Route("getPurchaseOrderDetail")]
        public async Task<GetPurchaseOrderResponseModel> GetPurchaseOrderDetail(int OrderID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            DataTable l_Data = new DataTable();
            DataTable l_DetailData = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.PurchaseOrderDetail l_PurchaseOrderDetail = new DB.Entities.PurchaseOrderDetail();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_PurchaseOrderDetail.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Starting PurchaseOrderDetail search.");

                // Update the query to include WarehouseID
                l_PurchaseOrderDetail.GetViewList($"OrderId = {OrderID}", "", ref l_DetailData, "");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - PurchaseOrderDetail searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating PurchaseOrderDetail.");

                // Ensure the new column is included in the response
                l_Response.DetailData = l_DetailData;

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "PurchaseOrderDetail fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - PurchaseOrderDetail are ready.");
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
        [Route("getItemSelected")]
        public async Task<GetPurchaseOrderResponseModel> GetItemSelected(string ItemID, string NDCItemID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            DataTable l_DetailData = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.InvFeedFromNDC l_InvFeedFromNDC = new DB.Entities.InvFeedFromNDC();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_InvFeedFromNDC.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Starting UPC search.");

                l_InvFeedFromNDC.GetViewList($"ItemID = '{ItemID}' AND NDCItemID = '{NDCItemID}'", "", ref l_DetailData, "");

                l_Response.ItemsData = l_DetailData;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Items fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Items are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_DetailData.Dispose();
            }

            return l_Response;
        }

        [HttpPost]
        [Route("updateqty")]
        public async Task<GetPurchaseOrderResponseModel> UpdateQty([FromBody] UpdateQtyRequestModel request)
        {
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            Result l_Result = new Result();
            PurchaseOrderDetail l_PurchaseOrderDetail = new PurchaseOrderDetail();

            try
            {
                l_PurchaseOrderDetail.UseConnection(CommonUtils.ConnectionString);

                if (request.Details != null && request.Details.Count > 0)
                {
                    foreach (var detail in request.Details)
                    {
                        l_Result = l_PurchaseOrderDetail.OrderQuantityUpdate(detail.OrderQty, detail.ItemID, request.Action);
                        if (!l_Result.IsSuccess)
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = $"Failed to update ItemID '{detail.ItemID}' with Quantity '{detail.OrderQty}'.";
                            return l_Response;
                        }
                    }
                }
                else
                {
                    l_Result = l_PurchaseOrderDetail.OrderQuantityUpdate(request.OrderQty, request.ItemID, request.Action);
                }

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = "Quantity updates completed successfully.";
                    l_Response.Description = $"Action '{request.Action}' processed successfully.";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        [HttpPost]
        [Route("markForRelease")]
        public async Task<GetPurchaseOrderResponseModel> MarkForRelease(int OrderId)
        {
            GetPurchaseOrderResponseModel l_Response = new GetPurchaseOrderResponseModel();
            Result l_Result = new Result();
            PurchaseOrders l_PurchaseOrders = new PurchaseOrders();
            MethodBase l_Me = MethodBase.GetCurrentMethod();

            try
            {
                l_PurchaseOrders.UseConnection(CommonUtils.ConnectionString);
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Processing Mark for Release for Order [{OrderId}].");

                l_Result = l_PurchaseOrders.MarkForRelease(OrderId);

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = $"Order [ {OrderId} ] marked for Release successfully!";
                    l_Response.Description = $"Action Release against '{OrderId}' processed successfully.";

                    this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Mark for Release completed successfully.");
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }
    }
}
