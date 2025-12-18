namespace Application.DTOs.Payroll;

public class EmployeePayrollInfoDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public string? Uan { get; set; }
    public string? PfAccountNumber { get; set; }
    public string? EsiNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankIfsc { get; set; }
    public string TaxRegime { get; set; } = "new";
    public string? PanNumber { get; set; }
    public string PayrollType { get; set; } = "employee";
    public bool IsPfApplicable { get; set; }
    public bool IsEsiApplicable { get; set; }
    public bool IsPtApplicable { get; set; }
    public bool OptedForRestrictedPf { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public DateTime? DateOfLeaving { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Compliance Fields
    public string ResidentialStatus { get; set; } = "resident";
    public DateTime? DateOfBirth { get; set; }
    public DateTime? TaxRegimeEffectiveFrom { get; set; }
    public string? WorkState { get; set; }

    // Computed properties for tax benefits
    public int? Age => DateOfBirth.HasValue
        ? (int)((DateTime.Today - DateOfBirth.Value).TotalDays / 365.25)
        : null;

    public bool IsSeniorCitizen => Age.HasValue && Age.Value >= 60;
    public bool IsSuperSeniorCitizen => Age.HasValue && Age.Value >= 80;

    // Navigation
    public string? EmployeeName { get; set; }
    public string? CompanyName { get; set; }
}

public class CreateEmployeePayrollInfoDto
{
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public string? Uan { get; set; }
    public string? PfAccountNumber { get; set; }
    public string? EsiNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankIfsc { get; set; }
    public string TaxRegime { get; set; } = "new";
    public string? PanNumber { get; set; }
    public string PayrollType { get; set; } = "employee";
    public bool IsPfApplicable { get; set; } = true;
    public bool IsEsiApplicable { get; set; } = false;
    public bool IsPtApplicable { get; set; } = true;
    public bool OptedForRestrictedPf { get; set; } = false;
    public DateTime? DateOfJoining { get; set; }

    // Compliance Fields
    public string ResidentialStatus { get; set; } = "resident";
    public DateTime? DateOfBirth { get; set; }
    public DateTime? TaxRegimeEffectiveFrom { get; set; }
    public string? WorkState { get; set; }
}

public class UpdateEmployeePayrollInfoDto
{
    public string? Uan { get; set; }
    public string? PfAccountNumber { get; set; }
    public string? EsiNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankIfsc { get; set; }
    public string? TaxRegime { get; set; }
    public string? PanNumber { get; set; }
    public string? PayrollType { get; set; }
    public bool? IsPfApplicable { get; set; }
    public bool? IsEsiApplicable { get; set; }
    public bool? IsPtApplicable { get; set; }
    public bool? OptedForRestrictedPf { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public DateTime? DateOfLeaving { get; set; }
    public bool? IsActive { get; set; }

    // Compliance Fields
    public string? ResidentialStatus { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? TaxRegimeEffectiveFrom { get; set; }
    public string? WorkState { get; set; }
}
