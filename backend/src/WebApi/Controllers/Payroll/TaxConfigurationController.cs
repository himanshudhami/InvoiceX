using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Payroll;

/// <summary>
/// Tax configuration read-only endpoints (tax slabs and PT slabs)
/// </summary>
[ApiController]
[Route("api/payroll/tax-config")]
[Produces("application/json")]
public class TaxConfigurationController : ControllerBase
{
    private readonly ITaxSlabRepository _taxSlabRepository;
    private readonly IProfessionalTaxSlabRepository _ptSlabRepository;

    public TaxConfigurationController(
        ITaxSlabRepository taxSlabRepository,
        IProfessionalTaxSlabRepository ptSlabRepository)
    {
        _taxSlabRepository = taxSlabRepository ?? throw new ArgumentNullException(nameof(taxSlabRepository));
        _ptSlabRepository = ptSlabRepository ?? throw new ArgumentNullException(nameof(ptSlabRepository));
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
}



