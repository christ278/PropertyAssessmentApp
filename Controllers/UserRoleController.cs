using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class UserRoleController : Controller
{
    private readonly IUserRoleService _roleService;
    private readonly ApplicationDbContext _context;

    public UserRoleController(IUserRoleService roleService, ApplicationDbContext context)
    {
        _roleService = roleService;
        _context = context;
    }

    // GET: /UserRole
    public IActionResult Index(string? search, int? divisionId)
    {
        ViewBag.Search = search;
        ViewBag.DivisionId = divisionId;
        ViewBag.TotalCount = _roleService.GetTotalCount();

        var roles = _roleService.GetAll(search);
        if (divisionId.HasValue && divisionId.Value > 0)
        {
            roles = roles.Where(r => r.TransactionStatusId.HasValue ? (_context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == r.TransactionStatusId.Value)?.DivisionId == divisionId.Value) : (r.Division == (DivisionType)divisionId.Value));
        }

        return View(roles);
    }

    // GET: /UserRole/Permissions
    public IActionResult Permissions(Guid? roleId, int? divisionId = null)
    {
        var allRoles = _roleService.GetAll().OrderBy(r => r.Division).ThenBy(r => r.TransactionStatusId ?? 0).ThenBy(r => r.RoleName).ToList();

        if (roleId.HasValue && !divisionId.HasValue)
        {
            var matchedRole = allRoles.FirstOrDefault(r => r.Id == roleId.Value);
            if (matchedRole?.Division != null)
            {
                divisionId = (int)matchedRole.Division.Value;
            }
            else if (matchedRole?.TransactionStatusId.HasValue == true)
            {
                var ts = matchedRole.TransactionStatusId.Value;
                if (ts >= 25 && ts <= 30) divisionId = 3;
                else if (ts >= 15 && ts <= 19) divisionId = 2;
                else if (ts >= 1 && ts <= 10) divisionId = 1;
            }
        }
        else if (divisionId.HasValue && !roleId.HasValue)
        {
            var firstInDiv = allRoles.FirstOrDefault(r => (int?)r.Division == divisionId.Value 
                || (divisionId.Value == 3 && r.TransactionStatusId >= 25 && r.TransactionStatusId <= 30)
                || (divisionId.Value == 2 && r.TransactionStatusId >= 15 && r.TransactionStatusId <= 19)
                || (divisionId.Value == 1 && r.TransactionStatusId >= 1 && r.TransactionStatusId <= 10));
            if (firstInDiv != null)
            {
                roleId = firstInDiv.Id;
            }
        }

        Guid selectedId = roleId ?? (allRoles.FirstOrDefault()?.Id ?? Guid.Empty);
        ViewBag.AllRoles = allRoles;
        ViewBag.SelectedRoleId = selectedId;
        ViewBag.SelectedDivisionId = divisionId;

        ViewBag.Roles = allRoles.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = r.TransactionStatusId.HasValue ? $"Step {r.TransactionStatusId}: {r.RoleName}" : r.RoleName
        }).ToList();

        return View();
    }

    // GET: /UserRole/GetRolePermissions?roleId=guid
    [HttpGet]
    public IActionResult GetRolePermissions(Guid roleId)
    {
        try
        {
            var modules = _context.SystemModules
                .Where(m => m.IsActive)
                .OrderBy(m => m.Category)
                .ThenBy(m => m.SortOrder)
                .ToList();

            var permissions = _context.RoleModulePermissions
                .Where(p => p.RoleId == roleId)
                .ToList();

            var grouped = modules
                .GroupBy(m => string.IsNullOrEmpty(m.Category) ? "General Modules" : m.Category)
                .Select(g => new RolePermissionCategoryGroupDTO
                {
                    CategoryName = g.Key,
                    Modules = g.Select(m =>
                    {
                        var perm = permissions.FirstOrDefault(p => p.ModuleId == m.ModuleId);
                        bool isAllowed = perm != null && (perm.CanView || perm.CanCreate || perm.CanEdit || perm.CanDelete || perm.CanApprove);
                        return new ModulePermissionDTO
                        {
                            ModuleId = m.ModuleId,
                            ModuleCode = m.ModuleCode,
                            ModuleName = m.ModuleName,
                            Category = m.Category ?? "General Modules",
                            ControllerName = m.ControllerName,
                            ActionName = m.ActionName,
                            Icon = m.Icon ?? "layers",
                            SortOrder = m.SortOrder,
                            PermissionId = perm?.PermissionId ?? 0,
                            IsAllowed = isAllowed,
                            CanView = isAllowed,
                            CanCreate = isAllowed,
                            CanEdit = isAllowed,
                            CanDelete = isAllowed,
                            CanApprove = isAllowed
                        };
                    }).ToList()
                }).ToList();

            return Json(new { success = true, data = grouped });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error loading permissions: " + ex.Message });
        }
    }

    // POST: /UserRole/SaveRolePermissions
    [HttpPost]
    public IActionResult SaveRolePermissions([FromBody] SaveRolePermissionsRequest request)
    {
        if (request == null || request.RoleId == Guid.Empty)
        {
            return Json(new { success = false, message = "Invalid role ID or request payload." });
        }

        try
        {
            var existing = _context.RoleModulePermissions.Where(p => p.RoleId == request.RoleId).ToList();
            _context.RoleModulePermissions.RemoveRange(existing);

            if (request.Permissions != null && request.Permissions.Any())
            {
                foreach (var item in request.Permissions)
                {
                    bool allowed = item.IsAllowed || item.CanView || item.CanCreate || item.CanEdit || item.CanDelete || item.CanApprove;
                    if (allowed)
                    {
                        _context.RoleModulePermissions.Add(new RoleModulePermission
                        {
                            RoleId = request.RoleId,
                            ModuleId = item.ModuleId,
                            CanView = true,
                            CanCreate = true,
                            CanEdit = true,
                            CanDelete = true,
                            CanApprove = true,
                            UpdatedDate = DateTime.Now
                        });
                    }
                }

                // RULE: If role has access to Pool Transactions, grant My Transactions workspace access as well
                var poolMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "POOL_TRANSACTIONS");
                var myMod = _context.SystemModules.FirstOrDefault(m => m.ModuleCode == "MY_TRANSACTIONS");
                if (poolMod != null && myMod != null)
                {
                    bool poolAllowed = _context.RoleModulePermissions.Local.Any(p => p.RoleId == request.RoleId && p.ModuleId == poolMod.ModuleId);
                    if (poolAllowed)
                    {
                        bool myAllowed = _context.RoleModulePermissions.Local.Any(p => p.RoleId == request.RoleId && p.ModuleId == myMod.ModuleId);
                        if (!myAllowed)
                        {
                            _context.RoleModulePermissions.Add(new RoleModulePermission
                            {
                                RoleId = request.RoleId,
                                ModuleId = myMod.ModuleId,
                                CanView = true,
                                CanCreate = true,
                                CanEdit = true,
                                CanDelete = true,
                                CanApprove = true,
                                UpdatedDate = DateTime.Now
                            });
                        }
                    }
                }
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "Form access rights updated successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error saving role permissions: " + ex.Message });
        }
    }

    // GET: /UserRole/Create
    public IActionResult Create()
    {
        return View(new UserRoleSetting());
    }

    // POST: /UserRole/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(UserRoleSetting role)
    {
        if (ModelState.IsValid)
        {
            _roleService.Add(role);
            TempData["SuccessMessage"] = $"User Role '{role.RoleName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        return View(role);
    }

    // GET: /UserRole/Edit/5
    public IActionResult Edit(Guid id)
    {
        var role = _roleService.GetById(id);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    // POST: /UserRole/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, UserRoleSetting role)
    {
        if (id != role.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            _roleService.Update(role);
            TempData["SuccessMessage"] = $"User Role '{role.RoleName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        return View(role);
    }

    // POST: /UserRole/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        _roleService.Delete(id);
        TempData["SuccessMessage"] = "User Role deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
