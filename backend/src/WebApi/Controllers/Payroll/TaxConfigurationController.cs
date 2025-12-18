using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Core.Common;
using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// Tax configuration endpoints (tax slabs and PT slabs) - read and write operations
/// </summary>
[ApiController]
[Route("api/payroll/tax-config")]
[Produces("application/json")]
public class TaxConfigurationController : ControllerBase
{
    private readonly ITaxSlabRepository _taxSlabRepository;
    private readonly IProfessionalTaxSlabRepository _ptSlabRepository;
    private readonly IProfessionalTaxSlabService _ptSlabService;

    public TaxConfigurationController(
        ITaxSlabRepository taxSlabRepository,
        IProfessionalTaxSlabRepository ptSlabRepository,
        IProfessionalTaxSlabService ptSlabService)
    {
        _taxSlabRepository = taxSlabRepository ?? throw new ArgumentNullException(nameof(taxSlabRepository));
        _ptSlabRepository = ptSlabRepository ?? throw new ArgumentNullException(nameof(ptSlabRepository));
        _ptSlabService = ptSlabService ?? throw new ArgumentNullException(nameof(ptSlabService));
    }

    /// <summary>
    /// Get all tax slabs
    /// </summary>
    [HttpGet("tax-slabs")]
    [ProducesResponseType(typeof(IEnumerable<TaxSlab>), 200)]
    public async Task<IActionResult> GetTaxSlabs(
        [FromQuery] string? regime = null,
        [FromQuery] string? financialYear = null)
    {
        IEnumerable<TaxSlab> slabs;

        if (!string.IsNullOrEmpty(regime) && !string.IsNullOrEmpty(financialYear))
        {
            slabs = await _taxSlabRepository.GetByRegimeAndYearAsync(regime, financialYear);
        }
        else if (!string.IsNullOrEmpty(financialYear))
        {
            slabs = await _taxSlabRepository.GetByFinancialYearAsync(financialYear);
        }
        else
        {
            slabs = await _taxSlabRepository.GetAllAsync();
        }

        // Filter to active only
        slabs = slabs.Where(s => s.IsActive).OrderBy(s => s.MinIncome);
        return Ok(slabs);
    }

    /// <summary>
    /// Get tax slab for a specific income amount
    /// </summary>
    [HttpGet("tax-slabs/for-income")]
    [ProducesResponseType(typeof(TaxSlab), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTaxSlabForIncome(
        [FromQuery] decimal income,
        [FromQuery] string regime = "new",
        [FromQuery] string financialYear = "2024-25")
    {
        var slab = await _taxSlabRepository.GetSlabForIncomeAsync(income, regime, financialYear);
        if (slab == null)
            return NotFound($"No tax slab found for income {income} with regime {regime} for FY {financialYear}");

        return Ok(slab);
    }

    /// <summary>
    /// Get all professional tax slabs
    /// </summary>
    [HttpGet("professional-tax-slabs")]
    [ProducesResponseType(typeof(IEnumerable<ProfessionalTaxSlab>), 200)]
    public async Task<IActionResult> GetProfessionalTaxSlabs([FromQuery] string? state = null)
    {
        IEnumerable<ProfessionalTaxSlab> slabs;

        if (!string.IsNullOrEmpty(state))
        {
            slabs = await _ptSlabRepository.GetByStateAsync(state);
        }
        else
        {
            slabs = await _ptSlabRepository.GetAllAsync();
        }

        // Filter to active only and order by income
        slabs = slabs.Where(s => s.IsActive).OrderBy(s => s.MinMonthlyIncome);
        return Ok(slabs);
    }

    /// <summary>
    /// Get professional tax slab for a specific monthly income
    /// </summary>
    [HttpGet("professional-tax-slabs/for-income")]
    [ProducesResponseType(typeof(ProfessionalTaxSlab), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfessionalTaxSlabForIncome(
        [FromQuery] decimal monthlyIncome,
        [FromQuery] string state = "Karnataka")
    {
        var slab = await _ptSlabRepository.GetSlabForIncomeAsync(monthlyIncome, state);
        if (slab == null)
            return NotFound($"No professional tax slab found for monthly income {monthlyIncome} in state {state}");

        return Ok(slab);
    }

    /// <summary>
    /// Get all distinct states that have PT slabs configured
    /// </summary>
    [HttpGet("professional-tax-slabs/states")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    public async Task<IActionResult> GetDistinctStates()
    {
        var result = await _ptSlabService.GetDistinctStatesAsync();
        return Ok(result.Value);
    }

    /// <summary>
    /// Get a professional tax slab by ID
    /// </summary>
    [HttpGet("professional-tax-slabs/{id:guid}")]
    [ProducesResponseType(typeof(ProfessionalTaxSlabDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfessionalTaxSlabById(Guid id)
    {
        var result = await _ptSlabService.GetByIdAsync(id);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, "An error occurred")
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new professional tax slab
    /// </summary>
    [HttpPost("professional-tax-slabs")]
    [ProducesResponseType(typeof(ProfessionalTaxSlabDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateProfessionalTaxSlab([FromBody] CreateProfessionalTaxSlabDto dto)
    {
        var result = await _ptSlabService.CreateAsync(dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Conflict => Conflict(result.Error.Message),
                _ => StatusCode(500, "An error occurred")
            };
        }

        return CreatedAtAction(
            nameof(GetProfessionalTaxSlabById),
            new { id = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Update an existing professional tax slab
    /// </summary>
    [HttpPut("professional-tax-slabs/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdateProfessionalTaxSlab(Guid id, [FromBody] UpdateProfessionalTaxSlabDto dto)
    {
        var result = await _ptSlabService.UpdateAsync(id, dto);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                ErrorType.Conflict => Conflict(result.Error.Message),
                _ => StatusCode(500, "An error occurred")
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Delete a professional tax slab
    /// </summary>
    [HttpDelete("professional-tax-slabs/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteProfessionalTaxSlab(Guid id)
    {
        var result = await _ptSlabService.DeleteAsync(id);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error.Message),
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, "An error occurred")
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Bulk create professional tax slabs (for imports)
    /// </summary>
    [HttpPost("professional-tax-slabs/bulk")]
    [ProducesResponseType(typeof(IEnumerable<ProfessionalTaxSlabDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BulkCreateProfessionalTaxSlabs([FromBody] IEnumerable<CreateProfessionalTaxSlabDto> dtos)
    {
        var result = await _ptSlabService.BulkCreateAsync(dtos);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error.Message),
                _ => StatusCode(500, "An error occurred")
            };
        }

        return StatusCode(201, result.Value);
    }

    /// <summary>
    /// Get the list of Indian states
    /// </summary>
    [HttpGet("indian-states")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    public IActionResult GetIndianStates()
    {
        return Ok(IndianStates.All);
    }

    /// <summary>
    /// Get the list of states that do NOT levy Professional Tax
    /// </summary>
    [HttpGet("no-pt-states")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    public IActionResult GetNoPtStates()
    {
        return Ok(IndianStates.NoPtStates);
    }
}





