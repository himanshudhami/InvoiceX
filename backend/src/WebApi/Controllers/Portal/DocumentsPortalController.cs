using Application.Interfaces;
using Application.DTOs.EmployeeDocuments;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Portal;

/// <summary>
/// Employee Portal Documents API endpoints
/// </summary>
[ApiController]
[Route("api/portal/documents")]
[Produces("application/json")]
[Authorize]
public class DocumentsPortalController : ControllerBase
{
    private readonly IEmployeeDocumentsService _service;

    public DocumentsPortalController(IEmployeeDocumentsService service)
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

    // ==================== Documents ====================

    /// <summary>
    /// Get all documents for the employee (including company-wide)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeDocumentSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetMyDocuments([FromQuery] string? documentType = null)
    {
        if (CurrentEmployeeId == null || CurrentCompanyId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetByEmployeeAsync(CurrentEmployeeId.Value, CurrentCompanyId.Value, documentType);
        return HandleResult(result);
    }

    /// <summary>
    /// Get document detail by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeDocumentDetailDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDocumentDetail(Guid id)
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetByIdAsync(id);
        return HandleResult(result);
    }

    // ==================== Document Requests ====================

    /// <summary>
    /// Get all document requests for the employee
    /// </summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(IEnumerable<DocumentRequestSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetMyRequests()
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.GetRequestsByEmployeeAsync(CurrentEmployeeId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a document request
    /// </summary>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(DocumentRequestSummaryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateDocumentRequestDto dto)
    {
        if (CurrentEmployeeId == null)
            return StatusCode(403, new { error = "Your account is not linked to an employee record" });

        var result = await _service.CreateRequestAsync(CurrentEmployeeId.Value, dto);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetMyRequests), result.Value);

        return result.Error!.Type switch
        {
            ErrorType.Validation => BadRequest(new { error = result.Error.Message }),
            _ => StatusCode(500, new { error = result.Error.Message })
        };
    }
}
