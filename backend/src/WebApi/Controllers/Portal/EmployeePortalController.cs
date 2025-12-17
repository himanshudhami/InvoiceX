using Application.DTOs.Portal;
using Application.Interfaces;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Portal
{
    /// <summary>
    /// Employee Portal API endpoints.
    /// All endpoints are scoped to the authenticated employee and require the EmployeeOnly policy.
    /// </summary>
    [ApiController]
    [Route("api/portal")]
    [Produces("application/json")]
    [Authorize]
    public class EmployeePortalController : ControllerBase
    {
        private readonly IEmployeePortalService _portalService;

        public EmployeePortalController(IEmployeePortalService portalService)
        {
            _portalService = portalService ?? throw new ArgumentNullException(nameof(portalService));
        }

        /// <summary>
        /// Get the employee ID from the JWT claims
        /// </summary>
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

        /// <summary>
        /// Handle Result and return appropriate HTTP response
        /// </summary>
        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Value);

            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
                ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
                ErrorType.Forbidden => StatusCode(403, new { error = result.Error.Message }),
                ErrorType.Unauthorized => Unauthorized(new { error = result.Error.Message }),
                _ => StatusCode(500, new { error = result.Error.Message })
            };
        }

        // ==================== Dashboard ====================

        /// <summary>
        /// Get employee portal dashboard data
        /// </summary>
        /// <returns>Dashboard data including profile, latest payslip, assets count, etc.</returns>
        /// <response code="200">Returns dashboard data</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(PortalDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetDashboard()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetDashboardAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        // ==================== Profile ====================

        /// <summary>
        /// Get the employee's own profile
        /// </summary>
        /// <returns>Employee profile with masked sensitive data</returns>
        /// <response code="200">Returns employee profile</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(EmployeeProfileDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMyProfile()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetMyProfileAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        // ==================== Payslips ====================

        /// <summary>
        /// Get all payslips for the employee
        /// </summary>
        /// <param name="year">Optional filter by year</param>
        /// <returns>List of payslip summaries</returns>
        /// <response code="200">Returns list of payslips</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        [HttpGet("payslips")]
        [ProducesResponseType(typeof(IEnumerable<PayslipSummaryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMyPayslips([FromQuery] int? year = null)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetMyPayslipsAsync(CurrentEmployeeId.Value, year);
            return HandleResult(result);
        }

        /// <summary>
        /// Get payslip detail by ID
        /// </summary>
        /// <param name="id">Payslip ID</param>
        /// <returns>Detailed payslip information</returns>
        /// <response code="200">Returns payslip detail</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - payslip belongs to another employee</response>
        /// <response code="404">Payslip not found</response>
        [HttpGet("payslips/{id}")]
        [ProducesResponseType(typeof(PayslipDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPayslipDetail(Guid id)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetPayslipDetailAsync(CurrentEmployeeId.Value, id);
            return HandleResult(result);
        }

        /// <summary>
        /// Get payslip by month and year
        /// </summary>
        /// <param name="month">Month (1-12)</param>
        /// <param name="year">Year (e.g., 2024)</param>
        /// <returns>Detailed payslip information</returns>
        /// <response code="200">Returns payslip detail</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        /// <response code="404">Payslip not found for the specified month/year</response>
        [HttpGet("payslips/{year:int}/{month:int}")]
        [ProducesResponseType(typeof(PayslipDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPayslipByMonth(int year, int month)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetPayslipByMonthAsync(CurrentEmployeeId.Value, month, year);
            return HandleResult(result);
        }

        // ==================== Assets ====================

        /// <summary>
        /// Get all assets currently assigned to the employee
        /// </summary>
        /// <returns>List of assigned assets</returns>
        /// <response code="200">Returns list of assigned assets</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        [HttpGet("assets")]
        [ProducesResponseType(typeof(IEnumerable<MyAssetDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMyAssets()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetMyAssetsAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        /// <summary>
        /// Get asset assignment history for the employee
        /// </summary>
        /// <returns>List of all asset assignments (including returned)</returns>
        /// <response code="200">Returns asset assignment history</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        [HttpGet("assets/history")]
        [ProducesResponseType(typeof(IEnumerable<MyAssetDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMyAssetHistory()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetMyAssetHistoryAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        // ==================== Subscriptions ====================

        /// <summary>
        /// Get all subscriptions assigned to the employee
        /// </summary>
        /// <returns>List of subscription assignments</returns>
        /// <response code="200">Returns list of subscriptions</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        [HttpGet("subscriptions")]
        [ProducesResponseType(typeof(IEnumerable<MySubscriptionDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMySubscriptions()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetMySubscriptionsAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        // ==================== Tax Declarations ====================

        /// <summary>
        /// Get all tax declarations for the employee
        /// </summary>
        /// <returns>List of tax declaration summaries</returns>
        /// <response code="200">Returns list of tax declarations</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        [HttpGet("tax-declarations")]
        [ProducesResponseType(typeof(IEnumerable<TaxDeclarationSummaryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMyTaxDeclarations()
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetMyTaxDeclarationsAsync(CurrentEmployeeId.Value);
            return HandleResult(result);
        }

        /// <summary>
        /// Get tax declaration detail by ID
        /// </summary>
        /// <param name="id">Tax declaration ID</param>
        /// <returns>Detailed tax declaration information</returns>
        /// <response code="200">Returns tax declaration detail</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - declaration belongs to another employee</response>
        /// <response code="404">Tax declaration not found</response>
        [HttpGet("tax-declarations/{id}")]
        [ProducesResponseType(typeof(TaxDeclarationDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTaxDeclarationDetail(Guid id)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetTaxDeclarationDetailAsync(CurrentEmployeeId.Value, id);
            return HandleResult(result);
        }

        /// <summary>
        /// Get tax declaration by financial year
        /// </summary>
        /// <param name="financialYear">Financial year (e.g., "2024-25")</param>
        /// <returns>Detailed tax declaration information</returns>
        /// <response code="200">Returns tax declaration detail</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - user is not linked to an employee</response>
        /// <response code="404">Tax declaration not found for the specified year</response>
        [HttpGet("tax-declarations/year/{financialYear}")]
        [ProducesResponseType(typeof(TaxDeclarationDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTaxDeclarationByYear(string financialYear)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.GetTaxDeclarationByYearAsync(CurrentEmployeeId.Value, financialYear);
            return HandleResult(result);
        }

        /// <summary>
        /// Update tax declaration (only if status is draft or rejected)
        /// </summary>
        /// <param name="id">Tax declaration ID</param>
        /// <param name="dto">Updated tax declaration data</param>
        /// <returns>Updated tax declaration detail</returns>
        /// <response code="200">Returns updated tax declaration</response>
        /// <response code="400">Invalid request or declaration cannot be updated</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - declaration belongs to another employee</response>
        /// <response code="404">Tax declaration not found</response>
        [HttpPut("tax-declarations/{id}")]
        [ProducesResponseType(typeof(TaxDeclarationDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateTaxDeclaration(Guid id, [FromBody] UpdateTaxDeclarationDto dto)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.UpdateTaxDeclarationAsync(CurrentEmployeeId.Value, id, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Submit tax declaration for verification
        /// </summary>
        /// <param name="id">Tax declaration ID</param>
        /// <returns>Updated tax declaration with submitted status</returns>
        /// <response code="200">Returns submitted tax declaration</response>
        /// <response code="400">Declaration cannot be submitted (not in draft status)</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - declaration belongs to another employee</response>
        /// <response code="404">Tax declaration not found</response>
        [HttpPost("tax-declarations/{id}/submit")]
        [ProducesResponseType(typeof(TaxDeclarationDetailDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SubmitTaxDeclaration(Guid id)
        {
            if (CurrentEmployeeId == null)
                return StatusCode(403, new { error = "Your account is not linked to an employee record" });

            var result = await _portalService.SubmitTaxDeclarationAsync(CurrentEmployeeId.Value, id);
            return HandleResult(result);
        }
    }
}
