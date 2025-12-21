using Application.Interfaces.Reports;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Reports
{
    /// <summary>
    /// Export reporting endpoints - Dashboard, Receivables Ageing, Forex, FEMA Compliance
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ExportReportingController : ControllerBase
    {
        private readonly IExportReportingService _service;

        public ExportReportingController(IExportReportingService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // ==================== Dashboard ====================

        /// <summary>
        /// Get comprehensive export dashboard with KPIs
        /// </summary>
        [HttpGet("dashboard/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetDashboard(Guid companyId)
        {
            var result = await _service.GetExportDashboardAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Receivables Ageing ====================

        /// <summary>
        /// Get export receivables ageing report
        /// </summary>
        [HttpGet("receivables-ageing/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetReceivablesAgeing(Guid companyId, [FromQuery] DateTime? asOfDate = null)
        {
            DateOnly? dateOnly = asOfDate.HasValue ? DateOnly.FromDateTime(asOfDate.Value) : null;
            var result = await _service.GetReceivablesAgeingAsync(companyId, dateOnly);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get customer-wise export receivables
        /// </summary>
        [HttpGet("customer-receivables/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCustomerWiseReceivables(Guid companyId, [FromQuery] DateTime? asOfDate = null)
        {
            DateOnly? dateOnly = asOfDate.HasValue ? DateOnly.FromDateTime(asOfDate.Value) : null;
            var result = await _service.GetCustomerWiseReceivablesAsync(companyId, dateOnly);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Forex Reports ====================

        /// <summary>
        /// Get forex gain/loss report for a period
        /// </summary>
        [HttpGet("forex-gain-loss/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetForexGainLossReport(
            Guid companyId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            var fromDateOnly = DateOnly.FromDateTime(fromDate);
            var toDateOnly = DateOnly.FromDateTime(toDate);
            var result = await _service.GetForexGainLossReportAsync(companyId, fromDateOnly, toDateOnly);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get unrealized forex position (current MTM)
        /// </summary>
        [HttpGet("unrealized-forex/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetUnrealizedForexPosition(
            Guid companyId,
            [FromQuery] DateTime? asOfDate = null,
            [FromQuery] decimal currentExchangeRate = 83.0m)
        {
            var dateOnly = asOfDate.HasValue ? DateOnly.FromDateTime(asOfDate.Value) : DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _service.GetUnrealizedForexPositionAsync(companyId, dateOnly, currentExchangeRate);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== FEMA Compliance ====================

        /// <summary>
        /// Get FEMA compliance dashboard
        /// </summary>
        [HttpGet("fema-dashboard/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetFemaComplianceDashboard(Guid companyId)
        {
            var result = await _service.GetFemaComplianceDashboardAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get FEMA violation alerts
        /// </summary>
        [HttpGet("fema-violations/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetFemaViolationAlerts(Guid companyId)
        {
            var result = await _service.GetFemaViolationAlertsAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Export Realization ====================

        /// <summary>
        /// Get export realization report
        /// </summary>
        [HttpGet("realization/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRealizationReport(
            Guid companyId,
            [FromQuery] string? financialYear = null)
        {
            var result = await _service.GetExportRealizationReportAsync(companyId, financialYear);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get realization trend (monthly)
        /// </summary>
        [HttpGet("realization-trend/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRealizationTrend(
            Guid companyId,
            [FromQuery] int months = 12)
        {
            var result = await _service.GetRealizationTrendAsync(companyId, months);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }
    }
}
