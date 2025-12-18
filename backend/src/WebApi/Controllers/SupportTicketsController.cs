using Application.Interfaces;
using Application.DTOs.SupportTickets;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

/// <summary>
/// Support Tickets management endpoints (Admin)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class SupportTicketsController : ControllerBase
{
    private readonly ISupportTicketsService _service;

    public SupportTicketsController(ISupportTicketsService service)
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

    // ==================== Tickets ====================

    /// <summary>
    /// Get ticket by ID with messages
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupportTicketDetailDto), 200)]
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
    /// Get paginated tickets
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<SupportTicketSummaryDto>), 200)]
    public async Task<IActionResult> GetPaged([FromQuery] SupportTicketsFilterRequest request)
    {
        var filters = new Dictionary<string, object>();
        if (request.CompanyId.HasValue)
            filters["company_id"] = request.CompanyId.Value;
        if (!string.IsNullOrEmpty(request.Status))
            filters["status"] = request.Status;
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

        var pagedResponse = new PagedResponse<SupportTicketSummaryDto>(
            result.Value.Items,
            result.Value.TotalCount,
            request.PageNumber,
            request.PageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Get tickets by company
    /// </summary>
    [HttpGet("company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<SupportTicketSummaryDto>), 200)]
    public async Task<IActionResult> GetByCompany(Guid companyId, [FromQuery] string? status = null)
    {
        var result = await _service.GetByCompanyAsync(companyId, status);

        if (result.IsFailure)
            return StatusCode(500, result.Error!.Message);

        return Ok(result.Value);
    }

    /// <summary>
    /// Update a ticket
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupportTicketDto dto)
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
    /// Add admin message to a ticket
    /// </summary>
    [HttpPost("{id}/messages")]
    [ProducesResponseType(typeof(TicketMessageDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] CreateTicketMessageDto dto)
    {
        if (!CurrentUserId.HasValue)
            return Unauthorized();

        var result = await _service.AddMessageAsync(id, CurrentUserId.Value, "admin", dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    // ==================== FAQ ====================

    /// <summary>
    /// Get all FAQ items
    /// </summary>
    [HttpGet("faq")]
    [ProducesResponseType(typeof(IEnumerable<FaqItemDto>), 200)]
    public async Task<IActionResult> GetFaq([FromQuery] Guid? companyId = null, [FromQuery] string? category = null)
    {
        var result = await _service.GetFaqItemsAsync(companyId, category);

        if (result.IsFailure)
            return StatusCode(500, result.Error!.Message);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get FAQ item by ID
    /// </summary>
    [HttpGet("faq/{id}")]
    [ProducesResponseType(typeof(FaqItemDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFaqById(Guid id)
    {
        var result = await _service.GetFaqByIdAsync(id);

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
    /// Create a new FAQ item
    /// </summary>
    [HttpPost("faq")]
    [ProducesResponseType(typeof(FaqItemDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateFaq([FromBody] CreateFaqDto dto)
    {
        var result = await _service.CreateFaqAsync(dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return CreatedAtAction(nameof(GetFaqById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update a FAQ item
    /// </summary>
    [HttpPut("faq/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateFaq(Guid id, [FromBody] UpdateFaqDto dto)
    {
        var result = await _service.UpdateFaqAsync(id, dto);

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
    /// Delete a FAQ item
    /// </summary>
    [HttpDelete("faq/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteFaq(Guid id)
    {
        var result = await _service.DeleteFaqAsync(id);

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
/// Filter request for support tickets
/// </summary>
public class SupportTicketsFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
}
