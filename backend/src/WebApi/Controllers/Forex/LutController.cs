using Application.Interfaces.Forex;
using Application.DTOs.Forex;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Forex
{
    /// <summary>
    /// LUT (Letter of Undertaking) management endpoints for GST Export compliance
    /// </summary>
    [ApiController]
    [Route("api/luts")]
    [Produces("application/json")]
    public class LutController : ControllerBase
    {
        private readonly ILutService _service;

        public LutController(ILutService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get LUT by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.IsFailure
                ? result.Error!.Type == ErrorType.NotFound ? NotFound(result.Error.Message) : BadRequest(result.Error.Message)
                : Ok(result.Value);
        }

        /// <summary>
        /// Get paginated LUTs with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPaged([FromQuery] LutFilterRequest request)
        {
            var result = await _service.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.CompanyId,
                request.FinancialYear,
                request.Status);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<object>(items, totalCount, request.PageNumber, request.PageSize));
        }

        /// <summary>
        /// Create a new LUT entry
        /// </summary>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateLutDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing LUT
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLutDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateAsync(id, dto);
            if (result.IsFailure)
            {
                return result.Error!.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : BadRequest(result.Error.Message);
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a LUT entry
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (result.IsFailure)
            {
                return result.Error!.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : BadRequest(result.Error.Message);
            }

            return NoContent();
        }

        // ==================== LUT-Specific Endpoints ====================

        /// <summary>
        /// Get all LUTs for a company
        /// </summary>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetByCompany(Guid companyId)
        {
            var result = await _service.GetPagedAsync(1, 100, companyId, null, null);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value.Items);
        }

        /// <summary>
        /// Get active LUT for a company (uses current financial year)
        /// </summary>
        [HttpGet("active/{companyId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetActiveCurrent(Guid companyId)
        {
            var currentFy = GetCurrentFinancialYear();
            var result = await _service.GetActiveAsync(companyId, currentFy);
            if (result.IsFailure)
            {
                return result.Error!.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : BadRequest(result.Error.Message);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get active LUT for a company in a specific financial year
        /// </summary>
        [HttpGet("active/{companyId}/{financialYear}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetActive(Guid companyId, string financialYear)
        {
            var result = await _service.GetActiveAsync(companyId, financialYear);
            if (result.IsFailure)
            {
                return result.Error!.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : BadRequest(result.Error.Message);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Validate LUT for a specific date
        /// </summary>
        [HttpGet("validate/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ValidateForDate(Guid companyId, [FromQuery] DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var result = await _service.GetValidForDateAsync(companyId, dateOnly);
            if (result.IsFailure)
                return Ok(new { isValid = false, message = result.Error!.Message });

            return Ok(new { isValid = true, lut = result.Value });
        }

        /// <summary>
        /// Validate LUT for an invoice
        /// </summary>
        [HttpPost("validate-for-invoice")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ValidateForInvoice([FromBody] LutInvoiceValidationRequest request)
        {
            var invoiceDate = DateOnly.FromDateTime(request.InvoiceDate);
            var result = await _service.ValidateLutForInvoiceAsync(request.CompanyId, invoiceDate);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Renew an expiring LUT
        /// </summary>
        [HttpPost("{id}/renew")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Renew(Guid id, [FromBody] CreateLutDto newLutData)
        {
            var result = await _service.RenewLutAsync(id, newLutData);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Cancel a LUT
        /// </summary>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelLutRequest request)
        {
            var result = await _service.CancelLutAsync(id, request.Reason);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        /// <summary>
        /// Get LUT expiry alerts (all companies)
        /// </summary>
        [HttpGet("expiry-alerts")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetExpiryAlerts([FromQuery] int daysBeforeExpiry = 30)
        {
            var result = await _service.GetExpiryAlertsAsync(null, daysBeforeExpiry);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get LUT expiry alerts for a specific company
        /// </summary>
        [HttpGet("expiry-alerts/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetExpiryAlertsForCompany(Guid companyId, [FromQuery] int daysBeforeExpiry = 30)
        {
            var result = await _service.GetExpiryAlertsAsync(companyId, daysBeforeExpiry);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get LUT compliance summary
        /// </summary>
        [HttpGet("compliance-summary/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetComplianceSummary(Guid companyId)
        {
            var result = await _service.GetComplianceSummaryAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get LUT utilization report
        /// </summary>
        [HttpGet("utilization/{companyId}/{financialYear}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetUtilizationReport(Guid companyId, string financialYear)
        {
            var result = await _service.GetUtilizationReportAsync(companyId, financialYear);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Helper Methods ====================

        private static string GetCurrentFinancialYear()
        {
            var today = DateTime.UtcNow;
            var year = today.Month >= 4 ? today.Year : today.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }
    }

    /// <summary>
    /// Filter request for LUTs
    /// </summary>
    public class LutFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? CompanyId { get; set; }
        public string? FinancialYear { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// Request for validating LUT for an invoice
    /// </summary>
    public class LutInvoiceValidationRequest
    {
        public Guid CompanyId { get; set; }
        public DateTime InvoiceDate { get; set; }
    }

    /// <summary>
    /// Request for cancelling a LUT
    /// </summary>
    public class CancelLutRequest
    {
        public string? Reason { get; set; }
    }
}
