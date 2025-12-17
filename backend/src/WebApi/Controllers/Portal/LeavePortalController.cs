using Application.DTOs.Leave;
using Application.Interfaces.Leave;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Portal
{
    /// <summary>
    /// Employee Portal Leave Management API endpoints.
    /// All endpoints are scoped to the authenticated employee.
    /// </summary>
    [ApiController]
    [Route("api/portal/leave")]
    [Produces("application/json")]
    [Authorize]
    public class LeavePortalController : ControllerBase
    {
        private readonly ILeaveService _leaveService;

        public LeavePortalController(ILeaveService leaveService)
        {
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

        // ==================== Dashboard ====================

        /// <summary>
        /// Get leave dashboard for the employee
        /// </summary>
        /// <returns>Leave dashboard with balances, upcoming leaves, and holidays</returns>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(LeaveDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetDashboard()
        {
            if (CurrentEmployeeId == null || CurrentCompanyId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.GetEmployeeLeaveDashboardAsync(CurrentEmployeeId.Value, CurrentCompanyId.Value);
            return HandleResult(result);
        }

        // ==================== Leave Balances ====================

        /// <summary>
        /// Get employee's leave balances for current financial year
        /// </summary>
        /// <param name="financialYear">Optional financial year (e.g., "2024-25")</param>
        /// <returns>List of leave balances by type</returns>
        [HttpGet("balances")]
        [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetBalances([FromQuery] string? financialYear = null)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.GetEmployeeBalancesAsync(CurrentEmployeeId.Value, financialYear);
            return HandleResult(result);
        }

        // ==================== Leave Types ====================

        /// <summary>
        /// Get available leave types for the company
        /// </summary>
        /// <returns>List of active leave types</returns>
        [HttpGet("types")]
        [ProducesResponseType(typeof(IEnumerable<LeaveTypeDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetLeaveTypes()
        {
            if (CurrentCompanyId == null)
                return StatusCode(403, new { error = "Company not found" });

            var result = await _leaveService.GetLeaveTypesAsync(CurrentCompanyId.Value, activeOnly: true);
            return HandleResult(result);
        }

        // ==================== Leave Applications ====================

        /// <summary>
        /// Get employee's leave applications
        /// </summary>
        /// <param name="status">Optional filter by status (pending, approved, rejected, cancelled, withdrawn)</param>
        /// <returns>List of leave applications</returns>
        [HttpGet("applications")]
        [ProducesResponseType(typeof(IEnumerable<LeaveApplicationSummaryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetApplications([FromQuery] string? status = null)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.GetEmployeeApplicationsAsync(CurrentEmployeeId.Value, status);
            return HandleResult(result);
        }

        /// <summary>
        /// Get leave application detail by ID
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <returns>Leave application detail</returns>
        [HttpGet("applications/{id}")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetApplication(Guid id)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.GetLeaveApplicationAsync(id);
            if (result.IsSuccess && result.Value!.EmployeeId != CurrentEmployeeId)
                return StatusCode(403, new { error = "Access denied to this leave application" });

            return HandleResult(result);
        }

        /// <summary>
        /// Apply for leave
        /// </summary>
        /// <param name="dto">Leave application data</param>
        /// <returns>Created leave application</returns>
        [HttpPost("apply")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveDto dto)
        {
            if (CurrentEmployeeId == null || CurrentCompanyId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.ApplyLeaveAsync(CurrentEmployeeId.Value, CurrentCompanyId.Value, dto);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetApplication), new { id = result.Value!.Id }, result.Value);

            return HandleResult(result);
        }

        /// <summary>
        /// Update pending leave application
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <param name="dto">Updated application data</param>
        /// <returns>Updated leave application</returns>
        [HttpPut("applications/{id}")]
        [ProducesResponseType(typeof(LeaveApplicationDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateApplication(Guid id, [FromBody] UpdateLeaveApplicationDto dto)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.UpdateLeaveApplicationAsync(CurrentEmployeeId.Value, id, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Withdraw pending leave application
        /// </summary>
        /// <param name="id">Application ID</param>
        /// <param name="dto">Optional cancellation reason</param>
        /// <returns>Success status</returns>
        [HttpPost("applications/{id}/withdraw")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> WithdrawApplication(Guid id, [FromBody] CancelLeaveDto? dto = null)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _leaveService.WithdrawLeaveAsync(CurrentEmployeeId.Value, id, dto?.Reason);
            return HandleResult(result);
        }

        // ==================== Holidays ====================

        /// <summary>
        /// Get company holidays for a year
        /// </summary>
        /// <param name="year">Calendar year (defaults to current year)</param>
        /// <returns>List of holidays</returns>
        [HttpGet("holidays")]
        [ProducesResponseType(typeof(IEnumerable<HolidayDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetHolidays([FromQuery] int? year = null)
        {
            if (CurrentCompanyId == null)
                return StatusCode(403, new { error = "Company not found" });

            var result = await _leaveService.GetHolidaysAsync(CurrentCompanyId.Value, year ?? DateTime.UtcNow.Year);
            return HandleResult(result);
        }

        // ==================== Calendar ====================

        /// <summary>
        /// Get leave calendar events for a date range
        /// </summary>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        /// <returns>List of calendar events (leaves and holidays)</returns>
        [HttpGet("calendar")]
        [ProducesResponseType(typeof(IEnumerable<LeaveCalendarEventDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCalendar([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            if (CurrentCompanyId == null)
                return StatusCode(403, new { error = "Company not found" });

            var result = await _leaveService.GetCalendarEventsAsync(CurrentCompanyId.Value, fromDate, toDate);
            return HandleResult(result);
        }

        // ==================== Utilities ====================

        /// <summary>
        /// Calculate leave days between two dates
        /// </summary>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        /// <returns>Leave calculation result</returns>
        [HttpGet("calculate")]
        [ProducesResponseType(typeof(LeaveCalculationDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CalculateLeaveDays([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            if (CurrentCompanyId == null)
                return StatusCode(403, new { error = "Company not found" });

            var result = await _leaveService.CalculateLeaveDaysAsync(CurrentCompanyId.Value, fromDate, toDate);
            return HandleResult(result);
        }
    }
}
