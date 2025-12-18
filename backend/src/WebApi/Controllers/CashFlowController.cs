using Application.DTOs.CashFlow;
using Application.Interfaces;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebApi.Controllers;

/// <summary>
/// Cash Flow Statement endpoints for AS-3 compliant financial reporting
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CashFlowController : ControllerBase
{
    private readonly ICashFlowService _cashFlowService;

    /// <summary>
    /// Initializes a new instance of the CashFlowController
    /// </summary>
    public CashFlowController(ICashFlowService cashFlowService)
    {
        _cashFlowService = cashFlowService ?? throw new ArgumentNullException(nameof(cashFlowService));
    }

    /// <summary>
    /// Get cash flow statement for a given period
    /// </summary>
    /// <param name="companyId">Optional company ID to filter by</param>
    /// <param name="year">Year for the cash flow statement (required)</param>
    /// <param name="month">Optional month (1-12) to filter by specific month</param>
    /// <returns>Cash flow statement DTO</returns>
    /// <response code="200">Returns cash flow statement successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(CashFlowStatementDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetCashFlowStatement(
        [FromQuery] Guid? companyId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        // Validate year
        if (!year.HasValue)
        {
            year = DateTime.Now.Year;
        }

        if (year.Value < 2000 || year.Value > 2100)
        {
            return BadRequest("Year must be between 2000 and 2100");
        }

        // Validate month if provided
        if (month.HasValue && (month.Value < 1 || month.Value > 12))
        {
            return BadRequest("Month must be between 1 and 12");
        }

        var result = await _cashFlowService.GetCashFlowStatementAsync(companyId, year.Value, month);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Internal => StatusCode(500, result.Error.Message),
                _ => BadRequest(result.Error.Message)
            };
        }

        return Ok(result.Value);
    }
}






