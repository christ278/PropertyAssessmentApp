using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public UserRoleSetting? GetUserRoleSetting(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        bool isSystemAdmin = roleClaim == nameof(UserRole.SystemAdmin) || user.IsInRole("SystemAdmin") || user.Identity?.Name == "admin";

        if (isSystemAdmin)
        {
            // System Administrator role
            return _context.UserRoleSettings.FirstOrDefault(r =>
                r.Id == Guid.Parse("50147D2F-24FE-4FA9-B270-9F217F58D333") ||
                r.RoleName == "System Administrator");
        }

        var workflowRoleClaim = user.FindFirst("WorkflowRoleId")?.Value;
        if (int.TryParse(workflowRoleClaim, out var statusId) && statusId > 0)
        {
            var byStatus = _context.UserRoleSettings.FirstOrDefault(r => r.TransactionStatusId == statusId && r.IsActive);
            if (byStatus != null) return byStatus;
        }

        // Fallback: match by Role claim/name if workflow status is not explicitly mapped
        if (!string.IsNullOrWhiteSpace(roleClaim))
        {
            var byRole = _context.UserRoleSettings.FirstOrDefault(r =>
                (r.RoleName == roleClaim ||
                 r.RoleName.Replace(" ", "").Equals(roleClaim, StringComparison.OrdinalIgnoreCase)) &&
                r.IsActive);
            if (byRole != null) return byRole;
        }

        return null;
    }

    public bool IsUserRoleSet(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        bool isSystemAdmin = roleClaim == nameof(UserRole.SystemAdmin) || user.IsInRole("SystemAdmin") || user.Identity?.Name == "admin";
        if (isSystemAdmin)
        {
            return true;
        }

        var roleSetting = GetUserRoleSetting(user);
        if (roleSetting == null)
        {
            return false;
        }

        // Verify if this role has at least ONE permission check in the system
        return _context.RoleModulePermissions.Any(p =>
            p.RoleId == roleSetting.Id &&
            (p.CanView || p.CanCreate || p.CanEdit || p.CanDelete || p.CanApprove));
    }

    public bool CanAccess(ClaimsPrincipal user, string controllerName, string? actionName = null)
    {
        return HasModuleAccess(user, controllerName, actionName);
    }

    public bool HasModuleAccess(ClaimsPrincipal user, string controllerName, string? actionName = null)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Core infrastructure unrestricted endpoints
        if (controllerName.Equals("Account", StringComparison.OrdinalIgnoreCase) ||
            controllerName.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
            controllerName.Equals("Location", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        bool isSystemAdmin = roleClaim == nameof(UserRole.SystemAdmin) || user.IsInRole("SystemAdmin") || user.Identity?.Name == "admin";

        // System Administrator can always access all forms
        if (isSystemAdmin)
        {
            return true;
        }

        var roleSetting = GetUserRoleSetting(user);
        if (roleSetting == null)
        {
            return false;
        }

        // Workflow Queues (Pool & MyTransactions) & Settings
        if (controllerName.Equals("Workflow", StringComparison.OrdinalIgnoreCase))
        {
            if (actionName != null && actionName.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                var settingsMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "WORKFLOW_SETTINGS");
                if (settingsMod == null) return false;
                return HasModuleRecordAccess(roleSetting.Id, settingsMod.ModuleId);
            }

            var poolMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "POOL_TRANSACTIONS");
            var myMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "MY_TRANSACTIONS");

            bool hasPoolAccess = poolMod != null && HasModuleRecordAccess(roleSetting.Id, poolMod.ModuleId);
            bool hasMyAccess = myMod != null && HasModuleRecordAccess(roleSetting.Id, myMod.ModuleId);

            // Pool Transactions Queue: requires Pool permission
            if (actionName != null && actionName.Equals("Pool", StringComparison.OrdinalIgnoreCase))
            {
                return hasPoolAccess;
            }

            // My Transactions:
            // RULE: "if the user has access in pool transaction it should have access in my transaction"
            if (actionName != null && actionName.Equals("MyTransactions", StringComparison.OrdinalIgnoreCase))
            {
                return hasMyAccess || hasPoolAccess;
            }

            // Claim action:
            // RULE: "if the user has access in pool transaction it should allow to accept it going to user my transaction"
            if (actionName != null && actionName.Equals("Claim", StringComparison.OrdinalIgnoreCase))
            {
                return hasPoolAccess;
            }

            // Other workflow actions (Forward, Defer, Release, History, Index)
            return hasMyAccess || hasPoolAccess;
        }

        // User Roles & Dynamic Permissions
        if (controllerName.Equals("UserRole", StringComparison.OrdinalIgnoreCase))
        {
            if (actionName != null && actionName.Equals("Permissions", StringComparison.OrdinalIgnoreCase))
            {
                var permMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "ROLE_PERMISSIONS");
                if (permMod == null) return false;
                return HasModuleRecordAccess(roleSetting.Id, permMod.ModuleId);
            }

            var roleMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "USER_ROLES");
            if (roleMod == null) return false;
            return HasModuleRecordAccess(roleSetting.Id, roleMod.ModuleId);
        }

        var module = GetModuleForRequest(controllerName, actionName);
        if (module == null)
        {
            return true;
        }

        // PER FORM ACCESS:
        // If the user has access to this form, ALL operations (View, Create, Edit/Update, Inactive/Delete, Details) are allowed!
        return HasModuleRecordAccess(roleSetting.Id, module.ModuleId);
    }

    private bool HasModuleRecordAccess(Guid roleId, int moduleId)
    {
        var perm = _context.RoleModulePermissions.FirstOrDefault(p =>
            p.RoleId == roleId &&
            p.ModuleId == moduleId);

        if (perm == null)
        {
            return false;
        }

        return perm.CanView || perm.CanCreate || perm.CanEdit || perm.CanDelete || perm.CanApprove;
    }

    public UserPermissionSummary GetUserPermissionSummary(ClaimsPrincipal user)
    {
        var summary = new UserPermissionSummary
        {
            IsAuthenticated = user.Identity?.IsAuthenticated == true
        };

        if (!summary.IsAuthenticated)
        {
            return summary;
        }

        var roleSetting = GetUserRoleSetting(user);
        summary.RoleName = roleSetting?.RoleName ?? user.FindFirst("WorkflowRoleName")?.Value;

        var workflowRoleClaim = user.FindFirst("WorkflowRoleId")?.Value;
        if (int.TryParse(workflowRoleClaim, out var statusId) && statusId > 0)
        {
            summary.WorkflowRoleId = statusId;
        }

        var allModules = _context.SystemModules
            .Where(m => m.IsActive && !string.IsNullOrEmpty(m.ControllerName))
            .ToList();

        summary.RestrictedControllers = allModules
            .Select(m => m.ControllerName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Check if role is set and has at least one check configured
        summary.IsRoleSet = roleSetting != null && _context.RoleModulePermissions.Any(p =>
            p.RoleId == roleSetting.Id &&
            (p.CanView || p.CanCreate || p.CanEdit || p.CanDelete || p.CanApprove));

        if (roleSetting == null || !summary.IsRoleSet)
        {
            return summary;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        bool isSystemAdmin = roleClaim == nameof(UserRole.SystemAdmin) || user.IsInRole("SystemAdmin") || user.Identity?.Name == "admin";

        if (isSystemAdmin)
        {
            summary.AllowedControllers.Add("UserRole");
            summary.AllowedCreateControllers.Add("UserRole");
        }

        var permissions = _context.RoleModulePermissions
            .Where(p => p.RoleId == roleSetting.Id)
            .ToList();

        foreach (var mod in allModules)
        {
            if (string.IsNullOrEmpty(mod.ControllerName)) continue;
            var perm = permissions.FirstOrDefault(p => p.ModuleId == mod.ModuleId);
            if (perm == null) continue;

            bool hasAnyCheck = perm.CanView || perm.CanCreate || perm.CanEdit || perm.CanDelete || perm.CanApprove;
            if (!hasAnyCheck) continue;

            // PER FORM: Having access to a form allows all actions (view, create, update, inactive)
            if (!summary.AllowedControllers.Contains(mod.ControllerName, StringComparer.OrdinalIgnoreCase))
            {
                summary.AllowedControllers.Add(mod.ControllerName);
            }

            if (!summary.AllowedCreateControllers.Contains(mod.ControllerName, StringComparer.OrdinalIgnoreCase))
            {
                summary.AllowedCreateControllers.Add(mod.ControllerName);
            }
        }

        // Workflow module: Pool Transactions grants My Transactions
        var poolModule = allModules.FirstOrDefault(m => m.ModuleCode == "POOL_TRANSACTIONS");
        var myModule = allModules.FirstOrDefault(m => m.ModuleCode == "MY_TRANSACTIONS");
        bool hasPool = poolModule != null && permissions.Any(p => p.ModuleId == poolModule.ModuleId && (p.CanView || p.CanCreate || p.CanEdit || p.CanDelete || p.CanApprove));
        bool hasMy = myModule != null && permissions.Any(p => p.ModuleId == myModule.ModuleId && (p.CanView || p.CanCreate || p.CanEdit || p.CanDelete || p.CanApprove));

        if (hasPool || hasMy)
        {
            if (!summary.AllowedControllers.Contains("Workflow", StringComparer.OrdinalIgnoreCase))
            {
                summary.AllowedControllers.Add("Workflow");
                summary.AllowedCreateControllers.Add("Workflow");
            }
        }

        return summary;
    }

    private SystemModule? GetModuleForRequest(string controllerName, string? actionName = null)
    {
        if (controllerName.Equals("Workflow", StringComparison.OrdinalIgnoreCase))
        {
            if (actionName != null && actionName.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                return _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "WORKFLOW_SETTINGS");
            }
            return null;
        }

        if (controllerName.Equals("UserRole", StringComparison.OrdinalIgnoreCase))
        {
            if (actionName != null && actionName.Equals("Permissions", StringComparison.OrdinalIgnoreCase))
            {
                return _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "ROLE_PERMISSIONS");
            }
            return _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "USER_ROLES");
        }

        return _context.SystemModules.FirstOrDefault(m =>
            m.IsActive &&
            m.ControllerName != null &&
            m.ControllerName.ToLower() == controllerName.ToLower());
    }
}
