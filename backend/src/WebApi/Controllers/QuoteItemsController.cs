using Application.Interfaces;
using Core.Entities;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.DTOs;
using WebApi.DTOs.Common;
using Application.DTOs.QuoteItems;

namespace WebApi.Controllers
{
    /// <summary>
    /// Quote items management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class QuoteItemsController : ControllerBase
    {
        private readonly IQuoteItemsService _service;

        /// <summary>
        /// Initializes a new instance of the QuoteItemsController
        /// </summary>
        public QuoteItemsController(IQuoteItemsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get Quote Item by ID
        /// </summary>
        /// <param name="id">The Quote Item ID</param>
        /// <returns>The Quote Item entity</returns>
        /// <response code="200">Returns the Quote Item entity</response>
        /// <response code="404">Quote Item not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(QuoteItems), 200)]
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
        /// Get paginated Quote Item entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of Quote Item entities</returns>
        /// <response code="200">Returns the paginated list of Quote Item entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<QuoteItems>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] QuoteItemsFilterRequest request)
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
            var response = new PagedResponse<QuoteItems>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(response);
        }

        /// <summary>
        /// Create a new Quote Item
        /// </summary>
        /// <param name="dto">The Quote Item to create</param>
        /// <returns>The created Quote Item</returns>
        /// <response code="201">Quote Item created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(QuoteItems), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateQuoteItemsDto dto)
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
        /// Update an existing Quote Item
        /// </summary>
        /// <param name="id">The Quote Item ID</param>
        /// <param name="dto">The Quote Item data to update</param>
        /// <returns>No content</returns>
        /// <response code="204">Quote Item updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">Quote Item not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuoteItemsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.Id = id;
            var result = await _service.UpdateAsync(id, dto);

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

        /// <summary>
        /// Delete a Quote Item by ID
        /// </summary>
        /// <param name="id">The Quote Item ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Quote Item deleted successfully</response>
        /// <response code="404">Quote Item not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Internal => StatusCode(500, result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return NoContent();
        }
    }
}
