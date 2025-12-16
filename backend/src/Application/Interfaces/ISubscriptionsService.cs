using Application.DTOs.Subscriptions;
using Core.Common;
using Core.Entities;
using SubscriptionsEntity = Core.Entities.Subscriptions;

namespace Application.Interfaces;

public interface ISubscriptionsService
{
    Task<Result<SubscriptionsEntity>> GetByIdAsync(Guid id);
    Task<Result<(IEnumerable<SubscriptionsEntity> Items, int TotalCount)>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortDescending = false, Dictionary<string, object>? filters = null);
    Task<Result<SubscriptionsEntity>> CreateAsync(CreateSubscriptionDto dto);
    Task<Result> UpdateAsync(Guid id, UpdateSubscriptionDto dto);
    Task<Result> DeleteAsync(Guid id);

    Task<Result<IEnumerable<SubscriptionAssignments>>> GetAssignmentsAsync(Guid subscriptionId);
    Task<Result<SubscriptionAssignments>> AddAssignmentAsync(Guid subscriptionId, CreateSubscriptionAssignmentDto dto);
    Task<Result> RevokeAssignmentAsync(Guid assignmentId, RevokeSubscriptionAssignmentDto dto);
    
    Task<Result> PauseSubscriptionAsync(Guid id, PauseSubscriptionDto dto);
    Task<Result> ResumeSubscriptionAsync(Guid id, ResumeSubscriptionDto dto);
    Task<Result> CancelSubscriptionAsync(Guid id, CancelSubscriptionDto dto);
}




