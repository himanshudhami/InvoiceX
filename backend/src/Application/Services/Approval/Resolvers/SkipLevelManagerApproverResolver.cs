using Core.Entities.Approval;
using Core.Interfaces;
using Core.Interfaces.Approval;

namespace Application.Services.Approval.Resolvers
{
    /// <summary>
    /// Resolves the skip-level manager (manager's manager) of the requestor as the approver
    /// </summary>
    public class SkipLevelManagerApproverResolver : IApproverResolver
    {
        private readonly IEmployeesRepository _employeesRepository;

        public SkipLevelManagerApproverResolver(IEmployeesRepository employeesRepository)
        {
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        public string ApproverType => ApproverTypes.SkipLevelManager;

        public async Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step)
        {
            var requestor = await _employeesRepository.GetByIdAsync(requestorId);
            if (requestor == null || !requestor.ManagerId.HasValue)
                return null;

            // Get the direct manager
            var directManager = await _employeesRepository.GetByIdAsync(requestor.ManagerId.Value);
            if (directManager == null)
                return null;

            // Return the manager's manager (skip level)
            return directManager.ManagerId;
        }
    }
}
