using Application.Interfaces;
using Application.DTOs.VendorPayments;
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
    /// Vendor Payments management endpoints
    /// </summary>
    [ApiController]
    [Route("api/vendor-payments")]
    [Produces("application/json")]
    [Authorize]
    public class VendorPaymentsController : CompanyAuthorizedController
    {
        private readonly IVendorPaymentsService _service;

        public VendorPaymentsController(IVendorPaymentsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Vendor Payment by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VendorPayment), 200)]
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
                return AccessDeniedDifferentCompanyResponse("Vendor Payment");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get Vendor Payment by ID with allocations
        /// </summary>
        [HttpGet("{id}/with-allocations")]
        [ProducesResponseType(typeof(VendorPayment), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByIdWithAllocations(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var result = await _service.GetByIdWithAllocationsAsync(id);

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
                return AccessDeniedDifferentCompanyResponse("Vendor Payment");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Vendor Payments for current company
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<VendorPayment>), 200)]
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
        /// Get paginated Vendor Payments
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<VendorPayment>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] VendorPaymentsFilterRequest request, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (effectiveCompanyId.HasValue)
                filters["company_id"] = effectiveCompanyId.Value;
            else
                filters.Remove("company_id");

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
            var response = new PagedResponse<VendorPayment>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Get unreconciled payments
        /// </summary>
        [HttpGet("unreconciled")]
        [ProducesResponseType(typeof(IEnumerable<VendorPayment>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetUnreconciled([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetUnreconciledAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get TDS payments for a financial year
        /// </summary>
        [HttpGet("tds/{financialYear}")]
        [ProducesResponseType(typeof(IEnumerable<VendorPayment>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTdsPayments(string financialYear, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetTdsPaymentsAsync(effectiveCompanyId.Value, financialYear);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get payments with pending TDS deposit
        /// </summary>
        [HttpGet("pending-tds-deposit")]
        [ProducesResponseType(typeof(IEnumerable<VendorPayment>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPendingTdsDeposit([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetPendingTdsDepositAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(result.Value);
        }

        /// <summary>
        /// Get total TDS deducted for a financial year
        /// </summary>
        [HttpGet("tds-total/{financialYear}")]
        [ProducesResponseType(typeof(decimal), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTotalTdsDeducted(string financialYear, [FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetTotalTdsDeductedAsync(effectiveCompanyId.Value, financialYear);

            if (result.IsFailure)
                return BadRequest(result.Error!.Message);

            return Ok(new { financialYear, totalTdsDeducted = result.Value });
        }

        /// <summary>
        /// Create a new Vendor Payment
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(VendorPayment), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create([FromBody] CreateVendorPaymentDto dto)
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
                    return CannotModifyDifferentCompanyResponse("create vendor payment for");

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
        /// Update an existing Vendor Payment
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorPaymentDto dto)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paymentResult = await _service.GetByIdAsync(id);
            if (paymentResult.IsFailure)
            {
                return paymentResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(paymentResult.Error.Message),
                    _ => BadRequest(paymentResult.Error.Message)
                };
            }

            if (!HasCompanyAccess(paymentResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Payment");

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
        /// Delete a Vendor Payment
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

            var paymentResult = await _service.GetByIdAsync(id);
            if (paymentResult.IsFailure)
            {
                return paymentResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(paymentResult.Error.Message),
                    _ => BadRequest(paymentResult.Error.Message)
                };
            }

            if (!HasCompanyAccess(paymentResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Payment");

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
        /// Mark TDS as deposited
        /// </summary>
        [HttpPost("{id}/mark-tds-deposited")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> MarkTdsDeposited(Guid id, [FromBody] MarkTdsDepositedRequest request)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var paymentResult = await _service.GetByIdAsync(id);
            if (paymentResult.IsFailure)
            {
                return paymentResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(paymentResult.Error.Message),
                    _ => BadRequest(paymentResult.Error.Message)
                };
            }

            if (!HasCompanyAccess(paymentResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Payment");

            var result = await _service.MarkTdsDepositedAsync(id, request.ChallanNumber, request.DepositDate);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Conflict => Conflict(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }

        /// <summary>
        /// Add allocation to payment
        /// </summary>
        [HttpPost("{id}/allocations")]
        [ProducesResponseType(typeof(VendorPaymentAllocation), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddAllocation(Guid id, [FromBody] CreateVendorPaymentAllocationDto dto)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var paymentResult = await _service.GetByIdAsync(id);
            if (paymentResult.IsFailure)
            {
                return paymentResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(paymentResult.Error.Message),
                    _ => BadRequest(paymentResult.Error.Message)
                };
            }

            if (!HasCompanyAccess(paymentResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor Payment");

            var result = await _service.AddAllocationAsync(id, dto);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return CreatedAtAction(nameof(GetByIdWithAllocations), new { id }, result.Value);
        }
    }

    public class MarkTdsDepositedRequest
    {
        public string ChallanNumber { get; set; } = string.Empty;
        public DateOnly DepositDate { get; set; }
    }
}
