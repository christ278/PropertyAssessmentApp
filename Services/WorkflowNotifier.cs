using Microsoft.AspNetCore.SignalR;
using PropertyAssessmentApp.Hubs;

namespace PropertyAssessmentApp.Services;

public class WorkflowNotifier : IWorkflowNotifier
{
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly ILogger<WorkflowNotifier> _logger;

    public WorkflowNotifier(IHubContext<WorkflowHub> hubContext, ILogger<WorkflowNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyTransactionCreatedAsync(Guid requestId, string requestType, string trackingNumber, int divisionId, string? ownerOrApplicant = null)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TransactionCreated", new
            {
                requestId = requestId,
                requestType = requestType,
                trackingNumber = trackingNumber,
                divisionId = divisionId,
                ownerOrApplicant = ownerOrApplicant ?? string.Empty
            });

            await _hubContext.Clients.All.SendAsync("PoolCountsChanged", new
            {
                eventType = "Created",
                divisionId = divisionId,
                trackingNumber = trackingNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast TransactionCreated via SignalR");
        }
    }

    public async Task NotifyTransactionClaimedAsync(Guid requestId, string requestType, int divisionId, string claimedBy, Guid claimedUserId)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TransactionClaimed", new
            {
                requestId = requestId,
                requestType = requestType,
                divisionId = divisionId,
                claimedBy = claimedBy,
                claimedUserId = claimedUserId
            });

            await _hubContext.Clients.All.SendAsync("PoolCountsChanged", new
            {
                eventType = "Claimed",
                divisionId = divisionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast TransactionClaimed via SignalR");
        }
    }

    public async Task NotifyTransactionReleasedAsync(Guid requestId, string requestType, int divisionId)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TransactionReleased", new
            {
                requestId = requestId,
                requestType = requestType,
                divisionId = divisionId
            });

            await _hubContext.Clients.All.SendAsync("PoolCountsChanged", new
            {
                eventType = "Released",
                divisionId = divisionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast TransactionReleased via SignalR");
        }
    }

    public async Task NotifyTransactionForwardedAsync(object payload)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TransactionForwarded", payload);
            await _hubContext.Clients.All.SendAsync("PoolCountsChanged", new
            {
                eventType = "Forwarded"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast TransactionForwarded via SignalR");
        }
    }

    public async Task NotifyTransactionDeferredAsync(Guid requestId, string requestType, int divisionId, string deferredBy, string remarks)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TransactionDeferred", new
            {
                requestId = requestId,
                requestType = requestType,
                divisionId = divisionId,
                deferredBy = deferredBy,
                remarks = remarks
            });

            await _hubContext.Clients.All.SendAsync("PoolCountsChanged", new
            {
                eventType = "Deferred",
                divisionId = divisionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast TransactionDeferred via SignalR");
        }
    }

    public async Task NotifyTransactionTaggedForApprovalAsync(Guid requestId, string requestType, int divisionId, string taggedBy, string remarks)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TransactionTaggedForApproval", new
            {
                requestId = requestId,
                requestType = requestType,
                divisionId = divisionId,
                taggedBy = taggedBy,
                remarks = remarks
            });

            await _hubContext.Clients.All.SendAsync("PoolCountsChanged", new
            {
                eventType = "TaggedForApproval",
                divisionId = divisionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast TransactionTaggedForApproval via SignalR");
        }
    }

    public async Task NotifyPoolCountsChangedAsync(int? divisionId = null)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("PoolCountsChanged", new
            {
                eventType = "CountUpdated",
                divisionId = divisionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast PoolCountsChanged via SignalR");
        }
    }
}
