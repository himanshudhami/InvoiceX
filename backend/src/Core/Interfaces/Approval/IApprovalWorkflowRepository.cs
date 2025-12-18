using Core.Entities.Approval;

namespace Core.Interfaces.Approval
{
    /// <summary>
    /// Repository interface for approval requests and request steps
    /// </summary>
    public interface IApprovalWorkflowRepository
    {
        // Request operations
        Task<ApprovalRequest?> GetByIdAsync(Guid id);
        Task<ApprovalRequest?> GetByIdWithStepsAsync(Guid id);
        Task<ApprovalRequest?> GetByActivityAsync(string activityType, Guid activityId);
        Task<IEnumerable<ApprovalRequest>> GetByRequestorAsync(Guid requestorId, string? status = null);
        Task<IEnumerable<ApprovalRequest>> GetByCompanyAsync(Guid companyId, string? status = null, string? activityType = null);
        Task<ApprovalRequest> AddAsync(ApprovalRequest request);
        Task UpdateAsync(ApprovalRequest request);
        Task UpdateStatusAsync(Guid requestId, string status, DateTime? completedAt = null);

        // Request step operations
        Task<ApprovalRequestStep?> GetStepByIdAsync(Guid stepId);
        Task<IEnumerable<ApprovalRequestStep>> GetStepsByRequestAsync(Guid requestId);
        Task<ApprovalRequestStep?> GetCurrentStepAsync(Guid requestId);
        Task<ApprovalRequestStep> AddStepAsync(ApprovalRequestStep step);
        Task UpdateStepAsync(ApprovalRequestStep step);
        Task UpdateStepStatusAsync(Guid stepId, string status, Guid? actionById, string? comments);

        // Pending approvals queries
        Task<IEnumerable<ApprovalRequestStep>> GetPendingApprovalsForUserAsync(Guid employeeId);
        Task<int> GetPendingApprovalsCountForUserAsync(Guid employeeId);

        // Bulk operations for creating request with steps
        Task<ApprovalRequest> CreateRequestWithStepsAsync(ApprovalRequest request, IEnumerable<ApprovalRequestStep> steps);
    }
}
