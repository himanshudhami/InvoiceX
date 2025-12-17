using Application.DTOs.Leave;
using Application.Interfaces.Leave;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Leave application management endpoints
    /// </summary>
    [ApiController]
    [Route("api/leave-applications")]
    [Produces("application/json")]
    public class LeaveApplicationsController : ControllerBase
    {
        private readonly ILeaveService _leaveService;
        // Hardcoded company ID for now - should come from auth context in production
        private static readonly Guid DefaultCompanyId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        /// <summary>
        /// Initializes a new instance of the LeaveApplicationsController
        /// </summary>
        public LeaveApplicationsController(ILeaveService leaveService)
        {
            _leaveService = leaveService ?? throw new ArgumentNullException(nameof(leaveService));
        }

        /// <summary>
        /// Get all leave applications (admin view)
        /// </summary>
        /// <param name="companyId">Optional company ID</param>
        /// <param name="employeeId">Optional employee ID filter</param>
        /// <param name="leaveTypeId">Optional leave type filter</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="toDate">Optional end date filter</param>
        /// <returns>List of leave applications</returns>
        /// <response code="200">Returns the leave applications</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LeaveApplicationSummaryDto>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? companyId = null,
            [FromQuery] Guid? employeeId = null,
            [FromQuery] Guid? leaveTypeId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;

            // If employee filter is provided, get that employee's applications
            if (employeeId.HasValue)
            {
                var result = await _leaveService.GetEmployeeApplicationsAsync(employeeId.Value, status);
                if (result.IsFailure)
                {
                    return BadRequest(result.Error!.Message);
                }
                return Ok(result.Value);
            }

            // For admin view, get pending approvals (can be expanded later)
            var pendingResult = await _leaveService.GetPendingApprovalsAsync(effectiveCompanyId);
            if (pendingResult.IsFailure)
            {
                return BadRequest(pendingResult.Error!.Message);
            }
            return Ok(pendingResult.Value);
        }

        /// <summary>
        /// Get pending leave applications for approval
        /// </summary>
        /// <param name="companyId">Optional company ID</param>
        /// <returns>List of pending leave applications</returns>
        /// <response code="200">Returns the pending applications</response>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<LeaveApplicationSummaryDto>), 200)]
        public async Task<IActionResult> GetPendingApprovals([FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var result = await _leaveService.GetPendingApprovalsAsync(effectiveCompanyId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get leave applications for an employee
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of leave applications</returns>
        /// <response code="200">Returns the leave applications</response>
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(typeof(IEnumerable<LeaveApplicationSummaryDto>), 200)]
        public async Task<IActionResult> GetEmployeeApplications(Guid employeeId, [FromQuery] string? status = null)
        {
            var result = await _leaveService.GetEmployeeApplicationsAsync(employeeId, status);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get a leave application by ID
        /// </summary>
        /// <param name="id">The application ID</param>
        /// <returns>The leave application</returns>
        /// <response code="200">Returns the leave application</response>
        /// <response code="404">Application not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _leaveService.GetLeaveApplicationAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Apply for leave
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <param name="dto">Leave application data</param>
        /// <param name="companyId">Optional company ID</param>
        /// <returns>The created leave application</returns>
        /// <response code="201">Leave application created successfully</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost("employee/{employeeId}/apply")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ApplyLeave(Guid employeeId, [FromBody] ApplyLeaveDto dto, [FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var result = await _leaveService.ApplyLeaveAsync(employeeId, effectiveCompanyId, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update a pending leave application
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <param name="id">The application ID</param>
        /// <param name="dto">Update data</param>
        /// <returns>The updated leave application</returns>
        /// <response code="200">Leave application updated successfully</response>
        /// <response code="400">Invalid request data or application cannot be updated</response>
        /// <response code="404">Application not found</response>
        [HttpPut("employee/{employeeId}/{id}")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateApplication(Guid employeeId, Guid id, [FromBody] UpdateLeaveApplicationDto dto)
        {
            var result = await _leaveService.UpdateLeaveApplicationAsync(employeeId, id, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Approve a leave application
        /// </summary>
        /// <param name="id">The application ID</param>
        /// <param name="dto">Approval data</param>
        /// <param name="approvedBy">The approver's employee ID</param>
        /// <returns>The approved leave application</returns>
        /// <response code="200">Leave application approved successfully</response>
        /// <response code="400">Application cannot be approved</response>
        /// <response code="404">Application not found</response>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveLeaveDto dto, [FromQuery] Guid approvedBy)
        {
            var result = await _leaveService.ApproveLeaveAsync(id, approvedBy, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Reject a leave application
        /// </summary>
        /// <param name="id">The application ID</param>
        /// <param name="dto">Rejection data with reason</param>
        /// <param name="rejectedBy">The rejecter's employee ID</param>
        /// <returns>The rejected leave application</returns>
        /// <response code="200">Leave application rejected successfully</response>
        /// <response code="400">Application cannot be rejected</response>
        /// <response code="404">Application not found</response>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectLeaveDto dto, [FromQuery] Guid rejectedBy)
        {
            var result = await _leaveService.RejectLeaveAsync(id, rejectedBy, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Cancel an approved leave application
        /// </summary>
        /// <param name="id">The application ID</param>
        /// <param name="dto">Cancellation data</param>
        /// <returns>The cancelled leave application</returns>
        /// <response code="200">Leave application cancelled successfully</response>
        /// <response code="400">Application cannot be cancelled</response>
        /// <response code="404">Application not found</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelLeaveDto dto)
        {
            var result = await _leaveService.CancelLeaveAsync(id, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Withdraw a pending leave application (employee action)
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <param name="id">The application ID</param>
        /// <param name="reason">Optional withdrawal reason</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Leave application withdrawn successfully</response>
        /// <response code="400">Application cannot be withdrawn</response>
        /// <response code="404">Application not found</response>
        [HttpPost("employee/{employeeId}/{id}/withdraw")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Withdraw(Guid employeeId, Guid id, [FromQuery] string? reason = null)
        {
            var result = await _leaveService.WithdrawLeaveAsync(employeeId, id, reason);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Calculate leave days between two dates
        /// </summary>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        /// <param name="companyId">Optional company ID</param>
        /// <returns>Leave calculation details</returns>
        /// <response code="200">Returns the calculation</response>
        /// <response code="400">Invalid date range</response>
        [HttpGet("calculate")]
        [ProducesResponseType(typeof(LeaveCalculationDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CalculateDays([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var result = await _leaveService.CalculateLeaveDaysAsync(effectiveCompanyId, fromDate, toDate);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get leave calendar events for a date range
        /// </summary>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        /// <param name="companyId">Optional company ID</param>
        /// <returns>List of calendar events</returns>
        /// <response code="200">Returns the calendar events</response>
        [HttpGet("calendar")]
        [ProducesResponseType(typeof(IEnumerable<LeaveCalendarEventDto>), 200)]
        public async Task<IActionResult> GetCalendarEvents([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var result = await _leaveService.GetCalendarEventsAsync(effectiveCompanyId, fromDate, toDate);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }
    }
}
