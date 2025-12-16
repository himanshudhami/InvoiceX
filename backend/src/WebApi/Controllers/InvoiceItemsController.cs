using Application.Interfaces;
using Application.DTOs.InvoiceItems;
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
    /// InvoiceItems management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InvoiceItemsController : ControllerBase
    {
        private readonly IInvoiceItemsService _service;

        /// <summary>
        /// Initializes a new instance of the InvoiceItemsController
        /// </summary>
        public InvoiceItemsController(IInvoiceItemsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get InvoiceItems by ID
        /// </summary>
        /// <param name="id">The InvoiceItems ID</param>
        /// <returns>The InvoiceItems entity</returns>
        /// <response code="200">Returns the InvoiceItems entity</response>
        /// <response code="404">InvoiceItems not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InvoiceItems), 200)]
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
        /// Get all InvoiceItems entities
        /// </summary>
        /// <returns>List of InvoiceItems entities</returns>
        /// <response code="200">Returns the list of InvoiceItems entities</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<InvoiceItems>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            
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
        /// Get paginated InvoiceItems entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of InvoiceItems entities</returns>
        /// <response code="200">Returns the paginated list of InvoiceItems entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<InvoiceItems>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] InvoiceItemsFilterRequest request)
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
            var response = new PagedResponse<InvoiceItems>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);
            
            return Ok(response);
        }

        /// <summary>
        /// Create a new InvoiceItems
        /// </summary>
        /// <param name="dto">The InvoiceItems to create</param>
        /// <returns>The created InvoiceItems</returns>
        /// <response code="201">InvoiceItems created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(InvoiceItems), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceItemsDto dto)
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
        /// Update an existing InvoiceItems
        /// </summary>
        /// <param name="id">The InvoiceItems ID</param>
        /// <param name="dto">The updated InvoiceItems data</param>
        /// <returns>No content</returns>
        /// <response code="204">InvoiceItems updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">InvoiceItems not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceItemsDto dto)
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
        /// Delete a InvoiceItems
        /// </summary>
        /// <param name="id">The InvoiceItems ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">InvoiceItems deleted successfully</response>
        /// <response code="404">InvoiceItems not found</response>
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