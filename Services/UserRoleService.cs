using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class UserRoleService : IUserRoleService
{
    private readonly ApplicationDbContext _context;

    public UserRoleService(ApplicationDbContext context)
    {
        _context = context;
        EnsureSeedRoles();
    }

    private void EnsureSeedRoles()
    {
        if (!_context.UserRoleSettings.Any(r => r.RoleName == "System Administrator"))
        {
            _context.UserRoleSettings.Add(new UserRoleSetting
            {
                Id = Guid.Parse("50147D2F-24FE-4FA9-B270-9F217F58D333"),
                RoleName = "System Administrator",
                Division = null,
                TransactionStatusId = null,
                Description = "Full administrative access, user accounts, and system configuration.",
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }

        var statuses = _context.TransactionStatuses.Where(s => s.TransactionStatusId > 0).ToList();
        foreach (var status in statuses)
        {
            var divType = status.DivisionId == 1 ? DivisionType.Division1_TaxAssessmentAndLandHolding
                        : status.DivisionId == 2 ? DivisionType.Division2_GIS_TaxMapping
                        : status.DivisionId == 3 ? DivisionType.Division3_RecordsAndCertification
                        : (DivisionType?)null;

            var existing = _context.UserRoleSettings.FirstOrDefault(r => r.TransactionStatusId == status.TransactionStatusId);
            var suffix = status.DivisionId == 2 ? " (GIS)" : status.DivisionId == 3 ? " (Records)" : "";
            var roleName = $"{status.Name}{suffix}";

            if (existing == null)
            {
                _context.UserRoleSettings.Add(new UserRoleSetting
                {
                    Id = Guid.NewGuid(),
                    RoleName = roleName,
                    TransactionStatusId = status.TransactionStatusId,
                    Division = divType,
                    Description = $"Workflow step {status.TransactionStatusId}: {status.Name} for Division {status.DivisionId}.",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                });
            }
            else
            {
                existing.RoleName = roleName;
                existing.Division = divType;
            }
        }

        // Ensure Print Tax Faas (ASSESSMENT_PRINT) module exists in SystemModules
        var printFaasMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "ASSESSMENT_PRINT");
        if (printFaasMod == null)
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "ASSESSMENT_PRINT",
                ModuleName = "Print Tax Faas (Records Printout)",
                Category = "FAAS & Transaction Requests",
                ControllerName = "Assessment",
                ActionName = "PrintPdf",
                Icon = "printer",
                SortOrder = 13,
                IsActive = true
            });
        }
        else if (printFaasMod.ModuleName != "Print Tax Faas (Records Printout)")
        {
            printFaasMod.ModuleName = "Print Tax Faas (Records Printout)";
        }

        // Ensure Print Tax Declaration (ASSESSMENT_PRINT_TD) module exists in SystemModules
        if (!_context.SystemModules.Any(m => m.ModuleCode == "ASSESSMENT_PRINT_TD"))
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "ASSESSMENT_PRINT_TD",
                ModuleName = "Print Tax Declaration",
                Category = "FAAS & Transaction Requests",
                ControllerName = "Assessment",
                ActionName = "PrintTaxDeclaration",
                Icon = "file-check-2",
                SortOrder = 14,
                IsActive = true
            });
        }

        // Ensure Executive Assessment Analytics (DASHBOARD_ANALYTICS) module exists in SystemModules
        var analyticsMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "DASHBOARD_ANALYTICS");
        if (analyticsMod == null)
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "DASHBOARD_ANALYTICS",
                ModuleName = "Executive Assessment Analytics",
                Category = "Dashboards",
                ControllerName = "Dashboard",
                ActionName = "Analytics",
                Icon = "bar-chart-3",
                SortOrder = 4,
                IsActive = true
            });
        }
        else
        {
            analyticsMod.ModuleName = "Executive Assessment Analytics";
            analyticsMod.Category = "Dashboards";
            analyticsMod.ControllerName = "Dashboard";
            analyticsMod.ActionName = "Analytics";
            analyticsMod.Icon = "bar-chart-3";
            analyticsMod.SortOrder = 4;
            analyticsMod.IsActive = true;
        }

        // Ensure User Performance & Division Statistics (DASHBOARD_USER_PERFORMANCE) module exists in SystemModules
        var userPerfMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "DASHBOARD_USER_PERFORMANCE");
        if (userPerfMod == null)
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "DASHBOARD_USER_PERFORMANCE",
                ModuleName = "User Performance & Division Stats",
                Category = "Dashboards",
                ControllerName = "Dashboard",
                ActionName = "UserPerformance",
                Icon = "users",
                SortOrder = 5,
                IsActive = true
            });
        }
        else
        {
            userPerfMod.ModuleName = "User Performance & Division Stats";
            userPerfMod.Category = "Dashboards";
            userPerfMod.ControllerName = "Dashboard";
            userPerfMod.ActionName = "UserPerformance";
            userPerfMod.Icon = "users";
            userPerfMod.SortOrder = 5;
            userPerfMod.IsActive = true;
        }

        // Deactivate obsolete/legacy ANALYTICS placeholder so only DASHBOARD_ANALYTICS is shown in Role Permissions
        var obsoleteAnalytics = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "ANALYTICS");
        if (obsoleteAnalytics != null && obsoleteAnalytics.IsActive)
        {
            obsoleteAnalytics.IsActive = false;
        }

        // Ensure RPT Reports modules exist in SystemModules
        if (!_context.SystemModules.Any(m => m.ModuleCode == "REPORT_DIRECTORY"))
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "REPORT_DIRECTORY",
                ModuleName = "RPT Reports & Certifications Directory",
                Category = "Reports & Certifications",
                ControllerName = "RPTReport",
                ActionName = "Index",
                Icon = "file-text",
                SortOrder = 30,
                IsActive = true
            });
        }

        var rptReports = _context.RPTReports.OrderBy(r => r.RPTReportId).ToList();
        int reportSort = 31;
        foreach (var r in rptReports)
        {
            var code = $"REPORT_{r.RPTReportId}";
            if (!_context.SystemModules.Any(m => m.ModuleCode == code))
            {
                _context.SystemModules.Add(new SystemModule
                {
                    ModuleCode = code,
                    ModuleName = r.Title,
                    Category = "Reports & Certifications",
                    ControllerName = "RPTReport",
                    ActionName = $"ViewReport/{r.RPTReportId}",
                    Icon = "file-badge",
                    SortOrder = reportSort++,
                    IsActive = true
                });
            }
        }

        if (!_context.SystemModules.Any(m => m.ModuleCode == "LAND_REFERENCE"))
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "LAND_REFERENCE",
                ModuleName = "Land Reference Settings (Valuation Schedules)",
                Category = "System Administration",
                ControllerName = "LandReference",
                ActionName = "Index",
                Icon = "sliders",
                SortOrder = 32,
                IsActive = true
            });
        }

        if (!_context.SystemModules.Any(m => m.ModuleCode == "BUILDING_REFERENCE"))
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "BUILDING_REFERENCE",
                ModuleName = "Building Reference Settings (BUCC & Depreciation)",
                Category = "System Administration",
                ControllerName = "BuildingReference",
                ActionName = "Index",
                Icon = "building-2",
                SortOrder = 33,
                IsActive = true
            });
        }

        if (!_context.SystemModules.Any(m => m.ModuleCode == "AUDIT_LOGS"))
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "AUDIT_LOGS",
                ModuleName = "System Audit Logs (All Users)",
                Category = "System Administration",
                ControllerName = "AuditLog",
                ActionName = "Index",
                Icon = "history",
                SortOrder = 43,
                IsActive = true
            });
        }

        if (!_context.SystemModules.Any(m => m.ModuleCode == "AUDIT_LOGS_SELF"))
        {
            _context.SystemModules.Add(new SystemModule
            {
                ModuleCode = "AUDIT_LOGS_SELF",
                ModuleName = "System Audit Logs (Self Logs Only)",
                Category = "System Administration",
                ControllerName = "AuditLog",
                ActionName = "Self",
                Icon = "user-check",
                SortOrder = 44,
                IsActive = true
            });
        }

        foreach (var obsoleteCode in new[] { "POOL_ASSIGN", "POOL_ASSIGN_ALL" })
        {
            var oldPoolAssign = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == obsoleteCode);
            if (oldPoolAssign != null && oldPoolAssign.IsActive)
            {
                oldPoolAssign.IsActive = false;
            }
        }

        var poolCategory = _context.SystemModules
            .Where(m => m.ModuleCode == "POOL_TRANSACTIONS")
            .Select(m => m.Category)
            .FirstOrDefault() ?? "Workflow & Queues";

        var poolAssignModules = new[]
        {
            new { Code = "POOL_ASSIGN_DIV1", Name = "Assign Pool Transactions (Division 1)", Action = "AssignDiv1", Sort = 26 },
            new { Code = "POOL_ASSIGN_DIV2", Name = "Assign Pool Transactions (Division 2)", Action = "AssignDiv2", Sort = 27 },
            new { Code = "POOL_ASSIGN_DIV3", Name = "Assign Pool Transactions (Division 3)", Action = "AssignDiv3", Sort = 28 }
        };

        foreach (var pam in poolAssignModules)
        {
            if (!_context.SystemModules.Any(m => m.ModuleCode == pam.Code))
            {
                _context.SystemModules.Add(new SystemModule
                {
                    ModuleCode = pam.Code,
                    ModuleName = pam.Name,
                    Category = poolCategory,
                    ControllerName = "Workflow",
                    ActionName = pam.Action,
                    Icon = "user-plus",
                    SortOrder = pam.Sort,
                    IsActive = true
                });
            }
        }

        _context.SaveChanges();

        var landRefMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "LAND_REFERENCE");
        var auditLogMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "AUDIT_LOGS");
        var poolAssignDivMods = _context.SystemModules
            .Where(m => m.ModuleCode == "POOL_ASSIGN_DIV1" || m.ModuleCode == "POOL_ASSIGN_DIV2" || m.ModuleCode == "POOL_ASSIGN_DIV3")
            .ToList();
        var printTdMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "ASSESSMENT_PRINT_TD");
        var adminRole = _context.UserRoleSettings.FirstOrDefault(r => r.RoleName == "System Administrator");
        if (adminRole != null)
        {
            bool addedAdminPoolAssign = false;
            foreach (var pam in poolAssignDivMods)
            {
                if (!_context.RoleModulePermissions.Any(p => p.RoleId == adminRole.Id && p.ModuleId == pam.ModuleId))
                {
                    _context.RoleModulePermissions.Add(new RoleModulePermission
                    {
                        RoleId = adminRole.Id,
                        ModuleId = pam.ModuleId,
                        CanView = true,
                        CanCreate = true,
                        CanEdit = true,
                        CanDelete = true,
                        CanApprove = true,
                        UpdatedDate = DateTime.Now
                    });
                    addedAdminPoolAssign = true;
                }
            }
            if (addedAdminPoolAssign) _context.SaveChanges();
        }
        if (auditLogMod != null && adminRole != null &&
            !_context.RoleModulePermissions.Any(p => p.RoleId == adminRole.Id && p.ModuleId == auditLogMod.ModuleId))
        {
            _context.RoleModulePermissions.Add(new RoleModulePermission
            {
                RoleId = adminRole.Id,
                ModuleId = auditLogMod.ModuleId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }
        if (landRefMod != null && adminRole != null &&
            !_context.RoleModulePermissions.Any(p => p.RoleId == adminRole.Id && p.ModuleId == landRefMod.ModuleId))
        {
            _context.RoleModulePermissions.Add(new RoleModulePermission
            {
                RoleId = adminRole.Id,
                ModuleId = landRefMod.ModuleId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }
        var bldgRefMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "BUILDING_REFERENCE");
        if (bldgRefMod != null && adminRole != null &&
            !_context.RoleModulePermissions.Any(p => p.RoleId == adminRole.Id && p.ModuleId == bldgRefMod.ModuleId))
        {
            _context.RoleModulePermissions.Add(new RoleModulePermission
            {
                RoleId = adminRole.Id,
                ModuleId = bldgRefMod.ModuleId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }
        if (printTdMod != null && adminRole != null &&
            !_context.RoleModulePermissions.Any(p => p.RoleId == adminRole.Id && p.ModuleId == printTdMod.ModuleId))
        {
            _context.RoleModulePermissions.Add(new RoleModulePermission
            {
                RoleId = adminRole.Id,
                ModuleId = printTdMod.ModuleId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }

        analyticsMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "DASHBOARD_ANALYTICS");
        if (analyticsMod != null && adminRole != null &&
            !_context.RoleModulePermissions.Any(p => p.RoleId == adminRole.Id && p.ModuleId == analyticsMod.ModuleId))
        {
            _context.RoleModulePermissions.Add(new RoleModulePermission
            {
                RoleId = adminRole.Id,
                ModuleId = analyticsMod.ModuleId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }

        userPerfMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "DASHBOARD_USER_PERFORMANCE");
        if (userPerfMod != null && adminRole != null &&
            !_context.RoleModulePermissions.Any(p => p.RoleId == adminRole.Id && p.ModuleId == userPerfMod.ModuleId))
        {
            _context.RoleModulePermissions.Add(new RoleModulePermission
            {
                RoleId = adminRole.Id,
                ModuleId = userPerfMod.ModuleId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true,
                CanApprove = true,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }

        var headRole = _context.UserRoleSettings.FirstOrDefault(r => r.RoleName == "City Assessor Head" || r.RoleName == "City Assessor");
        if (userPerfMod != null && headRole != null &&
            !_context.RoleModulePermissions.Any(p => p.RoleId == headRole.Id && p.ModuleId == userPerfMod.ModuleId))
        {
            _context.RoleModulePermissions.Add(new RoleModulePermission
            {
                RoleId = headRole.Id,
                ModuleId = userPerfMod.ModuleId,
                CanView = true,
                CanCreate = false,
                CanEdit = false,
                CanDelete = false,
                CanApprove = false,
                UpdatedDate = DateTime.Now
            });
            _context.SaveChanges();
        }
    }

    public IEnumerable<UserRoleSetting> GetAll(string? search = null)
    {
        var query = _context.UserRoleSettings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.RoleName.ToLower().Contains(term)
                                  || (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        var roles = query.OrderBy(r => r.Division).ThenBy(r => r.TransactionStatusId ?? 0).ThenBy(r => r.RoleName).ToList();

        // Populate Allowed Target Steps (Transfer to) from WorkflowSequence
        var sequences = _context.WorkflowSequences.ToList();
        var allStatuses = _context.TransactionStatuses.ToList();

        foreach (var r in roles)
        {
            if (r.TransactionStatusId.HasValue)
            {
                var targetIds = sequences
                    .Where(s => s.CurrentStatusId == r.TransactionStatusId.Value && s.NextStatusId.HasValue)
                    .Select(s => s.NextStatusId!.Value)
                    .Distinct()
                    .ToList();

                r.AllowedTargetSteps = allStatuses
                    .Where(s => targetIds.Contains(s.TransactionStatusId))
                    .Select(s => s.Name)
                    .ToList();
            }
            else if (r.RoleName.Contains("Admin", StringComparison.OrdinalIgnoreCase))
            {
                r.AllowedTargetSteps = new List<string> { "All Target Steps (Global Administrator)" };
            }
        }

        return roles;
    }

    public UserRoleSetting? GetById(Guid id)
    {
        var role = _context.UserRoleSettings.FirstOrDefault(r => r.Id == id);
        if (role != null)
        {
            if (role.TransactionStatusId.HasValue)
            {
                var targetIds = _context.WorkflowSequences
                    .Where(s => s.CurrentStatusId == role.TransactionStatusId.Value && s.NextStatusId.HasValue)
                    .Select(s => s.NextStatusId!.Value)
                    .Distinct()
                    .ToList();

                role.AllowedTargetSteps = _context.TransactionStatuses
                    .Where(s => targetIds.Contains(s.TransactionStatusId))
                    .Select(s => s.Name)
                    .ToList();
            }
            else if (role.RoleName.Contains("Admin", StringComparison.OrdinalIgnoreCase))
            {
                role.AllowedTargetSteps = new List<string> { "All Target Steps (Global Administrator)" };
            }
        }
        return role;
    }

    public void Add(UserRoleSetting role)
    {
        if (role.Id == Guid.Empty)
        {
            role.Id = Guid.NewGuid();
        }
        role.CreatedDate = DateTime.Now;
        role.UpdatedDate = DateTime.Now;

        _context.UserRoleSettings.Add(role);
        _context.SaveChanges();
    }

    public void Update(UserRoleSetting role)
    {
        var existing = GetById(role.Id);
        if (existing != null)
        {
            existing.RoleName = role.RoleName;
            existing.Division = role.Division;
            existing.Description = role.Description;
            existing.IsActive = role.IsActive;
            existing.UpdatedDate = DateTime.Now;

            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = GetById(id);
        if (item != null)
        {
            _context.UserRoleSettings.Remove(item);
            _context.SaveChanges();
        }
    }

    public int GetTotalCount()
    {
        return _context.UserRoleSettings.Count();
    }
}
