using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class AccountController : Controller
{
    private readonly IUserAccountService _userService;
    private readonly IPermissionService _permissionService;

    public AccountController(IUserAccountService userService, IPermissionService permissionService)
    {
        _userService = userService;
        _permissionService = permissionService;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _userService.GetByUsername(model.Username);

        if (user == null || !_userService.VerifyPassword(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            TempData["ErrorMessage"] = "Authentication failed: Invalid username or password.";
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Account is disabled. Please contact your system administrator.");
            TempData["ErrorMessage"] = "Account disabled. Access restricted.";
            return View(model);
        }

        // Update Last Login Timestamp
        user.LastLoginDate = DateTime.Now;
        _userService.Update(user);

        // Build Claims Principal
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("EmployeeNo", user.EmployeeNo),
            new Claim("Department", user.Department),
            new Claim("DivisionId", user.DivisionId?.ToString() ?? ""),
            new Claim("WorkflowRoleId", user.WorkflowRoleId?.ToString() ?? ""),
            new Claim("WorkflowRoleName", user.WorkflowRoleName ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
        return RedirectToLocal(returnUrl);
    }

    // POST: /Account/Logout
    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "You have been logged out successfully.";
        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && 
            Url.IsLocalUrl(returnUrl) && 
            !returnUrl.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) &&
            !returnUrl.Equals("/Account/Logout", StringComparison.OrdinalIgnoreCase) &&
            !returnUrl.Equals("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase) &&
            !returnUrl.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(returnUrl);
        }

        if (_permissionService.HasModuleAccess(User, "Workflow", "Pool"))
        {
            return RedirectToAction("Pool", "Workflow");
        }
        if (_permissionService.HasModuleAccess(User, "Workflow", "MyTransactions"))
        {
            return RedirectToAction("MyTransactions", "Workflow");
        }
        if (_permissionService.HasModuleAccess(User, "Assessment", "Index"))
        {
            return RedirectToAction("Index", "Assessment");
        }
        if (_permissionService.HasModuleAccess(User, "RequestForInspection", "Index"))
        {
            return RedirectToAction("Index", "RequestForInspection");
        }
        if (_permissionService.HasModuleAccess(User, "TaxMapping", "Index"))
        {
            return RedirectToAction("Index", "TaxMapping");
        }

        return RedirectToAction("Index", "Home");
    }
}
