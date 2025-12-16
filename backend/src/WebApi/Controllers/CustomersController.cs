using Application.Interfaces;
using Application.DTOs.Customers;
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
    /// Customers management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
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
        /// <response code="404">Customers not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Customers), 200)]
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
        /// Get all Customers entities
        /// </summary>
        /// <returns>List of Customers entities</returns>
        /// <response code="200">Returns the list of Customers entities</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Customers>), 200)]
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
        /// Get paginated Customers entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of Customers entities</returns>
        /// <response code="200">Returns the paginated list of Customers entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<Customers>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] CustomersFilterRequest request)
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
        [HttpPost]
        [ProducesResponseType(typeof(Customers), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateCustomersDto dto)
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
        /// Update an existing Customers
        /// </summary>
        /// <param name="id">The Customers ID</param>
        /// <param name="dto">The updated Customers data</param>
        /// <returns>No content</returns>
        /// <response code="204">Customers updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Customers not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomersDto dto)
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
        /// Delete a Customers
        /// </summary>
        /// <param name="id">The Customers ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Customers deleted successfully</response>
        /// <response code="404">Customers not found</response>
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