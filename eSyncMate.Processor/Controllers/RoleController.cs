using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class RoleController : ControllerBase
    {
        private readonly ILogger<RoleController> _logger;

        public RoleController(ILogger<RoleController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getRoles")]
        public async Task<GetRolesResponseModel> GetRoles()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetRolesResponseModel l_Response = new GetRolesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                Roles l_Entity = new Roles();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.GetList("", string.Empty, ref l_Data, "Id ASC");

                l_Response.Roles = new List<RoleDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    RoleDataModel l_Item = new RoleDataModel();
                    DBEntity.PopulateObjectFromRow(l_Item, l_Data, l_Row);
                    l_Response.Roles.Add(l_Item);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Roles fetched successfully!";
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
        [Route("saveRole")]
        public async Task<ResponseModel> SaveRole([FromBody] RoleDataModel model)
        {
            ResponseModel l_Response = new ResponseModel();
            Result l_Result = new Result();

            try
            {
                Roles l_Entity = new Roles();
                l_Entity.UseConnection(CommonUtils.ConnectionString);

                l_Entity.Name = model.Name;
                l_Entity.Description = model.Description;
                l_Entity.IsActive = model.IsActive;
                l_Entity.CreatedDate = DateTime.Now;
                l_Entity.CreatedBy = model.CreatedBy;

                if (model.Id > 0)
                {
                    l_Entity.Id = model.Id;
                    l_Result = l_Entity.Modify();
                }
                else
                {
                    l_Result = l_Entity.SaveNew();
                }

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = model.Id > 0 ? "Role updated successfully!" : "Role created successfully!";
                    l_Response.Description = l_Entity.Id.ToString();
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
        [Route("deleteRole")]
        public async Task<ResponseModel> DeleteRole([FromBody] RoleDataModel model)
        {
            ResponseModel l_Response = new ResponseModel();

            try
            {
                Roles l_Entity = new Roles();
                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.Id = model.Id;

                Result l_Result = l_Entity.Delete();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "Role deleted successfully!";
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

        [HttpGet]
        [Route("getModules")]
        public async Task<GetModulesResponseModel> GetModules()
        {
            GetModulesResponseModel l_Response = new GetModulesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                Modules l_Entity = new Modules();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.GetList("IsActive = 1", string.Empty, ref l_Data, "SortOrder ASC");

                l_Response.Modules = new List<ModuleDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    ModuleDataModel l_Item = new ModuleDataModel();
                    DBEntity.PopulateObjectFromRow(l_Item, l_Data, l_Row);
                    l_Response.Modules.Add(l_Item);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Modules fetched successfully!";
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
        [Route("getMenus")]
        public async Task<GetMenusResponseModel> GetMenus()
        {
            GetMenusResponseModel l_Response = new GetMenusResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                Menus l_Entity = new Menus();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.GetList("IsActive = 1 AND (IsHidden = 0 OR IsHidden IS NULL)", string.Empty, ref l_Data, "ModuleId ASC, SortOrder ASC");

                l_Response.Menus = new List<MenuDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    MenuDataModel l_Item = new MenuDataModel();
                    DBEntity.PopulateObjectFromRow(l_Item, l_Data, l_Row);
                    l_Response.Menus.Add(l_Item);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Menus fetched successfully!";
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
        [Route("getRoleMenus")]
        public async Task<GetRoleMenusResponseModel> GetRoleMenus(int roleId)
        {
            GetRoleMenusResponseModel l_Response = new GetRoleMenusResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                RoleMenus l_Entity = new RoleMenus();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.GetList($"RoleId = {roleId}", string.Empty, ref l_Data);

                l_Response.RoleMenus = new List<RoleMenuDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    RoleMenuDataModel l_Item = new RoleMenuDataModel();
                    DBEntity.PopulateObjectFromRow(l_Item, l_Data, l_Row);
                    l_Response.RoleMenus.Add(l_Item);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Role menus fetched successfully!";
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

        [HttpPost]
        [Route("saveRoleMenus")]
        public async Task<ResponseModel> SaveRoleMenus([FromBody] SaveRoleMenusRequestModel model)
        {
            ResponseModel l_Response = new ResponseModel();

            try
            {
                RoleMenus l_Entity = new RoleMenus();
                l_Entity.UseConnection(CommonUtils.ConnectionString);

                // Delete existing role menus
                l_Entity.DeleteByRoleId(model.RoleId);

                // Insert new role menus
                foreach (var menuItem in model.Menus)
                {
                    RoleMenus l_NewEntity = new RoleMenus();
                    l_NewEntity.UseConnection(CommonUtils.ConnectionString);
                    l_NewEntity.RoleId = model.RoleId;
                    l_NewEntity.MenuId = menuItem.MenuId;
                    l_NewEntity.CanView = menuItem.CanView;
                    l_NewEntity.CanAdd = menuItem.CanAdd;
                    l_NewEntity.CanEdit = menuItem.CanEdit;
                    l_NewEntity.CanDelete = menuItem.CanDelete;
                    l_NewEntity.CreatedDate = DateTime.Now;
                    l_NewEntity.CreatedBy = 0;

                    l_NewEntity.SaveNew();
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Role menus saved successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getUserRole")]
        public async Task<GetUserRoleResponseModel> GetUserRole(int userId)
        {
            GetUserRoleResponseModel l_Response = new GetUserRoleResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                UserRoles l_Entity = new UserRoles();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.GetList($"UserId = {userId}", string.Empty, ref l_Data);

                l_Response.UserRoles = new List<UserRoleDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    UserRoleDataModel l_Item = new UserRoleDataModel();
                    DBEntity.PopulateObjectFromRow(l_Item, l_Data, l_Row);
                    l_Response.UserRoles.Add(l_Item);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "User role fetched successfully!";
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

        [HttpPost]
        [Route("saveUserRole")]
        public async Task<ResponseModel> SaveUserRole([FromBody] SaveUserRoleRequestModel model)
        {
            ResponseModel l_Response = new ResponseModel();

            try
            {
                UserRoles l_Entity = new UserRoles();
                l_Entity.UseConnection(CommonUtils.ConnectionString);

                // Delete existing user role
                l_Entity.DeleteByUserId(model.UserId);

                // Insert new user role
                UserRoles l_NewEntity = new UserRoles();
                l_NewEntity.UseConnection(CommonUtils.ConnectionString);
                l_NewEntity.UserId = model.UserId;
                l_NewEntity.RoleId = model.RoleId;
                l_NewEntity.CreatedDate = DateTime.Now;
                l_NewEntity.CreatedBy = 0;

                Result l_Result = l_NewEntity.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "User role saved successfully!";
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

        [HttpGet]
        [Route("getUserMenus")]
        public async Task<GetUserMenusResponseModel> GetUserMenus(int userId, string company = "")
        {
            GetUserMenusResponseModel l_Response = new GetUserMenusResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                RoleMenus l_Entity = new RoleMenus();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);

                string criteria = $"UserId = {userId}";
                if (!string.IsNullOrEmpty(company))
                {
                    criteria += $" AND (Company IS NULL OR Company = '' OR Company LIKE '%{company}%')";
                }
                l_Entity.GetViewList(criteria, string.Empty, ref l_Data, "ModuleSortOrder ASC, MenuSortOrder ASC");

                var moduleDict = new Dictionary<int, UserMenuModuleModel>();

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    int moduleId = Convert.ToInt32(l_Row["ModuleId"]);
                    string roleName = l_Row["RoleName"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(l_Response.RoleName))
                        l_Response.RoleName = roleName;

                    if (!moduleDict.ContainsKey(moduleId))
                    {
                        moduleDict[moduleId] = new UserMenuModuleModel
                        {
                            ModuleId = moduleId,
                            ModuleName = l_Row["ModuleName"]?.ToString() ?? "",
                            ModuleTranslationKey = l_Row["ModuleTranslationKey"]?.ToString() ?? "",
                            ModuleIcon = l_Row["ModuleIcon"]?.ToString() ?? "",
                            ModuleSortOrder = Convert.ToInt32(l_Row["ModuleSortOrder"]),
                            MenuItems = new List<UserMenuItemModel>()
                        };
                    }

                    moduleDict[moduleId].MenuItems.Add(new UserMenuItemModel
                    {
                        MenuId = Convert.ToInt32(l_Row["MenuId"]),
                        MenuName = l_Row["MenuName"]?.ToString() ?? "",
                        MenuTranslationKey = l_Row["MenuTranslationKey"]?.ToString() ?? "",
                        Route = l_Row["Route"]?.ToString() ?? "",
                        MenuIcon = l_Row["MenuIcon"]?.ToString() ?? "",
                        IsExternalLink = Convert.ToBoolean(l_Row["IsExternalLink"]),
                        ExternalUrl = l_Row["ExternalUrl"] != DBNull.Value ? l_Row["ExternalUrl"]?.ToString() ?? "" : "",
                        MenuSortOrder = Convert.ToInt32(l_Row["MenuSortOrder"]),
                        CanView = Convert.ToBoolean(l_Row["CanView"]),
                        CanAdd = Convert.ToBoolean(l_Row["CanAdd"]),
                        CanEdit = Convert.ToBoolean(l_Row["CanEdit"]),
                        CanDelete = Convert.ToBoolean(l_Row["CanDelete"])
                    });
                }

                l_Response.Modules = moduleDict.Values.OrderBy(m => m.ModuleSortOrder).ToList();
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "User menus fetched successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                this._logger.LogCritical($"[RoleController.GetUserMenus] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getHiddenMenus")]
        public async Task<GetHiddenMenusResponseModel> GetHiddenMenus()
        {
            GetHiddenMenusResponseModel l_Response = new GetHiddenMenusResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                Menus l_Entity = new Menus();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.GetList("IsActive = 1 AND IsHidden = 1", string.Empty, ref l_Data, "ModuleId ASC, SortOrder ASC");

                l_Response.Menus = new List<MenuDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    MenuDataModel l_Item = new MenuDataModel();
                    DBEntity.PopulateObjectFromRow(l_Item, l_Data, l_Row);
                    l_Response.Menus.Add(l_Item);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Hidden menus fetched successfully!";
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
        [Route("getUserMenusDirect")]
        public async Task<GetUserMenusDirectResponseModel> GetUserMenusDirect(int userId)
        {
            GetUserMenusDirectResponseModel l_Response = new GetUserMenusDirectResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                UserMenus l_Entity = new UserMenus();
                l_Response.Code = (int)ResponseCodes.Error;

                l_Entity.UseConnection(CommonUtils.ConnectionString);
                l_Entity.GetList($"UserId = {userId}", string.Empty, ref l_Data);

                l_Response.UserMenus = new List<UserMenuDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    UserMenuDataModel l_Item = new UserMenuDataModel();
                    DBEntity.PopulateObjectFromRow(l_Item, l_Data, l_Row);
                    l_Response.UserMenus.Add(l_Item);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "User direct menus fetched successfully!";
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

        [HttpPost]
        [Route("saveUserMenusDirect")]
        public async Task<ResponseModel> SaveUserMenusDirect([FromBody] SaveUserMenusDirectRequestModel model)
        {
            ResponseModel l_Response = new ResponseModel();

            try
            {
                UserMenus l_Entity = new UserMenus();
                l_Entity.UseConnection(CommonUtils.ConnectionString);

                // Delete existing direct menu assignments for this user
                l_Entity.DeleteByUserId(model.UserId);

                // Insert new direct menu assignments
                foreach (var menuItem in model.Menus)
                {
                    UserMenus l_NewEntity = new UserMenus();
                    l_NewEntity.UseConnection(CommonUtils.ConnectionString);
                    l_NewEntity.UserId = model.UserId;
                    l_NewEntity.MenuId = menuItem.MenuId;
                    l_NewEntity.CanView = menuItem.CanView;
                    l_NewEntity.CanAdd = menuItem.CanAdd;
                    l_NewEntity.CanEdit = menuItem.CanEdit;
                    l_NewEntity.CanDelete = menuItem.CanDelete;
                    l_NewEntity.CreatedDate = DateTime.Now;
                    l_NewEntity.CreatedBy = 0;

                    l_NewEntity.SaveNew();
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "User direct menus saved successfully!";
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
