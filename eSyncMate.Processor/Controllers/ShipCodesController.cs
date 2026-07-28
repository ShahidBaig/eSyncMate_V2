using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;

namespace eSyncMate.Processor.Controllers
{
    /// <summary>
    /// Setup > ShipCodes Mapping. Customer-wise ShippingMethod -> LevelOfService / ShipMethodCode
    /// rows that SP_OrdersData (ASN) reads through dbo.fn_ResolveShipCode. CustomerID '*' is the
    /// generic fallback. Kong blocks PUT/DELETE, so create/update/delete are all POST.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class ShipCodesController : ControllerBase
    {
        private readonly ILogger<ShipCodesController> _logger;

        public ShipCodesController(ILogger<ShipCodesController> logger)
        {
            _logger = logger;
        }

        /// <summary>Same CustomerID + SourceMethod + MatchType + IsDefault already mapped?</summary>
        private static string DuplicateCriteria(string customerID, string sourceMethod, string matchType, bool isDefault)
        {
            string l_Criteria = $"CustomerID = '{SqlSearchHelper.EscapeLiteral(customerID)}'"
                              + $" AND MatchType = '{SqlSearchHelper.EscapeLiteral(matchType)}'"
                              + $" AND IsDefault = {(isDefault ? 1 : 0)}"
                              + $" AND ISNULL(SourceMethod, '') = '{SqlSearchHelper.EscapeLiteral(sourceMethod ?? string.Empty)}'";

            return l_Criteria;
        }

        /// <summary>A customer may have exactly one default (fallback) row.</summary>
        private static string DefaultExistsCriteria(string customerID, int excludeId = 0)
        {
            string l_Criteria = $"CustomerID = '{SqlSearchHelper.EscapeLiteral(customerID)}' AND IsDefault = 1";
            if (excludeId > 0) l_Criteria += $" AND Id <> {excludeId}";
            return l_Criteria;
        }

        private bool DefaultAlreadyExists(ShipCodeMappings entity, string customerID, int excludeId = 0)
        {
            DataTable l_Def = new DataTable();
            entity.GetList(DefaultExistsCriteria(customerID, excludeId), "Id", ref l_Def);
            bool l_Exists = l_Def.Rows.Count > 0;
            l_Def.Dispose();
            return l_Exists;
        }

        [HttpGet]
        [Route("getShipCodes")]
        public async Task<GetShipCodeResponseModel> GetShipCodes([FromQuery] ShipCodeSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetShipCodeResponseModel l_Response = new GetShipCodeResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                ShipCodeMappings l_ShipCodes = new ShipCodeMappings();
                l_ShipCodes.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;

                string l_Criteria = " 1 = 1";

                if (!string.IsNullOrEmpty(searchModel.CustomerID) && searchModel.CustomerID != "EMPTY")
                {
                    l_Criteria += $" AND CustomerID = '{SqlSearchHelper.EscapeLiteral(searchModel.CustomerID)}'";
                }

                if (!string.IsNullOrEmpty(searchModel.SearchValue) && searchModel.SearchValue != "ALL")
                {
                    // The search box is labelled Source Method, so match that column only.
                    string l_Search = SqlSearchHelper.EscapeLike(searchModel.SearchValue);
                    l_Criteria += $" AND SourceMethod LIKE '%{l_Search}%'{SqlSearchHelper.LikeEscapeClause}";
                }

                int totalCount = 0;

                // Group a customer's rows together: its own rows, defaults last, then by method.
                l_ShipCodes.GetViewListPaged(l_Criteria, string.Empty, ref l_Data,
                    "CustomerID, IsDefault, SourceMethod", searchModel.PageNumber, searchModel.PageSize, out totalCount);

                l_Response.ShipCodes = new List<ShipCodeDataModel>();

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    l_Response.ShipCodes.Add(new ShipCodeDataModel
                    {
                        Id = PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0),
                        CustomerID = PublicFunctions.ConvertNullAsString(l_Row["CustomerID"], string.Empty),
                        SourceMethod = PublicFunctions.ConvertNullAsString(l_Row["SourceMethod"], string.Empty),
                        MatchType = PublicFunctions.ConvertNullAsString(l_Row["MatchType"], string.Empty),
                        LevelOfService = PublicFunctions.ConvertNullAsString(l_Row["LevelOfService"], string.Empty),
                        ShippingMethod = PublicFunctions.ConvertNullAsString(l_Row["ShippingMethod"], string.Empty),
                        IsDefault = PublicFunctions.ConvertNullAsInteger(l_Row["IsDefault"], 0) == 1,
                        Priority = PublicFunctions.ConvertNullAsInteger(l_Row["Priority"], 1),
                        IsActive = PublicFunctions.ConvertNullAsInteger(l_Row["IsActive"], 0) == 1
                    });
                }

                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Ship codes fetched successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpPost]
        [Route("createShipCode")]
        public async Task<ShipCodesResponseModel> CreateShipCode([FromBody] SaveShipCodeDataModel shipCodeModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ShipCodesResponseModel l_Response = new ShipCodesResponseModel();
            Result l_Result = new Result();

            try
            {
                ShipCodeMappings l_ShipCodes = new ShipCodeMappings();
                l_ShipCodes.UseConnection(CommonUtils.ConnectionString);

                // Check 1: a default row is the customer's single fallback — one per customer, Source Method blank.
                if (shipCodeModel.IsDefault)
                {
                    shipCodeModel.SourceMethod = null;

                    if (DefaultAlreadyExists(l_ShipCodes, shipCodeModel.CustomerID))
                    {
                        l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                        l_Response.Description = $"Customer [ {shipCodeModel.CustomerID} ] already has a Default row. Only one Default is allowed per customer.";

                        return l_Response;
                    }
                }

                // Check 2: the same customer + source method + match type must not already be mapped.
                DataTable l_Dup = new DataTable();
                l_ShipCodes.GetList(DuplicateCriteria(shipCodeModel.CustomerID, shipCodeModel.SourceMethod, shipCodeModel.MatchType, shipCodeModel.IsDefault), "Id", ref l_Dup);
                bool l_Exists = l_Dup.Rows.Count > 0;
                l_Dup.Dispose();

                if (l_Exists)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"A mapping for customer [ {shipCodeModel.CustomerID} ] and source method [ {shipCodeModel.SourceMethod} ] already exists.";

                    return l_Response;
                }

                PublicFunctions.CopyTo(shipCodeModel, l_ShipCodes);

                l_ShipCodes.CreatedDate = DateTime.Now;

                l_Result = l_ShipCodes.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Ship code mapping for customer [ {shipCodeModel.CustomerID} ] has been created successfully!";
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
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        [HttpPost]
        [Route("updateShipCode")]
        public async Task<ShipCodesResponseModel> UpdateShipCode([FromBody] UpdateShipCodeDataModel shipCodeModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ShipCodesResponseModel l_Response = new ShipCodesResponseModel();
            Result l_Result = new Result();

            try
            {
                ShipCodeMappings l_ShipCodes = new ShipCodeMappings();
                l_ShipCodes.UseConnection(CommonUtils.ConnectionString);

                // Check 1: a default row is the customer's single fallback — one per customer, Source Method blank.
                if (shipCodeModel.IsDefault)
                {
                    shipCodeModel.SourceMethod = null;

                    if (DefaultAlreadyExists(l_ShipCodes, shipCodeModel.CustomerID, shipCodeModel.Id))
                    {
                        l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                        l_Response.Description = $"Customer [ {shipCodeModel.CustomerID} ] already has a Default row. Only one Default is allowed per customer.";

                        return l_Response;
                    }
                }

                // Check 2: the same customer + source method + match type must not already be mapped.
                DataTable l_Dup = new DataTable();
                l_ShipCodes.GetList(DuplicateCriteria(shipCodeModel.CustomerID, shipCodeModel.SourceMethod, shipCodeModel.MatchType, shipCodeModel.IsDefault) + $" AND Id <> {shipCodeModel.Id}", "Id", ref l_Dup);
                bool l_Exists = l_Dup.Rows.Count > 0;
                l_Dup.Dispose();

                if (l_Exists)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"A mapping for customer [ {shipCodeModel.CustomerID} ] and source method [ {shipCodeModel.SourceMethod} ] already exists.";

                    return l_Response;
                }

                PublicFunctions.CopyTo(shipCodeModel, l_ShipCodes);

                l_ShipCodes.ModifiedDate = DateTime.Now;

                l_Result = l_ShipCodes.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Ship code mapping for customer [ {shipCodeModel.CustomerID} ] has been updated successfully!";
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
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        // POST, not DELETE — the Kong proxy blocks DELETE
        [HttpPost]
        [Route("deleteShipCode/{id}")]
        public async Task<ShipCodesResponseModel> DeleteShipCode(int id)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ShipCodesResponseModel l_Response = new ShipCodesResponseModel();
            Result l_Result = new Result();

            try
            {
                ShipCodeMappings l_ShipCodes = new ShipCodeMappings();
                l_ShipCodes.UseConnection(CommonUtils.ConnectionString);

                if (!l_ShipCodes.GetObject(id).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.NotFound;
                    l_Response.Description = $"Ship code mapping [ {id} ] was not found.";

                    return l_Response;
                }

                l_ShipCodes.Id = id;
                l_Result = l_ShipCodes.Delete();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Ship code mapping has been deleted successfully!";
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
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }
    }
}
