using Application.DTOs.Approval;
using Core.Common;

namespace Application.Interfaces.Approval
{
    /// <summary>
    /// Service interface for approval template management (admin operations)
    /// </summary>
    public interface IApprovalTemplateService
    {
        #region Template Operations

        /// <summary>
        /// Gets all templates for a company
        /// </summary>
        Task<Result<IEnumerable<ApprovalWorkflowTemplateDto>>> GetByCompanyAsync(Guid companyId);

        /// <summary>
        /// Gets templates for a company filtered by activity type
        /// </summary>
        Task<Result<IEnumerable<ApprovalWorkflowTemplateDto>>> GetByCompanyAndActivityTypeAsync(Guid companyId, string activityType);

        /// <summary>
        /// Gets a template by ID with all steps
        /// </summary>
        Task<Result<ApprovalWorkflowTemplateDetailDto>> GetByIdAsync(Guid templateId);

        /// <summary>
        /// Creates a new approval workflow template
        /// </summary>
        Task<Result<ApprovalWorkflowTemplateDto>> CreateAsync(CreateApprovalTemplateDto dto);

        /// <summary>
        /// Updates an existing template
        /// </summary>
        Task<Result<ApprovalWorkflowTemplateDto>> UpdateAsync(Guid templateId, UpdateApprovalTemplateDto dto);

        /// <summary>
        /// Deletes a template and all its steps
        /// </summary>
        Task<Result> DeleteAsync(Guid templateId);

        /// <summary>
        /// Sets a template as the default for its activity type
        /// </summary>
        Task<Result> SetAsDefaultAsync(Guid templateId);

        /// <summary>
        /// Seeds default approval workflow templates for a newly created company
        /// Creates default templates for leave and asset_request activity types
        /// </summary>
        Task<Result> SeedDefaultTemplatesAsync(Guid companyId);

        #endregion

        #region Step Operations

        /// <summary>
        /// Adds a step to a template
        /// </summary>
        Task<Result<ApprovalWorkflowStepDto>> AddStepAsync(Guid templateId, CreateApprovalStepDto dto);

        /// <summary>
        /// Updates a step
        /// </summary>
        Task<Result<ApprovalWorkflowStepDto>> UpdateStepAsync(Guid stepId, UpdateApprovalStepDto dto);

        /// <summary>
        /// Deletes a step
        /// </summary>
        Task<Result> DeleteStepAsync(Guid stepId);

        /// <summary>
        /// Reorders steps in a template
        /// </summary>
        Task<Result> ReorderStepsAsync(Guid templateId, ReorderStepsDto dto);

        #endregion
    }
}
