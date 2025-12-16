using Application.Interfaces;
using Application.DTOs.Payments;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.DTOs;
using WebApi.DTOs.Common;

// DTOs from IPaymentsService
using IncomeSummaryDto = Application.Interfaces.IncomeSummaryDto;
using TdsSummaryDto = Application.Interfaces.TdsSummaryDto;

namespace WebApi.Controllers
{
    /// <summary>
    /// Payments management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentsService _service;

        /// <summary>
        /// Initializes a new instance of the PaymentsController
        /// </summary>
        public PaymentsController(IPaymentsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Payments by ID
        /// </summary>
        /// <param name="id">The Payments ID</param>
        /// <returns>The Payments entity</returns>
        /// <response code="200">Returns the Payments entity</response>
        /// <response code="404">Payments not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Payments), 200)]
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
        /// Get all Payments entities
        /// </summary>
        /// <returns>List of Payments entities</returns>
        /// <response code="200">Returns the list of Payments entities</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Payments>), 200)]
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
        /// Get paginated Payments entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of Payments entities</returns>
        /// <response code="200">Returns the paginated list of Payments entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Payments>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] PaymentsFilterRequest request)
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
            var response = new PagedResponse<Payments>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);
            
            return Ok(response);
        }

        /// <summary>
        /// Create a new Payments
        /// </summary>
        /// <param name="dto">The Payments to create</param>
        /// <returns>The created Payments</returns>
        /// <response code="201">Payments created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(Payments), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreatePaymentsDto dto)
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
        /// Update an existing Payments
        /// </summary>
        /// <param name="id">The Payments ID</param>
        /// <param name="dto">The updated Payments data</param>
        /// <returns>No content</returns>
        /// <response code="204">Payments updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Payments not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentsDto dto)
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
        /// Delete a Payments
        /// </summary>
        /// <param name="id">The Payments ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Payments deleted successfully</response>
        /// <response code="404">Payments not found</response>
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
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        // ==================== New Endpoints for Indian Tax Compliance ====================

        /// <summary>
        /// Get payments by invoice ID
        /// </summary>
        /// <param name="invoiceId">The invoice ID</param>
        /// <returns>List of payments for the invoice</returns>
        /// <response code="200">Returns the list of payments</response>
        [HttpGet("by-invoice/{invoiceId}")]
        [ProducesResponseType(typeof(IEnumerable<Payments>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByInvoiceId(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty)
                return BadRequest("Invoice ID cannot be empty");

            var result = await _service.GetByInvoiceIdAsync(invoiceId);

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
        /// Get payments by company ID
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <returns>List of payments for the company</returns>
        /// <response code="200">Returns the list of payments</response>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<Payments>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByCompanyId(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID cannot be empty");

            var result = await _service.GetByCompanyIdAsync(companyId);

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
        /// Get payments by customer ID
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>List of payments from the customer</returns>
        /// <response code="200">Returns the list of payments</response>
        [HttpGet("by-customer/{customerId}")]
        [ProducesResponseType(typeof(IEnumerable<Payments>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByCustomerId(Guid customerId)
        {
            if (customerId == Guid.Empty)
                return BadRequest("Customer ID cannot be empty");

            var result = await _service.GetByCustomerIdAsync(customerId);

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
        /// Get payments by financial year
        /// </summary>
        /// <param name="financialYear">Financial year in format YYYY-YY (e.g., 2024-25)</param>
        /// <param name="companyId">Optional company ID filter</param>
        /// <returns>List of payments for the financial year</returns>
        /// <response code="200">Returns the list of payments</response>
        [HttpGet("by-financial-year/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<Payments>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByFinancialYear(string financialYear, [FromQuery] Guid? companyId = null)
        {
            if (string.IsNullOrWhiteSpace(financialYear))
                return BadRequest("Financial year is required");

            var result = await _service.GetByFinancialYearAsync(financialYear, companyId);

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
        /// Get income summary for financial reports
        /// </summary>
        /// <param name="companyId">Optional company ID filter</param>
        /// <param name="financialYear">Optional financial year filter (e.g., 2024-25)</param>
        /// <param name="year">Optional calendar year filter</param>
        /// <param name="month">Optional month filter (1-12)</param>
        /// <returns>Income summary with gross, TDS, net, and INR totals</returns>
        /// <response code="200">Returns the income summary</response>
        [HttpGet("income-summary")]
        [ProducesResponseType(typeof(IncomeSummaryDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetIncomeSummary(
            [FromQuery] Guid? companyId = null,
            [FromQuery] string? financialYear = null,
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            var result = await _service.GetIncomeSummaryAsync(companyId, financialYear, year, month);

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
        /// Get TDS summary for compliance reporting
        /// Groups payments by customer and TDS section
        /// </summary>
        /// <param name="financialYear">Financial year in format YYYY-YY (required)</param>
        /// <param name="companyId">Optional company ID filter</param>
        /// <returns>TDS summary grouped by customer and section</returns>
        /// <response code="200">Returns the TDS summary</response>
        [HttpGet("tds-summary")]
        [ProducesResponseType(typeof(IEnumerable<TdsSummaryDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetTdsSummary(
            [FromQuery] string financialYear,
            [FromQuery] Guid? companyId = null)
        {
            if (string.IsNullOrWhiteSpace(financialYear))
                return BadRequest("Financial year is required for TDS summary");

            var result = await _service.GetTdsSummaryAsync(companyId, financialYear);

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
    }
}