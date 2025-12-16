using Application.Interfaces;
using Application.DTOs.Invoices;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class InvoicesController : ControllerBase
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
        /// <response code="404">Invoices not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Invoices), 200)]
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
        /// Get all Invoices entities
        /// </summary>
        /// <returns>List of Invoices entities</returns>
        /// <response code="200">Returns the list of Invoices entities</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Invoices>), 200)]
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
        /// Get paginated Invoices entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of Invoices entities</returns>
        /// <response code="200">Returns the paginated list of Invoices entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Invoices>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] InvoicesFilterRequest request)
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
        [HttpPost]
        [ProducesResponseType(typeof(Invoices), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateInvoicesDto dto)
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
        /// Update an existing Invoices
        /// </summary>
        /// <param name="id">The Invoices ID</param>
        /// <param name="dto">The updated Invoices data</param>
        /// <returns>No content</returns>
        /// <response code="204">Invoices updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Invoices not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoicesDto dto)
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
        /// Delete a Invoices
        /// </summary>
        /// <param name="id">The Invoices ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Invoices deleted successfully</response>
        /// <response code="404">Invoices not found</response>
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

        /// <summary>
        /// Duplicate an existing invoice
        /// </summary>
        /// <param name="id">The Invoices ID to duplicate</param>
        /// <returns>The duplicated invoice</returns>
        /// <response code="201">Invoice duplicated successfully</response>
        /// <response code="404">Invoice not found</response>
        [HttpPost("{id}/duplicate")]
        [ProducesResponseType(typeof(Invoices), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Duplicate(Guid id)
        {
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
        /// <response code="404">Invoice not found</response>
        [HttpPost("{id}/payments")]
        [ProducesResponseType(typeof(Payments), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RecordPayment(Guid id, [FromBody] Application.DTOs.Payments.CreatePaymentsDto paymentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        /// <response code="404">Invoice not found</response>
        [HttpGet("{id}/payments")]
        [ProducesResponseType(typeof(IEnumerable<Payments>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPayments(Guid id)
        {
            var invoiceResult = await _service.GetByIdAsync(id);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(invoiceResult.Error.Message),
                    _ => BadRequest(invoiceResult.Error.Message)
                };
            }

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