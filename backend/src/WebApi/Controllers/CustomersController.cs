using Application.Interfaces;
using Application.DTOs.Customers;
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
    /// Customers management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CustomersController : CompanyAuthorizedController
    {
        private readonly ICustomersService _service;

        /// <summary>
        /// Initializes a new instance of the CustomersController
        /// </summary>
        public CustomersController(ICustomersService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Customers by ID
        /// </summary>
        /// <param name="id">The Customers ID</param>
        /// <returns>The Customers entity</returns>
        /// <response code="200">Returns the Customers entity</response>
        /// <response code="403">Forbidden - customer belongs to different company</response>
        /// <response code="404">Customers not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Customers), 200)]
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
                return AccessDeniedDifferentCompanyResponse("Customer");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Customers entities for current company (or all for Admin/HR)
        /// </summary>
        /// <param name="companyId">Optional company ID filter (Admin/HR only)</param>
        /// <returns>List of Customers entities</returns>
        /// <response code="200">Returns the list of Customers entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Customers>), 200)]
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
        /// Get paginated Customers entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <param name="companyId">Optional company ID filter (Admin/HR only)</param>
        /// <returns>Paginated list of Customers entities</returns>
        /// <response code="200">Returns the paginated list of Customers entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Customers>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] CustomersFilterRequest request, [FromQuery] Guid? companyId = null)
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
            var response = new PagedResponse<Customers>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new Customers
        /// </summary>
        /// <param name="dto">The Customers to create</param>
        /// <returns>The created Customers</returns>
        /// <response code="201">Customers created successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - cannot create customer for different company</response>
        [HttpPost]
        [ProducesResponseType(typeof(Customers), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create([FromBody] CreateCustomersDto dto)
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

                // Enforce company isolation - ensure customer is created for current user's company
                if (dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId.Value)
                    return CannotModifyDifferentCompanyResponse("create customer for");

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
        /// Update an existing Customers
        /// </summary>
        /// <param name="id">The Customers ID</param>
        /// <param name="dto">The updated Customers data</param>
        /// <returns>No content</returns>
        /// <response code="204">Customers updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - customer belongs to different company</response>
        /// <response code="404">Customers not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomersDto dto)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate customer exists and user has access
            var customerResult = await _service.GetByIdAsync(id);
            if (customerResult.IsFailure)
            {
                return customerResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(customerResult.Error.Message),
                    _ => BadRequest(customerResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(customerResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Customer");

            // Non-Admin/HR cannot change company_id
            if (!IsAdminOrHR && dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId!.Value)
                return CannotModifyDifferentCompanyResponse("change customer's company");

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
        /// Delete a Customers
        /// </summary>
        /// <param name="id">The Customers ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Customers deleted successfully</response>
        /// <response code="403">Forbidden - customer belongs to different company</response>
        /// <response code="404">Customers not found</response>
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

            // Validate customer exists and user has access
            var customerResult = await _service.GetByIdAsync(id);
            if (customerResult.IsFailure)
            {
                return customerResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(customerResult.Error.Message),
                    _ => BadRequest(customerResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(customerResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Customer");

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
