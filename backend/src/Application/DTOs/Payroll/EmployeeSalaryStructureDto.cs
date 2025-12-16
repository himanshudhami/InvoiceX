namespace Application.DTOs.Payroll;

public class EmployeeSalaryStructureDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal AnnualCtc { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Hra { get; set; }
    public decimal DearnessAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal LtaAnnual { get; set; }
    public decimal BonusAnnual { get; set; }
    public decimal PfEmployerMonthly { get; set; }
    public decimal EsiEmployerMonthly { get; set; }
    public decimal GratuityMonthly { get; set; }
    public decimal MonthlyGross { get; set; }
    public bool IsActive { get; set; }
    public string? RevisionReason { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public string? EmployeeName { get; set; }
    public string? CompanyName { get; set; }
}

public class CreateEmployeeSalaryStructureDto
{
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public decimal AnnualCtc { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Hra { get; set; }
    public decimal DearnessAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal LtaAnnual { get; set; }
    public decimal BonusAnnual { get; set; }
    public decimal PfEmployerMonthly { get; set; }
    public decimal EsiEmployerMonthly { get; set; }
    public decimal GratuityMonthly { get; set; }
    public string? RevisionReason { get; set; }
    public string? ApprovedBy { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateEmployeeSalaryStructureDto
{
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal? AnnualCtc { get; set; }
    public decimal? BasicSalary { get; set; }
    public decimal? Hra { get; set; }
    public decimal? DearnessAllowance { get; set; }
    public decimal? ConveyanceAllowance { get; set; }
    public decimal? MedicalAllowance { get; set; }
    public decimal? SpecialAllowance { get; set; }
    public decimal? OtherAllowances { get; set; }
    public decimal? LtaAnnual { get; set; }
    public decimal? BonusAnnual { get; set; }
    public decimal? PfEmployerMonthly { get; set; }
    public decimal? EsiEmployerMonthly { get; set; }
    public decimal? GratuityMonthly { get; set; }
    public bool? IsActive { get; set; }
    public string? RevisionReason { get; set; }
    public string? ApprovedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class SalaryBreakdownDto
{
    public decimal AnnualCtc { get; set; }
    public decimal MonthlyCtc { get; set; }

    // Monthly Components
    public decimal BasicSalary { get; set; }
    public decimal Hra { get; set; }
    public decimal DearnessAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal MonthlyGross { get; set; }

    // Employer Contributions
    public decimal PfEmployer { get; set; }
    public decimal EsiEmployer { get; set; }
    public decimal Gratuity { get; set; }

    // Annual Components
    public decimal LtaAnnual { get; set; }
    public decimal BonusAnnual { get; set; }
}
