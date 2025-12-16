namespace Application.DTOs.Payroll;

/// <summary>
/// DTO for tax parameter read operations
/// </summary>
public class TaxParameterDto
{
    public Guid Id { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Regime { get; set; } = "new";
    public string ParameterCode { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public decimal ParameterValue { get; set; }
    public string ParameterType { get; set; } = "amount";
    public string? Description { get; set; }
    public string? LegalReference { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Display-friendly formatted value based on parameter type
    /// </summary>
    public string FormattedValue => ParameterType switch
    {
        "percentage" => $"{ParameterValue}%",
        "amount" => $"₹{ParameterValue:N0}",
        "threshold" => $"₹{ParameterValue:N0}",
        _ => ParameterValue.ToString("N2")
    };
}

/// <summary>
/// DTO for creating a new tax parameter
/// </summary>
public class CreateTaxParameterDto
{
    public string FinancialYear { get; set; } = string.Empty;
    public string Regime { get; set; } = "new";
    public string ParameterCode { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public decimal ParameterValue { get; set; }
    public string ParameterType { get; set; } = "amount";
    public string? Description { get; set; }
    public string? LegalReference { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// DTO for updating an existing tax parameter
/// </summary>
public class UpdateTaxParameterDto
{
    public string? ParameterName { get; set; }
    public decimal? ParameterValue { get; set; }
    public string? Description { get; set; }
    public string? LegalReference { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// DTO for retrieving all parameters for a regime/year as key-value pairs
/// </summary>
public class TaxParametersLookupDto
{
    public string FinancialYear { get; set; } = string.Empty;
    public string Regime { get; set; } = string.Empty;
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}
