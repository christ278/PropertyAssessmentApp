using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PropertyAssessmentApp.Hubs;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class WorkflowController : Controller
{
    private readonly IDocumentWorkflowService _workflowService;
    private readonly PropertyAssessmentApp.Data.ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly IWorkflowNotifier _workflowNotifier;
    private readonly ICharterSlaService _charterSlaService;
    private readonly IHttpClientFactory _httpClientFactory;

    public WorkflowController(
        IDocumentWorkflowService workflowService,
        PropertyAssessmentApp.Data.ApplicationDbContext context,
        IPermissionService permissionService,
        IHubContext<WorkflowHub> hubContext,
        IWorkflowNotifier workflowNotifier,
        ICharterSlaService charterSlaService,
        IHttpClientFactory httpClientFactory)
    {
        _workflowService = workflowService;
        _context = context;
        _permissionService = permissionService;
        _hubContext = hubContext;
        _workflowNotifier = workflowNotifier;
        _charterSlaService = charterSlaService;
        _httpClientFactory = httpClientFactory;
    }

    private static string GetTargetController(string requestType) => requestType switch
    {
        "TaxMappingRequest" => "TaxMapping",
        "TaxDeclarationRequest" => "TaxDeclarationRequest",
        "LandHoldingRequest" => "LandHoldingRequest",
        "PropertyAssessmentRequest" => "PropertyAssessmentRequest",
        "RequestForInspection" => "RequestForInspection",
        _ => requestType
    };

    private static bool IsReleasingStation(int? userStepId, string? workflowRoleName, string? userRole)
    {
        return (userStepId.HasValue && (userStepId.Value == 10 || userStepId.Value == 19 || userStepId.Value == 30))
            || (!string.IsNullOrEmpty(workflowRoleName) && workflowRoleName.Contains("Releas", StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(userRole) && userRole.Contains("Releas", StringComparison.OrdinalIgnoreCase));
    }

    private (Guid userId, string userName, string userRole, int? divisionId, int? workflowRoleId, string? workflowRoleName, bool isSuperAdminOrHead, bool isRegularEmployee) GetCurrentUserInfo()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var idStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(idStr, out var uId);

            var userAccount = _context.UserAccounts.FirstOrDefault(u => u.Id == uId);
            if (userAccount != null)
            {
                bool isSuper = userAccount.Role == UserRole.SystemAdmin || userAccount.Role == UserRole.CityAssessor;
                return (
                    userAccount.Id,
                    userAccount.FullName,
                    userAccount.Role.ToString(),
                    userAccount.DivisionId,
                    userAccount.WorkflowRoleId,
                    userAccount.WorkflowRoleName,
                    isSuper,
                    userAccount.IsRegularEmployee
                );
            }

            var uName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value 
                     ?? User.Identity.Name 
                     ?? "Assessor Staff";

            var uRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                     ?? "Property Appraiser";

            bool isHead = uRole.Contains("Admin", StringComparison.OrdinalIgnoreCase) 
                       || uRole.Contains("CityAssessor", StringComparison.OrdinalIgnoreCase)
                       || uRole.Contains("Head", StringComparison.OrdinalIgnoreCase);

            int? divId = null;
            var divClaim = User.FindFirst("DivisionId")?.Value;
            if (int.TryParse(divClaim, out var parsedDiv)) divId = parsedDiv;

            int? wfRoleId = null;
            var wfClaim = User.FindFirst("WorkflowRoleId")?.Value;
            if (int.TryParse(wfClaim, out var parsedWf)) wfRoleId = parsedWf;

            var wfRoleName = User.FindFirst("WorkflowRoleName")?.Value;

            bool isRegular = true;
            var regClaim = User.FindFirst("IsRegularEmployee")?.Value;
            if (bool.TryParse(regClaim, out var parsedReg)) isRegular = parsedReg;

            return (uId == Guid.Empty ? Guid.Parse("70000000-0000-0000-0000-000000000000") : uId, uName, uRole, divId, wfRoleId, wfRoleName, isHead, isRegular);
        }

        // Local dev fallback
        var adminAcc = _context.UserAccounts.FirstOrDefault(u => u.Role == UserRole.SystemAdmin);
        if (adminAcc != null)
        {
            return (adminAcc.Id, adminAcc.FullName, adminAcc.Role.ToString(), adminAcc.DivisionId, adminAcc.WorkflowRoleId, adminAcc.WorkflowRoleName, true, adminAcc.IsRegularEmployee);
        }

        return (Guid.Parse("70000000-0000-0000-0000-000000000000"), "System Administrator", "SystemAdmin", null, null, null, true, true);
    }

    // GET: /Workflow
    public IActionResult Index()
    {
        var userInfo = GetCurrentUserInfo();
        ViewBag.Div1Stats = _workflowService.GetDivisionStats(DivisionType.Division1_TaxAssessmentAndLandHolding, userInfo.userId);
        ViewBag.Div2Stats = _workflowService.GetDivisionStats(DivisionType.Division2_GIS_TaxMapping, userInfo.userId);
        
        return View();
    }

    // GET: /Workflow/Pool
    public IActionResult Pool(string? mode, int? divisionId, WorkflowStage? stage, string? search, string? type)
    {
        // If mode is explicitly requested as "my", redirect to the dedicated MyTransactions action
        if (!string.IsNullOrWhiteSpace(mode) && mode.Equals("my", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(MyTransactions), new { divisionId, stage, search, type });
        }

        var userInfo = GetCurrentUserInfo();

        int? userStepId = userInfo.workflowRoleId;
        if ((!userStepId.HasValue || userStepId.Value <= 0) && !userInfo.isSuperAdminOrHead)
        {
            var matchedRole = _context.UserRoleSettings.FirstOrDefault(r => 
                r.RoleName.Equals(userInfo.userRole, StringComparison.OrdinalIgnoreCase) || 
                (!string.IsNullOrEmpty(userInfo.workflowRoleName) && r.RoleName.Equals(userInfo.workflowRoleName, StringComparison.OrdinalIgnoreCase)));
            if (matchedRole?.TransactionStatusId.HasValue == true)
            {
                userStepId = matchedRole.TransactionStatusId.Value;
            }
            else
            {
                userStepId = (userInfo.divisionId ?? 1) switch
                {
                    2 => 15,
                    3 => 25,
                    _ => 1
                };
            }
        }

        bool isReleasingRole = IsReleasingStation(userStepId, userInfo.workflowRoleName, userInfo.userRole);

        // RBAC Enforcement:
        // Only Head (City Assessor) and Super Admin (System Administrator) can view or filter across all divisions & stages.
        // Releasing staff disregard division so that all transactions ready for releasing across all divisions appear in pool.
        // Ordinary users are strictly restricted to their assigned division and workflow stage/station from their User Account.
        if (!userInfo.isSuperAdminOrHead)
        {
            if (isReleasingRole)
            {
                // Disregard division for Releasing staff unless they explicitly choose a division filter
                if (!divisionId.HasValue || divisionId.Value <= 0)
                {
                    divisionId = null;
                }
                stage = null;
            }
            else
            {
                divisionId = userInfo.divisionId ?? 1;
                if (userStepId.HasValue && userStepId.Value > 0)
                {
                    stage = (WorkflowStage)userStepId.Value;
                }
            }
        }

        // Calculate counts
        var allItems = _workflowService.GetUnifiedQueue(
            divisionId: divisionId, 
            mode: null, 
            search: null,
            userWorkflowRoleId: userInfo.isSuperAdminOrHead ? null : userStepId,
            userWorkflowRoleName: userInfo.isSuperAdminOrHead ? null : userInfo.workflowRoleName,
            isSuperAdminOrHead: userInfo.isSuperAdminOrHead
        ).ToList();

        var totalPool = allItems.Count(i => !i.IsClaimed && i.CurrentStage != WorkflowStage.Completed && (
            userInfo.isSuperAdminOrHead 
            || (isReleasingRole 
                ? ((int)i.CurrentStage == 10 || (int)i.CurrentStage == 19 || (int)i.CurrentStage == 30 || (i.CurrentStageName != null && i.CurrentStageName.Contains("Releas", StringComparison.OrdinalIgnoreCase)))
                : (!userStepId.HasValue || (int)i.CurrentStage == userStepId.Value))));
        var totalMy = allItems.Count(i => i.IsClaimed && i.AssignedUserId == userInfo.userId && i.CurrentStage != WorkflowStage.Completed);

        var queue = _workflowService.GetUnifiedQueue(
            divisionId: divisionId,
            mode: "pool",
            stageFilter: stage,
            search: search,
            requestType: type,
            userId: userInfo.userId,
            userWorkflowRoleId: userInfo.isSuperAdminOrHead ? null : userStepId,
            userWorkflowRoleName: userInfo.isSuperAdminOrHead ? null : userInfo.workflowRoleName,
            isSuperAdminOrHead: userInfo.isSuperAdminOrHead
        );

        ViewBag.QueueMode = "pool";
        ViewBag.DivisionId = divisionId;
        ViewBag.StageFilter = stage;
        ViewBag.Search = search;
        ViewBag.TypeFilter = type;

        ViewBag.CurrentUserId = userInfo.userId;
        ViewBag.CurrentUserName = userInfo.userName;
        ViewBag.CurrentUserRole = userInfo.userRole;
        ViewBag.UserDivisionId = userInfo.divisionId;
        ViewBag.UserWorkflowRoleName = userInfo.workflowRoleName;
        ViewBag.UserStepId = userStepId;
        ViewBag.IsSuperAdminOrHead = userInfo.isSuperAdminOrHead;
        ViewBag.CanChangeDivision = userInfo.isSuperAdminOrHead || isReleasingRole;
        ViewBag.IsRegularEmployee = userInfo.isRegularEmployee;

        ViewBag.TotalPool = totalPool;
        ViewBag.TotalMy = totalMy;

        var targetDiv = (divisionId.HasValue && divisionId.Value >= 1 && divisionId.Value <= 3) 
            ? (DivisionType)divisionId.Value 
            : DivisionType.Division1_TaxAssessmentAndLandHolding;
        ViewBag.Stages = _workflowService.GetStagesForDivision(targetDiv);
        ViewBag.AvailableStatuses = _context.TransactionStatuses
            .Where(s => !divisionId.HasValue || s.DivisionId == divisionId.Value)
            .OrderBy(s => s.TransactionStatusId)
            .ToList();

        return View(queue);
    }

    // GET: /Workflow/GetPoolCounts
    [HttpGet]
    public IActionResult GetPoolCounts(int? divisionId)
    {
        var userInfo = GetCurrentUserInfo();

        int? userStepId = userInfo.workflowRoleId;
        if ((!userStepId.HasValue || userStepId.Value <= 0) && !userInfo.isSuperAdminOrHead)
        {
            var matchedRole = _context.UserRoleSettings.FirstOrDefault(r =>
                r.RoleName.Equals(userInfo.userRole, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(userInfo.workflowRoleName) && r.RoleName.Equals(userInfo.workflowRoleName, StringComparison.OrdinalIgnoreCase)));
            if (matchedRole?.TransactionStatusId.HasValue == true)
            {
                userStepId = matchedRole.TransactionStatusId.Value;
            }
        }

        bool isReleasingRole = (userStepId.HasValue && (userStepId.Value == 10 || userStepId.Value == 19 || userStepId.Value == 30))
            || (!string.IsNullOrEmpty(userInfo.workflowRoleName) && userInfo.workflowRoleName.Contains("Releas", StringComparison.OrdinalIgnoreCase));

        var allItems = _workflowService.GetUnifiedQueue(
            divisionId: null,
            mode: "all",
            userWorkflowRoleId: null,
            userWorkflowRoleName: null,
            isSuperAdminOrHead: true
        ).ToList();

        // Calculate pool count matching current division / user station
        var poolItems = allItems.Where(i => !i.IsClaimed && i.CurrentStage != WorkflowStage.Completed);
        if (divisionId.HasValue && divisionId.Value > 0)
        {
            poolItems = poolItems.Where(i => (int)i.Division == divisionId.Value);
        }
        else if (!userInfo.isSuperAdminOrHead && !isReleasingRole)
        {
            int effectiveDiv = userInfo.divisionId ?? 1;
            poolItems = poolItems.Where(i => (int)i.Division == effectiveDiv);
        }

        if (!userInfo.isSuperAdminOrHead)
        {
            if (isReleasingRole)
            {
                poolItems = poolItems.Where(i => (int)i.CurrentStage == 10 || (int)i.CurrentStage == 19 || (int)i.CurrentStage == 30
                    || (i.CurrentStageName != null && i.CurrentStageName.Contains("Releas", StringComparison.OrdinalIgnoreCase)));
            }
            else if (userStepId.HasValue)
            {
                poolItems = poolItems.Where(i => (int)i.CurrentStage == userStepId.Value);
            }
        }

        int totalPool = poolItems.Count();
        int totalMy = allItems.Count(i => i.IsClaimed && i.AssignedUserId == userInfo.userId && i.CurrentStage != WorkflowStage.Completed);
        int userNavPoolCount = _workflowService.GetUserPendingPoolCount(User);

        int div1Pool = allItems.Count(i => !i.IsClaimed && i.Division == DivisionType.Division1_TaxAssessmentAndLandHolding && i.CurrentStage != WorkflowStage.Completed);
        int div2Pool = allItems.Count(i => !i.IsClaimed && i.Division == DivisionType.Division2_GIS_TaxMapping && i.CurrentStage != WorkflowStage.Completed);
        int div3Pool = allItems.Count(i => !i.IsClaimed && i.Division == DivisionType.Division3_RecordsAndCertification && i.CurrentStage != WorkflowStage.Completed);

        return Json(new
        {
            success = true,
            totalPool = totalPool,
            totalMy = totalMy,
            userPoolCount = userNavPoolCount,
            div1Pool = div1Pool,
            div2Pool = div2Pool,
            div3Pool = div3Pool
        });
    }

    // GET: /Workflow/MyTransactions
    public IActionResult MyTransactions(int? divisionId, string? search, string? type, string? status)
    {
        var userInfo = GetCurrentUserInfo();

        int? userStepId = userInfo.workflowRoleId;
        if ((!userStepId.HasValue || userStepId.Value <= 0) && !userInfo.isSuperAdminOrHead)
        {
            var matchedRole = _context.UserRoleSettings.FirstOrDefault(r => 
                r.RoleName.Equals(userInfo.userRole, StringComparison.OrdinalIgnoreCase) || 
                (!string.IsNullOrEmpty(userInfo.workflowRoleName) && r.RoleName.Equals(userInfo.workflowRoleName, StringComparison.OrdinalIgnoreCase)));
            if (matchedRole?.TransactionStatusId.HasValue == true)
            {
                userStepId = matchedRole.TransactionStatusId.Value;
            }
            else
            {
                userStepId = (userInfo.divisionId ?? 1) switch
                {
                    2 => 15,
                    3 => 25,
                    _ => 1
                };
            }
        }

        bool isReleasingRole = IsReleasingStation(userStepId, userInfo.workflowRoleName, userInfo.userRole);

        // RBAC Enforcement:
        // If the user is not admin or assessors head, division is locked strictly to their assigned division.
        // For Releasing staff, disregard division so they can process releasing across all divisions.
        if (!userInfo.isSuperAdminOrHead)
        {
            if (isReleasingRole)
            {
                if (!divisionId.HasValue || divisionId.Value <= 0)
                {
                    divisionId = null;
                }
            }
            else
            {
                divisionId = userInfo.divisionId ?? 1;
            }
        }

        var allItems = _workflowService.GetUnifiedQueue(
            divisionId: divisionId, 
            mode: null, 
            search: null,
            userWorkflowRoleId: userInfo.isSuperAdminOrHead ? null : userStepId,
            userWorkflowRoleName: userInfo.isSuperAdminOrHead ? null : userInfo.workflowRoleName,
            isSuperAdminOrHead: userInfo.isSuperAdminOrHead
        ).ToList();

        var totalPool = allItems.Count(i => !i.IsClaimed && i.CurrentStage != WorkflowStage.Completed && (
            userInfo.isSuperAdminOrHead 
            || (isReleasingRole 
                ? ((int)i.CurrentStage == 10 || (int)i.CurrentStage == 19 || (int)i.CurrentStage == 30 || (i.CurrentStageName != null && i.CurrentStageName.Contains("Releas", StringComparison.OrdinalIgnoreCase)))
                : (!userStepId.HasValue || (int)i.CurrentStage == userStepId.Value))));
        var myItems = allItems.Where(i => i.IsClaimed && i.AssignedUserId == userInfo.userId && i.CurrentStage != WorkflowStage.Completed).ToList();
        var totalMy = myItems.Count;
        var totalDeferred = myItems.Count(i => i.IsDeferred || i.CurrentStage == WorkflowStage.Returned || i.StatusText.Contains("Deferred", StringComparison.OrdinalIgnoreCase));
        var totalCancelled = myItems.Count(i => i.StatusText.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) || i.StatusText.Contains("Rejected", StringComparison.OrdinalIgnoreCase));
        var totalNeedApproval = myItems.Count(i => !i.IsDeferred && !i.StatusText.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) && !i.StatusText.Contains("Rejected", StringComparison.OrdinalIgnoreCase) && i.CurrentStage != WorkflowStage.Completed);

        var queue = _workflowService.GetUnifiedQueue(
            divisionId: divisionId,
            mode: "my",
            stageFilter: null,
            search: search,
            requestType: type,
            userId: userInfo.userId,
            userWorkflowRoleId: userInfo.isSuperAdminOrHead ? null : userStepId,
            userWorkflowRoleName: userInfo.isSuperAdminOrHead ? null : userInfo.workflowRoleName,
            isSuperAdminOrHead: userInfo.isSuperAdminOrHead
        ).ToList();

        // Status Filter: Deferred, Cancelled, or Need Approval (plus All)
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLower();
            if (s == "deferred")
            {
                queue = queue.Where(i => i.IsDeferred || i.CurrentStage == WorkflowStage.Returned || i.StatusText.Contains("Deferred", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (s == "cancelled")
            {
                queue = queue.Where(i => i.StatusText.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) || i.StatusText.Contains("Rejected", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (s == "need_approval" || s == "need approval" || s == "approval")
            {
                queue = queue.Where(i => !i.IsDeferred && !i.StatusText.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) && !i.StatusText.Contains("Rejected", StringComparison.OrdinalIgnoreCase) && i.CurrentStage != WorkflowStage.Completed).ToList();
            }
        }

        ViewBag.QueueMode = "my";
        ViewBag.StatusFilter = status ?? "";
        ViewBag.DivisionId = divisionId;
        ViewBag.Search = search;
        ViewBag.TypeFilter = type;

        ViewBag.CurrentUserId = userInfo.userId;
        ViewBag.CurrentUserName = userInfo.userName;
        ViewBag.CurrentUserRole = userInfo.userRole;
        ViewBag.UserDivisionId = userInfo.divisionId;
        ViewBag.UserWorkflowRoleName = userInfo.workflowRoleName;
        ViewBag.IsSuperAdminOrHead = userInfo.isSuperAdminOrHead;
        ViewBag.CanChangeDivision = userInfo.isSuperAdminOrHead || isReleasingRole;
        ViewBag.IsRegularEmployee = userInfo.isRegularEmployee;

        ViewBag.TotalPool = totalPool;
        ViewBag.TotalMy = totalMy;
        ViewBag.TotalDeferred = totalDeferred;
        ViewBag.TotalCancelled = totalCancelled;
        ViewBag.TotalNeedApproval = totalNeedApproval;

        var targetDiv = (divisionId.HasValue && divisionId.Value >= 1 && divisionId.Value <= 3) 
            ? (DivisionType)divisionId.Value 
            : DivisionType.Division1_TaxAssessmentAndLandHolding;
        ViewBag.Stages = _workflowService.GetStagesForDivision(targetDiv);

        return View(queue);
    }

    // Alias: /Workflow/My
    public IActionResult My(int? divisionId, string? search, string? type, string? status)
    {
        return RedirectToAction(nameof(MyTransactions), new { divisionId, search, type, status });
    }

    // GET: /Workflow/Division1Queue
    public IActionResult Division1Queue(string? mode, WorkflowStage? stage, string? search)
    {
        return RedirectToAction(nameof(Pool), new { divisionId = 1, stage, search });
    }

    // GET: /Workflow/Division2Queue
    public IActionResult Division2Queue(string? mode, WorkflowStage? stage, string? search)
    {
        return RedirectToAction(nameof(Pool), new { divisionId = 2, stage, search });
    }

    // POST: /Workflow/Claim
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(Guid requestId, string requestType, DivisionType division, string? returnUrl = null)
    {
        if (!_permissionService.CanAccess(User, "Workflow", "Pool"))
        {
            TempData["ErrorMessage"] = "Your user role does not have permission to claim or process transactions from the Department Pool.";
            return Redirect(returnUrl ?? Url.Action(nameof(Pool), new { divisionId = (int)division })!);
        }

        var userInfo = GetCurrentUserInfo();

        bool success = _workflowService.ClaimDocument(requestId, requestType, userInfo.userId, userInfo.userName);

        if (success)
        {
            TempData["SuccessMessage"] = $"Transaction claimed successfully! It has been moved to your personal workspace ('My Transactions').";

            await _workflowNotifier.NotifyTransactionClaimedAsync(requestId, requestType, (int)division, userInfo.userName, userInfo.userId);
        }
        else
        {
            TempData["ErrorMessage"] = "Notice: This transaction has already been claimed by another staff member.";
        }

        // Once claimed from Pool, route directly to the user's My Transactions form
        return RedirectToAction(nameof(MyTransactions), new { divisionId = (int)division });
    }

    // POST: /Workflow/Release
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(Guid requestId, string requestType, DivisionType division, string? returnUrl = null)
    {
        if (!_permissionService.CanAccess(User, "Workflow", "MyTransactions") && !_permissionService.CanAccess(User, "Workflow", "Pool"))
        {
            TempData["ErrorMessage"] = "Your user role does not have permission to release this transaction type.";
            return Redirect(returnUrl ?? Url.Action(nameof(MyTransactions), new { divisionId = (int)division })!);
        }

        bool success = _workflowService.ReleaseDocumentToPool(requestId, requestType);

        if (success)
        {
            TempData["SuccessMessage"] = "Transaction released back to the Shared Pool.";

            await _workflowNotifier.NotifyTransactionReleasedAsync(requestId, requestType, (int)division);
        }
        else
        {
            TempData["ErrorMessage"] = "Could not release transaction.";
        }

        return RedirectToAction(nameof(MyTransactions), new { divisionId = (int)division });
    }

    // POST: /Workflow/Defer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Defer(Guid requestId, string requestType, DivisionType division, string remarks, string? returnUrl = null)
    {
        if (!_permissionService.CanAccess(User, "Workflow", "MyTransactions") && !_permissionService.CanAccess(User, "Workflow", "Pool"))
        {
            TempData["ErrorMessage"] = "Your user role does not have permission to return or defer this transaction type.";
            return Redirect(returnUrl ?? Url.Action(nameof(MyTransactions), new { divisionId = (int)division })!);
        }

        var userInfo = GetCurrentUserInfo();

        if (string.IsNullOrWhiteSpace(remarks))
        {
            TempData["ErrorMessage"] = "Remarks / Reason for deferral is mandatory when returning a transaction.";
            return RedirectToAction(nameof(MyTransactions), new { divisionId = (int)division });
        }

        _workflowService.DeferDocument(requestId, requestType, division, userInfo.userId, userInfo.userName, userInfo.userRole, remarks);

        TempData["SuccessMessage"] = "Transaction has been marked as Deferred and retained in your workspace.";

        await _workflowNotifier.NotifyTransactionDeferredAsync(requestId, requestType, (int)division, userInfo.userName, remarks);

        // When deferred, transaction stays in My Transactions
        return RedirectToAction(nameof(MyTransactions), new { divisionId = (int)division });
    }

    // POST: /Workflow/Forward
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Forward(
        Guid requestId,
        string requestType,
        DivisionType division,
        int targetStage,
        string forwardedByName,
        string role,
        string remarks,
        string actionTaken = "Forwarded",
        string? orNumber = null,
        DateTime? orDateIssued = null,
        string? returnUrl = null)
    {
        if (!_permissionService.CanAccess(User, "Workflow", "MyTransactions") && !_permissionService.CanAccess(User, "Workflow", "Pool"))
        {
            TempData["ErrorMessage"] = "Your user role does not have permission to route or forward this transaction type.";
            return Redirect(returnUrl ?? Url.Action(nameof(MyTransactions), new { divisionId = (int)division })!);
        }

        // Validate that OR Number is provided before allowing transfer for Tax Mapping, Land Holding, Tax Declaration, and Property Assessment
        bool isORRequiredType = requestType == "TaxMappingRequest" 
                             || requestType == "LandHoldingRequest" 
                             || requestType == "TaxDeclarationRequest"
                             || requestType == "PropertyAssessmentRequest";

        if (isORRequiredType)
        {
            var hasExistingOr = _workflowService.HasOfficialReceipt(requestId, requestType);
            if (string.IsNullOrWhiteSpace(orNumber) && !hasExistingOr)
            {
                TempData["ErrorMessage"] = "Official Receipt (O.R.) Number is required before transferring this transaction to the next stage.";
                return Redirect(returnUrl ?? Url.Action(nameof(MyTransactions), new { divisionId = (int)division })!);
            }
        }

        var userInfo = GetCurrentUserInfo();
        var senderName = string.IsNullOrWhiteSpace(forwardedByName) ? userInfo.userName : forwardedByName;
        var senderRole = string.IsNullOrWhiteSpace(role) ? userInfo.userRole : role;

        // If action is "For Approval" or user is Non-Regular employee, tag it For Approval and return to pool
        if (actionTaken == "For Approval" || (!userInfo.isRegularEmployee && !userInfo.isSuperAdminOrHead))
        {
            _workflowService.TagDocumentForApproval(requestId, requestType, division, userInfo.userId, senderName, senderRole, remarks);
            TempData["SuccessMessage"] = "Transaction has been flagged 'For Approval' and returned to the shared department pool.";
            
            await _workflowNotifier.NotifyTransactionTaggedForApprovalAsync(requestId, requestType, (int)division, senderName, remarks);

            return RedirectToAction(nameof(Pool), new { divisionId = (int)division });
        }

        var stageEnum = (WorkflowStage)targetStage;

        var log = _workflowService.ForwardDocument(
            requestId,
            requestType,
            division,
            stageEnum,
            senderName,
            senderRole,
            remarks,
            actionTaken,
            orNumber,
            orDateIssued);

        // Real-Time SignalR Broadcast to all connected receiving stations and staff
        var targetStatus = _context.TransactionStatuses.FirstOrDefault(s => s.TransactionStatusId == targetStage);
        var payload = new
        {
            requestId = requestId,
            requestType = requestType,
            trackingNumber = log.TrackingNumber,
            divisionId = (int)division,
            divisionName = division.ToString(),
            targetStageId = targetStage,
            targetStageName = targetStatus?.Name ?? stageEnum.ToString(),
            targetRole = log.AssignedToRole,
            forwardedByName = senderName,
            forwardedByRole = senderRole,
            remarks = remarks,
            timestamp = log.Timestamp.ToString("MMM dd, yyyy hh:mm tt"),
            orNumber = orNumber
        };

        await _workflowNotifier.NotifyTransactionForwardedAsync(payload);

        TempData["SuccessMessage"] = $"Document successfully routed to {log.AssignedToRole}. It is now pooled for the target department.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(MyTransactions), new { divisionId = (int)division });
    }

    // POST: /Workflow/TagForApproval
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TagForApproval(Guid requestId, string requestType, DivisionType division, string remarks, string? returnUrl = null)
    {
        if (!_permissionService.CanAccess(User, "Workflow", "MyTransactions") && !_permissionService.CanAccess(User, "Workflow", "Pool"))
        {
            TempData["ErrorMessage"] = "Your user role does not have permission to process or tag transactions.";
            return Redirect(returnUrl ?? Url.Action(nameof(MyTransactions), new { divisionId = (int)division })!);
        }

        var userInfo = GetCurrentUserInfo();
        var effectiveRemarks = string.IsNullOrWhiteSpace(remarks) ? "Flagged For Approval by Non-Regular staff" : remarks;

        _workflowService.TagDocumentForApproval(requestId, requestType, division, userInfo.userId, userInfo.userName, userInfo.userRole, effectiveRemarks);

        TempData["SuccessMessage"] = "Transaction has been successfully flagged 'For Approval' and returned to the shared department pool.";

        await _workflowNotifier.NotifyTransactionTaggedForApprovalAsync(requestId, requestType, (int)division, userInfo.userName, effectiveRemarks);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Pool), new { divisionId = (int)division });
    }

    // GET: /Workflow/GetNextWorkflowStages
    [HttpGet]
    public IActionResult GetNextWorkflowStages(Guid requestId, string requestType)
    {
        var userInfo = GetCurrentUserInfo();
        var stages = _workflowService.GetNextStagesForRequest(requestId, requestType, userInfo.userId);
        return Json(new { success = true, stages });
    }

    // GET: /Workflow/GetTransactionTimeline?id=guid&trackingNumber=TRK-xxx
    [HttpGet]
    public IActionResult GetTransactionTimeline(Guid? id, string? trackingNumber)
    {
        var timeline = _workflowService.GetTransactionTimeline(id, trackingNumber);
        if (timeline == null)
        {
            return Json(new { success = false, message = "Transaction record not found in system." });
        }
        return Json(new { success = true, timeline = timeline, data = timeline });
    }

    // GET: /Workflow/Timeline?id=guid&trackingNumber=TRK-xxx
    [HttpGet]
    public IActionResult Timeline(Guid? id, string? trackingNumber)
    {
        var timeline = _workflowService.GetTransactionTimeline(id, trackingNumber);
        if (timeline == null)
        {
            TempData["ErrorMessage"] = "No transaction found matching the specified reference number.";
            return RedirectToAction(nameof(Track), new { search = trackingNumber });
        }

        var userInfo = GetCurrentUserInfo();
        ViewBag.CurrentUserId = userInfo.userId;
        ViewBag.CurrentUserName = userInfo.userName;
        ViewBag.CurrentUserRole = userInfo.userRole;
        ViewBag.IsSuperAdminOrHead = userInfo.isSuperAdminOrHead;

        return View(timeline);
    }

    // GET: /Workflow/Track?search=xxx&divisionId=1&status=pending
    [HttpGet]
    public IActionResult Track(string? search, int? divisionId, string? status)
    {
        var userInfo = GetCurrentUserInfo();
        bool isReleasingRole = IsReleasingStation(userInfo.workflowRoleId, userInfo.workflowRoleName, userInfo.userRole);

        // RBAC Enforcement:
        // Ordinary users are restricted to their assigned division from their User Account.
        // Releasing staff can view and track across all divisions.
        if (!userInfo.isSuperAdminOrHead && !isReleasingRole)
        {
            divisionId = userInfo.divisionId ?? 1;
        }

        // Fetch all transactions in the division (Intake & Filed Registry)
        var allItems = _workflowService.GetUnifiedQueue(
            divisionId: divisionId,
            mode: null,
            stageFilter: null,
            search: search,
            requestType: null,
            userId: null
        ).ToList();

        ViewBag.Search = search;
        ViewBag.DivisionId = divisionId;
        ViewBag.StatusFilter = status ?? "";

        // Status Counts matching exact user rules: Pending, On Going, For Approval, Deferred, Released
        ViewBag.TotalFiled = allItems.Count;
        ViewBag.TotalPending = allItems.Count(i => i.StatusText == "Pending");
        ViewBag.TotalOnGoing = allItems.Count(i => i.StatusText == "On Going");
        ViewBag.TotalForApproval = allItems.Count(i => i.StatusText == "For Approval");
        ViewBag.TotalDeferred = allItems.Count(i => i.StatusText == "Deferred");
        ViewBag.TotalReleased = allItems.Count(i => i.StatusText == "Released");

        // Backwards compatibility
        ViewBag.TotalCompleted = ViewBag.TotalReleased;
        ViewBag.TotalInProgress = ViewBag.TotalOnGoing;
        ViewBag.TotalQueued = ViewBag.TotalPending;

        var filteredItems = allItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filteredItems = filteredItems.Where(i => i.StatusText.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.CurrentUserId = userInfo.userId;
        ViewBag.CurrentUserName = userInfo.userName;
        ViewBag.CurrentUserRole = userInfo.userRole;
        ViewBag.UserWorkflowRoleName = userInfo.workflowRoleName;
        ViewBag.IsSuperAdminOrHead = userInfo.isSuperAdminOrHead;
        ViewBag.CanChangeDivision = userInfo.isSuperAdminOrHead || isReleasingRole;
        ViewBag.IsRegularEmployee = userInfo.isRegularEmployee;

        return View(filteredItems.ToList());
    }

    // GET: /Workflow/Settings
    public IActionResult Settings()
    {
        ViewBag.Div1Setting = _workflowService.GetDivisionSetting(DivisionType.Division1_TaxAssessmentAndLandHolding);
        ViewBag.Div2Setting = _workflowService.GetDivisionSetting(DivisionType.Division2_GIS_TaxMapping);
        ViewBag.Div3Setting = _workflowService.GetDivisionSetting(DivisionType.Division3_RecordsAndCertification);
        ViewBag.DivisionSettings = _context.WorkflowDivisionSettings.OrderBy(d => (int)d.Division).ToList();
        ViewBag.Div1Stages = _workflowService.GetStagesForDivision(DivisionType.Division1_TaxAssessmentAndLandHolding);
        ViewBag.Div2Stages = _workflowService.GetStagesForDivision(DivisionType.Division2_GIS_TaxMapping);

        ViewBag.TransactionTypes = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
        {
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "1", Text = "Request for Inspection" },
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "2", Text = "Property Assessment" },
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "3", Text = "Tax Mapping" },
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "4", Text = "Tax Declaration" },
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "5", Text = "Land Holding" }
        };

        ViewBag.AllStatuses = _context.TransactionStatuses.OrderBy(s => s.TransactionStatusId).ToList();
        var sequences = _context.WorkflowSequences.ToList();

        return View(sequences);
    }

    // POST: /Workflow/UpdateWorkflowSettings
    [HttpPost]
    public IActionResult UpdateWorkflowSettings([FromBody] UpdateWorkflowSettingsRequest request)
    {
        if (request == null || request.TransactionTypeId <= 0)
        {
            return Json(new { success = false, message = "Invalid request payload." });
        }

        try
        {
            var divisionType = request.TransactionTypeId switch
            {
                1 or 2 => DivisionType.Division1_TaxAssessmentAndLandHolding,
                3 => DivisionType.Division2_GIS_TaxMapping,
                4 or 5 => DivisionType.Division3_RecordsAndCertification,
                _ => (DivisionType)0
            };

            if ((int)divisionType > 0 && request.Sequences != null)
            {
                var divSetting = _context.WorkflowDivisionSettings.FirstOrDefault(d => d.Division == divisionType);
                if (divSetting != null && divSetting.CharterTargetMinutes > 0)
                {
                    int allocated = request.Sequences
                        .GroupBy(s => s.CurrentStatusId)
                        .Sum(g => Math.Max(0, g.First().EstimatedMinutes));
                    if (allocated > divSetting.CharterTargetMinutes)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Total section time ({allocated} mins) exceeds the Division {(int)divisionType} budget ({divSetting.CharterTargetMinutes} mins). Please adjust the section minutes or increase the division time."
                        });
                    }
                }
            }

            var existing = _context.WorkflowSequences.Where(w => w.TransactionTypeId == request.TransactionTypeId).ToList();
            _context.WorkflowSequences.RemoveRange(existing);

            if (request.Sequences != null && request.Sequences.Any())
            {
                foreach (var seqDto in request.Sequences)
                {
                    _context.WorkflowSequences.Add(new WorkflowSequence
                    {
                        TransactionTypeId = request.TransactionTypeId,
                        CurrentStatusId = seqDto.CurrentStatusId,
                        NextStatusId = seqDto.NextStatusId,
                        IsApproval = seqDto.IsApproval,
                        EstimatedMinutes = seqDto.EstimatedMinutes < 0 ? 0 : seqDto.EstimatedMinutes
                    });
                }
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "Workflow sequence updated successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error updating workflow sequence: " + ex.Message });
        }
    }

    // POST: /Workflow/UpdateSetting
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateSetting(WorkflowDivisionSetting setting)
    {
        if (ModelState.IsValid)
        {
            _workflowService.UpdateDivisionSetting(setting);
            TempData["SuccessMessage"] = $"Workflow settings for {setting.Division} updated successfully.";
        }
        return RedirectToAction(nameof(Settings));
    }

    // POST: /Workflow/AddStage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddStage(DivisionType division, string stageName, string assignedRole, string? description, bool isApprovalStage)
    {
        if (!string.IsNullOrWhiteSpace(stageName) && !string.IsNullOrWhiteSpace(assignedRole))
        {
            _workflowService.AddStage(division, stageName, assignedRole, description, isApprovalStage);
            TempData["SuccessMessage"] = $"Added new workflow step '{stageName}' to {division}.";
        }
        return RedirectToAction(nameof(Settings));
    }

    // POST: /Workflow/UpdateStage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStage(WorkflowStageSetting stage)
    {
        if (ModelState.IsValid)
        {
            _workflowService.UpdateStage(stage);
            TempData["SuccessMessage"] = $"Updated stage '{stage.StageName}'.";
        }
        return RedirectToAction(nameof(Settings));
    }

    // POST: /Workflow/DeleteStage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteStage(Guid stageId)
    {
        _workflowService.DeleteStage(stageId);
        TempData["SuccessMessage"] = "Workflow stage removed.";
        return RedirectToAction(nameof(Settings));
    }

    // POST: /Workflow/MoveStage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MoveStage(Guid stageId, bool moveUp)
    {
        _workflowService.MoveStage(stageId, moveUp);
        TempData["SuccessMessage"] = "Workflow sequence updated.";
        return RedirectToAction(nameof(Settings));
    }

    // =========================================================================
    // OFFICE HOURS, BREAKTIME & HOLIDAY CALENDAR (CITIZEN'S CHARTER SLA)
    // =========================================================================

    [HttpGet]
    public IActionResult Calendar()
    {
        var setting = _charterSlaService.GetScheduleSetting();
        var holidays = _charterSlaService.GetHolidays(activeOnly: false);
        ViewBag.Setting = setting;
        ViewBag.Holidays = holidays;
        ViewBag.PauseReason = _charterSlaService.GetClockPauseReason(DateTime.Now, setting, holidays);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateOfficeSchedule(OfficeScheduleSetting model)
    {
        if (model.WorkEndTime <= model.WorkStartTime)
        {
            TempData["ErrorMessage"] = "Office End Time must be later than Office Start Time.";
            return RedirectToAction(nameof(Calendar));
        }

        if (model.HasBreakTime && (model.BreakEndTime <= model.BreakStartTime || model.BreakStartTime < model.WorkStartTime || model.BreakEndTime > model.WorkEndTime))
        {
            TempData["ErrorMessage"] = "Breaktime must fall within Office Start and End Times, with Break End later than Break Start.";
            return RedirectToAction(nameof(Calendar));
        }

        _charterSlaService.UpdateScheduleSetting(model);
        TempData["SuccessMessage"] = "Office hours, breaktime, and working days updated successfully.";
        return RedirectToAction(nameof(Calendar));
    }

    [HttpPost]
    public IActionResult SaveHoliday([FromBody] OfficeHoliday holiday)
    {
        if (holiday == null || string.IsNullOrWhiteSpace(holiday.Name))
        {
            return Json(new { success = false, message = "Holiday name is required." });
        }

        var saved = _charterSlaService.SaveHoliday(holiday);
        return Json(new { success = true, holiday = saved, message = $"Holiday '{saved.Name}' saved." });
    }

    [HttpPost]
    public IActionResult DeleteHoliday(int id)
    {
        bool deleted = _charterSlaService.DeleteHoliday(id);
        return Json(new { success = deleted, message = deleted ? "Holiday removed." : "Holiday not found." });
    }

    [HttpPost]
    public IActionResult ToggleHoliday(int id)
    {
        bool toggled = _charterSlaService.ToggleHoliday(id);
        return Json(new { success = toggled, message = toggled ? "Holiday status updated." : "Holiday not found." });
    }

    private sealed class NagerHolidayApiItem
    {
        public string? Date { get; set; }
        public string? LocalName { get; set; }
        public string? Name { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> FetchNagerHolidays(int year, int month, string countryCode = "PH")
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
        {
            return Json(new { success = false, message = "Invalid year or month." });
        }

        string code = string.IsNullOrWhiteSpace(countryCode) ? "PH" : countryCode.Trim().ToUpperInvariant();
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{code}";
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = $"Nager.Date returned HTTP {(int)resp.StatusCode}." });
            }

            var json = await resp.Content.ReadAsStringAsync();
            var items = System.Text.Json.JsonSerializer.Deserialize<List<NagerHolidayApiItem>>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var existingDates = _charterSlaService.GetHolidays(activeOnly: false)
                .Select(h => h.HolidayDate.Date)
                .ToHashSet();

            int added = 0;
            int matchedInMonth = 0;
            foreach (var item in items)
            {
                if (!DateTime.TryParse(item.Date, out var parsedDate)) continue;
                if (parsedDate.Year != year || parsedDate.Month != month) continue;

                matchedInMonth++;
                var holidayName = !string.IsNullOrWhiteSpace(item.LocalName) ? item.LocalName!.Trim() : (item.Name?.Trim() ?? "Public Holiday");
                if (!existingDates.Contains(parsedDate.Date))
                {
                    added++;
                }
                _charterSlaService.SaveHoliday(new OfficeHoliday
                {
                    HolidayDate = parsedDate.Date,
                    Name = holidayName,
                    IsRecurringYearly = false,
                    IsActive = true
                });
            }

            var allHolidays = _charterSlaService.GetHolidays(activeOnly: false)
                .Select(h => new
                {
                    id = h.Id,
                    date = h.HolidayDate.ToString("yyyy-MM-dd"),
                    monthDay = h.HolidayDate.ToString("MM-dd"),
                    name = h.Name,
                    isRecurringYearly = h.IsRecurringYearly,
                    isActive = h.IsActive
                }).ToList();

            string monthLabel = new DateTime(year, month, 1).ToString("MMMM yyyy");
            string msg = matchedInMonth == 0
                ? $"No {code} public holidays found in Nager.Date for {monthLabel}."
                : $"Fetched {matchedInMonth} {code} holiday(s) for {monthLabel} ({added} newly added).";

            return Json(new { success = true, matchedCount = matchedInMonth, addedCount = added, holidays = allHolidays, message = msg });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to fetch Nager.Date holidays: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult SaveDivisionCharterTime(int divisionId, int charterTargetMinutes)
    {
        if (divisionId < 1 || divisionId > 3 || charterTargetMinutes < 0)
        {
            return Json(new { success = false, message = "Invalid division or duration." });
        }

        var division = (DivisionType)divisionId;
        var setting = _context.WorkflowDivisionSettings.FirstOrDefault(d => d.Division == division);
        if (setting == null)
        {
            setting = new WorkflowDivisionSetting
            {
                Division = division,
                DivisionName = $"Division {divisionId}",
                CharterTargetMinutes = charterTargetMinutes,
                UpdatedDate = DateTime.Now
            };
            _context.WorkflowDivisionSettings.Add(setting);
        }
        else
        {
            setting.CharterTargetMinutes = charterTargetMinutes;
            setting.UpdatedDate = DateTime.Now;
        }

        _context.SaveChanges();
        return Json(new { success = true, divisionId, charterTargetMinutes, message = $"Division {divisionId} transaction time set to {charterTargetMinutes} min(s)." });
    }
}
