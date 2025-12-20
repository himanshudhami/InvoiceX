using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll;

/// <summary>
/// Abstraction for tax rate data sources.
/// Allows switching between legacy database tables and Tax Rule Packs.
/// </summary>
public interface ITaxRateProvider
{
    /// <summary>
    /// Provider name for logging and debugging
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Get active rule pack ID if using rule packs (null for legacy provider)
    /// </summary>
    Task<Guid?> GetActiveRulePackIdAsync(string financialYear);

    /// <summary>
    /// Get income tax slabs for a specific regime, financial year, and taxpayer category
    /// </summary>
    /// <param name="regime">"old" or "new"</param>
    /// <param name="financialYear">e.g., "2024-25"</param>
    /// <param name="category">"all", "senior", or "super_senior"</param>
    Task<IEnumerable<TaxSlabInfo>> GetTaxSlabsAsync(string regime, string financialYear, string category = "all");

    /// <summary>
    /// Get all tax parameters for a regime and financial year
    /// </summary>
    Task<Dictionary<string, decimal>> GetTaxParametersAsync(string regime, string financialYear);

    /// <summary>
    /// Get TDS rate for a specific section
    /// </summary>
    /// <param name="sectionCode">e.g., "194J", "194C", "194H"</param>
    /// <param name="payeeType">"individual", "company", "huf", "partnership"</param>
    /// <param name="hasPan">Whether payee has provided PAN</param>
    /// <param name="transactionDate">Date of transaction for FY determination</param>
    Task<TdsRateInfo?> GetTdsRateAsync(string sectionCode, string payeeType, bool hasPan, DateTime transactionDate);
}

/// <summary>
/// Normalized tax slab information
/// </summary>
public class TaxSlabInfo
{
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal Rate { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// TDS rate information for a specific section
/// </summary>
public class TdsRateInfo
{
    public string SectionCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal? ThresholdAmount { get; set; }
    public string? ThresholdType { get; set; } // "per_transaction" or "annual"
    public bool IsExemptBelowThreshold { get; set; }
}
