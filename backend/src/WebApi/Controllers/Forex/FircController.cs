using Application.Interfaces.Forex;
using Application.DTOs.Forex;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Forex
{
    /// <summary>
    /// FIRC (Foreign Inward Remittance Certificate) management endpoints for FEMA compliance
    /// </summary>
    [ApiController]
    [Route("api/fircs")]
    [Produces("application/json")]
    public class FircController : ControllerBase
    {
        private readonly IFircReconciliationService _service;

        public FircController(IFircReconciliationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get FIRC by ID
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
        /// Get paginated FIRCs with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPaged([FromQuery] FircFilterRequest request)
        {
            var result = await _service.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.CompanyId,
                request.SearchTerm,
                request.Status,
                request.EdpmsReported);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<object>(items, totalCount, request.PageNumber, request.PageSize));
        }

        /// <summary>
        /// Create a new FIRC entry
        /// </summary>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateFircDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing FIRC
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFircDto dto)
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
        /// Delete a FIRC entry
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

        /// <summary>
        /// Get all FIRCs for a company
        /// </summary>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetByCompany(Guid companyId)
        {
            var result = await _service.GetPagedAsync(1, 100, companyId, null, null, null);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value.Items);
        }

        /// <summary>
        /// Get FIRCs pending reconciliation (unlinked to payments/invoices)
        /// </summary>
        [HttpGet("pending-reconciliation/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPendingReconciliation(Guid companyId)
        {
            var result = await _service.GetUnlinkedAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== FIRC-Specific Endpoints ====================

        /// <summary>
        /// Link FIRC to a payment
        /// </summary>
        [HttpPost("{fircId}/link-payment/{paymentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> LinkToPayment(Guid fircId, Guid paymentId)
        {
            var result = await _service.LinkToPaymentAsync(fircId, paymentId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        /// <summary>
        /// Link FIRC to invoices
        /// </summary>
        [HttpPost("{fircId}/link-invoices")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> LinkToInvoices(Guid fircId, [FromBody] List<FircInvoiceAllocationDto> allocations)
        {
            var result = await _service.LinkToInvoicesAsync(fircId, allocations);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        /// <summary>
        /// Get unlinked FIRCs (not linked to any payment)
        /// </summary>
        [HttpGet("unlinked/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetUnlinked(Guid companyId)
        {
            var result = await _service.GetUnlinkedAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Auto-match FIRCs with payments
        /// </summary>
        [HttpPost("auto-match/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> AutoMatch(Guid companyId, [FromQuery] decimal amountTolerance = 0.01m, [FromQuery] int dateTolerance = 5)
        {
            var result = await _service.AutoMatchFircsAsync(companyId, amountTolerance, dateTolerance);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Mark FIRC as reported to EDPMS
        /// </summary>
        [HttpPost("{fircId}/mark-edpms-reported")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> MarkEdpmsReported(Guid fircId, [FromBody] EdpmsReportRequest request)
        {
            var reportDate = request.ReportDate.HasValue
                ? DateOnly.FromDateTime(request.ReportDate.Value)
                : DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _service.MarkEdpmsReportedAsync(fircId, reportDate, request.Reference);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return NoContent();
        }

        /// <summary>
        /// Get pending EDPMS reporting
        /// </summary>
        [HttpGet("pending-edpms/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPendingEdpmsReporting(Guid companyId)
        {
            var result = await _service.GetPendingEdpmsReportingAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get EDPMS compliance summary
        /// </summary>
        [HttpGet("edpms-summary/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetEdpmsComplianceSummary(Guid companyId)
        {
            var result = await _service.GetEdpmsComplianceSummaryAsync(companyId);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get realization alerts (approaching/past 270-day deadline)
        /// </summary>
        [HttpGet("realization-alerts/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRealizationAlerts(Guid companyId, [FromQuery] int alertDaysBeforeDeadline = 30)
        {
            var result = await _service.GetRealizationAlertsAsync(companyId, alertDaysBeforeDeadline);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get realization summary
        /// </summary>
        [HttpGet("realization-summary/{companyId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRealizationSummary(Guid companyId, [FromQuery] DateTime? asOfDate = null)
        {
            DateOnly? dateOnly = asOfDate.HasValue ? DateOnly.FromDateTime(asOfDate.Value) : null;
            var result = await _service.GetRealizationSummaryAsync(companyId, dateOnly);
            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }
    }

    /// <summary>
    /// Filter request for FIRCs
    /// </summary>
    public class FircFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public Guid? CompanyId { get; set; }
        public string? Status { get; set; }
        public bool? EdpmsReported { get; set; }
    }

    /// <summary>
    /// Request for marking EDPMS reported
    /// </summary>
    public class EdpmsReportRequest
    {
        public DateTime? ReportDate { get; set; }
        public string? Reference { get; set; }
    }
}
