namespace Application.DTOs.Hierarchy
{
    /// <summary>
    /// DTO for displaying an employee in hierarchy context
    /// </summary>
    public class EmployeeHierarchyDto
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? Email { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int ReportingLevel { get; set; }
        public bool IsManager { get; set; }
        public int DirectReportsCount { get; set; }
    }

    /// <summary>
    /// DTO for org tree visualization
    /// </summary>
    public class OrgTreeNodeDto
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
        public List<OrgTreeNodeDto> Children { get; set; } = new();
    }

    /// <summary>
    /// DTO for updating an employee's manager
    /// </summary>
    public class UpdateManagerDto
    {
        public Guid? ManagerId { get; set; }
    }

    /// <summary>
    /// DTO for hierarchy statistics
    /// </summary>
    public class HierarchyStatsDto
    {
        public int TotalEmployees { get; set; }
        public int TotalManagers { get; set; }
        public int TopLevelEmployees { get; set; }
        public int MaxDepth { get; set; }
        public double AverageTeamSize { get; set; }
    }

    /// <summary>
    /// DTO for manager summary (used in dropdowns, etc.)
    /// </summary>
    public class ManagerSummaryDto
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public int DirectReportsCount { get; set; }
    }

    /// <summary>
    /// DTO for direct reports view
    /// </summary>
    public class DirectReportsDto
    {
        public Guid ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public int TotalDirectReports { get; set; }
        public int TotalSubordinates { get; set; }
        public List<EmployeeHierarchyDto> DirectReports { get; set; } = new();
    }

    /// <summary>
    /// DTO for reporting chain view (manager hierarchy from bottom to top)
    /// </summary>
    public class ReportingChainDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public List<EmployeeHierarchyDto> Chain { get; set; } = new();
    }
}
