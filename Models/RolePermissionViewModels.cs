using System.Collections.Generic;

namespace PropertyAssessmentApp.Models;

public class ModulePermissionDTO
{
    public int ModuleId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public int PermissionId { get; set; }
    public bool IsAllowed { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
}

public class RolePermissionCategoryGroupDTO
{
    public string CategoryName { get; set; } = string.Empty;
    public List<ModulePermissionDTO> Modules { get; set; } = new();
}

public class SaveRolePermissionsRequest
{
    public Guid RoleId { get; set; }
    public List<ModulePermissionSaveItem> Permissions { get; set; } = new();
}

public class ModulePermissionSaveItem
{
    public int ModuleId { get; set; }
    public bool IsAllowed { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
}
