using Application.Interfaces;
using Application.DTOs.Dashboard;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebApi.Controllers.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Dashboard endpoints for retrieving aggregated business data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class DashboardController : CompanyAuthorizedController
    {
        private readonly IDashboardService _dashboardService;

        /// <summary>
        /// Initializes a new instance of the DashboardController
        /// </summary>
        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        }

        /// <summary>
        /// Get comprehensive dashboard data including statistics and recent invoices
        /// </summary>
        /// <param name="company">Optional company ID filter (Admin/HR only). Required for Admin/HR users.</param>
        /// <returns>Dashboard data with statistics and recent invoices</returns>
        /// <response code="200">Returns dashboard data successfully</response>
        /// <response code="400">Bad request - company ID required for Admin/HR</response>
        /// <response code="403">Forbidden - company ID not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(DashboardDataDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDashboardData([FromQuery] Guid? company = null)
        {
            // Get effective company ID based on role
            // Admin/HR: uses query param if provided, otherwise their active company_id from JWT
            // Regular users: uses their company_id from JWT
            var effectiveCompanyId = GetEffectiveCompanyId(company);

            // All users need a company context for dashboard
            if (!effectiveCompanyId.HasValue)
                return CompanyIdNotFoundResponse();

            var result = await _dashboardService.GetDashboardDataAsync(effectiveCompanyId.Value);

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
