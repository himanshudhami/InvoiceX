using Application.DTOs.Leave;
using Application.Interfaces.Leave;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Leave types management endpoints
    /// </summary>
    [ApiController]
    [Route("api/leave-types")]
    [Produces("application/json")]
    public class LeaveTypesController : ControllerBase
    {
        private readonly ILeaveService _leaveService;
        // Hardcoded company ID for now - should come from auth context in production
        private static readonly Guid DefaultCompanyId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        /// <summary>
        /// Initializes a new instance of the LeaveTypesController
        /// </summary>
        public LeaveTypesController(ILeaveService leaveService)
        {
            _leaveService = leaveService ?? throw new ArgumentNullException(nameof(leaveService));
        }

        /// <summary>
        /// Get all leave types for the company
        /// </summary>
        /// <param name="companyId">Optional company ID (defaults to system company)</param>
        /// <param name="activeOnly">Whether to return only active leave types</param>
        /// <returns>List of leave types</returns>
        /// <response code="200">Returns the list of leave types</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LeaveTypeDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? companyId = null, [FromQuery] bool activeOnly = false)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var result = await _leaveService.GetLeaveTypesAsync(effectiveCompanyId, activeOnly);

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
        /// Get a leave type by ID
        /// </summary>
        /// <param name="id">The leave type ID</param>
        /// <returns>The leave type</returns>
        /// <response code="200">Returns the leave type</response>
        /// <response code="404">Leave type not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LeaveTypeDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _leaveService.GetLeaveTypeByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    ErrorType.Validation => BadRequest(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new leave type
        /// </summary>
        /// <param name="dto">Leave type creation data</param>
        /// <param name="companyId">Optional company ID</param>
        /// <returns>The created leave type</returns>
        /// <response code="201">Leave type created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="409">Leave type code already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(LeaveTypeDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateLeaveTypeDto dto, [FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var result = await _leaveService.CreateLeaveTypeAsync(effectiveCompanyId, dto);

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
        /// Update an existing leave type
        /// </summary>
        /// <param name="id">The leave type ID</param>
        /// <param name="dto">Leave type update data</param>
        /// <returns>The updated leave type</returns>
        /// <response code="200">Leave type updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Leave type not found</response>
        /// <response code="409">Leave type code already exists</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(LeaveTypeDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaveTypeDto dto)
        {
            var result = await _leaveService.UpdateLeaveTypeAsync(id, dto);

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

            return Ok(result.Value);
        }

        /// <summary>
        /// Delete a leave type
        /// </summary>
        /// <param name="id">The leave type ID</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Leave type deleted successfully</response>
        /// <response code="404">Leave type not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _leaveService.DeleteLeaveTypeAsync(id);

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
