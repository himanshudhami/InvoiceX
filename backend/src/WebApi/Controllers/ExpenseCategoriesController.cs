using Application.DTOs.Expense;
using Application.Interfaces.Expense;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

/// <summary>
/// Expense category management endpoints.
/// Provides CRUD operations for admin-defined expense categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ExpenseCategoriesController : CompanyAuthorizedController
{
    private readonly IExpenseCategoryService _categoryService;
    private readonly ILogger<ExpenseCategoriesController> _logger;

    public ExpenseCategoriesController(
        IExpenseCategoryService categoryService,
        ILogger<ExpenseCategoriesController> logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all expense categories for a company.
    /// </summary>
    /// <param name="companyId">Optional company ID (Admin/HR can specify)</param>
    /// <param name="includeInactive">Include inactive categories</param>
    /// <returns>List of expense categories</returns>
    [HttpGet("company/{companyId?}")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseCategoryDto>), 200)]
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
    /// Get active expense categories for dropdown selection.
    /// </summary>
    /// <param name="companyId">Optional company ID</param>
    /// <returns>Simplified list for dropdowns</returns>
    [HttpGet("select")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseCategorySelectDto>), 200)]
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
    /// Get paginated expense categories.
    /// </summary>
    [HttpGet("paged")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(PagedResponse<ExpenseCategoryDto>), 200)]
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
        var pagedResponse = new PagedResponse<ExpenseCategoryDto>(
            items,
            totalCount,
            pageNumber,
            pageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Get an expense category by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExpenseCategoryDto), 200)]
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
            return AccessDeniedDifferentCompanyResponse("Expense category");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new expense category.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(ExpenseCategoryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseCategoryDto dto,
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
    /// Update an existing expense category.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(typeof(ExpenseCategoryDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseCategoryDto dto)
    {
        // First check if category exists and user has access
        var existingResult = await _categoryService.GetByIdAsync(id);
        if (existingResult.IsFailure)
        {
            return NotFound(existingResult.Error!.Message);
        }

        if (!HasCompanyAccess(existingResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Expense category");
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
    /// Delete an expense category.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminHrOnly")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
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
            return AccessDeniedDifferentCompanyResponse("Expense category");
        }

        var result = await _categoryService.DeleteAsync(id);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
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

        return Ok(new { message = "Default expense categories seeded successfully" });
    }
}
