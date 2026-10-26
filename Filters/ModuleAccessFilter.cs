using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Filters;

public class ModuleAccessFilter : IActionFilter
{
    private readonly IPermissionService _permissionService;

    public ModuleAccessFilter(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
        var actionName = context.RouteData.Values["action"]?.ToString() ?? "";

        // Unrestricted system controllers
        if (controllerName.Equals("Account", StringComparison.OrdinalIgnoreCase) ||
            controllerName.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
            controllerName.Equals("Location", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Check 1: Is user role set?
        if (!_permissionService.IsUserRoleSet(user))
        {
            HandleAccessDenied(context, "User role is not being set. Please contact your system administrator.");
            return;
        }

        // Check 2: Does user role have access to this module/form?
        if (!_permissionService.HasModuleAccess(user, controllerName, actionName))
        {
            HandleAccessDenied(context, "Your user role does not have access assigned to this form.");
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private void HandleAccessDenied(ActionExecutingContext context, string message)
    {
        var request = context.HttpContext.Request;
        bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                      (request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase));

        if (isAjax)
        {
            context.Result = new JsonResult(new { success = false, message });
            return;
        }

        if (context.Controller is Controller controller)
        {
            controller.TempData["ErrorMessage"] = message;
        }

        string referer = request.Headers.Referer.ToString();
        string currentPath = request.Path.ToString();

        if (string.IsNullOrWhiteSpace(referer) || referer.Contains(currentPath, StringComparison.OrdinalIgnoreCase))
        {
            referer = "/Home/Index";
        }

        context.Result = new RedirectResult(referer);
    }
}
