using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;
using Intercom.Core;
using Intercom.Data;
using Newtonsoft.Json;
using System.Security.Claims;
using Renci.SshNet.Common;
using System.Linq;

namespace eSyncMate.Processor.Controllers
{
    [ApiController]
    [Route("api/v1/shipmentFromNDC")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class ShipmentFromNDCController : ControllerBase
    {
        private readonly ILogger<ShipmentFromNDCController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public ShipmentFromNDCController(ILogger<ShipmentFromNDCController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getShipmentFromNDC/{ShipmentID}/{TransactionDate}/{Status}/{PoNumber}")]
        public async Task<GetShipmentFromNDCResponseModel> GetShipmentFromNDC(string ShipmentID = "", string TransactionDate = "", string Status = "", string PoNumber = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetShipmentFromNDCResponseModel l_Response = new GetShipmentFromNDCResponseModel();
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
                ShipmentFromNDC l_ShipmentFromNDC = new ShipmentFromNDC();

                if (TransactionDate == "1999-01-01")
                {
                    TransactionDate = string.Empty;
                }
                
                if (ShipmentID == "EMPTY")
                {
                    ShipmentID = string.Empty;
                }
                if (Status == "EMPTY")
                {
                    Status = string.Empty;
                }

                if (PoNumber == "EMPTY")
                {
                    PoNumber = string.Empty;
                }

                if (Status == "EMPTY")
                {
                    Status = string.Empty;
                }

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_ShipmentFromNDC.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"Status <> 'DELETED'";

                if (!string.IsNullOrEmpty(ShipmentID))
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"ShipmentID LIKE '%{ShipmentID}%'";
                }

                if (!string.IsNullOrEmpty(Status))
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"Status = '{Status}'";
                }

               
                if (!string.IsNullOrEmpty(TransactionDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,TransactionDate) >= '{TransactionDate}'";
                }
                

                if (!string.IsNullOrEmpty(PoNumber))
                {
                    l_Criteria += $" AND PoNumber = '{PoNumber}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring ShipmentFromNDC search.");

                l_ShipmentFromNDC.GetViewList(l_Criteria, string.Empty, ref l_Data, "TransactionDate DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ShipmentFromNDCRow searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating ShipmentFromNDC.");

                l_Response.ShipmentFromNDC = l_Data;

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "ShipmentFromNDCRow fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ShipmentFromNDCRow are ready.");
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

        //[HttpGet]
        //[Route("getShipmentFromNDCFiles/{CustomerId}/{ItemId}/{BatchID}")]
        //public async Task<GetShipmentFromNDCFilesResponseModel> GetShipmentFromNDCFiles(string CustomerId, String ItemId, string BatchID)
        //{
        //    MethodBase l_Me = MethodBase.GetCurrentMethod();
        //    GetShipmentFromNDCFilesResponseModel l_Response = new GetShipmentFromNDCFilesResponseModel();
        //    DataTable l_Data = new DataTable();

        //    try
        //    {
        //        string l_Criteria = string.Empty;
        //        SCSShipmentFromNDCFeedData l_ShipmentFromNDCData = new SCSShipmentFromNDCFeedData();

        //        l_Response.Code = (int)ResponseCodes.Error;

        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

        //        l_ShipmentFromNDCData.UseConnection(CommonUtils.ConnectionString);

        //        l_Criteria = $"CustomerId = '{CustomerId}'";

        //        if (!string.IsNullOrEmpty(ItemId))
        //        {
        //            l_Criteria += $" AND ItemId = '{ItemId}'";
        //        }

        //        if (!string.IsNullOrEmpty(BatchID))
        //        {
        //            l_Criteria += $" AND BatchID = '{BatchID}'";
        //        }

        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring ShipmentFromNDC files search.");

        //        l_ShipmentFromNDCData.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id");

        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ShipmentFromNDC files searched {{{l_Data.Rows.Count}}}.");
        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating order files.");

        //        l_Response.Files = new List<ShipmentFromNDCFileModel>();
        //        foreach (DataRow l_Row in l_Data.Rows)
        //        {
        //            ShipmentFromNDCFileModel l_ShipmentFromNDCFile = new ShipmentFromNDCFileModel();

        //            DBEntity.PopulateObjectFromRow(l_ShipmentFromNDCFile, l_Data, l_Row);

        //            l_Response.Files.Add(l_ShipmentFromNDCFile);
        //        }

        //        l_Response.Code = (int)ResponseCodes.Success;
        //        l_Response.Message = "ShipmentFromNDC Files fetched successfully!";

        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ShipmentFromNDC Files are ready.");
        //    }
        //    catch (Exception ex)
        //    {
        //        l_Response.Code = (int)ResponseCodes.Exception;
        //        this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
        //    }
        //    finally
        //    {
        //        l_Data.Dispose();
        //    }

        //    return l_Response;
        //}

        [HttpGet]
        [Route("getBatchData/{BatchID}")]
        public async Task<GetShipmentFromNDCResponseModel> GetBatchData(string BatchID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetShipmentFromNDCResponseModel l_Response = new GetShipmentFromNDCResponseModel();
            DataTable l_Data = new DataTable();
            ShipmentFromNDC l_ShipmentFromNDC = new ShipmentFromNDC();

            try
            {
                string l_Criteria = string.Empty;

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_ShipmentFromNDC.UseConnection(CommonUtils.ConnectionString);

                l_ShipmentFromNDC.GetBatchWiseData($"ShipmentFromNDC_ID = '{BatchID}'", ref l_Data);

                l_Response.ShipmentDetailFromNDC = l_Data;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Batch Wise ShipmentFromNDC fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Batch Wise ShipmentFromNDC are ready.");
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

        //[HttpGet]
        //[Route("getBatchWiseItemID")]
        //public async Task<GetShipmentFromNDCResponseModel> getBatchWiseItemID(string ItemID, string BatchID)
        //{
        //    MethodBase l_Me = MethodBase.GetCurrentMethod();
        //    GetShipmentFromNDCResponseModel l_Response = new GetShipmentFromNDCResponseModel();
        //    DataTable l_Data = new DataTable();
        //    ShipmentFromNDC l_ShipmentFromNDC = new ShipmentFromNDC();

        //    try
        //    {
        //        string l_Criteria = string.Empty;
        //        SCSShipmentFromNDCFeedData l_ShipmentFromNDCData = new SCSShipmentFromNDCFeedData();

        //        l_Response.Code = (int)ResponseCodes.Error;

        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

        //        l_ShipmentFromNDC.UseConnection(CommonUtils.ConnectionString);

        //        l_ShipmentFromNDC.GetBatchWiseData($"BatchID = '{BatchID}' AND ItemID LIKE '%{ItemID}%'", ref l_Data);

        //        l_Response.BatchWiseShipmentFromNDC = l_Data;
        //        l_Response.Code = (int)ResponseCodes.Success;
        //        l_Response.Message = "Batch Wise ShipmentFromNDC fetched successfully!";

        //        this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Batch Wise ShipmentFromNDC are ready.");
        //    }
        //    catch (Exception ex)
        //    {
        //        l_Response.Code = (int)ResponseCodes.Exception;
        //        this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
        //    }
        //    finally
        //    {
        //        l_Data.Dispose();
        //    }

        //    return l_Response;
        //}


    }
}
