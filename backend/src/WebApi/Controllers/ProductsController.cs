using Application.Interfaces;
using Application.DTOs.Products;
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
    /// Products management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ProductsController : CompanyAuthorizedController
    {
        private readonly IProductsService _service;

        /// <summary>
        /// Initializes a new instance of the ProductsController
        /// </summary>
        public ProductsController(IProductsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Products by ID
        /// </summary>
        /// <param name="id">The Products ID</param>
        /// <returns>The Products entity</returns>
        /// <response code="200">Returns the Products entity</response>
        /// <response code="403">Forbidden - product belongs to different company</response>
        /// <response code="404">Products not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Products), 200)]
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
                return AccessDeniedDifferentCompanyResponse("Product");

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all Products entities for current company (or all for Admin/HR)
        /// </summary>
        /// <param name="companyId">Optional company ID filter (Admin/HR only)</param>
        /// <returns>List of Products entities</returns>
        /// <response code="200">Returns the list of Products entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Products>), 200)]
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
        /// Get paginated Products entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <param name="companyId">Optional company ID filter (Admin/HR only)</param>
        /// <returns>Paginated list of Products entities</returns>
        /// <response code="200">Returns the paginated list of Products entities</response>
        /// <response code="403">Forbidden - company ID not found</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Products>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPaged([FromQuery] ProductsFilterRequest request, [FromQuery] Guid? companyId = null)
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
            var response = new PagedResponse<Products>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new Products
        /// </summary>
        /// <param name="dto">The Products to create</param>
        /// <returns>The created Products</returns>
        /// <response code="201">Products created successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - cannot create product for different company</response>
        [HttpPost]
        [ProducesResponseType(typeof(Products), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create([FromBody] CreateProductsDto dto)
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

                // Enforce company isolation - ensure product is created for current user's company
                if (dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId.Value)
                    return CannotModifyDifferentCompanyResponse("create product for");

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
        /// Update an existing Products
        /// </summary>
        /// <param name="id">The Products ID</param>
        /// <param name="dto">The updated Products data</param>
        /// <returns>No content</returns>
        /// <response code="204">Products updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="403">Forbidden - product belongs to different company</response>
        /// <response code="404">Products not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductsDto dto)
        {
            // Non-Admin/HR users require company ID in token
            if (RequiresCompanyIsolation && CurrentCompanyId == null)
                return CompanyIdNotFoundResponse();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate product exists and user has access
            var productResult = await _service.GetByIdAsync(id);
            if (productResult.IsFailure)
            {
                return productResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(productResult.Error.Message),
                    _ => BadRequest(productResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(productResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Product");

            // Non-Admin/HR cannot change company_id
            if (!IsAdminOrHR && dto.CompanyId.HasValue && dto.CompanyId.Value != CurrentCompanyId!.Value)
                return CannotModifyDifferentCompanyResponse("change product's company");

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
        /// Delete a Products
        /// </summary>
        /// <param name="id">The Products ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Products deleted successfully</response>
        /// <response code="403">Forbidden - product belongs to different company</response>
        /// <response code="404">Products not found</response>
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

            // Validate product exists and user has access
            var productResult = await _service.GetByIdAsync(id);
            if (productResult.IsFailure)
            {
                return productResult.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(productResult.Error.Message),
                    _ => BadRequest(productResult.Error.Message)
                };
            }

            // Validate company access (Admin/HR can access any, others only their company)
            if (!HasCompanyAccess(productResult.Value!.CompanyId))
                return AccessDeniedDifferentCompanyResponse("Product");

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
