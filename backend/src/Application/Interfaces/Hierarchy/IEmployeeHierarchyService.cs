using Application.DTOs.Hierarchy;
using Core.Common;

namespace Application.Interfaces.Hierarchy
{
    /// <summary>
    /// Service interface for employee hierarchy operations
    /// </summary>
    public interface IEmployeeHierarchyService
    {
        /// <summary>
        /// Get an employee with hierarchy details
        /// </summary>
        Task<Result<EmployeeHierarchyDto>> GetEmployeeHierarchyAsync(Guid employeeId);

        /// <summary>
        /// Get direct reports for a manager
        /// </summary>
        Task<Result<DirectReportsDto>> GetDirectReportsAsync(Guid managerId);

        /// <summary>
        /// Get all subordinates (recursive) for a manager
        /// </summary>
        Task<Result<IEnumerable<EmployeeHierarchyDto>>> GetAllSubordinatesAsync(Guid managerId);

        /// <summary>
        /// Get the reporting chain for an employee (managers up to top)
        /// </summary>
        Task<Result<ReportingChainDto>> GetReportingChainAsync(Guid employeeId);

        /// <summary>
        /// Get the org tree for visualization
        /// </summary>
        Task<Result<IEnumerable<OrgTreeNodeDto>>> GetOrgTreeAsync(Guid companyId, Guid? rootEmployeeId = null);

        /// <summary>
        /// Get all managers in a company
        /// </summary>
        Task<Result<IEnumerable<ManagerSummaryDto>>> GetManagersAsync(Guid? companyId = null);

        /// <summary>
        /// Update an employee's manager
        /// </summary>
        Task<Result<EmployeeHierarchyDto>> UpdateManagerAsync(Guid employeeId, UpdateManagerDto dto);

        /// <summary>
        /// Check if a user can approve for an employee (is in reporting chain)
        /// </summary>
        Task<Result<bool>> CanApproveForEmployeeAsync(Guid approverId, Guid employeeId);

        /// <summary>
        /// Get hierarchy statistics for a company
        /// </summary>
        Task<Result<HierarchyStatsDto>> GetHierarchyStatsAsync(Guid companyId);

        /// <summary>
        /// Get employees without managers (top-level)
        /// </summary>
        Task<Result<IEnumerable<EmployeeHierarchyDto>>> GetTopLevelEmployeesAsync(Guid companyId);

        /// <summary>
        /// Validate if setting a manager would create a circular reference
        /// </summary>
        Task<Result<bool>> ValidateManagerAssignmentAsync(Guid employeeId, Guid managerId);
    }
}
