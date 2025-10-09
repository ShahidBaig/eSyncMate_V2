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
using Intercom.Data;
using static eSyncMate.Processor.Models.CarrierLoadTenderViewModel;
using System.Threading.Tasks;
using System.Threading;
using MySqlX.XDevAPI.Relational;
using Mysqlx.Crud;

namespace eSyncMate.Processor.Controllers
{
    [ApiController]
    [Route("EDIProcessor/api/v1/carrierLoadTender")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class CarrierLoadTenderController : ControllerBase
    {
        private readonly ILogger<CarrierLoadTenderController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public CarrierLoadTenderController(ILogger<CarrierLoadTenderController> logger, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
        {
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
        }

        [HttpGet]
        [Route("getCarrierLoadTender/{CarrierLoadTenderId}/{FromDate}/{ToDate}/{ShipmentId}/{ShipmentShipperNo}/{Status}/{CustomerName}")]
        public async Task<GetCarrierLoadTenderResponseModel> GetCarrierLoadTender(int CarrierLoadTenderId = 0, string FromDate = "", string ToDate = "", string ShipmentId = "", string ShipmentShipperNo = "", string Status = "", string CustomerName = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCarrierLoadTenderResponseModel l_Response = new GetCarrierLoadTenderResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                CarrierLoadTender l_CarrierLoadTender = new CarrierLoadTender();

                if (FromDate == "1999-01-01")
                {
                    FromDate = string.Empty;
                }
                if (ToDate == "1999-01-01")
                {
                    ToDate = string.Empty;
                }
                if (ShipmentId == "EMPTY")
                {
                    ShipmentId = string.Empty;
                }
                if (ShipmentShipperNo == "EMPTY")
                {
                    ShipmentShipperNo = string.Empty;
                }
                if (CustomerName == "EMPTY")
                {
                    CustomerName = string.Empty;
                }
                if (Status == "Select Status")
                {
                    Status = string.Empty;
                }

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CarrierLoadTender.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"Status <> 'DELETED'";

                if (CarrierLoadTenderId > 0)
                {
                    l_Criteria += $" AND Id = {CarrierLoadTenderId}";
                }

                if (!string.IsNullOrEmpty(ShipmentId))
                {
                    l_Criteria += $" AND ShipmentId = '{ShipmentId}'";
                }

                if (!string.IsNullOrEmpty(ShipmentShipperNo))
                {
                    l_Criteria += $" AND ShipperNo = '{ShipmentShipperNo}'";
                }

                if (!string.IsNullOrEmpty(Status))
                {
                    l_Criteria += $" AND Status = '{Status}'";
                }

                if (!string.IsNullOrEmpty(FromDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) >= '{FromDate}'";
                }

                if (!string.IsNullOrEmpty(ToDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{ToDate}'";
                }

                if (!string.IsNullOrEmpty(CustomerName))
                {
                    l_Criteria += $" AND CustomerName = '{CustomerName}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring CarrierLoadTender search.");

                l_CarrierLoadTender.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CarrierLoadTender searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating CarrierLoadTender.");

                //l_Response.CarrierLoadTenderData = l_Data;
                l_Response.CarrierLoadTender = new List<CarrierLoadTenderDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CarrierLoadTenderDataModel l_CarrierLoadTenderRow = new CarrierLoadTenderDataModel();

                    DBEntity.PopulateObjectFromRow(l_CarrierLoadTenderRow, l_Data, l_Row);

                    l_Response.CarrierLoadTender.Add(l_CarrierLoadTenderRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "CarrierLoadTender fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CarrierLoadTender are ready.");
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
        [Route("getCarrierLoadTenderFiles/{CarrierLoadTenderId}")]
        public async Task<GetCarrierLoadTenderFilesResponseModel> GetCarrierLoadTenderFiles(int CarrierLoadTenderId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCarrierLoadTenderFilesResponseModel l_Response = new GetCarrierLoadTenderFilesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                CarrierLoadTenderData l_CarrierLoadTenderData = new CarrierLoadTenderData();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CarrierLoadTenderData.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"CarrierLoadTenderId = {CarrierLoadTenderId}";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring CarrierLoadTender files search.");

                l_CarrierLoadTenderData.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CarrierLoadTender files searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating CarrierLoadTender files.");

                l_Response.Files = new List<CarrierLoadTenderFileModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CarrierLoadTenderFileModel l_CarrierLoadTenderFile = new CarrierLoadTenderFileModel();

                    DBEntity.PopulateObjectFromRow(l_CarrierLoadTenderFile, l_Data, l_Row);

                    l_Response.Files.Add(l_CarrierLoadTenderFile);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "CarrierLoadTender Files fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CarrierLoadTender Files are ready.");
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
        [Route("updateAckStatus")]
        public async Task<ActionResult<GetCarrierLoadTenderResponseModel>> UpdateAckStatus([FromBody] CarrierLoadTenderDataModel carrierModel)
        {
            GetCarrierLoadTenderResponseModel l_Response = new GetCarrierLoadTenderResponseModel();
            Result l_Result = new Result();
            bool isResult = false;
            DataTable l_Data = new DataTable();

            try
            {
                CarrierLoadTender l_CarrierLoadTender = new CarrierLoadTender();
                l_CarrierLoadTender.UseConnection(CommonUtils.ConnectionString);

                isResult = l_CarrierLoadTender.UpdateAckStatus(carrierModel.ShipmentId, carrierModel.AckStatus);

                if (isResult)
                {
                    l_CarrierLoadTender.GetViewList($"ShipmentId = {carrierModel.ShipmentId}", string.Empty, ref l_Data, "Id DESC");

                    foreach (DataRow row in l_Data.Rows)
                    {
                        CarrierLoadTender.InsertMySqlData(row["CustomerName"].ToString(), row["ShipperNo"].ToString(), row["ShipmentId"].ToString(), row["EquipmentNo"].ToString(), row["ConsigneeName"].ToString(), row["LastConsigneeName"].ToString(), CommonUtils.MySqlConnectionString);
                    }

                    l_Response.Code = (int)ResponseCodes.Success; ;
                    l_Response.Message = "Data updated successfully";
                    l_Response.Description = $"Shipment[{carrierModel.ShipmentId}] has been updated successfully!";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "There is some issue, please try again later.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }



        [HttpGet]
        [Route("getCarrierLoadTenderAckData")]
        public async Task<GetCarrierLoadTenderAckDataResponseModel> CarrierLoadTenderAckData()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCarrierLoadTenderAckDataResponseModel l_Response = new GetCarrierLoadTenderAckDataResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                CarrierLoadTender l_CarrierLoadTender = new CarrierLoadTender();
                l_CarrierLoadTender.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring CarrierLoadTender search.");

                l_CarrierLoadTender.GetAckData(ref l_Data);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CarrierLoadTender searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating CarrierLoadTender.");

                l_Response.AckData = new List<CarrierLoadTenderAckDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CarrierLoadTenderAckDataModel l_CarrierLoadTenderRow = new CarrierLoadTenderAckDataModel();

                    DBEntity.PopulateObjectFromRow(l_CarrierLoadTenderRow, l_Data, l_Row);

                    l_Response.AckData.Add(l_CarrierLoadTenderRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "CarrierLoadTenderAck fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CarrierLoadTenderAck are ready.");
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
        [Route("updateTrackStatus")]
        public async Task<ActionResult<GetCarrierLoadTenderResponseModel>> UpdateTrackStatus(int tenderID, string trackStatus, string consigneeAddress, string consigneeCity, string consigneeState, string consigneeZip, string consigneeCountry, string equipmentNo, string manualequipmentNo)
        {
            GetCarrierLoadTenderResponseModel l_Response = new GetCarrierLoadTenderResponseModel();
            bool isResult = false;
            bool isAddresses = false;

            try
            {
                CarrierLoadTender l_CarrierLoadTender = new CarrierLoadTender();
                l_CarrierLoadTender.UseConnection(CommonUtils.ConnectionString);

                isResult = l_CarrierLoadTender.UpdateTrackStatus(tenderID, trackStatus);

                if (manualequipmentNo == "Empty")
                {
                    manualequipmentNo = "";
                }

                if (isResult)
                    isAddresses = l_CarrierLoadTender.UpdateAddresses(tenderID, consigneeAddress, consigneeCity, consigneeState, consigneeZip, consigneeCountry, equipmentNo, manualequipmentNo);

                if (isAddresses)
                {
                    isAddresses = false;

                    l_CarrierLoadTender.GetObject(tenderID);

                    string jsonData = l_CarrierLoadTender.Files.Where(f => f.Type == "204-JSON").ToArray()[0].Data;
                    CarrierLoadTenderViewModel carrierData = JsonConvert.DeserializeObject<CarrierLoadTenderViewModel>(jsonData);

                    if (carrierData != null)
                    {
                        carrierData.ConsigneeAddress = consigneeAddress;
                        carrierData.ConsigneeCity = consigneeCity;
                        carrierData.ConsigneeState = consigneeState;
                        carrierData.ConsigneeZip = consigneeZip;
                        carrierData.ConsigneeCountry = consigneeCountry;

                        jsonData = JsonConvert.SerializeObject(carrierData);

                        CarrierLoadTenderData l_Data = new CarrierLoadTenderData();

                        l_Data.UseConnection(string.Empty, l_CarrierLoadTender.Connection);
                        l_Data.DeleteWithType(l_CarrierLoadTender.Id, "204-JSON");

                        l_Data.Type = "204-JSON";
                        l_Data.Data = jsonData;
                        l_Data.CreatedBy = l_CarrierLoadTender.CreatedBy;
                        l_Data.CreatedDate = DateTime.Now;
                        l_Data.CarrierLoadTenderId = l_CarrierLoadTender.Id;

                        isAddresses = l_Data.SaveNew().IsSuccess;
                    }
                }

                if (isResult && isAddresses)
                {
                    l_Response.Code = (int)ResponseCodes.Success; ;
                    l_Response.Message = "Data updated successfully";
                    l_Response.Description = "Data updated successfully";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "There is some issue, please try again later.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getEdiFilesCounter")]
        public async Task<GetEdiFilesCounterResponseModel> GetEdiFilesCounter(string FromDate = "", string ToDate = "", int Customer = 0, string ShipmentID = "", string ShipperNo = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetEdiFilesCounterResponseModel l_Response = new GetEdiFilesCounterResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = $"CustomerId = {Customer} AND DocDate BETWEEN CONVERT(DATE,'{FromDate}') AND CONVERT(DATE,'{ToDate}')";

                if (ShipmentID != "EMPTY")
                {
                    l_Criteria += $" AND ShipmentID = '{ShipmentID}'";
                }

                if (ShipperNo != "EMPTY")
                {
                    l_Criteria += $" AND ShipperNo = '{ShipperNo}'";
                }

                CarrierLoadTender l_CarrierLoadTender = new CarrierLoadTender();
                l_CarrierLoadTender.UseConnection(CommonUtils.ConnectionString);
                l_CarrierLoadTender.GetFileCounts(ref l_Data, l_Criteria);

                l_Response.EdiCounterData = new List<EdiFilesCounterDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    EdiFilesCounterDataModel l_EDICountRow = new EdiFilesCounterDataModel();
                    DBEntity.PopulateObjectFromRow(l_EDICountRow, l_Data, l_Row);
                    l_Response.EdiCounterData.Add(l_EDICountRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "FilesCount fetched successfully!";
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
        [Route("getStatesData")]
        public async Task<GetSatesDataResponseModel> GetStatesData()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetSatesDataResponseModel l_Response = new GetSatesDataResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                CarrierLoadTender l_CarrierLoadTender = new CarrierLoadTender();
                l_CarrierLoadTender.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;

                l_CarrierLoadTender.GetStatesData(ref l_Data);

                l_Response.StatesData = new List<StatesDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    StatesDataModel l_StatesRow = new StatesDataModel();

                    DBEntity.PopulateObjectFromRow(l_StatesRow, l_Data, l_Row);

                    l_Response.StatesData.Add(l_StatesRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "States Data fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - StatesData is ready.");
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
        [Route("setCLTStatus")]
        public async Task<ResponseModel> SetCLTStatus(int CLTId, string Status,string CompletionDate)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ResponseModel l_Response = new ResponseModel();
            Result l_Result = new Result();
            try
            {
                CarrierLoadTender l_CLT = new CarrierLoadTender();
                string l_Criteria = string.Empty;

                l_Response.Code = (int)ResponseCodes.Error;

                l_CLT.UseConnection(CommonUtils.ConnectionString);

                l_CLT.GetObject(CLTId);

                if (l_CLT.TrackStatus == "D1")
                {
                     l_Result = l_CLT.SetCLTStatus(CLTId, "ReadyToComplete","");

                    if (l_Result.IsSuccess)
                    {
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = "Shipment status updated successfully! it will mark Complete after sending the EDI 214.";
                    }
                }
                else
                {
                    l_Result = l_CLT.SetCLTStatus(CLTId, Status, CompletionDate);
                    if (l_Result.IsSuccess)
                    {
                        l_Response.Code = (int)ResponseCodes.Success;
                        l_Response.Message = "Shipment status updated successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }


        [HttpGet]
        [Route("getItemTypes")]
        public async Task<GetItemTypesResponseModel> GetItemTypes(string ERPCustomerID = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetItemTypesResponseModel l_Response = new GetItemTypesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                SCS_ItemsType l_ItemsType = new SCS_ItemsType();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_ItemsType.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Item Types search.");

                l_ItemsType.GetList("CustomerID = '" + ERPCustomerID + "'", string.Empty, ref l_Data, "Item_Type ASC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Item Types searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Item Types.");

                l_Response.ItemTypes = new List<ItemTypesDataModel>();
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    ItemTypesDataModel l_ItemTypesRow = new ItemTypesDataModel();

                //    DBEntity.PopulateObjectFromRow(l_ItemTypesRow, l_Data, l_Row);

                //    l_Response.ItemTypes.Add(l_ItemTypesRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Item Types fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Item Types are ready.");
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
}
