using Core.Entities.Approval;
using Core.Interfaces;
using Core.Interfaces.Approval;

namespace Application.Services.Approval.Resolvers
{
    /// <summary>
    /// Resolves an approver based on their role/designation (e.g., HR, Finance, Admin).
    /// Finds an active employee in the same company with matching designation or department.
    /// </summary>
    public class RoleBasedApproverResolver : IApproverResolver
    {
        private readonly IEmployeesRepository _employeesRepository;

        public RoleBasedApproverResolver(IEmployeesRepository employeesRepository)
        {
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        public string ApproverType => ApproverTypes.Role;

        public async Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step)
        {
            if (string.IsNullOrEmpty(step.ApproverRole))
                return null;

            var requestor = await _employeesRepository.GetByIdAsync(requestorId);
            if (requestor == null || !requestor.CompanyId.HasValue)
                return null;

            // Get all employees in the same company
            var allEmployees = await _employeesRepository.GetAllAsync();
            var companyEmployees = allEmployees
                .Where(e => e.CompanyId == requestor.CompanyId &&
                           e.Status == "active" &&
                           e.Id != requestorId)
                .ToList();

            // Find employee with matching role/designation
            // First try to match by designation
            var roleMatch = companyEmployees.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.Designation) &&
                e.Designation.Contains(step.ApproverRole, StringComparison.OrdinalIgnoreCase));

            if (roleMatch != null)
                return roleMatch.Id;

            // If no designation match, try department
            roleMatch = companyEmployees.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.Department) &&
                e.Department.Equals(step.ApproverRole, StringComparison.OrdinalIgnoreCase));

            return roleMatch?.Id;
        }
    }
}
