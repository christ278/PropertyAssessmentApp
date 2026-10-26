using System.Security.Claims;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class UserPermissionSummary
{
    public bool IsAuthenticated { get; set; }
    public bool IsRoleSet { get; set; }
    public string? RoleName { get; set; }
    public int? WorkflowRoleId { get; set; }
    public List<string> AllowedControllers { get; set; } = new();
    public List<string> AllowedCreateControllers { get; set; } = new();
    public List<string> RestrictedControllers { get; set; } = new();
}

public interface IPermissionService
{
    bool IsUserRoleSet(ClaimsPrincipal user);
    bool HasModuleAccess(ClaimsPrincipal user, string controllerName, string? actionName = null);
    bool CanAccess(ClaimsPrincipal user, string controllerName, string? actionName = null);
    UserPermissionSummary GetUserPermissionSummary(ClaimsPrincipal user);
    UserRoleSetting? GetUserRoleSetting(ClaimsPrincipal user);
}
