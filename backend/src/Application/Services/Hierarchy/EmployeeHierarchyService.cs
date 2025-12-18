using Application.DTOs.Hierarchy;
using Application.Interfaces.Hierarchy;
using Core.Common;
using Core.Interfaces;
using Core.Interfaces.Hierarchy;

namespace Application.Services.Hierarchy
{
    /// <summary>
    /// Service implementation for employee hierarchy operations
    /// </summary>
    public class EmployeeHierarchyService : IEmployeeHierarchyService
    {
        private readonly IEmployeeHierarchyRepository _hierarchyRepository;
        private readonly IEmployeesRepository _employeesRepository;

        public EmployeeHierarchyService(
            IEmployeeHierarchyRepository hierarchyRepository,
            IEmployeesRepository employeesRepository)
        {
            _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        public async Task<Result<EmployeeHierarchyDto>> GetEmployeeHierarchyAsync(Guid employeeId)
        {
            if (employeeId == Guid.Empty)
                return Error.Validation("Employee ID is required");

            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee == null)
                return Error.NotFound($"Employee with ID {employeeId} not found");

            string? managerName = null;
            if (employee.ManagerId.HasValue)
            {
                var manager = await _employeesRepository.GetByIdAsync(employee.ManagerId.Value);
                managerName = manager?.EmployeeName;
            }

            var directReportsCount = await _hierarchyRepository.GetDirectReportsCountAsync(employeeId);

            return Result<EmployeeHierarchyDto>.Success(new EmployeeHierarchyDto
            {
                Id = employee.Id,
                EmployeeName = employee.EmployeeName,
                EmployeeId = employee.EmployeeId,
                Email = employee.Email,
                Department = employee.Department,
                Designation = employee.Designation,
                ManagerId = employee.ManagerId,
                ManagerName = managerName,
                ReportingLevel = employee.ReportingLevel,
                IsManager = employee.IsManager,
                DirectReportsCount = directReportsCount
            });
        }

        public async Task<Result<DirectReportsDto>> GetDirectReportsAsync(Guid managerId)
        {
            if (managerId == Guid.Empty)
                return Error.Validation("Manager ID is required");

            var manager = await _employeesRepository.GetByIdAsync(managerId);
            if (manager == null)
                return Error.NotFound($"Manager with ID {managerId} not found");

            var directReports = await _hierarchyRepository.GetDirectReportsAsync(managerId);
            var totalSubordinates = await _hierarchyRepository.GetAllSubordinatesCountAsync(managerId);

            var reportsList = new List<EmployeeHierarchyDto>();
            foreach (var emp in directReports)
            {
                var reportsCount = await _hierarchyRepository.GetDirectReportsCountAsync(emp.Id);
                reportsList.Add(new EmployeeHierarchyDto
                {
                    Id = emp.Id,
                    EmployeeName = emp.EmployeeName,
                    EmployeeId = emp.EmployeeId,
                    Email = emp.Email,
                    Department = emp.Department,
                    Designation = emp.Designation,
                    ManagerId = emp.ManagerId,
                    ManagerName = manager.EmployeeName,
                    ReportingLevel = emp.ReportingLevel,
                    IsManager = emp.IsManager,
                    DirectReportsCount = reportsCount
                });
            }

            return Result<DirectReportsDto>.Success(new DirectReportsDto
            {
                ManagerId = managerId,
                ManagerName = manager.EmployeeName,
                TotalDirectReports = reportsList.Count,
                TotalSubordinates = totalSubordinates,
                DirectReports = reportsList
            });
        }

        public async Task<Result<IEnumerable<EmployeeHierarchyDto>>> GetAllSubordinatesAsync(Guid managerId)
        {
            if (managerId == Guid.Empty)
                return Error.Validation("Manager ID is required");

            var manager = await _employeesRepository.GetByIdAsync(managerId);
            if (manager == null)
                return Error.NotFound($"Manager with ID {managerId} not found");

            var subordinates = await _hierarchyRepository.GetAllSubordinatesAsync(managerId);
            var result = new List<EmployeeHierarchyDto>();

            foreach (var emp in subordinates)
            {
                string? empManagerName = null;
                if (emp.ManagerId.HasValue)
                {
                    var empManager = await _employeesRepository.GetByIdAsync(emp.ManagerId.Value);
                    empManagerName = empManager?.EmployeeName;
                }

                var reportsCount = await _hierarchyRepository.GetDirectReportsCountAsync(emp.Id);
                result.Add(new EmployeeHierarchyDto
                {
                    Id = emp.Id,
                    EmployeeName = emp.EmployeeName,
                    EmployeeId = emp.EmployeeId,
                    Email = emp.Email,
                    Department = emp.Department,
                    Designation = emp.Designation,
                    ManagerId = emp.ManagerId,
                    ManagerName = empManagerName,
                    ReportingLevel = emp.ReportingLevel,
                    IsManager = emp.IsManager,
                    DirectReportsCount = reportsCount
                });
            }

            return Result<IEnumerable<EmployeeHierarchyDto>>.Success(result);
        }

