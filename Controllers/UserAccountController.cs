using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class UserAccountController : Controller
{
    private readonly IUserAccountService _userService;
    private readonly IUserRoleService _roleService;
    private readonly ApplicationDbContext _context;

    public UserAccountController(IUserAccountService userService, IUserRoleService roleService, ApplicationDbContext context)
    {
        _userService = userService;
        _roleService = roleService;
        _context = context;
    }

    private void PopulateDivisionsAndStatuses(int? selectedDivisionId = null)
    {
        ViewBag.Divisions = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Division 1: Appraisal and Assessment" },
            new SelectListItem { Value = "2", Text = "Division 2: Tax Mapping" },
            new SelectListItem { Value = "3", Text = "Division 3: Records" }
        };

        ViewBag.AllWorkflowStatuses = _context.TransactionStatuses.OrderBy(s => s.TransactionStatusId).ToList();
    }

    // GET: /UserAccount
    public IActionResult Index(string? search, UserRole? role, bool? isActive, int? divisionId)
    {
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.IsActive = isActive;
        ViewBag.DivisionId = divisionId;

        PopulateDivisionsAndStatuses(divisionId);

        ViewBag.TotalCount = _userService.GetTotalCount();
        ViewBag.ActiveCount = _userService.GetActiveCount();
        ViewBag.InactiveCount = _userService.GetInactiveCount();

        var records = _userService.GetAll(search, role, isActive, divisionId);
        return View(records);
    }

    // GET: /UserAccount/GetWorkflowRolesByDivision?divisionId=1
    [HttpGet]
    public IActionResult GetWorkflowRolesByDivision(int divisionId)
    {
        var statuses = _context.TransactionStatuses
            .Where(s => s.DivisionId == divisionId)
            .OrderBy(s => s.TransactionStatusId)
            .Select(s => new {
                id = s.TransactionStatusId,
                name = s.Name,
                divisionId = s.DivisionId,
                displayName = $"Step {s.TransactionStatusId}: {s.Name}"
            })
            .ToList();
        return Json(statuses);
    }

    // GET: /UserAccount/Details/{guid}
    public IActionResult Details(Guid id)
    {
        var user = _userService.GetById(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    // GET: /UserAccount/Create
    public IActionResult Create()
    {
        ViewBag.UserRoles = _roleService.GetAll();
        PopulateDivisionsAndStatuses();

        var model = new UserAccount
        {
            Id = Guid.NewGuid(),
            EmployeeNo = $"EMP-{DateTime.Now.Year}-{new Random().Next(100, 999)}",
            IsActive = true
        };
        return View(model);
    }

    // POST: /UserAccount/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(UserAccount model)
    {
        var existing = _userService.GetByUsername(model.Username);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(UserAccount.Username), "Username already exists. Please choose a unique username.");
        }

        if (model.WorkflowRoleId.HasValue && model.WorkflowRoleId.Value > 0)
        {
            var status = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == model.WorkflowRoleId.Value);
            if (status != null)
            {
                model.WorkflowRoleName = status.Name;
            }
        }

        if (ModelState.IsValid)
        {
            _userService.Add(model);
            TempData["SuccessMessage"] = $"User Account '{model.Username}' ({model.FullName}) created successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.UserRoles = _roleService.GetAll();
        PopulateDivisionsAndStatuses(model.DivisionId);
        TempData["ErrorMessage"] = "Failed to create user account. Please check the validation errors.";
        return View(model);
    }

    // GET: /UserAccount/Edit/{guid}
    public IActionResult Edit(Guid id)
    {
        var user = _userService.GetById(id);
        if (user == null)
        {
            return NotFound();
        }
        ViewBag.UserRoles = _roleService.GetAll();
        PopulateDivisionsAndStatuses(user.DivisionId);
        return View(user);
    }

    // POST: /UserAccount/Edit/{guid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, UserAccount model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = _userService.GetByUsername(model.Username);
        if (existing != null && existing.Id != model.Id)
        {
            ModelState.AddModelError(nameof(UserAccount.Username), "Username already belongs to another account.");
        }

        if (model.WorkflowRoleId.HasValue && model.WorkflowRoleId.Value > 0)
        {
            var status = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == model.WorkflowRoleId.Value);
            if (status != null)
            {
                model.WorkflowRoleName = status.Name;
            }
        }

        if (ModelState.IsValid)
        {
            _userService.Update(model);
            TempData["SuccessMessage"] = $"User Account '{model.Username}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.UserRoles = _roleService.GetAll();
        PopulateDivisionsAndStatuses(model.DivisionId);
        TempData["ErrorMessage"] = "Failed to update user account. Please check the validation errors.";
        return View(model);
    }

    // POST: /UserAccount/ToggleStatus/{guid}
    // SOFT DELETE ACTION: Sets IsActive = false instead of hard deleting records!
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleStatus(Guid id, bool setStatus)
    {
        var user = _userService.GetById(id);
        if (user != null)
        {
            _userService.SetActiveStatus(id, setStatus);
            var statusText = setStatus ? "Activated" : "Inactivated (Soft Deleted)";
            TempData["SuccessMessage"] = $"User Account '{user.Username}' was set to {statusText}.";
        }
        return RedirectToAction(nameof(Index));
    }
}
