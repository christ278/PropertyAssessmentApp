using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PropertyAssessmentApp.Helpers;

namespace PropertyAssessmentApp.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class JwtAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip authorization if action is decorated with [AllowAnonymous]
        if (context.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any())
        {
            return;
        }

        string? authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "Missing or malformed Authorization header. Expected 'Bearer <token>'."
            })
            {
                StatusCode = 401
            };
            return;
        }

        string token = authHeader["Bearer ".Length..].Trim();
        if (!JwtHelper.ValidateToken(token, out var payload, out var error))
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = $"Unauthorized: {error}"
            })
            {
                StatusCode = 401
            };
            return;
        }

        // Store validated user claims in HttpContext.Items
        context.HttpContext.Items["JwtUser"] = payload;
    }
}
