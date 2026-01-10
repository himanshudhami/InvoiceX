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
    /// GSTR-3B Filing Pack endpoints.
    /// Generates consolidated GSTR-3B data with drill-down to source documents.
    /// Tables: 3.1 (Outward supplies), 4 (ITC), 5 (Exempt supplies).
    /// </summary>
    [ApiController]
    [Route("api/gst/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class Gstr3bController : ControllerBase
    {
        private readonly IGstr3bService _service;

        public Gstr3bController(IGstr3bService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // ==================== Filing Generation ====================

        /// <summary>
        /// Generate GSTR-3B filing pack for a return period
        /// </summary>
        /// <param name="request">Company ID and return period</param>
        /// <returns>Complete GSTR-3B filing with all tables</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(Gstr3bFilingDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Generate([FromBody] GenerateGstr3bRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _service.GenerateFilingPackAsync(
                request.CompanyId,
                request.ReturnPeriod,
                userId,
                request.Regenerate);

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

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Get GSTR-3B filing by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Gstr3bFilingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetFilingByIdAsync(id);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get GSTR-3B filing for a specific period
        /// </summary>
        [HttpGet("{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr3bFilingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByPeriod(Guid companyId, string returnPeriod)
        {
            var result = await _service.GetFilingByPeriodAsync(companyId, returnPeriod);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Individual Tables ====================

        /// <summary>
        /// Build Table 3.1 - Outward supplies (preview without saving)
        /// </summary>
        [HttpGet("table/3.1/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr3bTable31Dto), 200)]
        public async Task<IActionResult> GetTable31(Guid companyId, string returnPeriod)
        {
            var result = await _service.BuildTable31Async(companyId, returnPeriod);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Build Table 4 - ITC (preview without saving)
        /// </summary>
        [HttpGet("table/4/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr3bTable4Dto), 200)]
        public async Task<IActionResult> GetTable4(Guid companyId, string returnPeriod)
        {
            var result = await _service.BuildTable4Async(companyId, returnPeriod);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Build Table 5 - Exempt supplies (preview without saving)
        /// </summary>
        [HttpGet("table/5/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr3bTable5Dto), 200)]
        public async Task<IActionResult> GetTable5(Guid companyId, string returnPeriod)
        {
            var result = await _service.BuildTable5Async(companyId, returnPeriod);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Drill-down ====================

        /// <summary>
        /// Get line items for a filing (optionally filtered by table)
        /// </summary>
        [HttpGet("{filingId:guid}/line-items")]
        [ProducesResponseType(typeof(IEnumerable<Gstr3bLineItemDto>), 200)]
        public async Task<IActionResult> GetLineItems(Guid filingId, [FromQuery] string? tableCode = null)
        {
            var result = await _service.GetLineItemsAsync(filingId, tableCode);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get source documents for a line item (drill-down)
        /// </summary>
        [HttpGet("drilldown/{lineItemId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<Gstr3bSourceDocumentDto>), 200)]
        public async Task<IActionResult> GetSourceDocuments(Guid lineItemId)
        {
            var result = await _service.GetSourceDocumentsAsync(lineItemId);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Variance ====================

        /// <summary>
        /// Get variance compared to previous period
        /// </summary>
        [HttpGet("variance/{companyId:guid}/{returnPeriod}")]
        [ProducesResponseType(typeof(Gstr3bVarianceSummaryDto), 200)]
        public async Task<IActionResult> GetVariance(Guid companyId, string returnPeriod)
        {
            var result = await _service.GetVarianceAsync(companyId, returnPeriod);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        // ==================== Filing Workflow ====================

        /// <summary>
        /// Mark filing as reviewed
        /// </summary>
        [HttpPost("{filingId:guid}/review")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkAsReviewed(Guid filingId, [FromBody] ReviewGstr3bRequestDto? request)
        {
            var userId = GetCurrentUserId();
            var result = await _service.MarkAsReviewedAsync(filingId, userId, request?.Notes);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => StatusCode(500, result.Error.Message)
                };
            }

            return Ok(new { message = "Filing marked as reviewed" });
        }

        /// <summary>
        /// Mark filing as filed (with ARN from GSTN)
        /// </summary>
        [HttpPost("{filingId:guid}/filed")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> MarkAsFiled(Guid filingId, [FromBody] FileGstr3bRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _service.MarkAsFiledAsync(filingId, request.Arn, request.FilingDate, userId);

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

            return Ok(new { message = "Filing marked as filed", arn = request.Arn });
        }

        // ==================== History ====================

        /// <summary>
        /// Get filing history for a company
        /// </summary>
        [HttpGet("history/{companyId:guid}")]
        [ProducesResponseType(typeof(PagedResponse<Gstr3bFilingHistoryDto>), 200)]
        public async Task<IActionResult> GetHistory(
            Guid companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? financialYear = null,
            [FromQuery] string? status = null)
        {
            var result = await _service.GetFilingHistoryAsync(companyId, pageNumber, pageSize, financialYear, status);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            var (items, totalCount) = result.Value;
            var response = new PagedResponse<Gstr3bFilingHistoryDto>(items, totalCount, pageNumber, pageSize);

            return Ok(response);
        }

        // ==================== Export ====================

        /// <summary>
        /// Export filing to JSON (GSTN format)
        /// </summary>
        [HttpGet("{filingId:guid}/export/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ExportJson(Guid filingId)
        {
            var result = await _service.ExportToJsonAsync(filingId);

            if (result.IsFailure)
                return NotFound(result.Error!.Message);

            return Content(result.Value!, "application/json");
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
