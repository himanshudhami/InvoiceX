using Application.Services;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/tax-rule-packs")]
[Authorize]
public class TaxRulePacksController : ControllerBase
{
    private readonly TaxRulePackService _service;
    private readonly ILogger<TaxRulePacksController> _logger;

    public TaxRulePacksController(TaxRulePackService service, ILogger<TaxRulePacksController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all tax rule packs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaxRulePackDto>>> GetAll()
    {
        var packs = await _service.GetAllPacksAsync();
        return Ok(packs.Select(MapToDto));
    }

    /// <summary>
    /// Get tax rule pack by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxRulePackDto>> GetById(Guid id)
    {
        var pack = await _service.GetPackByIdAsync(id);
        if (pack == null) return NotFound();
        return Ok(MapToDto(pack));
    }

    /// <summary>
    /// Get active tax rule pack for a financial year
    /// </summary>
    [HttpGet("active/{financialYear}")]
    public async Task<ActionResult<TaxRulePackDto>> GetActive(string financialYear)
    {
        var pack = await _service.GetActivePackAsync(financialYear);
        if (pack == null) return NotFound($"No active rule pack for FY {financialYear}");
        return Ok(MapToDto(pack));
    }

    /// <summary>
    /// Get active tax rule pack for a specific date
    /// </summary>
    [HttpGet("active/date/{date}")]
    public async Task<ActionResult<TaxRulePackDto>> GetActiveForDate(DateTime date)
    {
        var pack = await _service.GetActivePackForDateAsync(date);
        if (pack == null) return NotFound($"No active rule pack for date {date:yyyy-MM-dd}");
        return Ok(MapToDto(pack));
    }

    /// <summary>
    /// Create a new tax rule pack
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaxRulePackDto>> Create([FromBody] CreateTaxRulePackDto dto)
    {
        var pack = MapFromCreateDto(dto);
        var username = User.Identity?.Name ?? "system";
        var created = await _service.CreatePackAsync(pack, username);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    /// <summary>
    /// Activate a tax rule pack (supersedes any currently active pack for same FY)
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult> Activate(Guid id)
    {
        var username = User.Identity?.Name ?? "system";
        var success = await _service.ActivatePackAsync(id, username);
        if (!success) return NotFound();
        return Ok(new { message = "Rule pack activated successfully" });
    }

    /// <summary>
    /// Delete a draft tax rule pack
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var success = await _service.DeletePackAsync(id);
        if (!success) return NotFound("Rule pack not found or cannot be deleted (only draft packs can be deleted)");
        return NoContent();
    }

    /// <summary>
    /// Calculate TDS for a payment
    /// </summary>
    [HttpPost("calculate/tds")]
    public async Task<ActionResult<TdsCalculationResult>> CalculateTds([FromBody] TdsCalculationRequest request)
    {
        var result = await _service.GetTdsRateAsync(
            request.SectionCode,
            request.PayeeType,
            request.Amount,
            request.HasPan,
            request.TransactionDate
        );

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Calculate income tax
    /// </summary>
    [HttpPost("calculate/income-tax")]
    public async Task<ActionResult<IncomeTaxCalculationResult>> CalculateIncomeTax([FromBody] IncomeTaxCalculationRequest request)
    {
        var result = await _service.CalculateIncomeTaxAsync(
            request.TaxableIncome,
            request.Regime,
            request.AgeCategory,
            request.FinancialYear
        );

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get PF/ESI rates for a financial year
    /// </summary>
    [HttpGet("pf-esi-rates/{financialYear}")]
    public async Task<ActionResult<PfEsiRates>> GetPfEsiRates(string financialYear)
    {
        var rates = await _service.GetPfEsiRatesAsync(financialYear);
        if (rates == null) return NotFound($"No PF/ESI rates found for FY {financialYear}");
        return Ok(rates);
    }

    /// <summary>
    /// Get current financial year
    /// </summary>
    [HttpGet("current-fy")]
    [AllowAnonymous]
    public ActionResult<string> GetCurrentFy()
    {
        var fy = TaxRulePackService.GetFinancialYear(DateTime.Today);
        return Ok(new { financialYear = fy });
    }

    #region DTOs

    private static TaxRulePackDto MapToDto(TaxRulePack pack)
    {
        return new TaxRulePackDto
        {
            Id = pack.Id,
            PackCode = pack.PackCode,
            PackName = pack.PackName,
            FinancialYear = pack.FinancialYear,
            Version = pack.Version,
            SourceNotification = pack.SourceNotification,
            Description = pack.Description,
            Status = pack.Status,
            IncomeTaxSlabs = pack.IncomeTaxSlabs?.RootElement,
            StandardDeductions = pack.StandardDeductions?.RootElement,
            RebateThresholds = pack.RebateThresholds?.RootElement,
            CessRates = pack.CessRates?.RootElement,
            SurchargeRates = pack.SurchargeRates?.RootElement,
            TdsRates = pack.TdsRates?.RootElement,
            PfEsiRates = pack.PfEsiRates?.RootElement,
            ProfessionalTaxConfig = pack.ProfessionalTaxConfig?.RootElement,
            GstRates = pack.GstRates?.RootElement,
            CreatedAt = pack.CreatedAt,
            CreatedBy = pack.CreatedBy,
            UpdatedAt = pack.UpdatedAt,
            UpdatedBy = pack.UpdatedBy,
            ActivatedAt = pack.ActivatedAt,
            ActivatedBy = pack.ActivatedBy
        };
    }

    private static TaxRulePack MapFromCreateDto(CreateTaxRulePackDto dto)
    {
        return new TaxRulePack
        {
            PackCode = dto.PackCode,
            PackName = dto.PackName,
            FinancialYear = dto.FinancialYear,
            SourceNotification = dto.SourceNotification,
            Description = dto.Description
        };
    }

    #endregion
}

#region DTOs

public class TaxRulePackDto
{
    public Guid Id { get; set; }
    public string PackCode { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? SourceNotification { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "draft";

    public System.Text.Json.JsonElement? IncomeTaxSlabs { get; set; }
    public System.Text.Json.JsonElement? StandardDeductions { get; set; }
    public System.Text.Json.JsonElement? RebateThresholds { get; set; }
    public System.Text.Json.JsonElement? CessRates { get; set; }
    public System.Text.Json.JsonElement? SurchargeRates { get; set; }
    public System.Text.Json.JsonElement? TdsRates { get; set; }
    public System.Text.Json.JsonElement? PfEsiRates { get; set; }
    public System.Text.Json.JsonElement? ProfessionalTaxConfig { get; set; }
    public System.Text.Json.JsonElement? GstRates { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivatedBy { get; set; }
}

public class CreateTaxRulePackDto
{
    public string PackCode { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public string? SourceNotification { get; set; }
    public string? Description { get; set; }
}

public class TdsCalculationRequest
{
    public string SectionCode { get; set; } = string.Empty;
    public string PayeeType { get; set; } = "individual";
    public decimal Amount { get; set; }
    public bool HasPan { get; set; } = true;
    public DateTime TransactionDate { get; set; } = DateTime.Today;
}

public class IncomeTaxCalculationRequest
{
    public decimal TaxableIncome { get; set; }
    public string Regime { get; set; } = "new";
    public string? AgeCategory { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
}

#endregion
