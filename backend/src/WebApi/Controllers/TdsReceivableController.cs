using Application.Interfaces;
using Application.DTOs.TdsReceivable;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// TDS Receivable management endpoints - tracks TDS deducted by customers
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TdsReceivableController : ControllerBase
    {
        private readonly ITdsReceivableService _service;

        /// <summary>
        /// Initializes a new instance of the TdsReceivableController
        /// </summary>
        public TdsReceivableController(ITdsReceivableService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get TDS receivable by ID
        /// </summary>
        /// <param name="id">The TDS receivable ID</param>
        /// <returns>The TDS receivable entry</returns>
        /// <response code="200">Returns the TDS receivable</response>
        /// <response code="404">TDS receivable not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TdsReceivable), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all TDS receivables
        /// </summary>
        /// <returns>List of TDS receivables</returns>
        /// <response code="200">Returns the list of TDS receivables</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TdsReceivable>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

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

        /// <summary>
        /// Get paginated TDS receivables with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of TDS receivables</returns>
        /// <response code="200">Returns the paginated list of TDS receivables</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<TdsReceivable>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] TdsReceivableFilterRequest request)
        {
            var result = await _service.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.GetFilters());

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            var (items, totalCount) = result.Value;
            var response = new PagedResponse<TdsReceivable>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new TDS receivable entry
        /// </summary>
        /// <param name="dto">The TDS receivable to create</param>
        /// <returns>The created TDS receivable</returns>
        /// <response code="201">TDS receivable created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(TdsReceivable), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateTdsReceivableDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing TDS receivable entry
        /// </summary>
        /// <param name="id">The TDS receivable ID</param>
        /// <param name="dto">The updated TDS receivable data</param>
        /// <returns>No content</returns>
        /// <response code="204">TDS receivable updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">TDS receivable not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTdsReceivableDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateAsync(id, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a TDS receivable entry
        /// </summary>
        /// <param name="id">The TDS receivable ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">TDS receivable deleted successfully</response>
        /// <response code="404">TDS receivable not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);

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

        // ==================== Specialized Endpoints ====================

        /// <summary>
        /// Get TDS receivables by company and financial year
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="financialYear">Financial year (e.g., '2024-25')</param>
        /// <returns>List of TDS receivables</returns>
        /// <response code="200">Returns the list of TDS receivables</response>
        [HttpGet("by-company/{companyId}/fy/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<TdsReceivable>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByCompanyAndFY(Guid companyId, string financialYear)
        {
            var result = await _service.GetByCompanyAndFYAsync(companyId, financialYear);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get TDS receivables by company, financial year, and quarter
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="financialYear">Financial year (e.g., '2024-25')</param>
        /// <param name="quarter">Quarter (Q1, Q2, Q3, Q4)</param>
        /// <returns>List of TDS receivables</returns>
        /// <response code="200">Returns the list of TDS receivables</response>
        [HttpGet("by-company/{companyId}/fy/{financialYear}/q/{quarter}")]
        [ProducesResponseType(typeof(IEnumerable<TdsReceivable>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByCompanyFYQuarter(Guid companyId, string financialYear, string quarter)
        {
            var result = await _service.GetByCompanyFYQuarterAsync(companyId, financialYear, quarter);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get TDS receivables by customer
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>List of TDS receivables</returns>
        /// <response code="200">Returns the list of TDS receivables</response>
        [HttpGet("by-customer/{customerId}")]
        [ProducesResponseType(typeof(IEnumerable<TdsReceivable>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByCustomer(Guid customerId)
        {
            var result = await _service.GetByCustomerAsync(customerId);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get unmatched TDS entries (not yet matched with Form 26AS)
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="financialYear">Optional financial year filter</param>
        /// <returns>List of unmatched TDS receivables</returns>
        /// <response code="200">Returns the list of unmatched TDS receivables</response>
        [HttpGet("unmatched/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<TdsReceivable>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetUnmatched(Guid companyId, [FromQuery] string? financialYear = null)
        {
            var result = await _service.GetUnmatchedAsync(companyId, financialYear);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get TDS entries by status
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="status">Status (pending, matched, claimed, disputed, written_off)</param>
        /// <returns>List of TDS receivables</returns>
        /// <response code="200">Returns the list of TDS receivables</response>
        [HttpGet("by-status/{companyId}/{status}")]
        [ProducesResponseType(typeof(IEnumerable<TdsReceivable>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByStatus(Guid companyId, string status)
        {
            var result = await _service.GetByStatusAsync(companyId, status);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get TDS summary for a financial year
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="financialYear">Financial year (e.g., '2024-25')</param>
        /// <returns>TDS summary</returns>
        /// <response code="200">Returns the TDS summary</response>
        [HttpGet("summary/{companyId}/{financialYear}")]
        [ProducesResponseType(typeof(TdsSummary), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSummary(Guid companyId, string financialYear)
        {
            var result = await _service.GetSummaryAsync(companyId, financialYear);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Match TDS entry with Form 26AS
        /// </summary>
        /// <param name="id">The TDS receivable ID</param>
        /// <param name="dto">Form 26AS amount</param>
        /// <returns>No content</returns>
        /// <response code="204">TDS entry matched successfully</response>
        /// <response code="404">TDS receivable not found</response>
        [HttpPost("{id}/match-26as")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MatchWith26As(Guid id, [FromBody] Match26AsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.MatchWith26AsAsync(id, dto);

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
        /// Update TDS entry status
        /// </summary>
        /// <param name="id">The TDS receivable ID</param>
        /// <param name="dto">New status data</param>
        /// <returns>No content</returns>
        /// <response code="204">Status updated successfully</response>
        /// <response code="404">TDS receivable not found</response>
        [HttpPost("{id}/update-status")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateStatusAsync(id, dto);

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
    }

}

namespace WebApi.DTOs
{
    /// <summary>
    /// Filter request for TDS receivables
    /// </summary>
    public class TdsReceivableFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;

        /// <summary>
        /// Filter by company ID
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Filter by financial year
        /// </summary>
        public string? FinancialYear { get; set; }

        /// <summary>
        /// Filter by quarter
        /// </summary>
        public string? Quarter { get; set; }

        /// <summary>
        /// Filter by status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by matched status
        /// </summary>
        public bool? MatchedWith26As { get; set; }

        /// <summary>
        /// Get filters dictionary
        /// </summary>
        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();
            if (CompanyId.HasValue)
                filters["company_id"] = CompanyId.Value;
            if (!string.IsNullOrWhiteSpace(FinancialYear))
                filters["financial_year"] = FinancialYear;
            if (!string.IsNullOrWhiteSpace(Quarter))
                filters["quarter"] = Quarter;
            if (!string.IsNullOrWhiteSpace(Status))
                filters["status"] = Status;
            if (MatchedWith26As.HasValue)
                filters["matched_with_26as"] = MatchedWith26As.Value;
            return filters;
        }
    }
}
