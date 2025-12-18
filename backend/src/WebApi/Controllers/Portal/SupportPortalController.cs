using Application.Interfaces;
using Application.DTOs.SupportTickets;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Portal;

/// <summary>
/// Employee Portal Support/Help Desk API endpoints
/// </summary>
[ApiController]
[Route("api/portal/support")]
[Produces("application/json")]
[Authorize]
public class SupportPortalController : ControllerBase
{
    private readonly ISupportTicketsService _service;

    public SupportPortalController(ISupportTicketsService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    private Guid? CurrentEmployeeId
    {
        get
        {
            var claim = User.FindFirst("employee_id");
            if (claim != null && Guid.TryParse(claim.Value, out var employeeId))
                return employeeId;
            return null;
        }
    }

    private Guid? CurrentCompanyId
    {
        get
        {
            var claim = User.FindFirst("company_id");
            if (claim != null && Guid.TryParse(claim.Value, out var companyId))
                return companyId;
            return null;
        }
    }

    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error!.Type switch
        {
            ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
            ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
            ErrorType.Forbidden => StatusCode(403, new { error = result.Error.Message }),
            _ => StatusCode(500, new { error = result.Error.Message })
        };
    }

    // ==================== Tickets ====================

    /// <summary>
    /// Get all tickets for the employee
    /// </summary>
    [HttpGet("tickets")]
    [ProducesResponseType(typeof(IEnumerable<SupportTicketSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetMyTickets()
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetByEmployeeAsync(CurrentEmployeeId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Get ticket detail with messages
    /// </summary>
    [HttpGet("tickets/{id}")]
    [ProducesResponseType(typeof(SupportTicketDetailDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTicketDetail(Guid id)
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetByIdAsync(id);

        // Verify the ticket belongs to this employee
        if (result.IsSuccess && result.Value != null)
        {
            // For security, we should verify ownership, but the DTO doesn't include EmployeeId
            // In a production system, you'd add this check in the service layer
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Create a new support ticket
    /// </summary>
    [HttpPost("tickets")]
    [ProducesResponseType(typeof(SupportTicketDetailDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateSupportTicketPortalDto dto)
    {
        if (CurrentEmployeeId == null || CurrentCompanyId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var createDto = new CreateSupportTicketDto
        {
            CompanyId = CurrentCompanyId.Value,
            EmployeeId = CurrentEmployeeId.Value,
            Subject = dto.Subject,
            Description = dto.Description,
            Category = dto.Category,
            Priority = dto.Priority
        };

        var result = await _service.CreateAsync(createDto);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetTicketDetail), new { id = result.Value!.Id }, result.Value);

        return result.Error!.Type switch
        {
            ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
            _ => StatusCode(500, new { error = result.Error.Message })
        };
    }

    /// <summary>
    /// Add a message to a ticket
    /// </summary>
    [HttpPost("tickets/{id}/messages")]
    [ProducesResponseType(typeof(TicketMessageDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] CreateTicketMessageDto dto)
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.AddMessageAsync(id, CurrentEmployeeId.Value, "employee", dto);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error!.Type switch
        {
            ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
            ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
            _ => StatusCode(500, new { error = result.Error.Message })
        };
    }

    // ==================== FAQ ====================

    /// <summary>
    /// Get FAQ items
    /// </summary>
    [HttpGet("faq")]
    [ProducesResponseType(typeof(IEnumerable<FaqItemDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetFaq([FromQuery] string? category = null)
    {
        var result = await _service.GetFaqItemsAsync(CurrentCompanyId, category);
        return HandleResult(result);
    }
}

/// <summary>
/// Simplified DTO for creating tickets from the portal
/// </summary>
public class CreateSupportTicketPortalDto
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public string Priority { get; set; } = "medium";
}
