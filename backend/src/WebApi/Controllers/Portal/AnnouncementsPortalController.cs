using Application.Interfaces;
using Application.DTOs.Announcements;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Portal;

/// <summary>
/// Employee Portal Announcements API endpoints
/// </summary>
[ApiController]
[Route("api/portal/announcements")]
[Produces("application/json")]
[Authorize]
public class AnnouncementsPortalController : ControllerBase
{
    private readonly IAnnouncementsService _service;

    public AnnouncementsPortalController(IAnnouncementsService service)
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

    /// <summary>
    /// Get all active announcements for the employee
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AnnouncementSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAnnouncements()
    {
        if (CurrentEmployeeId == null || CurrentCompanyId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetActiveForEmployeeAsync(CurrentCompanyId.Value, CurrentEmployeeId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Get announcement detail by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AnnouncementDetailDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetByIdAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Get unread announcements count
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (CurrentEmployeeId == null || CurrentCompanyId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetUnreadCountAsync(CurrentCompanyId.Value, CurrentEmployeeId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Mark an announcement as read
    /// </summary>
    [HttpPost("{id}/read")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.MarkAsReadAsync(id, CurrentEmployeeId.Value);

        if (result.IsSuccess)
            return Ok(new { message = "Marked as read" });

        return result.Error!.Type switch
        {
            ErrorType.NotFound => NotFound(new { error = result.Error.Message }),
            _ => StatusCode(500, new { error = result.Error.Message })
        };
    }
}
