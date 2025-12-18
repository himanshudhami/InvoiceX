using Application.Interfaces;
using Application.DTOs.Quotes;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Controllers.Common;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Quotes management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class QuotesController : CompanyAuthorizedController
    {
        private readonly IQuotesService _service;

        /// <summary>
        /// Initializes a new instance of the QuotesController
        /// </summary>
        public QuotesController(IQuotesService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Quote by ID
        /// </summary>
        /// <param name="id">The Quote ID</param>
        /// <returns>The Quote entity</returns>
        /// <response code="200">Returns the Quote entity</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Quotes), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

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

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Quote entities for current company (or all for Admin/HR)
        /// </summary>
        /// <param name="company">Optional company ID filter (Admin/HR only)</param>
        /// <returns>List of Quote entities</returns>
        /// <response code="200">Returns the list of Quote entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Quotes>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? company = null)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = new Dictionary<string, object>();
            var effectiveCompanyId = GetEffectiveCompanyId(company);
            if (effectiveCompanyId.HasValue)
            {
                filters["company_id"] = effectiveCompanyId.Value;
            }

            var result = await _service.GetPagedAsync(1, 100, null, null, false, filters);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value.Items);
        }

        /// <summary>
        /// Get paginated Quote entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <param name="company">Optional company ID filter (Admin/HR only)</param>
        /// <returns>Paginated list of Quote entities</returns>
        /// <response code="200">Returns the paginated list of Quote entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Quotes>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] QuotesFilterRequest request, [FromQuery] Guid? company = null)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();

            // Apply company filter based on role
            var effectiveCompanyId = GetEffectiveCompanyId(company);
            if (effectiveCompanyId.HasValue)
            {
                filters["company_id"] = effectiveCompanyId.Value;
            }
            else
            {
                // Remove any company_id filter for Admin/HR viewing all
                filters.Remove("company_id");
            }

            var result = await _service.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                filters);

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
            var response = new PagedResponse<Quotes>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new Quote
        /// </summary>
        /// <param name="dto">The Quote to create</param>
        /// <returns>The created Quote</returns>
        /// <response code="201">Quote created successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - cannot create quote for different company</response>
        [HttpPost]
        [ProducesResponseType(typeof(Quotes), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create([FromBody] CreateQuotesDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Admin/HR can create for any company (must specify company_id)
            // Regular users can only create for their own company
            if (IsAdminOrHR)
            {
                // Admin/HR must specify company_id when creating
                if (!dto.CompanyId.HasValue)
                    return BadRequest(new { error = "Company ID is required" });
            }
            else
            {
                // Non-Admin/HR users require company ID in token
                if (CurrentCompanyId == null)
                    return CompanyIdNotFoundResponse();

                // Enforce company isolation - ensure quote is created for current user's company
                if (dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId.Value)
                    return CannotModifyDifferentCompanyResponse("create quote for");

                // Set company_id from token if not provided
                if (!dto.CompanyId.HasValue)
                {
                    dto.CompanyId = CurrentCompanyId.Value;
                }
            }

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
        /// Update an existing Quote
        /// </summary>
        /// <param name="id">The Quote ID</param>
        /// <param name="dto">The Quote data to update</param>
        /// <returns>No content</returns>
        /// <response code="204">Quote updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuotesDto dto)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate quote exists and user has access
            var quoteResult = await _service.GetByIdAsync(id);
            if (quoteResult.IsFailure)
            {
                return quoteResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(quoteResult.Error.Message),
                    _ => BadRequest(quoteResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(quoteResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            // Non-Admin/HR cannot change company_id
            if (!IsAdminOrHR && dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId!.Value)
                return CannotModifyDifferentCompanyResponse("change quote's company");

            dto.Id = id;
            var result = await _service.UpdateAsync(id, dto);

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
        /// Delete a Quote by ID
        /// </summary>
        /// <param name="id">The Quote ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Quote deleted successfully</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate quote exists and user has access
            var quoteResult = await _service.GetByIdAsync(id);
            if (quoteResult.IsFailure)
            {
                return quoteResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(quoteResult.Error.Message),
                    _ => BadRequest(quoteResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(quoteResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            var result = await _service.DeleteAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Duplicate an existing quote
        /// </summary>
        /// <param name="id">The Quote ID to duplicate</param>
        /// <returns>The duplicated Quote</returns>
        /// <response code="201">Quote duplicated successfully</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpPost("{id}/duplicate")]
        [ProducesResponseType(typeof(Quotes), 201)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Duplicate(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate quote exists and user has access
            var quoteResult = await _service.GetByIdAsync(id);
            if (quoteResult.IsFailure)
            {
                return quoteResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(quoteResult.Error.Message),
                    _ => BadRequest(quoteResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(quoteResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            var result = await _service.DuplicateAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Send quote to customer
        /// </summary>
        /// <param name="id">The Quote ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Quote sent successfully</response>
        /// <response code="400">Invalid operation</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpPost("{id}/send")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Send(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate quote exists and user has access
            var quoteResult = await _service.GetByIdAsync(id);
            if (quoteResult.IsFailure)
            {
                return quoteResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(quoteResult.Error.Message),
                    _ => BadRequest(quoteResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(quoteResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            var result = await _service.SendAsync(id);

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
        /// Accept quote
        /// </summary>
        /// <param name="id">The Quote ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Quote accepted successfully</response>
        /// <response code="400">Invalid operation</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpPost("{id}/accept")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Accept(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate quote exists and user has access
            var quoteResult = await _service.GetByIdAsync(id);
            if (quoteResult.IsFailure)
            {
                return quoteResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(quoteResult.Error.Message),
                    _ => BadRequest(quoteResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(quoteResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            var result = await _service.AcceptAsync(id);

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
        /// Reject quote
        /// </summary>
        /// <param name="id">The Quote ID</param>
        /// <param name="request">Optional rejection reason</param>
        /// <returns>No content</returns>
        /// <response code="204">Quote rejected successfully</response>
        /// <response code="400">Invalid operation</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectQuoteRequest? request = null)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate quote exists and user has access
            var quoteResult = await _service.GetByIdAsync(id);
            if (quoteResult.IsFailure)
            {
                return quoteResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(quoteResult.Error.Message),
                    _ => BadRequest(quoteResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(quoteResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            var reason = request?.Reason;
            var result = await _service.RejectAsync(id, reason);

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
        /// Convert quote to invoice
        /// </summary>
        /// <param name="id">The Quote ID</param>
        /// <returns>The created Invoice</returns>
        /// <response code="201">Quote converted to invoice successfully</response>
        /// <response code="400">Invalid operation</response>
        /// <response code="403">Forbidden - quote belongs to different company</response>
        /// <response code="404">Quote not found</response>
        [HttpPost("{id}/convert-to-invoice")]
        [ProducesResponseType(typeof(Invoices), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ConvertToInvoice(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate quote exists and user has access
            var quoteResult = await _service.GetByIdAsync(id);
            if (quoteResult.IsFailure)
            {
                return quoteResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(quoteResult.Error.Message),
                    _ => BadRequest(quoteResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(quoteResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Quote");

            var result = await _service.ConvertToInvoiceAsync(id);

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

            return CreatedAtAction("GetById", "Invoices", new { id = result.Value!.Id }, result.Value);
        }
    }

    /// <summary>
    /// Request model for rejecting a quote
    /// </summary>
    public class RejectQuoteRequest
    {
        /// <summary>
        /// Reason for rejection
        /// </summary>
        public string? Reason { get; set; }
    }
}
