namespace Application.DTOs.Payroll;

/// <summary>
/// DTO for Professional Tax Slab
/// </summary>
public class ProfessionalTaxSlabDto
{
    public Guid Id { get; set; }
    public string State { get; set; } = string.Empty;
    public decimal MinMonthlyIncome { get; set; }
    public decimal? MaxMonthlyIncome { get; set; }
    public decimal MonthlyTax { get; set; }
    public decimal? FebruaryTax { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new Professional Tax Slab
/// </summary>
public class CreateProfessionalTaxSlabDto
{
    /// <summary>
    /// State name (e.g., 'Karnataka', 'Maharashtra')
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Minimum monthly income for this slab (inclusive)
    /// </summary>
    public decimal MinMonthlyIncome { get; set; }

    /// <summary>
    /// Maximum monthly income for this slab (null means no upper limit)
    /// </summary>
    public decimal? MaxMonthlyIncome { get; set; }

    /// <summary>
    /// Monthly professional tax amount
    /// </summary>
    public decimal MonthlyTax { get; set; }

    /// <summary>
    /// Special PT amount for February (optional - for states like Karnataka, Maharashtra)
    /// </summary>
    public decimal? FebruaryTax { get; set; }

    /// <summary>
    /// Date from which this slab is effective
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Date until which this slab is effective
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Whether the slab is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating a Professional Tax Slab
/// </summary>
public class UpdateProfessionalTaxSlabDto
{
    public string State { get; set; } = string.Empty;
    public decimal MinMonthlyIncome { get; set; }
    public decimal? MaxMonthlyIncome { get; set; }
    public decimal MonthlyTax { get; set; }
    public decimal? FebruaryTax { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// List of Indian states for PT slab configuration
/// </summary>
public static class IndianStates
{
    public static readonly string[] All = new[]
    {
        "Andhra Pradesh",
        "Arunachal Pradesh",
        "Assam",
        "Bihar",
        "Chhattisgarh",
        "Delhi",
        "Goa",
        "Gujarat",
        "Haryana",
        "Himachal Pradesh",
        "Jharkhand",
        "Karnataka",
        "Kerala",
        "Madhya Pradesh",
        "Maharashtra",
        "Manipur",
        "Meghalaya",
        "Mizoram",
        "Nagaland",
        "Odisha",
        "Punjab",
        "Rajasthan",
        "Sikkim",
        "Tamil Nadu",
        "Telangana",
        "Tripura",
        "Uttar Pradesh",
        "Uttarakhand",
        "West Bengal"
    };

    /// <summary>
    /// States that do NOT levy Professional Tax
    /// </summary>
    public static readonly string[] NoPtStates = new[]
    {
        "Delhi",
        "Haryana",
        "Himachal Pradesh",
        "Jammu and Kashmir",
        "Punjab",
        "Rajasthan",
        "Uttar Pradesh",
        "Uttarakhand"
    };
}
