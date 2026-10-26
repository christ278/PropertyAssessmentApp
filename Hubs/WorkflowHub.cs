using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PropertyAssessmentApp.Hubs;

public class WorkflowHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            var divisionId = user.FindFirst("DivisionId")?.Value;
            if (!string.IsNullOrEmpty(divisionId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Division_{divisionId}");
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
            }

            var workflowRoleId = user.FindFirst("WorkflowRoleId")?.Value;
            if (!string.IsNullOrEmpty(workflowRoleId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"WorkflowRole_{workflowRoleId}");
            }
        }

        // Global group for real-time assessor updates
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllStaff");

        await base.OnConnectedAsync();
    }

    public async Task JoinDivision(int divisionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Division_{divisionId}");
    }

    public async Task LeaveDivision(int divisionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Division_{divisionId}");
    }
}
