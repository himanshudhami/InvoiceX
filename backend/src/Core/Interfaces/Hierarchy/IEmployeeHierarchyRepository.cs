using Core.Entities;

namespace Core.Interfaces.Hierarchy
{
    /// <summary>
    /// Repository interface for employee hierarchy operations
    /// </summary>
    public interface IEmployeeHierarchyRepository
    {
        /// <summary>
        /// Get all direct reports (immediate subordinates) for a manager
        /// </summary>
        Task<IEnumerable<Employees>> GetDirectReportsAsync(Guid managerId);

        /// <summary>
        /// Get all subordinates (recursive) for a manager, including nested reports
        /// </summary>
        Task<IEnumerable<Employees>> GetAllSubordinatesAsync(Guid managerId);

        /// <summary>
        /// Get the reporting chain from an employee up to the top of hierarchy
        /// </summary>
        Task<IEnumerable<Employees>> GetReportingChainAsync(Guid employeeId);

        /// <summary>
        /// Check if a manager is in the reporting chain of an employee
        /// </summary>
        Task<bool> IsInReportingChainAsync(Guid managerId, Guid employeeId);

        /// <summary>
        /// Get the org tree starting from a root (or all roots if not specified)
        /// </summary>
        Task<IEnumerable<OrgTreeNode>> GetOrgTreeAsync(Guid companyId, Guid? rootEmployeeId = null);

        /// <summary>
        /// Get all employees who are managers (have direct reports)
        /// </summary>
        Task<IEnumerable<Employees>> GetManagersAsync(Guid? companyId = null);

        /// <summary>
        /// Get all top-level employees (no manager) for a company
        /// </summary>
        Task<IEnumerable<Employees>> GetTopLevelEmployeesAsync(Guid companyId);

        /// <summary>
        /// Update the manager for an employee
        /// </summary>
        Task UpdateManagerAsync(Guid employeeId, Guid? managerId);

        /// <summary>
        /// Get the count of direct reports for a manager
        /// </summary>
        Task<int> GetDirectReportsCountAsync(Guid managerId);

        /// <summary>
        /// Get the count of all subordinates (recursive) for a manager
        /// </summary>
        Task<int> GetAllSubordinatesCountAsync(Guid managerId);

        /// <summary>
        /// Validate that setting a manager won't create a circular reference
        /// </summary>
        Task<bool> WouldCreateCircularReferenceAsync(Guid employeeId, Guid managerId);

        /// <summary>
        /// Get employees at a specific reporting level within a company
        /// </summary>
        Task<IEnumerable<Employees>> GetEmployeesByReportingLevelAsync(Guid companyId, int level);
    }

    /// <summary>
    /// Represents a node in the organizational tree
    /// </summary>
    public class OrgTreeNode
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? Email { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public Guid? ManagerId { get; set; }
        public int ReportingLevel { get; set; }
        public int DirectReportsCount { get; set; }
        public int TotalSubordinatesCount { get; set; }
        public List<OrgTreeNode> Children { get; set; } = new();
    }
}
