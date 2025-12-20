using Application.Interfaces;
using Application.DTOs.PaymentAllocations;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Payment allocation management endpoints
    /// Enables tracking of partial payments, advance payments, and invoice settlements
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PaymentAllocationsController : ControllerBase
    {
        private readonly IPaymentAllocationService _service;

        /// <summary>
        /// Initializes a new instance of the PaymentAllocationsController
        /// </summary>
        public PaymentAllocationsController(IPaymentAllocationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // ==================== Basic CRUD ====================

        /// <summary>
        /// Get allocation by ID
        /// </summary>
        /// <param name="id">The allocation ID</param>
        /// <returns>The allocation entity</returns>
        /// <response code="200">Returns the allocation</response>
        /// <response code="404">Allocation not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PaymentAllocation), 200)]
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
        /// Get all allocations
        /// </summary>
        /// <returns>List of allocations</returns>
        /// <response code="200">Returns the list of allocations</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PaymentAllocation>), 200)]
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
        /// Get paginated allocations with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of allocations</returns>
        /// <response code="200">Returns the paginated list</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<PaymentAllocation>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] PaymentAllocationsFilterRequest request)
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
            var response = new PagedResponse<PaymentAllocation>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new allocation
        /// </summary>
        /// <param name="dto">The allocation to create</param>
        /// <returns>The created allocation</returns>
        /// <response code="201">Allocation created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(PaymentAllocation), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreatePaymentAllocationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);

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

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing allocation
        /// </summary>
        /// <param name="id">The allocation ID</param>
        /// <param name="dto">The updated allocation data</param>
        /// <returns>No content</returns>
        /// <response code="204">Allocation updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Allocation not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentAllocationDto dto)
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
        /// Delete an allocation
        /// </summary>
        /// <param name="id">The allocation ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Allocation deleted successfully</response>
        /// <response code="404">Allocation not found</response>
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

        // ==================== Query Endpoints ====================

        /// <summary>
        /// Get all allocations for a payment
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <returns>List of allocations for the payment</returns>
        /// <response code="200">Returns the list of allocations</response>
        [HttpGet("by-payment/{paymentId}")]
        [ProducesResponseType(typeof(IEnumerable<PaymentAllocation>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByPaymentId(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
                return BadRequest("Payment ID cannot be empty");

            var result = await _service.GetByPaymentIdAsync(paymentId);

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
        /// Get all allocations for an invoice
        /// </summary>
        /// <param name="invoiceId">The invoice ID</param>
        /// <returns>List of allocations for the invoice</returns>
        /// <response code="200">Returns the list of allocations</response>
        [HttpGet("by-invoice/{invoiceId}")]
        [ProducesResponseType(typeof(IEnumerable<PaymentAllocation>), 200)]
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
        /// Get all allocations for a company
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <returns>List of allocations for the company</returns>
        /// <response code="200">Returns the list of allocations</response>
        [HttpGet("by-company/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<PaymentAllocation>), 200)]
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

        // ==================== Allocation Operations ====================

        /// <summary>
        /// Allocate a payment to one or more invoices (bulk allocation)
        /// </summary>
        /// <param name="dto">The allocation request with list of invoices and amounts</param>
        /// <returns>The created allocations</returns>
        /// <response code="201">Allocations created successfully</response>
        /// <response code="400">Invalid input or insufficient unallocated amount</response>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(IEnumerable<PaymentAllocation>), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AllocatePayment([FromBody] BulkAllocationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.AllocatePaymentAsync(dto);

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

            return StatusCode(201, result.Value);
        }

        /// <summary>
        /// Get unallocated amount for a payment
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <returns>The unallocated amount</returns>
        /// <response code="200">Returns the unallocated amount</response>
        [HttpGet("unallocated/{paymentId}")]
        [ProducesResponseType(typeof(decimal), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUnallocatedAmount(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
                return BadRequest("Payment ID cannot be empty");

            var result = await _service.GetUnallocatedAmountAsync(paymentId);

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

            return Ok(new { unallocatedAmount = result.Value });
        }

        /// <summary>
        /// Get payment allocation summary with all allocation details
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <returns>Payment allocation summary</returns>
        /// <response code="200">Returns the allocation summary</response>
        [HttpGet("summary/{paymentId}")]
        [ProducesResponseType(typeof(PaymentAllocationSummaryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPaymentAllocationSummary(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
                return BadRequest("Payment ID cannot be empty");

            var result = await _service.GetPaymentAllocationSummaryAsync(paymentId);

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

            return Ok(result.Value);
        }

        // ==================== Invoice Status Endpoints ====================

        /// <summary>
        /// Get payment status for an invoice (derived from allocations)
        /// </summary>
        /// <param name="invoiceId">The invoice ID</param>
        /// <returns>Invoice payment status</returns>
        /// <response code="200">Returns the invoice payment status</response>
        [HttpGet("invoice-status/{invoiceId}")]
        [ProducesResponseType(typeof(InvoicePaymentStatusDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetInvoicePaymentStatus(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty)
                return BadRequest("Invoice ID cannot be empty");

            var result = await _service.GetInvoicePaymentStatusAsync(invoiceId);

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

            return Ok(result.Value);
        }

        /// <summary>
        /// Get payment status for all invoices of a company
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="financialYear">Optional financial year filter</param>
        /// <returns>List of invoice payment statuses</returns>
        /// <response code="200">Returns the list of invoice payment statuses</response>
        [HttpGet("company-invoice-status/{companyId}")]
        [ProducesResponseType(typeof(IEnumerable<InvoicePaymentStatusDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetCompanyInvoicePaymentStatus(
            Guid companyId,
            [FromQuery] string? financialYear = null)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID cannot be empty");

            var result = await _service.GetCompanyInvoicePaymentStatusAsync(companyId, financialYear);

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

        // ==================== Bulk Operations ====================

        /// <summary>
        /// Remove all allocations for a payment
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Allocations removed successfully</response>
        /// <response code="404">Payment not found</response>
        [HttpDelete("by-payment/{paymentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RemoveAllAllocations(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
                return BadRequest("Payment ID cannot be empty");

            var result = await _service.RemoveAllAllocationsAsync(paymentId);

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
