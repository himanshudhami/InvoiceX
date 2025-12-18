using Application.Interfaces;
using Application.DTOs.Invoices;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Controllers.Common;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Invoices management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class InvoicesController : CompanyAuthorizedController
    {
        private readonly IInvoicesService _service;

        /// <summary>
        /// Initializes a new instance of the InvoicesController
        /// </summary>
        public InvoicesController(IInvoicesService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Invoices by ID
        /// </summary>
        /// <param name="id">The Invoices ID</param>
        /// <returns>The Invoices entity</returns>
        /// <response code="200">Returns the Invoices entity</response>
        /// <response code="403">Forbidden - invoice belongs to different company</response>
        /// <response code="404">Invoices not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Invoices), 200)]
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
                return AccessDeniedDifferentCompanyResponse("Invoice");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Invoices entities for current company (or all for Admin/HR)
        /// </summary>
        /// <param name="company">Optional company ID filter (Admin/HR only)</param>
        /// <returns>List of Invoices entities</returns>
        /// <response code="200">Returns the list of Invoices entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Invoices>), 200)]
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
        /// Get paginated Invoices entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <param name="company">Optional company ID filter (Admin/HR only)</param>
        /// <returns>Paginated list of Invoices entities</returns>
        /// <response code="200">Returns the paginated list of Invoices entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Invoices>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] InvoicesFilterRequest request, [FromQuery] Guid? company = null)
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
            var response = new PagedResponse<Invoices>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new Invoices
        /// </summary>
        /// <param name="dto">The Invoices to create</param>
        /// <returns>The created Invoices</returns>
        /// <response code="201">Invoices created successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - cannot create invoice for different company</response>
        [HttpPost]
        [ProducesResponseType(typeof(Invoices), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create([FromBody] CreateInvoicesDto dto)
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

                // Enforce company isolation - ensure invoice is created for current user's company
                if (dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId.Value)
                    return CannotModifyDifferentCompanyResponse("create invoice for");

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
        /// Update an existing Invoices
        /// </summary>
        /// <param name="id">The Invoices ID</param>
        /// <param name="dto">The updated Invoices data</param>
        /// <returns>No content</returns>
        /// <response code="204">Invoices updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - invoice belongs to different company</response>
        /// <response code="404">Invoices not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoicesDto dto)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate invoice exists and user has access
            var invoiceResult = await _service.GetByIdAsync(id);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(invoiceResult.Error.Message),
                    _ => BadRequest(invoiceResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Invoice");

            // Non-Admin/HR cannot change company_id
            if (!IsAdminOrHR && dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId!.Value)
                return CannotModifyDifferentCompanyResponse("change invoice's company");

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
        /// Delete a Invoices
        /// </summary>
        /// <param name="id">The Invoices ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Invoices deleted successfully</response>
        /// <response code="403">Forbidden - invoice belongs to different company</response>
        /// <response code="404">Invoices not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate invoice exists and user has access
            var invoiceResult = await _service.GetByIdAsync(id);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(invoiceResult.Error.Message),
                    _ => BadRequest(invoiceResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Invoice");

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

        /// <summary>
        /// Duplicate an existing invoice
        /// </summary>
        /// <param name="id">The Invoices ID to duplicate</param>
        /// <returns>The duplicated invoice</returns>
        /// <response code="201">Invoice duplicated successfully</response>
        /// <response code="403">Forbidden - invoice belongs to different company</response>
        /// <response code="404">Invoice not found</response>
        [HttpPost("{id}/duplicate")]
        [ProducesResponseType(typeof(Invoices), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Duplicate(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate invoice exists and user has access
            var invoiceResult = await _service.GetByIdAsync(id);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(invoiceResult.Error.Message),
                    _ => BadRequest(invoiceResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Invoice");

            var result = await _service.DuplicateAsync(id);

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

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Record a payment for an invoice
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <param name="paymentDto">Payment details including amount and INR amount</param>
        /// <returns>The created payment</returns>
        /// <response code="201">Payment recorded successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - invoice belongs to different company</response>
        /// <response code="404">Invoice not found</response>
        [HttpPost("{id}/payments")]
        [ProducesResponseType(typeof(Payments), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RecordPayment(Guid id, [FromBody] Application.DTOs.Payments.CreatePaymentsDto paymentDto)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate invoice exists and user has access
            var invoiceResult = await _service.GetByIdAsync(id);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(invoiceResult.Error.Message),
                    _ => BadRequest(invoiceResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Invoice");

            var result = await _service.RecordPaymentAsync(id, paymentDto);

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

            return CreatedAtAction(nameof(GetById), new { id }, result.Value);
        }

        /// <summary>
        /// Get all payments for an invoice
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <returns>List of payments</returns>
        /// <response code="200">Returns list of payments</response>
        /// <response code="403">Forbidden - invoice belongs to different company</response>
        /// <response code="404">Invoice not found</response>
        [HttpGet("{id}/payments")]
        [ProducesResponseType(typeof(IEnumerable<Payments>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPayments(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var invoiceResult = await _service.GetByIdAsync(id);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(invoiceResult.Error.Message),
                    _ => BadRequest(invoiceResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Invoice");

            // Get payments for this invoice
            var paymentsService = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.IPaymentsService>();
            var allPayments = await paymentsService.GetAllAsync();

            if (allPayments.IsFailure)
            {
                return StatusCode(500, allPayments.Error!.Message);
            }

            var invoicePayments = allPayments.Value?.Where(p => p.InvoiceId == id) ?? Enumerable.Empty<Payments>();
            return Ok(invoicePayments);
        }
    }
}
