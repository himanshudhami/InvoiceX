using Application.DTOs.Gst;
using Application.Interfaces.Gst;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Gst
{
    /// <summary>
    /// GSTR-2B Ingestion and Reconciliation endpoints.
    /// Handles import of GSTR-2B JSON data and reconciliation with vendor invoices.
    /// Supports accept/reject workflow for mismatches.
    /// </summary>
    [ApiController]
    [Route("api/gst/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class Gstr2bController : ControllerBase
    {
        private readonly IGstr2bService _service;

        public Gstr2bController(IGstr2bService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // ==================== Import ====================

        /// <summary>
        /// Import GSTR-2B JSON data
        /// </summary>
        /// <param name="request">Company ID, return period, and JSON data</param>
        /// <returns>Import record with processing status</returns>
        [HttpPost("import")]
        [ProducesResponseType(typeof(Gstr2bImportDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Import([FromBody] ImportGstr2bRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _service.ImportGstr2bAsync(
                request.CompanyId,
                request.ReturnPeriod,
                request.JsonData,
                request.FileName,
                userId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetImportById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Get import by ID
        /// </summary>
        [HttpGet("import/{id:guid}")]
        [ProducesResponseType(typeof(Gstr2bImportDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetImportById(Guid id)
        {
            var result = await _service.GetImportByIdAsync(id);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get import for a specific period
        /// </summary>
        [HttpGet("import/period/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr2bImportDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetImportByPeriod(Guid companyId, string returnPeriod)
        {
            var result = await _service.GetImportByPeriodAsync(companyId, returnPeriod);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get imports for a company (paged)
        /// </summary>
        [HttpGet("imports/{companyId:guid}")]
        [ProducesResponseType(typeof(PagedResponse<Gstr2bImportDto>), 200)]
        public async Task<IActionResult> GetImports(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? status = null)
        {
            var result = await _service.GetImportsAsync(companyId, pageNumber, pageSize, status);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            var response = new PagedResponse<Gstr2bImportDto>(items, totalCount, pageNumber, pageSize);

            return Ok(response);
        }

        /// <summary>
        /// Delete an import and all its invoices
        /// </summary>
        [HttpDelete("imports/{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteImport(Guid id)
        {
            var result = await _service.DeleteImportAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return NoContent();
        }

        // ==================== Reconciliation ====================

        /// <summary>
        /// Run reconciliation on an import
        /// </summary>
        [HttpPost("reconcile")]
        [ProducesResponseType(typeof(Gstr2bReconciliationSummaryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RunReconciliation([FromBody] RunReconciliationRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.RunReconciliationAsync(request.ImportId, request.Force);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get reconciliation summary for a period
        /// </summary>
        [HttpGet("reconciliation-summary/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr2bReconciliationSummaryDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReconciliationSummary(Guid companyId, string returnPeriod)
        {
            var result = await _service.GetReconciliationSummaryAsync(companyId, returnPeriod);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get supplier-wise summary for a period
        /// </summary>
        [HttpGet("supplier-summary/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(IEnumerable<Gstr2bSupplierSummaryDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSupplierSummary(Guid companyId, string returnPeriod)
        {
            var result = await _service.GetSupplierSummaryAsync(companyId, returnPeriod);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get ITC comparison (GSTR-2B vs Books)
        /// </summary>
        [HttpGet("itc-comparison/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr2bItcComparisonDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetItcComparison(Guid companyId, string returnPeriod)
        {
            var result = await _service.GetItcComparisonAsync(companyId, returnPeriod);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Invoices ====================

        /// <summary>
        /// Get invoices for an import (paged)
        /// </summary>
        [HttpGet("invoices/{importId:guid}")]
        [ProducesResponseType(typeof(PagedResponse<Gstr2bInvoiceListItemDto>), 200)]
        public async Task<IActionResult> GetInvoices(
            Guid importId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? matchStatus = null,
            [FromQuery] string? invoiceType = null,
            [FromQuery] string? searchTerm = null)
        {
            var result = await _service.GetInvoicesAsync(
                importId, pageNumber, pageSize, matchStatus, invoiceType, searchTerm);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            var response = new PagedResponse<Gstr2bInvoiceListItemDto>(items, totalCount, pageNumber, pageSize);

            return Ok(response);
        }

        /// <summary>
        /// Get invoice details by ID
        /// </summary>
        [HttpGet("invoice/{id:guid}")]
        [ProducesResponseType(typeof(Gstr2bInvoiceDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetInvoiceById(Guid id)
        {
            var result = await _service.GetInvoiceByIdAsync(id);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get unmatched invoices for a period
        /// </summary>
        [HttpGet("mismatches/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(IEnumerable<Gstr2bInvoiceListItemDto>), 200)]
        public async Task<IActionResult> GetUnmatchedInvoices(Guid companyId, string returnPeriod)
        {
            var result = await _service.GetUnmatchedInvoicesAsync(companyId, returnPeriod);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Actions ====================

        /// <summary>
        /// Accept a mismatch (user confirms the GSTR-2B data)
        /// </summary>
        [HttpPost("accept")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AcceptMismatch([FromBody] AcceptMismatchRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _service.AcceptMismatchAsync(request.InvoiceId, userId, request.Notes);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(new { message = "Mismatch accepted" });
        }

        /// <summary>
        /// Reject an invoice (user marks as invalid/not for ITC)
        /// </summary>
        [HttpPost("reject")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RejectInvoice([FromBody] RejectInvoiceRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _service.RejectInvoiceAsync(request.InvoiceId, userId, request.Reason);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(new { message = "Invoice rejected" });
        }

        /// <summary>
        /// Manually match a GSTR-2B invoice to a vendor invoice
        /// </summary>
        [HttpPost("manual-match")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ManualMatch([FromBody] ManualMatchRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _service.ManualMatchAsync(
                request.Gstr2bInvoiceId,
                request.VendorInvoiceId,
                userId,
                request.Notes);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(new { message = "Manual match created" });
        }

        /// <summary>
        /// Reset action (undo accept/reject)
        /// </summary>
        [HttpPost("reset/{invoiceId:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetAction(Guid invoiceId)
        {
            var result = await _service.ResetActionAsync(invoiceId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(new { message = "Action reset" });
        }

        // ==================== Helpers ====================

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
