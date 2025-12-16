using Application.Interfaces;
using Application.DTOs.InvoiceTemplates;
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
    /// InvoiceTemplates management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InvoiceTemplatesController : ControllerBase
    {
        private readonly IInvoiceTemplatesService _service;

        /// <summary>
        /// Initializes a new instance of the InvoiceTemplatesController
        /// </summary>
        public InvoiceTemplatesController(IInvoiceTemplatesService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get InvoiceTemplates by ID
        /// </summary>
        /// <param name="id">The InvoiceTemplates ID</param>
        /// <returns>The InvoiceTemplates entity</returns>
        /// <response code="200">Returns the InvoiceTemplates entity</response>
        /// <response code="404">InvoiceTemplates not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InvoiceTemplates), 200)]
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
        /// Get all InvoiceTemplates entities
        /// </summary>
        /// <returns>List of InvoiceTemplates entities</returns>
        /// <response code="200">Returns the list of InvoiceTemplates entities</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<InvoiceTemplates>), 200)]
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
        /// Get paginated InvoiceTemplates entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of InvoiceTemplates entities</returns>
        /// <response code="200">Returns the paginated list of InvoiceTemplates entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<InvoiceTemplates>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] InvoiceTemplatesFilterRequest request)
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
            var response = new PagedResponse<InvoiceTemplates>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);
            
            return Ok(response);
        }

        /// <summary>
        /// Create a new InvoiceTemplates
        /// </summary>
        /// <param name="dto">The InvoiceTemplates to create</param>
        /// <returns>The created InvoiceTemplates</returns>
        /// <response code="201">InvoiceTemplates created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(InvoiceTemplates), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceTemplatesDto dto)
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
        /// Update an existing InvoiceTemplates
        /// </summary>
        /// <param name="id">The InvoiceTemplates ID</param>
        /// <param name="dto">The updated InvoiceTemplates data</param>
        /// <returns>No content</returns>
        /// <response code="204">InvoiceTemplates updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">InvoiceTemplates not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceTemplatesDto dto)
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
        /// Delete a InvoiceTemplates
        /// </summary>
        /// <param name="id">The InvoiceTemplates ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">InvoiceTemplates deleted successfully</response>
        /// <response code="404">InvoiceTemplates not found</response>
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