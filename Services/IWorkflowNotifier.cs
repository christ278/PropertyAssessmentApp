namespace PropertyAssessmentApp.Services;

public interface IWorkflowNotifier
{
    Task NotifyTransactionCreatedAsync(Guid requestId, string requestType, string trackingNumber, int divisionId, string? ownerOrApplicant = null);
    Task NotifyTransactionClaimedAsync(Guid requestId, string requestType, int divisionId, string claimedBy, Guid claimedUserId);
    Task NotifyTransactionReleasedAsync(Guid requestId, string requestType, int divisionId);
    Task NotifyTransactionForwardedAsync(object payload);
    Task NotifyTransactionDeferredAsync(Guid requestId, string requestType, int divisionId, string deferredBy, string remarks);
    Task NotifyTransactionTaggedForApprovalAsync(Guid requestId, string requestType, int divisionId, string taggedBy, string remarks);
    Task NotifyPoolCountsChangedAsync(int? divisionId = null);
}
