namespace Application.DTOs.Payroll;

/// <summary>
/// DTO for salary component read operations
/// </summary>
public class SalaryComponentDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = "earning";

    // Calculation Flags
    public bool IsPfWage { get; set; }
    public bool IsEsiWage { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsPtWage { get; set; }

    // Proration
    public bool ApplyProration { get; set; }
    public string ProrationBasis { get; set; } = "calendar_days";

    // Display
    public int DisplayOrder { get; set; }
    public bool ShowOnPayslip { get; set; }
    public string? PayslipGroup { get; set; }

    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Computed display value for UI
    public string WageBasesDisplay => string.Join(", ", GetWageBases());

    private IEnumerable<string> GetWageBases()
    {
        if (IsPfWage) yield return "PF";
        if (IsEsiWage) yield return "ESI";
        if (IsTaxable) yield return "Tax";
        if (IsPtWage) yield return "PT";
    }
}

/// <summary>
/// DTO for creating a new salary component
/// </summary>
public class CreateSalaryComponentDto
{
    public Guid? CompanyId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = "earning";

    public bool IsPfWage { get; set; }
    public bool IsEsiWage { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsPtWage { get; set; }

    public bool ApplyProration { get; set; } = true;
    public string ProrationBasis { get; set; } = "calendar_days";

    public int DisplayOrder { get; set; } = 100;
    public bool ShowOnPayslip { get; set; } = true;
    public string? PayslipGroup { get; set; }

    public string? CreatedBy { get; set; }
}

/// <summary>
/// DTO for updating an existing salary component
/// </summary>
public class UpdateSalaryComponentDto
{
    public string? ComponentName { get; set; }
    public string? ComponentType { get; set; }

    public bool? IsPfWage { get; set; }
    public bool? IsEsiWage { get; set; }
    public bool? IsTaxable { get; set; }
    public bool? IsPtWage { get; set; }

    public bool? ApplyProration { get; set; }
    public string? ProrationBasis { get; set; }

    public int? DisplayOrder { get; set; }
    public bool? ShowOnPayslip { get; set; }
    public string? PayslipGroup { get; set; }

    public bool? IsActive { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// DTO for a simplified component list with wage flags
/// </summary>
public class SalaryComponentWageFlagsDto
{
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public bool IsPfWage { get; set; }
    public bool IsEsiWage { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsPtWage { get; set; }
    public bool ApplyProration { get; set; }
}
