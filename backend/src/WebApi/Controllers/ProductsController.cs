using Application.Interfaces;
using Application.DTOs.Products;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class ProductsController : ControllerBase
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
        /// <response code="404">Products not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Products), 200)]
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
        /// Get all Products entities
        /// </summary>
        /// <returns>List of Products entities</returns>
        /// <response code="200">Returns the list of Products entities</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Products>), 200)]
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
        /// Get paginated Products entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of Products entities</returns>
        /// <response code="200">Returns the paginated list of Products entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Products>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] ProductsFilterRequest request)
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
        [HttpPost]
        [ProducesResponseType(typeof(Products), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateProductsDto dto)
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
        /// Update an existing Products
        /// </summary>
        /// <param name="id">The Products ID</param>
        /// <param name="dto">The updated Products data</param>
        /// <returns>No content</returns>
        /// <response code="204">Products updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Products not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductsDto dto)
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
        /// Delete a Products
        /// </summary>
        /// <param name="id">The Products ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Products deleted successfully</response>
        /// <response code="404">Products not found</response>
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
    }
}