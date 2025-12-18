using Application.DTOs.Leave;
using Application.Interfaces.Leave;
using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Holiday management endpoints
    /// </summary>
    [ApiController]
    [Route("api/holidays")]
    [Produces("application/json")]
    public class HolidaysController : ControllerBase
    {
        private readonly ILeaveService _leaveService;
        // Hardcoded company ID for now - should come from auth context in production
        private static readonly Guid DefaultCompanyId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        /// <summary>
        /// Initializes a new instance of the HolidaysController
        /// </summary>
        public HolidaysController(ILeaveService leaveService)
        {
            _leaveService = leaveService ?? throw new ArgumentNullException(nameof(leaveService));
        }

        /// <summary>
        /// Get all holidays for a year
        /// </summary>
        /// <param name="year">The year (defaults to current year)</param>
        /// <param name="companyId">Optional company ID. If not provided, returns holidays for all companies.</param>
        /// <returns>List of holidays</returns>
        /// <response code="200">Returns the list of holidays</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HolidayDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] int? year = null, [FromQuery] Guid? companyId = null)
        {
            var effectiveYear = year ?? DateTime.UtcNow.Year;

            // If companyId is provided, filter by company; otherwise return all holidays for the year
            var result = companyId.HasValue
                ? await _leaveService.GetHolidaysAsync(companyId.Value, effectiveYear)
                : await _leaveService.GetHolidaysAsync(effectiveYear);

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
        /// Get a holiday by ID
        /// </summary>
        /// <param name="id">The holiday ID</param>
        /// <returns>The holiday</returns>
        /// <response code="200">Returns the holiday</response>
        /// <response code="404">Holiday not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(HolidayDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _leaveService.GetHolidayByIdAsync(id);

            if (result.IsFailure)
            {
                return result.Error!.Type switch
                {
                    ErrorType.NotFound => NotFound(result.Error.Message),
                    _ => BadRequest(result.Error.Message)
                };
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new holiday
        /// </summary>
        /// <param name="dto">Holiday creation data</param>
        /// <param name="companyId">Optional company ID</param>
        /// <returns>The created holiday</returns>
        /// <response code="201">Holiday created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="409">Holiday already exists on this date</response>
        [HttpPost]
        [ProducesResponseType(typeof(HolidayDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateHolidayDto dto, [FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;
            var result = await _leaveService.CreateHolidayAsync(effectiveCompanyId, dto);

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
        /// Update an existing holiday
        /// </summary>
        /// <param name="id">The holiday ID</param>
        /// <param name="dto">Holiday update data</param>
        /// <returns>The updated holiday</returns>
        /// <response code="200">Holiday updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Holiday not found</response>
        /// <response code="409">Holiday already exists on the new date</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(HolidayDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHolidayDto dto)
        {
            var result = await _leaveService.UpdateHolidayAsync(id, dto);

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
        /// Delete a holiday
        /// </summary>
        /// <param name="id">The holiday ID</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">Holiday deleted successfully</response>
        /// <response code="404">Holiday not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _leaveService.DeleteHolidayAsync(id);

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

        /// <summary>
        /// Copy holidays from one year to another
        /// </summary>
        /// <param name="fromYear">Source year</param>
        /// <param name="toYear">Target year</param>
        /// <param name="companyId">Optional company ID</param>
        /// <returns>List of created holidays</returns>
        /// <response code="200">Holidays copied successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost("copy")]
        [ProducesResponseType(typeof(IEnumerable<HolidayDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CopyHolidays([FromQuery] int fromYear, [FromQuery] int toYear, [FromQuery] Guid? companyId = null)
        {
            var effectiveCompanyId = companyId ?? DefaultCompanyId;

            // Get holidays from source year
            var sourceResult = await _leaveService.GetHolidaysAsync(effectiveCompanyId, fromYear);
            if (sourceResult.IsFailure)
            {
                return BadRequest(sourceResult.Error!.Message);
            }

            var sourceHolidays = sourceResult.Value!.ToList();
            if (!sourceHolidays.Any())
            {
                return BadRequest($"No holidays found for year {fromYear}");
            }

            var createdHolidays = new List<HolidayDto>();
            var errors = new List<string>();

            foreach (var holiday in sourceHolidays)
            {
                // Calculate new date by adjusting year
                var newDate = new DateTime(toYear, holiday.Date.Month,
                    Math.Min(holiday.Date.Day, DateTime.DaysInMonth(toYear, holiday.Date.Month)));

                var createDto = new CreateHolidayDto
                {
                    Name = holiday.Name,
                    Date = newDate,
                    IsOptional = holiday.IsOptional,
                    Description = holiday.Description
                };

                var createResult = await _leaveService.CreateHolidayAsync(effectiveCompanyId, createDto);
                if (createResult.IsSuccess)
                {
                    createdHolidays.Add(createResult.Value!);
                }
                else if (createResult.Error!.Type != ErrorType.Conflict)
                {
                    errors.Add($"{holiday.Name}: {createResult.Error.Message}");
                }
                // Skip conflicts (holiday already exists on that date)
            }

            if (errors.Any() && !createdHolidays.Any())
            {
                return BadRequest($"Failed to copy holidays: {string.Join(", ", errors)}");
            }

            return Ok(createdHolidays);
        }
    }
}
