using Application.DTOs.Approval;
using Application.DTOs.Hierarchy;
using Application.DTOs.Leave;
using Application.Interfaces.Approval;
using Application.Interfaces.Hierarchy;
using Application.Interfaces.Leave;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Portal
{
    /// <summary>
    /// Employee Portal Manager-specific API endpoints.
    /// Provides team management and approval capabilities for managers.
    /// </summary>
    [ApiController]
    [Route("api/portal/manager")]
    [Produces("application/json")]
    [Authorize]
    public class ManagerPortalController : ControllerBase
    {
        private readonly IApprovalWorkflowService _approvalService;
        private readonly IEmployeeHierarchyService _hierarchyService;
        private readonly ILeaveService _leaveService;

        public ManagerPortalController(
            IApprovalWorkflowService approvalService,
            IEmployeeHierarchyService hierarchyService,
            ILeaveService leaveService)
        {
            _approvalService = approvalService ?? throw new ArgumentNullException(nameof(approvalService));
            _hierarchyService = hierarchyService ?? throw new ArgumentNullException(nameof(hierarchyService));
            _leaveService = leaveService ?? throw new ArgumentNullException(nameof(leaveService));
        }

        private Guid? CurrentEmployeeId
        {
            get
            {
                var claim = User.FindFirst("employee_id");
                if (claim != null && Guid.TryParse(claim.Value, out var employeeId))
                    return employeeId;
                return null;
            }
        }

        private Guid? CurrentCompanyId
        {
            get
            {
                var claim = User.FindFirst("company_id");
                if (claim != null && Guid.TryParse(claim.Value, out var companyId))
                    return companyId;
                return null;
            }
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Value);

            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
                ErrorType.Forbidden => StatusCode(403, new { error = result.Error.Message }),
                ErrorType.Conflict => Conflict(new { error = result.Error.Message }),
                _ => StatusCode(500, new { error = result.Error.Message })
            };
        }

        private IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
                return Ok(new { success = true });

            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
                ErrorType.Forbidden => StatusCode(403, new { error = result.Error.Message }),
                ErrorType.Conflict => Conflict(new { error = result.Error.Message }),
                _ => StatusCode(500, new { error = result.Error.Message })
            };
        }

        // ==================== Team Management ====================

        /// <summary>
        /// Get manager's direct reports
        /// </summary>
        /// <returns>List of direct reports with their details</returns>
        [HttpGet("team")]
        [ProducesResponseType(typeof(DirectReportsDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMyTeam()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _hierarchyService.GetDirectReportsAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all subordinates (including indirect reports)
        /// </summary>
        /// <returns>List of all subordinates</returns>
        [HttpGet("team/all")]
        [ProducesResponseType(typeof(IEnumerable<EmployeeHierarchyDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAllSubordinates()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _hierarchyService.GetAllSubordinatesAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        /// <summary>
        /// Get manager dashboard stats
        /// </summary>
        /// <returns>Dashboard statistics for the manager</returns>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ManagerDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetDashboard()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            // Get direct reports
            var teamResult = await _hierarchyService.GetDirectReportsAsync(CurrentEmployeeId.Value);
            var directReportsCount = teamResult.IsSuccess ? teamResult.Value!.DirectReports.Count() : 0;

            // Get pending approvals count
            var pendingResult = await _approvalService.GetPendingApprovalsCountAsync(CurrentEmployeeId.Value);
            var pendingApprovalsCount = pendingResult.IsSuccess ? pendingResult.Value : 0;

            // Get all subordinates count
            var allSubordinatesResult = await _hierarchyService.GetAllSubordinatesAsync(CurrentEmployeeId.Value);
            var totalTeamSize = allSubordinatesResult.IsSuccess ? allSubordinatesResult.Value!.Count() : 0;

            var dashboard = new ManagerDashboardDto
            {
                DirectReportsCount = directReportsCount,
                TotalTeamSize = totalTeamSize,
                PendingApprovalsCount = pendingApprovalsCount
            };

            return Ok(dashboard);
        }

        // ==================== Approval Management ====================

        /// <summary>
        /// Get pending approvals for the manager
        /// </summary>
        /// <returns>List of pending approval requests</returns>
        [HttpGet("approvals/pending")]
        [ProducesResponseType(typeof(IEnumerable<PendingApprovalDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPendingApprovals()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _approvalService.GetPendingApprovalsForUserAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        /// <summary>
        /// Get count of pending approvals
        /// </summary>
        /// <returns>Count of pending approvals</returns>
        [HttpGet("approvals/pending/count")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPendingApprovalsCount()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _approvalService.GetPendingApprovalsCountAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        /// <summary>
        /// Get approval request details
        /// </summary>
        /// <param name="requestId">Approval request ID</param>
        /// <returns>Approval request details</returns>
        [HttpGet("approvals/{requestId}")]
        [ProducesResponseType(typeof(ApprovalRequestDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetApprovalDetails(Guid requestId)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _approvalService.GetRequestStatusAsync(requestId);
            return HandleResult(result);
        }

        /// <summary>
        /// Approve a request
        /// </summary>
        /// <param name="requestId">Approval request ID</param>
        /// <param name="dto">Approval details</param>
        /// <returns>Updated approval request</returns>
        [HttpPost("approvals/{requestId}/approve")]
        [ProducesResponseType(typeof(ApprovalRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(Guid requestId, [FromBody] ApproveRequestDto dto)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _approvalService.ApproveAsync(requestId, CurrentEmployeeId.Value, dto);

            if (result.IsSuccess && result.Value!.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                // Update the activity status based on activity type
                if (result.Value.ActivityType == "leave")
                {
                    await _leaveService.UpdateLeaveStatusAsync(result.Value.ActivityId, "approved");
                }
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Reject a request
        /// </summary>
        /// <param name="requestId">Approval request ID</param>
        /// <param name="dto">Rejection details</param>
        /// <returns>Updated approval request</returns>
        [HttpPost("approvals/{requestId}/reject")]
        [ProducesResponseType(typeof(ApprovalRequestDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid requestId, [FromBody] RejectRequestDto dto)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _approvalService.RejectAsync(requestId, CurrentEmployeeId.Value, dto);

            if (result.IsSuccess && result.Value!.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                // Update the activity status based on activity type
                if (result.Value.ActivityType == "leave")
                {
                    await _leaveService.UpdateLeaveStatusAsync(result.Value.ActivityId, "rejected", dto.Reason);
                }
            }

            return HandleResult(result);
        }

        // ==================== Team Leave Management ====================

        /// <summary>
        /// Get team's leave applications (direct reports)
        /// </summary>
        /// <param name="status">Optional filter by status</param>
        /// <returns>List of team leave applications</returns>
        [HttpGet("team/leaves")]
        [ProducesResponseType(typeof(IEnumerable<TeamLeaveApplicationDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTeamLeaves([FromQuery] string? status = null)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.GetTeamLeaveApplicationsAsync(CurrentEmployeeId.Value, status);
            return HandleResult(result);
        }
    }

    /// <summary>
    /// Manager dashboard statistics DTO
    /// </summary>
    public class ManagerDashboardDto
    {
        public int DirectReportsCount { get; set; }
        public int TotalTeamSize { get; set; }
        public int PendingApprovalsCount { get; set; }
    }
}
