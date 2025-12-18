using Application.Interfaces;
using Application.DTOs.Announcements;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

/// <summary>
/// Announcements management endpoints (Admin)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementsService _service;

    public AnnouncementsController(IAnnouncementsService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    private Guid? CurrentUserId
    {
        get
        {
            var claim = User.FindFirst("user_id");
            if (claim != null && Guid.TryParse(claim.Value, out var userId))
                return userId;
            return null;
        }
    }

    /// <summary>
    /// Get announcement by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AnnouncementDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

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
    /// Get paginated announcements
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<AnnouncementSummaryDto>), 200)]
    public async Task<IActionResult> GetPaged([FromQuery] AnnouncementsFilterRequest request)
    {
        var filters = new Dictionary<string, object>();
        if (request.CompanyId.HasValue)
            filters["company_id"] = request.CompanyId.Value;
        if (!string.IsNullOrEmpty(request.Category))
            filters["category"] = request.Category;
        if (!string.IsNullOrEmpty(request.Priority))
            filters["priority"] = request.Priority;

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
                _ => StatusCode(500, result.Error.Message)
            };
        }

        var pagedResponse = new PagedResponse<AnnouncementSummaryDto>(
            result.Value.Items,
            result.Value.TotalCount,
            request.PageNumber,
            request.PageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Get announcements by company
    /// </summary>
    [HttpGet("company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<AnnouncementSummaryDto>), 200)]
    public async Task<IActionResult> GetByCompany(Guid companyId, [FromQuery] bool activeOnly = false)
    {
        var result = await _service.GetByCompanyAsync(companyId, activeOnly);

        if (result.IsFailure)
            return StatusCode(500, result.Error!.Message);

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new announcement
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AnnouncementDetailDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
    {
        if (CurrentUserId.HasValue)
            dto.CreatedBy = CurrentUserId.Value;

        var result = await _service.CreateAsync(dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an announcement
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnnouncementDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Delete an announcement
    /// </summary>
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
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return NoContent();
    }
}

/// <summary>
/// Filter request for announcements
/// </summary>
public class AnnouncementsFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
}
