using Application.DTOs.Expense;
using Application.Interfaces.Expense;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers.Portal;

/// <summary>
/// Employee portal expense endpoints.
/// Allows employees to create, submit, and manage their own expense claims.
/// </summary>
[ApiController]
[Route("api/portal/expenses")]
[Produces("application/json")]
[Authorize]
public class PortalExpensesController : CompanyAuthorizedController
{
    private readonly IExpenseClaimService _expenseService;
    private readonly IExpenseCategoryService _categoryService;
    private readonly ILogger<PortalExpensesController> _logger;

    public PortalExpensesController(
        IExpenseClaimService expenseService,
        IExpenseCategoryService categoryService,
        ILogger<PortalExpensesController> logger)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get expense categories for dropdown.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseCategorySelectDto>), 200)]
    public async Task<IActionResult> GetCategories()
    {
        var companyId = GetEffectiveCompanyId();
        if (!companyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _categoryService.GetSelectListAsync(companyId.Value);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get my expense claims (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ExpenseClaimDto>), 200)]
    public async Task<IActionResult> GetMyExpenses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.GetPagedByEmployeeAsync(
            CurrentEmployeeId.Value, pageNumber, pageSize, status);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        var (items, totalCount) = result.Value;
        var pagedResponse = new PagedResponse<ExpenseClaimDto>(
            items,
            totalCount,
            pageNumber,
            pageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Get an expense claim by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.GetByIdAsync(id);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        // Employees can only view their own expenses
        if (result.Value!.EmployeeId != CurrentEmployeeId.Value)
        {
            return StatusCode(403, "You can only view your own expense claims");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new expense claim (draft).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseClaimDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseClaimDto dto)
    {
        var companyId = GetEffectiveCompanyId();
        if (!companyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.CreateAsync(companyId.Value, CurrentEmployeeId.Value, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.NotFound => NotFound(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update a draft expense claim.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseClaimDto dto)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.UpdateAsync(id, CurrentEmployeeId.Value, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Submit an expense claim for approval.
    /// </summary>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Submit(Guid id)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.SubmitAsync(id, CurrentEmployeeId.Value);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancel an expense claim.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.CancelAsync(id, CurrentEmployeeId.Value);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(new { message = "Expense claim cancelled successfully" });
    }

    /// <summary>
    /// Delete a draft expense claim.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.DeleteAsync(id, CurrentEmployeeId.Value);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(new { message = "Expense claim deleted successfully" });
    }

    /// <summary>
    /// Get attachments for an expense claim.
    /// </summary>
    [HttpGet("{id}/attachments")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseAttachmentDto>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAttachments(Guid id)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        // First verify the claim belongs to this employee
        var claimResult = await _expenseService.GetByIdAsync(id);
        if (claimResult.IsFailure)
        {
            return NotFound(claimResult.Error!.Message);
        }

        if (claimResult.Value!.EmployeeId != CurrentEmployeeId.Value)
        {
            return StatusCode(403, "You can only view attachments for your own expense claims");
        }

        var result = await _expenseService.GetAttachmentsAsync(id);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Add an attachment to a draft expense claim.
    /// </summary>
    [HttpPost("{id}/attachments")]
    [ProducesResponseType(typeof(ExpenseAttachmentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddAttachment(Guid id, [FromBody] AddExpenseAttachmentDto dto)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.AddAttachmentAsync(id, CurrentEmployeeId.Value, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Created($"/api/portal/expenses/{id}/attachments/{result.Value!.Id}", result.Value);
    }

    /// <summary>
    /// Remove an attachment from a draft expense claim.
    /// </summary>
    [HttpDelete("{id}/attachments/{attachmentId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveAttachment(Guid id, Guid attachmentId)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.RemoveAttachmentAsync(id, attachmentId, CurrentEmployeeId.Value);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(new { message = "Attachment removed successfully" });
    }
}
