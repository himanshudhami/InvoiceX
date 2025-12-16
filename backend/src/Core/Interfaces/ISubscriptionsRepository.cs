using Core.Entities;

namespace Core.Interfaces;

public interface ISubscriptionsRepository
{
    Task<Subscriptions?> GetByIdAsync(Guid id);
    Task<IEnumerable<Subscriptions>> GetAllAsync(Guid? companyId = null);
    Task<(IEnumerable<Subscriptions> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);

    Task<Subscriptions> AddAsync(Subscriptions entity);
    Task UpdateAsync(Subscriptions entity);
    Task DeleteAsync(Guid id);

    Task<IEnumerable<SubscriptionAssignments>> GetAssignmentsAsync(Guid subscriptionId);
    Task<SubscriptionAssignments> AddAssignmentAsync(SubscriptionAssignments assignment);
    Task RevokeAssignmentAsync(Guid assignmentId, DateTime? revokedOn);
    Task PauseSubscriptionAsync(Guid subscriptionId, DateTime? pausedOn);
    Task ResumeSubscriptionAsync(Guid subscriptionId, DateTime? resumedOn);
    Task CancelSubscriptionAsync(Guid subscriptionId, DateTime? cancelledOn);
}




