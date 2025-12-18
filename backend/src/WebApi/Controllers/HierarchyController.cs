using Application.DTOs.Hierarchy;
using Application.Interfaces.Hierarchy;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Employee hierarchy and organizational structure endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class HierarchyController : ControllerBase
    {
        private readonly IEmployeeHierarchyService _hierarchyService;

        /// <summary>
        /// Initializes a new instance of the HierarchyController
        /// </summary>
        public HierarchyController(IEmployeeHierarchyService hierarchyService)
        {
            _hierarchyService = hierarchyService ?? throw new ArgumentNullException(nameof(hierarchyService));
        }

        /// <summary>
        /// Get employee hierarchy details
        /// </summary>
        /// <param name="employeeId">The Employee ID</param>
        /// <returns>Employee with hierarchy information</returns>
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(typeof(EmployeeHierarchyDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEmployeeHierarchy(Guid employeeId)
        {
            var result = await _hierarchyService.GetEmployeeHierarchyAsync(employeeId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get direct reports for a manager
        /// </summary>
        /// <param name="managerId">The Manager's Employee ID</param>
        /// <returns>Direct reports with summary</returns>
        [HttpGet("direct-reports/{managerId}")]
        [ProducesResponseType(typeof(DirectReportsDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDirectReports(Guid managerId)
        {
            var result = await _hierarchyService.GetDirectReportsAsync(managerId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all subordinates (recursive) for a manager
        /// </summary>
        /// <param name="managerId">The Manager's Employee ID</param>
        /// <returns>All subordinates in the hierarchy</returns>
        [HttpGet("subordinates/{managerId}")]
        [ProducesResponseType(typeof(IEnumerable<EmployeeHierarchyDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAllSubordinates(Guid managerId)
        {
            var result = await _hierarchyService.GetAllSubordinatesAsync(managerId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get reporting chain for an employee (managers up to top)
        /// </summary>
        /// <param name="employeeId">The Employee ID</param>
        /// <returns>Reporting chain from employee to top</returns>
        [HttpGet("reporting-chain/{employeeId}")]
        [ProducesResponseType(typeof(ReportingChainDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReportingChain(Guid employeeId)
        {
            var result = await _hierarchyService.GetReportingChainAsync(employeeId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get organizational tree for visualization
        /// </summary>
        /// <param name="companyId">The Company ID</param>
        /// <param name="rootEmployeeId">Optional: Start from specific employee</param>
        /// <returns>Hierarchical org tree structure</returns>
        [HttpGet("org-tree")]
        [ProducesResponseType(typeof(IEnumerable<OrgTreeNodeDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetOrgTree([FromQuery] Guid companyId, [FromQuery] Guid? rootEmployeeId = null)
        {
            var result = await _hierarchyService.GetOrgTreeAsync(companyId, rootEmployeeId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all managers
        /// </summary>
        /// <param name="companyId">Optional: Filter by company</param>
        /// <returns>List of managers</returns>
        [HttpGet("managers")]
        [ProducesResponseType(typeof(IEnumerable<ManagerSummaryDto>), 200)]
        public async Task<IActionResult> GetManagers([FromQuery] Guid? companyId = null)
        {
            var result = await _hierarchyService.GetManagersAsync(companyId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get top-level employees (no manager) for a company
        /// </summary>
        /// <param name="companyId">The Company ID</param>
        /// <returns>Top-level employees</returns>
        [HttpGet("top-level")]
        [ProducesResponseType(typeof(IEnumerable<EmployeeHierarchyDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetTopLevelEmployees([FromQuery] Guid companyId)
        {
            var result = await _hierarchyService.GetTopLevelEmployeesAsync(companyId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get hierarchy statistics for a company
        /// </summary>
        /// <param name="companyId">The Company ID</param>
        /// <returns>Hierarchy statistics</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(HierarchyStatsDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetHierarchyStats([FromQuery] Guid companyId)
        {
            var result = await _hierarchyService.GetHierarchyStatsAsync(companyId);
            return HandleResult(result);
        }

        /// <summary>
        /// Update employee's manager
        /// </summary>
        /// <param name="employeeId">The Employee ID</param>
        /// <param name="dto">New manager assignment</param>
        /// <returns>Updated employee hierarchy info</returns>
        [HttpPut("employee/{employeeId}/manager")]
        [ProducesResponseType(typeof(EmployeeHierarchyDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateManager(Guid employeeId, [FromBody] UpdateManagerDto dto)
        {
            var result = await _hierarchyService.UpdateManagerAsync(employeeId, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Validate if manager assignment is valid (no circular reference)
        /// </summary>
        /// <param name="employeeId">The Employee ID</param>
        /// <param name="managerId">The proposed Manager ID</param>
        /// <returns>True if assignment is valid</returns>
        [HttpGet("validate-manager")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ValidateManagerAssignment([FromQuery] Guid employeeId, [FromQuery] Guid managerId)
        {
            var result = await _hierarchyService.ValidateManagerAssignmentAsync(employeeId, managerId);
            return HandleResult(result);
        }

        /// <summary>
        /// Check if user can approve for an employee (is in reporting chain)
        /// </summary>
        /// <param name="approverId">The potential approver's Employee ID</param>
        /// <param name="employeeId">The Employee ID</param>
        /// <returns>True if approver can approve for employee</returns>
        [HttpGet("can-approve")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CanApproveForEmployee([FromQuery] Guid approverId, [FromQuery] Guid employeeId)
        {
            var result = await _hierarchyService.CanApproveForEmployeeAsync(approverId, employeeId);
            return HandleResult(result);
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(result.Value);
        }
    }
}
