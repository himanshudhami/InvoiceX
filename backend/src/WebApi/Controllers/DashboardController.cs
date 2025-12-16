using Application.Interfaces;
using Application.DTOs.Dashboard;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    /// <summary>
    /// Dashboard endpoints for retrieving aggregated business data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
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
        /// <returns>Dashboard data with statistics and recent invoices</returns>
        /// <response code="200">Returns dashboard data successfully</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(DashboardDataDto), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDashboardData()
        {
            var result = await _dashboardService.GetDashboardDataAsync();
            
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