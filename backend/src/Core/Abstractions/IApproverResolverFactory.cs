using Core.Entities.Approval;
using Core.Interfaces.Approval;

namespace Core.Abstractions
{
    /// <summary>
    /// Factory interface for resolving the appropriate IApproverResolver based on approver type.
    /// </summary>
    public interface IApproverResolverFactory
    {
        /// <summary>
        /// Gets the resolver for the specified approver type
        /// </summary>
        /// <param name="approverType">The approver type (e.g., direct_manager, role, specific_user)</param>
        /// <returns>The resolver implementation or null if not found</returns>
        IApproverResolver? GetResolver(string approverType);

        /// <summary>
        /// Resolves the approver for a workflow step
        /// </summary>
        /// <param name="requestorId">The ID of the employee who made the request</param>
        /// <param name="step">The workflow step containing approver configuration</param>
        /// <returns>The ID of the resolved approver, or null if no approver could be resolved</returns>
        Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step);
    }
}
