using Core.Entities.Approval;
using Core.Interfaces;
using Core.Interfaces.Approval;

namespace Application.Services.Approval.Resolvers
{
    /// <summary>
    /// Resolves a specific user configured in the workflow step as the approver
    /// </summary>
    public class SpecificUserApproverResolver : IApproverResolver
    {
        private readonly IEmployeesRepository _employeesRepository;

        public SpecificUserApproverResolver(IEmployeesRepository employeesRepository)
        {
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        public string ApproverType => ApproverTypes.SpecificUser;

        public async Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step)
        {
            if (!step.ApproverUserId.HasValue)
                return null;

            // Verify the user exists and is active
            var approver = await _employeesRepository.GetByIdAsync(step.ApproverUserId.Value);
            if (approver == null || approver.Status != "active")
                return null;

            // Don't allow self-approval
            if (approver.Id == requestorId)
                return null;

            return step.ApproverUserId;
        }
    }
}
