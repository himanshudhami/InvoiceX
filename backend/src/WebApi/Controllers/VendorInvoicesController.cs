using Application.Interfaces;
using Application.DTOs.VendorInvoices;
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
    /// Vendor Invoices (Purchase Bills) management endpoints
    /// </summary>
    [ApiController]
    [Route("api/vendor-invoices")]
    [Produces("application/json")]
    [Authorize]
    public class VendorInvoicesController : CompanyAuthorizedController
    {
        private readonly IVendorInvoicesService _service;

        /// <summary>
        /// Initializes a new instance of the VendorInvoicesController
        /// </summary>
        public VendorInvoicesController(IVendorInvoicesService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Vendor Invoice by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VendorInvoice), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
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

            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Invoice");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get Vendor Invoice by ID with line items
        /// </summary>
        [HttpGet("{id}/with-items")]
        [ProducesResponseType(typeof(VendorInvoice), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByIdWithItems(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = await _service.GetByIdWithItemsAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            if (!HasCompanyAccess(result.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Invoice");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Vendor Invoices for current company
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<VendorInvoice>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = new Dictionary<string, object>();
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
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
        /// Get paginated Vendor Invoices with filtering
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<VendorInvoice>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] VendorInvoicesFilterRequest request, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (effectiveCompanyId.HasValue)
            {
                filters["company_id"] = effectiveCompanyId.Value;
            }
            else
            {
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
            var response = new PagedResponse<VendorInvoice>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Get pending approval invoices
        /// </summary>
        [HttpGet("pending-approval")]
        [ProducesResponseType(typeof(IEnumerable<VendorInvoice>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPendingApproval([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetPendingApprovalAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get unpaid invoices
        /// </summary>
        [HttpGet("unpaid")]
        [ProducesResponseType(typeof(IEnumerable<VendorInvoice>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetUnpaid([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetUnpaidAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get overdue invoices
        /// </summary>
        [HttpGet("overdue")]
        [ProducesResponseType(typeof(IEnumerable<VendorInvoice>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetOverdue([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetOverdueAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get ITC eligible invoices
        /// </summary>
        [HttpGet("itc-eligible")]
        [ProducesResponseType(typeof(IEnumerable<VendorInvoice>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetItcEligible([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetItcEligibleAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get invoices not matched with GSTR-2B
        /// </summary>
        [HttpGet("unmatched-gstr2b")]
        [ProducesResponseType(typeof(IEnumerable<VendorInvoice>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetUnmatchedWithGstr2B([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetUnmatchedWithGstr2BAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new Vendor Invoice
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(VendorInvoice), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create([FromBody] CreateVendorInvoiceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsAdminOrHR)
            {
                if (!dto.CompanyId.HasValue)
                    return BadRequest(new { error = "Company ID is required" });
            }
            else
            {
                if (CurrentCompanyId == null)
                    return CompanyIdNotFoundResponse();

                if (dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId.Value)
                    return CannotModifyDifferentCompanyResponse("create vendor invoice for");

                if (!dto.CompanyId.HasValue)
                    dto.CompanyId = CurrentCompanyId.Value;
            }

            var result = await _service.CreateAsync(dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        /// <summary>
        /// Update an existing Vendor Invoice
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorInvoiceDto dto)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var invoiceResult = await _service.GetByIdAsync(id);
            if (invoiceResult.IsFailure)
            {
                return invoiceResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(invoiceResult.Error.Message),
                    _ => BadRequest(invoiceResult.Error.Message)
                };
            }

            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Invoice");

            if (!IsAdminOrHR && dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId!.Value)
                return CannotModifyDifferentCompanyResponse("change vendor invoice's company");

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
        /// Delete a Vendor Invoice
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(Guid id)
        {
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

            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Invoice");

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
        /// Approve a Vendor Invoice
        /// </summary>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveInvoiceRequest? request = null)
        {
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

            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Invoice");

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return BadRequest(new { error = "User ID not found in token" });

            var result = await _service.ApproveAsync(id, userId, request?.Notes);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Update invoice status
        /// </summary>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
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

            if (!HasCompanyAccess(invoiceResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Invoice");

            var result = await _service.UpdateStatusAsync(id, request.Status);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("user_id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;
            return Guid.Empty;
        }
    }

    public class ApproveInvoiceRequest
    {
        public string? Notes { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
