using Application.Interfaces;
using Application.DTOs.Vendors;
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
    /// Vendors management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class VendorsController : CompanyAuthorizedController
    {
        private readonly IVendorsService _service;

        /// <summary>
        /// Initializes a new instance of the VendorsController
        /// </summary>
        public VendorsController(IVendorsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Vendor by ID
        /// </summary>
        /// <param name="id">The Vendor ID</param>
        /// <returns>The Vendor entity</returns>
        /// <response code="200">Returns the Vendor entity</response>
        /// <response code="403">Forbidden - vendor belongs to different company</response>
        /// <response code="404">Vendor not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Vendors), 200)]
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
                return AccessDeniedDifferentCompanyResponse("Vendor");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Vendors for current company (or all for Admin/HR)
        /// </summary>
        /// <param name="companyId">Optional company ID filter (Admin/HR only)</param>
        /// <returns>List of Vendors</returns>
        /// <response code="200">Returns the list of Vendors</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Vendors>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null)
        {
            // Non-Admin/HR users require company ID in token
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
        /// Get paginated Vendors with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <param name="companyId">Optional company ID filter (Admin/HR only)</param>
        /// <returns>Paginated list of Vendors</returns>
        /// <response code="200">Returns the paginated list of Vendors</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Vendors>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] VendorsFilterRequest request, [FromQuery] Guid? companyId = null)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var filters = request.GetFilters();

            // Apply company filter based on role
            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
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
            var response = new PagedResponse<Vendors>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Get all MSME vendors for a company
        /// </summary>
        /// <param name="companyId">Optional company ID (Admin/HR only)</param>
        /// <returns>List of MSME vendors</returns>
        [HttpGet("msme")]
        [ProducesResponseType(typeof(IEnumerable<Vendors>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMsmeVendors([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetMsmeVendorsAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all TDS-applicable vendors for a company
        /// </summary>
        /// <param name="companyId">Optional company ID (Admin/HR only)</param>
        /// <returns>List of TDS-applicable vendors</returns>
        [HttpGet("tds-applicable")]
        [ProducesResponseType(typeof(IEnumerable<Vendors>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTdsApplicableVendors([FromQuery] Guid? companyId = null)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            var effectiveCompanyId = GetEffectiveCompanyId(companyId);
            if (!effectiveCompanyId.HasValue)
                return BadRequest(new { error = "Company ID is required" });

            var result = await _service.GetTdsApplicableVendorsAsync(effectiveCompanyId.Value);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Get vendor's outstanding balance
        /// </summary>
        /// <param name="id">The Vendor ID</param>
        /// <returns>Outstanding balance</returns>
        [HttpGet("{id}/outstanding")]
        [ProducesResponseType(typeof(decimal), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetOutstandingBalance(Guid id)
        {
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Verify vendor exists and user has access
            var vendorResult = await _service.GetByIdAsync(id);
            if (vendorResult.IsFailure)
            {
                return vendorResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(vendorResult.Error.Message),
                    _ => BadRequest(vendorResult.Error.Message)
                };
            }

            if (!HasCompanyAccess(vendorResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor");

            var result = await _service.GetOutstandingBalanceAsync(id);

            if (result.IsFailure)
            {
                return BadRequest(result.Error!.Message);
            }

            return Ok(new { vendorId = id, outstandingBalance = result.Value });
        }

        /// <summary>
        /// Create a new Vendor
        /// </summary>
        /// <param name="dto">The Vendor to create</param>
        /// <returns>The created Vendor</returns>
        /// <response code="201">Vendor created successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - cannot create vendor for different company</response>
        [HttpPost]
        [ProducesResponseType(typeof(Vendors), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create([FromBody] CreateVendorsDto dto)
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

                // Enforce company isolation - ensure vendor is created for current user's company
                if (dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId.Value)
                    return CannotModifyDifferentCompanyResponse("create vendor for");

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
        /// Update an existing Vendor
        /// </summary>
        /// <param name="id">The Vendor ID</param>
        /// <param name="dto">The updated Vendor data</param>
        /// <returns>No content</returns>
        /// <response code="204">Vendor updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - vendor belongs to different company</response>
        /// <response code="404">Vendor not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorsDto dto)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate vendor exists and user has access
            var vendorResult = await _service.GetByIdAsync(id);
            if (vendorResult.IsFailure)
            {
                return vendorResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(vendorResult.Error.Message),
                    _ => BadRequest(vendorResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(vendorResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor");

            // Non-Admin/HR cannot change company_id
            if (!IsAdminOrHR && dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId!.Value)
                return CannotModifyDifferentCompanyResponse("change vendor's company");

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
        /// Delete a Vendor
        /// </summary>
        /// <param name="id">The Vendor ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Vendor deleted successfully</response>
        /// <response code="403">Forbidden - vendor belongs to different company</response>
        /// <response code="404">Vendor not found</response>
        /// <response code="409">Cannot delete vendor with outstanding balance</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            // Validate vendor exists and user has access
            var vendorResult = await _service.GetByIdAsync(id);
            if (vendorResult.IsFailure)
            {
                return vendorResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(vendorResult.Error.Message),
                    _ => BadRequest(vendorResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(vendorResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Vendor");

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
    }
}
