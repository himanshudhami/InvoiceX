using Application.DTOs.Document;
using Application.Interfaces.Document;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

/// <summary>
/// Document category management endpoints.
/// Provides CRUD operations for admin-defined document categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class DocumentCategoriesController : CompanyAuthorizedController
{
    private readonly IDocumentCategoryService _categoryService;
    private readonly ILogger<DocumentCategoriesController> _logger;

    public DocumentCategoriesController(
        IDocumentCategoryService categoryService,
        ILogger<DocumentCategoriesController> logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all document categories for a company.
    /// </summary>
    /// <param name="companyId">Optional company ID (Admin/HR can specify)</param>
    /// <param name="includeInactive">Include inactive categories</param>
    /// <returns>List of document categories</returns>
    [HttpGet("company/{companyId?}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCategoryDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetByCompany(Guid? companyId = null, [FromQuery] bool includeInactive = false)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _categoryService.GetByCompanyAsync(effectiveCompanyId.Value, includeInactive);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get active document categories for dropdown selection.
    /// </summary>
    /// <param name="companyId">Optional company ID</param>
    /// <returns>Simplified list for dropdowns</returns>
    [HttpGet("select")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCategorySelectDto>), 200)]
    public async Task<IActionResult> GetSelectList([FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _categoryService.GetSelectListAsync(effectiveCompanyId.Value);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get paginated document categories.
    /// </summary>
    [HttpGet("paged")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(PagedResponse<DocumentCategoryDto>), 200)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _categoryService.GetPagedAsync(
            effectiveCompanyId.Value, pageNumber, pageSize, searchTerm, includeInactive);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        var (items, totalCount) = result.Value;
        var pagedResponse = new PagedResponse<DocumentCategoryDto>(
            items,
            totalCount,
            pageNumber,
            pageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Get a document category by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentCategoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _categoryService.GetByIdAsync(id);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        // Verify company access
        if (!HasCompanyAccess(result.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Document category");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new document category.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(DocumentCategoryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentCategoryDto dto,
        [FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _categoryService.CreateAsync(effectiveCompanyId.Value, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Conflict => Conflict(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing document category.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(DocumentCategoryDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentCategoryDto dto)
    {
        // First check if category exists and user has access
        var existingResult = await _categoryService.GetByIdAsync(id);
        if (existingResult.IsFailure)
        {
            return NotFound(existingResult.Error!.Message);
        }

        if (!HasCompanyAccess(existingResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Document category");
        }

        var result = await _categoryService.UpdateAsync(id, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Conflict => Conflict(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a document category.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        // First check if category exists and user has access
        var existingResult = await _categoryService.GetByIdAsync(id);
        if (existingResult.IsFailure)
        {
            return NotFound(existingResult.Error!.Message);
        }

        if (!HasCompanyAccess(existingResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Document category");
        }

        var result = await _categoryService.DeleteAsync(id);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(new { message = "Category deleted successfully" });
    }

    /// <summary>
    /// Seed default categories for a company.
    /// </summary>
    [HttpPost("seed")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SeedDefaults([FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _categoryService.SeedDefaultCategoriesAsync(effectiveCompanyId.Value);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(new { message = "Default categories seeded successfully" });
    }
}
