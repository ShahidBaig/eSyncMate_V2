using System;
using System.Collections.Generic;

namespace eSyncMate.Processor.Models
{
    public class RoleDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class GetRolesResponseModel : ResponseModel
    {
        public List<RoleDataModel> Roles { get; set; }
    }

    public class ModuleDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TranslationKey { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class GetModulesResponseModel : ResponseModel
    {
        public List<ModuleDataModel> Modules { get; set; }
    }

    public class MenuDataModel
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TranslationKey { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsExternalLink { get; set; }
        public string ExternalUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string Company { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class GetMenusResponseModel : ResponseModel
    {
        public List<MenuDataModel> Menus { get; set; }
    }

    public class RoleMenuDataModel
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int MenuId { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class GetRoleMenusResponseModel : ResponseModel
    {
        public List<RoleMenuDataModel> RoleMenus { get; set; }
    }

    public class SaveRoleMenusRequestModel
    {
        public int RoleId { get; set; }
        public List<RoleMenuItemModel> Menus { get; set; }
    }

    public class RoleMenuItemModel
    {
        public int MenuId { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class UserRoleDataModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class GetUserRoleResponseModel : ResponseModel
    {
        public List<UserRoleDataModel> UserRoles { get; set; }
    }

    public class SaveUserRoleRequestModel
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }

    // User menu tree models (returned after login)
    public class UserMenuModuleModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string ModuleTranslationKey { get; set; } = string.Empty;
        public string ModuleIcon { get; set; } = string.Empty;
        public int ModuleSortOrder { get; set; }
        public List<UserMenuItemModel> MenuItems { get; set; } = new List<UserMenuItemModel>();
    }

    public class UserMenuItemModel
    {
        public int MenuId { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public string MenuTranslationKey { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string MenuIcon { get; set; } = string.Empty;
        public bool IsExternalLink { get; set; }
        public string ExternalUrl { get; set; } = string.Empty;
        public int MenuSortOrder { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class GetUserMenusResponseModel : ResponseModel
    {
        public string RoleName { get; set; } = string.Empty;
        public List<UserMenuModuleModel> Modules { get; set; } = new List<UserMenuModuleModel>();
    }
}
