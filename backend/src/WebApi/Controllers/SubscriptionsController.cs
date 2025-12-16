using Application.DTOs.Subscriptions;
using Application.Interfaces;
using Application.Services.Subscriptions;
using Core.Common;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionsService _service;
    private readonly SubscriptionExpenseService _expenseService;

    public SubscriptionsController(ISubscriptionsService service, SubscriptionExpenseService expenseService)
    {
        _service = service;
        _expenseService = expenseService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Subscriptions), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<Subscriptions>), 200)]
    public async Task<IActionResult> GetPaged([FromQuery] SubscriptionsFilterRequest request)
    {
        var result = await _service.GetPagedAsync(request.PageNumber, request.PageSize, request.SearchTerm, request.SortBy, request.SortDescending, request.GetFilters());
        if (result.IsFailure) return FromError(result.Error!);
        var (items, total) = result.Value;
        return Ok(new PagedResponse<Subscriptions>(items, total, request.PageNumber, request.PageSize));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Subscriptions), 201)]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubscriptionDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpGet("{id}/assignments")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionAssignments>), 200)]
    public async Task<IActionResult> GetAssignments(Guid id)
    {
        var result = await _service.GetAssignmentsAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{id}/assign")]
    [ProducesResponseType(typeof(SubscriptionAssignments), 201)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] CreateSubscriptionAssignmentDto dto)
    {
        var result = await _service.AddAssignmentAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetAssignments), new { id }, result.Value);
    }

    [HttpPost("assignments/{assignmentId}/revoke")]
    public async Task<IActionResult> Revoke(Guid assignmentId, [FromBody] RevokeSubscriptionAssignmentDto dto)
    {
        var result = await _service.RevokeAssignmentAsync(assignmentId, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpPost("{id}/pause")]
    public async Task<IActionResult> Pause(Guid id, [FromBody] PauseSubscriptionDto dto)
    {
        var result = await _service.PauseSubscriptionAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpPost("{id}/resume")]
    public async Task<IActionResult> Resume(Guid id, [FromBody] ResumeSubscriptionDto dto)
    {
        var result = await _service.ResumeSubscriptionAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelSubscriptionDto dto)
    {
        var result = await _service.CancelSubscriptionAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return NoContent();
    }

    [HttpGet("expenses/monthly")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionMonthlyExpenseDto>), 200)]
    public async Task<IActionResult> GetMonthlyExpenses(
        [FromQuery] int year,
        [FromQuery] int? month = null,
        [FromQuery] Guid? companyId = null)
    {
        var result = await _expenseService.GetMonthlyExpensesAsync(year, month, companyId);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("expenses/report")]
    [ProducesResponseType(typeof(SubscriptionCostReportDto), 200)]
    public async Task<IActionResult> GetCostReport([FromQuery] Guid? companyId = null)
    {
        var result = await _expenseService.GetCostReportAsync(companyId);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    private IActionResult FromError(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => BadRequest(error.Message),
            ErrorType.NotFound => NotFound(error.Message),
            _ => StatusCode(500, error.Message)
        };
}




