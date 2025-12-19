using Application.Interfaces;
using Application.DTOs.EmployeeDocuments;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

/// <summary>
/// Employee Documents management endpoints (Admin)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "AdminHrOnly")]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly IEmployeeDocumentsService _service;

    public EmployeeDocumentsController(IEmployeeDocumentsService service)
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

    // ==================== Documents ====================

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeDocumentDetailDto), 200)]
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
    /// Get paginated documents
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<EmployeeDocumentSummaryDto>), 200)]
    public async Task<IActionResult> GetPaged([FromQuery] EmployeeDocumentsFilterRequest request)
    {
        var filters = new Dictionary<string, object>();
        if (request.CompanyId.HasValue)
            filters["company_id"] = request.CompanyId.Value;
        if (request.EmployeeId.HasValue)
            filters["employee_id"] = request.EmployeeId.Value;
        if (!string.IsNullOrEmpty(request.DocumentType))
            filters["document_type"] = request.DocumentType;
        if (request.IsCompanyWide.HasValue)
            filters["is_company_wide"] = request.IsCompanyWide.Value;

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

        var pagedResponse = new PagedResponse<EmployeeDocumentSummaryDto>(
            result.Value.Items,
            result.Value.TotalCount,
            request.PageNumber,
            request.PageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Get all documents for a company (includes company-specific and company-wide)
    /// </summary>
    [HttpGet("company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeDocumentSummaryDto>), 200)]
    public async Task<IActionResult> GetByCompany(Guid companyId)
    {
        var result = await _service.GetByCompanyAsync(companyId);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get documents for a specific employee
    /// </summary>
    [HttpGet("employee/{employeeId}/company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeDocumentSummaryDto>), 200)]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, Guid companyId, [FromQuery] string? documentType = null)
    {
        var result = await _service.GetByEmployeeAsync(employeeId, companyId, documentType);

        if (result.IsFailure)
            return StatusCode(500, result.Error!.Message);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get company-wide documents
    /// </summary>
    [HttpGet("company/{companyId}/company-wide")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeDocumentSummaryDto>), 200)]
    public async Task<IActionResult> GetCompanyWide(Guid companyId)
    {
        var result = await _service.GetCompanyWideAsync(companyId);

        if (result.IsFailure)
            return StatusCode(500, result.Error!.Message);

        return Ok(result.Value);
    }

    /// <summary>
    /// Upload a new document
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDocumentDetailDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDocumentDto dto)
    {
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
    /// Update a document
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDocumentDto dto)
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
    /// Delete a document
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

    // ==================== Document Requests ====================

    /// <summary>
    /// Get pending document requests for a company
    /// </summary>
    [HttpGet("requests/pending/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentRequestSummaryDto>), 200)]
    public async Task<IActionResult> GetPendingRequests(Guid companyId)
    {
        var result = await _service.GetPendingRequestsAsync(companyId);

        if (result.IsFailure)
            return StatusCode(500, result.Error!.Message);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get document request by ID
    /// </summary>
    [HttpGet("requests/{id}")]
    [ProducesResponseType(typeof(DocumentRequestSummaryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRequestById(Guid id)
    {
        var result = await _service.GetRequestByIdAsync(id);

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
    /// Process a document request (approve/reject)
    /// </summary>
    [HttpPut("requests/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ProcessRequest(Guid id, [FromBody] UpdateDocumentRequestDto dto)
    {
        if (!CurrentUserId.HasValue)
            return Unauthorized();

        var result = await _service.UpdateRequestAsync(id, CurrentUserId.Value, dto);

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
}

/// <summary>
/// Filter request for employee documents
/// </summary>
public class EmployeeDocumentsFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? DocumentType { get; set; }
    public bool? IsCompanyWide { get; set; }
}