        public async Task<Result<ReportingChainDto>> GetReportingChainAsync(Guid employeeId)
        {
            if (employeeId == Guid.Empty)
                return Error.Validation("Employee ID is required");

            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee == null)
                return Error.NotFound($"Employee with ID {employeeId} not found");

            var chain = await _hierarchyRepository.GetReportingChainAsync(employeeId);
            var chainList = new List<EmployeeHierarchyDto>();

            foreach (var emp in chain)
            {
                string? managerName = null;
                if (emp.ManagerId.HasValue)
                {
                    var mgr = await _employeesRepository.GetByIdAsync(emp.ManagerId.Value);
                    managerName = mgr?.EmployeeName;
                }

                chainList.Add(new EmployeeHierarchyDto
                {
                    Id = emp.Id,
                    EmployeeName = emp.EmployeeName,
                    EmployeeId = emp.EmployeeId,
                    Email = emp.Email,
                    Department = emp.Department,
                    Designation = emp.Designation,
                    ManagerId = emp.ManagerId,
                    ManagerName = managerName,
                    ReportingLevel = emp.ReportingLevel,
                    IsManager = emp.IsManager
                });
            }

            return Result<ReportingChainDto>.Success(new ReportingChainDto
            {
                EmployeeId = employeeId,
                EmployeeName = employee.EmployeeName,
                Chain = chainList
            });
        }

        public async Task<Result<IEnumerable<OrgTreeNodeDto>>> GetOrgTreeAsync(Guid companyId, Guid? rootEmployeeId = null)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            var tree = await _hierarchyRepository.GetOrgTreeAsync(companyId, rootEmployeeId);
            var result = tree.Select(MapToOrgTreeNodeDto).ToList();

