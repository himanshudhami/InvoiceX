using Core.Entities.Approval;
using Core.Interfaces;
using Core.Interfaces.Approval;

namespace Application.Services.Approval.Resolvers
{
    /// <summary>
    /// Resolves the direct manager of the requestor as the approver
    /// </summary>
    public class DirectManagerApproverResolver : IApproverResolver
    {
        private readonly IEmployeesRepository _employeesRepository;

        public DirectManagerApproverResolver(IEmployeesRepository employeesRepository)
        {
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        public string ApproverType => ApproverTypes.DirectManager;

        public async Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step)
        {
            var requestor = await _employeesRepository.GetByIdAsync(requestorId);
            if (requestor == null)
                return null;

            // Return the direct manager
            return requestor.ManagerId;
        }
    }
}
