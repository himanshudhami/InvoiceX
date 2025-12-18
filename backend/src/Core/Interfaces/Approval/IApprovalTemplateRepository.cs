using Core.Entities.Approval;

namespace Core.Interfaces.Approval
{
    /// <summary>
    /// Repository interface for approval workflow templates and steps
    /// </summary>
    public interface IApprovalTemplateRepository
    {
        // Template operations
        Task<ApprovalWorkflowTemplate?> GetByIdAsync(Guid id);
        Task<ApprovalWorkflowTemplate?> GetByIdWithStepsAsync(Guid id);
        Task<IEnumerable<ApprovalWorkflowTemplate>> GetByCompanyAsync(Guid companyId);
        Task<IEnumerable<ApprovalWorkflowTemplate>> GetByCompanyAndActivityTypeAsync(Guid companyId, string activityType);
        Task<ApprovalWorkflowTemplate?> GetDefaultTemplateAsync(Guid companyId, string activityType);
        Task<ApprovalWorkflowTemplate> AddAsync(ApprovalWorkflowTemplate template);
        Task UpdateAsync(ApprovalWorkflowTemplate template);
        Task DeleteAsync(Guid id);
        Task SetAsDefaultAsync(Guid templateId, Guid companyId, string activityType);

        // Step operations
        Task<ApprovalWorkflowStep?> GetStepByIdAsync(Guid stepId);
        Task<IEnumerable<ApprovalWorkflowStep>> GetStepsByTemplateAsync(Guid templateId);
        Task<ApprovalWorkflowStep> AddStepAsync(ApprovalWorkflowStep step);
        Task UpdateStepAsync(ApprovalWorkflowStep step);
        Task DeleteStepAsync(Guid stepId);
        Task ReorderStepsAsync(Guid templateId, IEnumerable<Guid> orderedStepIds);
        Task<int> GetMaxStepOrderAsync(Guid templateId);
    }
}
