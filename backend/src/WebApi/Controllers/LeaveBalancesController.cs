using Application.DTOs.Leave;
using Application.Interfaces.Leave;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Employee leave balance management endpoints
    /// </summary>
    [ApiController]
    [Route("api/leave-balances")]
    [Produces("application/json")]
    public class LeaveBalancesController : ControllerBase
    {
        private readonly ILeaveService _leaveService;
        // Hardcoded company ID for now - should come from auth context in production
        private static readonly Guid DefaultCompanyId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        /// <summary>
        /// Initializes a new instance of the LeaveBalancesController
        /// </summary>
        public LeaveBalancesController(ILeaveService leaveService)
        {
            _leaveService = leaveService ?? throw new ArgumentNullException(nameof(leaveService));
        }

        /// <summary>
        /// Get all leave balances (admin view)
        /// </summary>
        /// <param name="companyId">Optional company ID</param>
        /// <param name="employeeId">Optional employee ID filter</param>
        /// <param name="leaveTypeId">Optional leave type filter</param>
        /// <param name="financialYear">Optional financial year (defaults to current)</param>
        /// <returns>List of leave balances</returns>
        /// <response code="200">Returns the leave balances</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? companyId = null,
            [FromQuery] Guid? employeeId = null,
            [FromQuery] Guid? leaveTypeId = null,
            [FromQuery] string? financialYear = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var effectiveYear = financialYear ?? GetCurrentFinancialYear();

            // If employeeId is provided, return that employee's balances
            if (employeeId.HasValue)
            {
                var result = await _leaveService.GetEmployeeBalancesAsync(employeeId.Value, effectiveYear);
                if (result.IsFailure)
                {
                    return BadRequest(result.Error!.Message);
                }
                return Ok(result.Value);
            }

            // Return all company balances for admin view
            var companyResult = await _leaveService.GetCompanyBalancesAsync(effectiveCompanyId, effectiveYear);
            if (companyResult.IsFailure)
            {
                return BadRequest(companyResult.Error!.Message);
            }
            return Ok(companyResult.Value);
        }

        private static string GetCurrentFinancialYear()
        {
            var now = DateTime.UtcNow;
            return now.Month >= 4
                ? $"{now.Year}-{(now.Year + 1) % 100:D2}"
                : $"{now.Year - 1}-{now.Year % 100:D2}";
        }

        /// <summary>
        /// Get leave balances for an employee
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <param name="financialYear">Optional financial year (defaults to current)</param>
        /// <returns>List of leave balances</returns>
        /// <response code="200">Returns the leave balances</response>
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), 200)]
        public async Task<IActionResult> GetEmployeeBalances(Guid employeeId, [FromQuery] string? financialYear = null)
        {
            var result = await _leaveService.GetEmployeeBalancesAsync(employeeId, financialYear);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Initialize leave balances for an employee for a financial year
        /// </summary>
        /// <param name="dto">Initialization request</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Balances initialized successfully</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost("initialize")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> InitializeBalances([FromBody] InitializeBalancesRequest dto)
        {
            var effectiveCompanyId = dto.CompanyId ?? DefaultCompanyId;
            var result = await _leaveService.InitializeEmployeeBalancesAsync(dto.EmployeeId, effectiveCompanyId, dto.FinancialYear);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Adjust leave balance for an employee
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <param name="dto">Adjustment data</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Balance adjusted successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Balance not found</response>
        [HttpPost("employee/{employeeId}/adjust")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AdjustBalance(Guid employeeId, [FromBody] AdjustLeaveBalanceDto dto)
        {
            var result = await _leaveService.AdjustBalanceAsync(employeeId, dto);

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

            return NoContent();
        }

        /// <summary>
        /// Carry forward balances from one year to another for an employee
        /// </summary>
        /// <param name="dto">Carry forward request</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Balances carried forward successfully</response>
        /// <response code="400">Invalid request data</response>
        [HttpPost("carry-forward")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CarryForwardBalances([FromBody] CarryForwardRequest dto)
        {
            var result = await _leaveService.CarryForwardBalancesAsync(dto.EmployeeId, dto.FromYear, dto.ToYear);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }
    }

    /// <summary>
    /// Request to initialize leave balances
    /// </summary>
    public class InitializeBalancesRequest
    {
        public Guid EmployeeId { get; set; }
        public Guid? CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to carry forward balances
    /// </summary>
    public class CarryForwardRequest
    {
        public Guid EmployeeId { get; set; }
        public string FromYear { get; set; } = string.Empty;
        public string ToYear { get; set; } = string.Empty;
    }
}
