using Core.Entities.Approval;
using Core.Interfaces;
using Core.Interfaces.Approval;

namespace Application.Services.Approval.Resolvers
{
    /// <summary>
    /// Resolves the department head of the requestor as the approver.
    /// The department head is determined by finding a manager at the top of the department hierarchy.
    /// </summary>
    public class DepartmentHeadApproverResolver : IApproverResolver
    {
        private readonly IEmployeesRepository _employeesRepository;

        public DepartmentHeadApproverResolver(IEmployeesRepository employeesRepository)
        {
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        public string ApproverType => ApproverTypes.DepartmentHead;

        public async Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step)
        {
            var requestor = await _employeesRepository.GetByIdAsync(requestorId);
            if (requestor == null || string.IsNullOrEmpty(requestor.Department) || !requestor.CompanyId.HasValue)
                return null;

            // Get all employees in the same company and department
            var allEmployees = await _employeesRepository.GetAllAsync();
            var departmentEmployees = allEmployees
                .Where(e => e.CompanyId == requestor.CompanyId &&
                           e.Department == requestor.Department &&
                           e.Status == "active" &&
                           e.Id != requestorId)
                .ToList();

            // Find department head - someone with no manager within the department
            // or someone at the lowest reporting level who is a manager
            var potentialHeads = departmentEmployees
                .Where(e => e.IsManager)
                .OrderBy(e => e.ReportingLevel)
                .ToList();

            // If we have managers in the department, pick the highest level one (lowest reporting level)
            if (potentialHeads.Any())
            {
                return potentialHeads.First().Id;
            }

            // Fallback: Walk up the requestor's hierarchy to find first manager outside department
            // who manages people in this department
            var currentManager = requestor.ManagerId.HasValue
                ? await _employeesRepository.GetByIdAsync(requestor.ManagerId.Value)
                : null;

            while (currentManager != null)
            {
                // If manager is in same department, they might be department head
                if (currentManager.Department == requestor.Department && currentManager.IsManager)
                {
                    return currentManager.Id;
                }

                // Move up the chain
                if (currentManager.ManagerId.HasValue)
                {
                    currentManager = await _employeesRepository.GetByIdAsync(currentManager.ManagerId.Value);
                }
                else
                {
                    // Reached top, use last manager in chain
                    return currentManager.Id;
                }
            }

            return null;
        }
    }
}
