using Application.Interfaces;
using Application.DTOs.TaxRates;
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
    /// TaxRates management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TaxRatesController : ControllerBase
    {
        private readonly ITaxRatesService _service;

        /// <summary>
        /// Initializes a new instance of the TaxRatesController
        /// </summary>
        public TaxRatesController(ITaxRatesService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get TaxRates by ID
        /// </summary>
        /// <param name="id">The TaxRates ID</param>
        /// <returns>The TaxRates entity</returns>
        /// <response code="200">Returns the TaxRates entity</response>
        /// <response code="404">TaxRates not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TaxRates), 200)]
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
        /// Get all TaxRates entities
        /// </summary>
        /// <returns>List of TaxRates entities</returns>
        /// <response code="200">Returns the list of TaxRates entities</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TaxRates>), 200)]
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
        /// Get paginated TaxRates entities with filtering and sorting
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of TaxRates entities</returns>
        /// <response code="200">Returns the paginated list of TaxRates entities</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResponse<TaxRates>), 200)]
        public async Task<IActionResult> GetPaged([FromQuery] TaxRatesFilterRequest request)
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
            var response = new PagedResponse<TaxRates>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);
            
            return Ok(response);
        }

        /// <summary>
        /// Create a new TaxRates
        /// </summary>
        /// <param name="dto">The TaxRates to create</param>
        /// <returns>The created TaxRates</returns>
        /// <response code="201">TaxRates created successfully</response>
        /// <response code="400">Invalid input</response>
        [HttpPost]
        [ProducesResponseType(typeof(TaxRates), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateTaxRatesDto dto)
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
        /// Update an existing TaxRates
        /// </summary>
        /// <param name="id">The TaxRates ID</param>
        /// <param name="dto">The updated TaxRates data</param>
        /// <returns>No content</returns>
        /// <response code="204">TaxRates updated successfully</response>
        /// <response code="400">Invalid input</response>
        /// <response code="404">TaxRates not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxRatesDto dto)
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
        /// Delete a TaxRates
        /// </summary>
        /// <param name="id">The TaxRates ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">TaxRates deleted successfully</response>
        /// <response code="404">TaxRates not found</response>
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