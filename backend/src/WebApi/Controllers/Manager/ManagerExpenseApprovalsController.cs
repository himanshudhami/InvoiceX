using Application.DTOs.Expense;
using Application.Interfaces.Expense;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Common;

namespace WebApi.Controllers.Manager;

/// <summary>
/// Manager expense approval endpoints.
/// Allows managers to view and approve/reject expense claims from their team.
/// </summary>
[ApiController]
[Route("api/manager/expenses")]
[Produces("application/json")]
[Authorize(Policy = "ManagerOrAbove")]
public class ManagerExpenseApprovalsController : CompanyAuthorizedController
{
    private readonly IExpenseClaimService _expenseService;
    private readonly ILogger<ManagerExpenseApprovalsController> _logger;

    public ManagerExpenseApprovalsController(
        IExpenseClaimService expenseService,
        ILogger<ManagerExpenseApprovalsController> logger)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get pending expense claims from direct reports.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseClaimDto>), 200)]
    public async Task<IActionResult> GetPending()
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        var result = await _expenseService.GetPendingForManagerAsync(CurrentEmployeeId.Value);

        if (result.IsFailure)
        {
            return StatusCode(500, result.Error!.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a pending expense claim by ID.
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

        // Verify this is from manager's team (done via the GetPendingForManagerAsync check)
        // For now, verify company access
        if (!HasCompanyAccess(result.Value!.CompanyId))
        {
            return AccessDeniedDifferentCompanyResponse("Expense claim");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Approve an expense claim.
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Approve(Guid id)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        // Verify claim exists and is in pending status
        var claimResult = await _expenseService.GetByIdAsync(id);
        if (claimResult.IsFailure)
        {
            return NotFound(claimResult.Error!.Message);
        }

        if (!HasCompanyAccess(claimResult.Value!.CompanyId))
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
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        _logger.LogInformation(
            "Expense claim {ClaimId} approved by manager {ManagerId}",
            id, CurrentEmployeeId.Value);

        return Ok(result.Value);
    }

    /// <summary>
    /// Reject an expense claim.
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(ExpenseClaimDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectExpenseClaimDto dto)
    {
        if (!CurrentEmployeeId.HasValue)
        {
            return Unauthorized("Employee ID not found in token");
        }

        // Verify claim exists
        var claimResult = await _expenseService.GetByIdAsync(id);
        if (claimResult.IsFailure)
        {
            return NotFound(claimResult.Error!.Message);
        }

        if (!HasCompanyAccess(claimResult.Value!.CompanyId))
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
                ErrorType.Forbidden => StatusCode(403, result.Error.Message),
                _ => StatusCode(500, result.Error.Message)
            };
        }

        _logger.LogInformation(
            "Expense claim {ClaimId} rejected by manager {ManagerId}: {Reason}",
            id, CurrentEmployeeId.Value, dto.Reason);

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
        // Verify claim exists and manager has access
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
