using Application.DTOs.Expense;
using Application.Interfaces.Expense;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

/// <summary>
/// Expense claims management endpoints (Admin view).
/// Provides read and reimbursement operations for all expense claims.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "AdminHrOnly")]
public class ExpenseClaimsController : CompanyAuthorizedController
{
    private readonly IExpenseClaimService _expenseService;
    private readonly ILogger<ExpenseClaimsController> _logger;

    public ExpenseClaimsController(
        IExpenseClaimService expenseService,
        ILogger<ExpenseClaimsController> logger)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get paginated expense claims for a company.
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<ExpenseClaimDto>), 200)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] ExpenseClaimFilterRequest request,
        [FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _expenseService.GetPagedAsync(effectiveCompanyId.Value, request);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        var (items, totalCount) = result.Value;
        var pagedResponse = new PagedResponse<ExpenseClaimDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Ok(pagedResponse);
    }

    /// <summary>
    /// Get an expense claim by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _expenseService.GetByIdAsync(id);

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
            return AccessDeniedDifferentCompanyResponse("Expense claim");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get expense claims by status.
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseClaimDto>), 200)]
    public async Task<IActionResult> GetByStatus(string status, [FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var request = new ExpenseClaimFilterRequest
        {
            Status = status,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _expenseService.GetPagedAsync(effectiveCompanyId.Value, request);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value.Items);
    }

    /// <summary>
    /// Get expense summary for a company.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ExpenseSummaryDto), 200)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? companyId = null)
    {
        var effectiveCompanyId = GetEffectiveCompanyId(companyId);
        if (!effectiveCompanyId.HasValue)
        {
            return CompanyIdNotFoundResponse();
        }

        var result = await _expenseService.GetSummaryAsync(effectiveCompanyId.Value, fromDate, toDate);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Mark an expense claim as reimbursed (Accountant/Admin).
    /// </summary>
    [HttpPost("{id}/reimburse")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reimburse(Guid id, [FromBody] ReimburseExpenseClaimDto dto)
    {
        // First check if claim exists and user has access
        var existingResult = await _expenseService.GetByIdAsync(id);
        if (existingResult.IsFailure)
        {
            return NotFound(existingResult.Error!.Message);
        }

        if (!HasCompanyAccess(existingResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Expense claim");
        }

        var result = await _expenseService.ReimburseAsync(id, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Approve an expense claim (Manager/Admin).
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(Guid id)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        // First check if claim exists and user has access
        var existingResult = await _expenseService.GetByIdAsync(id);
        if (existingResult.IsFailure)
        {
            return NotFound(existingResult.Error!.Message);
        }

        if (!HasCompanyAccess(existingResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Expense claim");
        }

        var result = await _expenseService.ApproveAsync(id, CurrentEmployeeId.Value);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Reject an expense claim (Manager/Admin).
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectExpenseClaimDto dto)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        // First check if claim exists and user has access
        var existingResult = await _expenseService.GetByIdAsync(id);
        if (existingResult.IsFailure)
        {
            return NotFound(existingResult.Error!.Message);
        }

        if (!HasCompanyAccess(existingResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Expense claim");
        }

        var result = await _expenseService.RejectAsync(id, CurrentEmployeeId.Value, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get attachments for an expense claim.
    /// </summary>
    [HttpGet("{id}/attachments")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseAttachmentDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAttachments(Guid id)
    {
        // First verify the claim exists and user has access
        var claimResult = await _expenseService.GetByIdAsync(id);
        if (claimResult.IsFailure)
        {
            return NotFound(claimResult.Error!.Message);
        }

        if (!HasCompanyAccess(claimResult.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Expense claim");
        }

        var result = await _expenseService.GetAttachmentsAsync(id);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }
}
