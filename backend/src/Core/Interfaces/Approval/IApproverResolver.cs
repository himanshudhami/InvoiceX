using Core.Entities.Approval;

namespace Core.Interfaces.Approval
{
    /// <summary>
    /// Strategy interface for resolving the appropriate approver based on approver type.
    /// Different implementations handle different approver types (DirectManager, Role, etc.)
    /// </summary>
    public interface IApproverResolver
    {
        /// <summary>
        /// The approver type this resolver handles (e.g., "direct_manager", "role")
        /// </summary>
        string ApproverType { get; }

        /// <summary>
        /// Resolves the employee who should approve for the given requestor and step configuration
        /// </summary>
        /// <param name="requestorId">The employee requesting approval</param>
        /// <param name="step">The workflow step configuration</param>
        /// <returns>The employee ID of the resolved approver, or null if no approver found</returns>
        Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step);
    }
}
