using Application.DTOs.Loans;
using Application.Interfaces;
using Core.Common;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;
using WebApi.DTOs.Common;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LoansController : ControllerBase
{
    private readonly ILoansService _service;

    public LoansController(ILoansService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Loan), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponse<Loan>), 200)]
    public async Task<IActionResult> GetPaged([FromQuery] LoansFilterRequest request)
    {
        var result = await _service.GetPagedAsync(request.PageNumber, request.PageSize, request.SearchTerm, request.SortBy, request.SortDescending, request.GetFilters());
        if (result.IsFailure) return FromError(result.Error!);
        var (items, total) = result.Value;
        return Ok(new PagedResponse<Loan>(items, total, request.PageNumber, request.PageSize));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Loan), 201)]
    public async Task<IActionResult> Create([FromBody] CreateLoanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsFailure) return FromError(result.Error!);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLoanDto dto)
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

    [HttpGet("{id}/schedule")]
    [ProducesResponseType(typeof(LoanScheduleDto), 200)]
    public async Task<IActionResult> GetSchedule(Guid id)
    {
        var result = await _service.GetScheduleAsync(id);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{id}/emi-payment")]
    [ProducesResponseType(typeof(Loan), 200)]
    public async Task<IActionResult> RecordEmiPayment(Guid id, [FromBody] CreateEmiPaymentDto dto)
    {
        var result = await _service.RecordEmiPaymentAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{id}/prepayment")]
    [ProducesResponseType(typeof(Loan), 200)]
    public async Task<IActionResult> RecordPrepayment(Guid id, [FromBody] PrepaymentDto dto)
    {
        var result = await _service.RecordPrepaymentAsync(id, dto);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpPost("{id}/foreclose")]
    [ProducesResponseType(typeof(Loan), 200)]
    public async Task<IActionResult> Foreclose(Guid id, [FromBody] string? notes = null)
    {
        var result = await _service.ForecloseLoanAsync(id, notes);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("outstanding")]
    [ProducesResponseType(typeof(IEnumerable<Loan>), 200)]
    public async Task<IActionResult> GetOutstanding([FromQuery] Guid? companyId = null)
    {
        var result = await _service.GetOutstandingLoansAsync(companyId);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("{id}/interest")]
    [ProducesResponseType(typeof(decimal), 200)]
    public async Task<IActionResult> GetTotalInterestPaid(Guid id, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var result = await _service.GetTotalInterestPaidAsync(id, fromDate, toDate);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    [HttpGet("interest-payments")]
    [ProducesResponseType(typeof(IEnumerable<LoanTransaction>), 200)]
    public async Task<IActionResult> GetInterestPayments([FromQuery] Guid? companyId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var result = await _service.GetInterestPaymentsAsync(companyId, fromDate, toDate);
        if (result.IsFailure) return FromError(result.Error!);
        return Ok(result.Value);
    }

    private IActionResult FromError(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new { error.Message }),
            ErrorType.Validation => BadRequest(new { error.Message }),
            ErrorType.Unauthorized => Unauthorized(new { error.Message }),
            _ => StatusCode(500, new { error.Message })
        };
    }
}

