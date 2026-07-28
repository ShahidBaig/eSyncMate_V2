using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;
using System.Security.Claims;

namespace eSyncMate.Processor.Controllers
{
    /// <summary>
    /// Setup > Ship Nodes. Drives two tables through one screen:
    /// source = TARGETPLUS -> TargetPlusShipNodes, source = WALMART -> WalmartShipNodes.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class ShipNodesController : ControllerBase
    {
        private readonly ILogger<ShipNodesController> _logger;

        public ShipNodesController(ILogger<ShipNodesController> logger)
        {
            _logger = logger;
        }

        /// <summary>Restricts non-admin users to the customers assigned to them.</summary>
        private string GetCustomerFilter()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity == null)
            {
                return string.Empty;
            }

            var userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

            if (!userData.IsSuperAdmin && !string.IsNullOrEmpty(userData.Customers))
            {
                return $" AND CustomerID IN ({userData.Customers})";
            }

            return string.Empty;
        }

        private static string DuplicateMessage(string p_Clash, string p_WHSID, string p_ShipNode, string p_CustomerID)
        {
            return p_Clash == "WHSID"
                ? $"Warehouse [ {p_WHSID} ] already has a ship node for customer [ {p_CustomerID} ]."
                : $"Ship node [ {p_ShipNode} ] is already mapped to another warehouse of customer [ {p_CustomerID} ].";
        }

        [HttpGet]
        [Route("getShipNodes")]
        public async Task<GetShipNodeResponseModel> GetShipNodes([FromQuery] ShipNodeSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetShipNodeResponseModel l_Response = new GetShipNodeResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                ShipNodes l_ShipNodes = new ShipNodes(searchModel.Source);
                l_ShipNodes.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;

                string l_Criteria = " 1 = 1";

                if (!string.IsNullOrEmpty(searchModel.CustomerID) && searchModel.CustomerID != "EMPTY")
                {
                    l_Criteria += $" AND CustomerID = '{SqlSearchHelper.EscapeLiteral(searchModel.CustomerID)}'";
                }

                if (!string.IsNullOrEmpty(searchModel.SearchValue) && searchModel.SearchValue != "ALL")
                {
                    // Warehouse codes and ship nodes can contain LIKE wildcards
                    string l_Search = SqlSearchHelper.EscapeLike(searchModel.SearchValue);
                    l_Criteria += $" AND (WHSID LIKE '%{l_Search}%'{SqlSearchHelper.LikeEscapeClause}"
                                + $" OR ShipNode LIKE '%{l_Search}%'{SqlSearchHelper.LikeEscapeClause})";
                }

                l_Criteria += GetCustomerFilter();

                int totalCount = 0;

                // No source given -> both tables in one list
                bool l_Combined = string.IsNullOrEmpty(searchModel.Source);

                if (l_Combined)
                {
                    l_ShipNodes.GetCombinedListPaged(l_Criteria, ref l_Data, searchModel.PageNumber, searchModel.PageSize, out totalCount);
                }
                else
                {
                    l_ShipNodes.GetListPaged(l_Criteria, ref l_Data, searchModel.PageNumber, searchModel.PageSize, out totalCount);
                }

                l_Response.ShipNodes = new List<ShipNodeDataModel>();

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    l_Response.ShipNodes.Add(new ShipNodeDataModel
                    {
                        ID = PublicFunctions.ConvertNullAsInteger(l_Row["ID"], 0),
                        WHSID = PublicFunctions.ConvertNullAsString(l_Row["WHSID"], string.Empty),
                        ShipNode = PublicFunctions.ConvertNullAsString(l_Row["ShipNode"], string.Empty),
                        CustomerID = PublicFunctions.ConvertNullAsString(l_Row["CustomerID"], string.Empty),
                        APIName = PublicFunctions.ConvertNullAsString(l_Row["APIName"], string.Empty),
                        Source = l_Combined
                            ? PublicFunctions.ConvertNullAsString(l_Row["Source"], string.Empty)
                            : (ShipNodes.IsWalmart(searchModel.Source) ? ShipNodes.SourceWalmart : ShipNodes.SourceTargetPlus)
                    });
                }

                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Ship Nodes fetched successfully!";
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

        /// <summary>Distinct warehouses already mapped — used to populate the WHSID dropdown.</summary>
        [HttpGet]
        [Route("getWarehouses")]
        public async Task<GetWarehouseListResponseModel> GetWarehouses([FromQuery] string source = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetWarehouseListResponseModel l_Response = new GetWarehouseListResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                ShipNodes l_ShipNodes = new ShipNodes(source);
                l_ShipNodes.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;
                l_Response.Warehouses = new List<string>();

                if (string.IsNullOrEmpty(source))
                {
                    l_ShipNodes.GetCombinedWarehouses(ref l_Data);
                }
                else
                {
                    l_ShipNodes.GetWarehouses(ref l_Data);
                }

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    l_Response.Warehouses.Add(PublicFunctions.ConvertNullAsString(l_Row["WHSID"], string.Empty));
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Warehouses fetched successfully!";
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

        /// <summary>Customers that already have ship nodes — populates the list screen filter.</summary>
        [HttpGet]
        [Route("getCustomers")]
        public async Task<GetShipNodeCustomersResponseModel> GetCustomers([FromQuery] string source = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetShipNodeCustomersResponseModel l_Response = new GetShipNodeCustomersResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                ShipNodes l_ShipNodes = new ShipNodes(source);
                l_ShipNodes.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;
                l_Response.Customers = new List<string>();

                if (string.IsNullOrEmpty(source))
                {
                    l_ShipNodes.GetCombinedCustomers(ref l_Data);
                }
                else
                {
                    l_ShipNodes.GetCustomers(ref l_Data);
                }

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    l_Response.Customers.Add(PublicFunctions.ConvertNullAsString(l_Row["CustomerID"], string.Empty));
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Customers fetched successfully!";
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
        [Route("createShipNode")]
        public async Task<ShipNodesResponseModel> CreateShipNode([FromBody] SaveShipNodeDataModel shipNodeModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ShipNodesResponseModel l_Response = new ShipNodesResponseModel();
            Result l_Result = new Result();

            try
            {
                // Source is derived from the customer's marketplace (Walmart -> WalmartShipNodes,
                // everyone else -> TargetPlusShipNodes) so the user no longer picks a Partner on screen.
                shipNodeModel.Source = DeriveSourceFromCustomer(shipNodeModel.CustomerID);
                shipNodeModel.APIName = ShipNodes.IsWalmart(shipNodeModel.Source) ? "WalmartAPI" : string.Empty;

                ShipNodes l_ShipNodes = new ShipNodes(shipNodeModel.Source);
                l_ShipNodes.UseConnection(CommonUtils.ConnectionString);

                string l_Clash = l_ShipNodes.GetDuplicate(shipNodeModel.CustomerID, shipNodeModel.WHSID, shipNodeModel.ShipNode);

                if (!string.IsNullOrEmpty(l_Clash))
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = DuplicateMessage(l_Clash, shipNodeModel.WHSID, shipNodeModel.ShipNode, shipNodeModel.CustomerID);

                    return l_Response;
                }

                l_ShipNodes.WHSID = shipNodeModel.WHSID;
                l_ShipNodes.ShipNode = shipNodeModel.ShipNode;
                l_ShipNodes.CustomerID = shipNodeModel.CustomerID;
                l_ShipNodes.APIName = shipNodeModel.APIName;

                l_Result = l_ShipNodes.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Ship node for warehouse [ {shipNodeModel.WHSID} ] has been created successfully!";
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

        /// <summary>
        /// Determine the ship-node source (table) from the customer's marketplace:
        /// Walmart customers -> WalmartShipNodes, everyone else -> TargetPlusShipNodes.
        /// </summary>
        private string DeriveSourceFromCustomer(string customerID)
        {
            try
            {
                Customers l_Customer = new Customers();
                l_Customer.UseConnection(CommonUtils.ConnectionString);
                if (l_Customer.GetObject("ERPCustomerID", customerID).IsSuccess)
                {
                    string l_Market = (l_Customer.Marketplace ?? string.Empty).ToUpperInvariant();
                    if (l_Market.Contains("WALMART"))
                        return ShipNodes.SourceWalmart;
                }
            }
            catch { /* fall through to default */ }

            // Fallback: WAL* customer ids are Walmart too
            if (!string.IsNullOrEmpty(customerID) && customerID.ToUpperInvariant().StartsWith("WAL"))
                return ShipNodes.SourceWalmart;

            return ShipNodes.SourceTargetPlus;
        }

        [HttpPost]
        [Route("updateShipNode")]
        public async Task<ShipNodesResponseModel> UpdateShipNode([FromBody] UpdateShipNodeDataModel shipNodeModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ShipNodesResponseModel l_Response = new ShipNodesResponseModel();
            Result l_Result = new Result();

            try
            {
                ShipNodes l_ShipNodes = new ShipNodes(shipNodeModel.Source);
                l_ShipNodes.UseConnection(CommonUtils.ConnectionString);

                string l_Clash = l_ShipNodes.GetDuplicate(shipNodeModel.CustomerID, shipNodeModel.WHSID, shipNodeModel.ShipNode, shipNodeModel.ID);

                if (!string.IsNullOrEmpty(l_Clash))
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = DuplicateMessage(l_Clash, shipNodeModel.WHSID, shipNodeModel.ShipNode, shipNodeModel.CustomerID);

                    return l_Response;
                }

                l_ShipNodes.ID = shipNodeModel.ID;
                l_ShipNodes.WHSID = shipNodeModel.WHSID;
                l_ShipNodes.ShipNode = shipNodeModel.ShipNode;
                l_ShipNodes.CustomerID = shipNodeModel.CustomerID;
                l_ShipNodes.APIName = shipNodeModel.APIName;

                l_Result = l_ShipNodes.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Ship node for warehouse [ {shipNodeModel.WHSID} ] has been updated successfully!";
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
        [Route("deleteShipNode/{id}")]
        public async Task<ShipNodesResponseModel> DeleteShipNode(int id, [FromQuery] string source = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ShipNodesResponseModel l_Response = new ShipNodesResponseModel();
            Result l_Result = new Result();

            try
            {
                ShipNodes l_ShipNodes = new ShipNodes(source);
                l_ShipNodes.UseConnection(CommonUtils.ConnectionString);

                if (!l_ShipNodes.GetObject(id).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.NotFound;
                    l_Response.Description = $"Ship node [ {id} ] was not found.";

                    return l_Response;
                }

                l_ShipNodes.ID = id;
                l_Result = l_ShipNodes.Delete();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Ship node for warehouse [ {l_ShipNodes.WHSID} ] has been deleted successfully!";
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