            return Result<IEnumerable<OrgTreeNodeDto>>.Success(result);
        }

        private static OrgTreeNodeDto MapToOrgTreeNodeDto(OrgTreeNode node)
        {
            return new OrgTreeNodeDto
            {
                Id = node.Id,
                EmployeeName = node.EmployeeName,
                EmployeeId = node.EmployeeId,
                Email = node.Email,
                Department = node.Department,
                Designation = node.Designation,
                ManagerId = node.ManagerId,
                ReportingLevel = node.ReportingLevel,
                DirectReportsCount = node.DirectReportsCount,
                TotalSubordinatesCount = node.TotalSubordinatesCount,
                Children = node.Children.Select(MapToOrgTreeNodeDto).ToList()
            };
        }

        public async Task<Result<IEnumerable<ManagerSummaryDto>>> GetManagersAsync(Guid? companyId = null)
        {
            var managers = await _hierarchyRepository.GetManagersAsync(companyId);
            var result = new List<ManagerSummaryDto>();

            foreach (var mgr in managers)
            {
                var reportsCount = await _hierarchyRepository.GetDirectReportsCountAsync(mgr.Id);
                result.Add(new ManagerSummaryDto
                {
                    Id = mgr.Id,
                    EmployeeName = mgr.EmployeeName,
                    EmployeeId = mgr.EmployeeId,
                    Department = mgr.Department,
                    Designation = mgr.Designation,
                    DirectReportsCount = reportsCount
                });
            }

            return Result<IEnumerable<ManagerSummaryDto>>.Success(result);
        }

        public async Task<Result<EmployeeHierarchyDto>> UpdateManagerAsync(Guid employeeId, UpdateManagerDto dto)
        {
            if (employeeId == Guid.Empty)
                return Error.Validation("Employee ID is required");

            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee == null)
                return Error.NotFound($"Employee with ID {employeeId} not found");

            // Validate new manager if specified
            if (dto.ManagerId.HasValue)
            {
                if (dto.ManagerId.Value == employeeId)
                    return Error.Validation("An employee cannot be their own manager");

                var newManager = await _employeesRepository.GetByIdAsync(dto.ManagerId.Value);
                if (newManager == null)
                    return Error.NotFound($"Manager with ID {dto.ManagerId} not found");

                // Check for circular reference
                var wouldCreateCircular = await _hierarchyRepository.WouldCreateCircularReferenceAsync(employeeId, dto.ManagerId.Value);
                if (wouldCreateCircular)
                    return Error.Validation("This assignment would create a circular reference in the hierarchy");
            }

            await _hierarchyRepository.UpdateManagerAsync(employeeId, dto.ManagerId);

            // Return updated hierarchy info
            return await GetEmployeeHierarchyAsync(employeeId);
        }

        public async Task<Result<bool>> CanApproveForEmployeeAsync(Guid approverId, Guid employeeId)
        {
            if (approverId == Guid.Empty)
                return Error.Validation("Approver ID is required");

            if (employeeId == Guid.Empty)
                return Error.Validation("Employee ID is required");

            // An employee cannot approve their own requests
            if (approverId == employeeId)
                return Result<bool>.Success(false);

            // Check if approver is in the employee's reporting chain
            var isInChain = await _hierarchyRepository.IsInReportingChainAsync(approverId, employeeId);
            return Result<bool>.Success(isInChain);
        }

        public async Task<Result<HierarchyStatsDto>> GetHierarchyStatsAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            var allEmployees = await _employeesRepository.GetAllAsync();
            var companyEmployees = allEmployees.Where(e => e.CompanyId == companyId).ToList();

            var totalEmployees = companyEmployees.Count;
            var totalManagers = companyEmployees.Count(e => e.IsManager);
            var topLevelEmployees = companyEmployees.Count(e => e.ManagerId == null);
            var maxDepth = companyEmployees.Any() ? companyEmployees.Max(e => e.ReportingLevel) : 0;

            double averageTeamSize = 0;
            if (totalManagers > 0)
            {
                var managerIds = companyEmployees.Where(e => e.IsManager).Select(e => e.Id).ToList();
                var totalReports = 0;
                foreach (var managerId in managerIds)
                {
                    totalReports += await _hierarchyRepository.GetDirectReportsCountAsync(managerId);
                }
                averageTeamSize = (double)totalReports / totalManagers;
            }

            return Result<HierarchyStatsDto>.Success(new HierarchyStatsDto
            {
                TotalEmployees = totalEmployees,
                TotalManagers = totalManagers,
                TopLevelEmployees = topLevelEmployees,
                MaxDepth = maxDepth,
                AverageTeamSize = Math.Round(averageTeamSize, 2)
            });
        }

        public async Task<Result<IEnumerable<EmployeeHierarchyDto>>> GetTopLevelEmployeesAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            var topLevel = await _hierarchyRepository.GetTopLevelEmployeesAsync(companyId);
            var result = new List<EmployeeHierarchyDto>();

            foreach (var emp in topLevel)
            {
                var reportsCount = await _hierarchyRepository.GetDirectReportsCountAsync(emp.Id);
                result.Add(new EmployeeHierarchyDto
                {
                    Id = emp.Id,
                    EmployeeName = emp.EmployeeName,
                    EmployeeId = emp.EmployeeId,
                    Email = emp.Email,
                    Department = emp.Department,
                    Designation = emp.Designation,
                    ManagerId = null,
                    ManagerName = null,
                    ReportingLevel = emp.ReportingLevel,
                    IsManager = emp.IsManager,
                    DirectReportsCount = reportsCount
                });
            }

            return Result<IEnumerable<EmployeeHierarchyDto>>.Success(result);
        }

        public async Task<Result<bool>> ValidateManagerAssignmentAsync(Guid employeeId, Guid managerId)
        {
            if (employeeId == Guid.Empty)
                return Error.Validation("Employee ID is required");

            if (managerId == Guid.Empty)
                return Error.Validation("Manager ID is required");

            if (employeeId == managerId)
                return Result<bool>.Success(false);

            var wouldCreateCircular = await _hierarchyRepository.WouldCreateCircularReferenceAsync(employeeId, managerId);
            return Result<bool>.Success(!wouldCreateCircular);
        }
    }
}
