namespace Application.DTOs.Payroll;

public class PayrollTransactionDto
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? SalaryStructureId { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public string PayrollType { get; set; } = "employee";

    // Attendance
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int LopDays { get; set; }

    // Earnings
    public decimal BasicEarned { get; set; }
    public decimal HraEarned { get; set; }
    public decimal DaEarned { get; set; }
    public decimal ConveyanceEarned { get; set; }
    public decimal MedicalEarned { get; set; }
    public decimal SpecialAllowanceEarned { get; set; }
    public decimal OtherAllowancesEarned { get; set; }
    public decimal LtaPaid { get; set; }
    public decimal BonusPaid { get; set; }
    public decimal Arrears { get; set; }
    public decimal Reimbursements { get; set; }
    public decimal Incentives { get; set; }
    public decimal OtherEarnings { get; set; }
    public decimal GrossEarnings { get; set; }

    // Deductions
    public decimal PfEmployee { get; set; }
    public decimal EsiEmployee { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TdsDeducted { get; set; }
    public decimal LoanRecovery { get; set; }
    public decimal AdvanceRecovery { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    // Net Pay
    public decimal NetPayable { get; set; }

    // Employer Contributions
    public decimal PfEmployer { get; set; }
    public decimal PfAdminCharges { get; set; }
    public decimal PfEdli { get; set; }
    public decimal EsiEmployer { get; set; }
    public decimal GratuityProvision { get; set; }
    public decimal TotalEmployerCost { get; set; }

    // TDS
    public string? TdsCalculation { get; set; }
    public decimal? TdsHrOverride { get; set; }
    public string? TdsOverrideReason { get; set; }

    // Payment
    public string Status { get; set; } = "computed";
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? BankAccount { get; set; }
    public string? Remarks { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public string? EmployeeName { get; set; }
    public string? CompanyName { get; set; }
    public string MonthName => new DateTime(PayrollYear, PayrollMonth, 1).ToString("MMMM");
}

public class CreatePayrollTransactionDto
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? SalaryStructureId { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public string PayrollType { get; set; } = "employee";

    // Attendance
    public int WorkingDays { get; set; } = 30;
    public int PresentDays { get; set; } = 30;
    public int LopDays { get; set; } = 0;

    // Variable Earnings
    public decimal LtaPaid { get; set; }
    public decimal BonusPaid { get; set; }
    public decimal Arrears { get; set; }
    public decimal Reimbursements { get; set; }
    public decimal Incentives { get; set; }
    public decimal OtherEarnings { get; set; }

    // Other Deductions
    public decimal LoanRecovery { get; set; }
    public decimal AdvanceRecovery { get; set; }
    public decimal OtherDeductions { get; set; }

    public string? Remarks { get; set; }
}

public class UpdatePayrollTransactionDto
{
    public int? WorkingDays { get; set; }
    public int? PresentDays { get; set; }
    public int? LopDays { get; set; }

    // Variable Earnings
    public decimal? LtaPaid { get; set; }
    public decimal? BonusPaid { get; set; }
    public decimal? Arrears { get; set; }
    public decimal? Reimbursements { get; set; }
    public decimal? Incentives { get; set; }
    public decimal? OtherEarnings { get; set; }

    // Other Deductions
    public decimal? LoanRecovery { get; set; }
    public decimal? AdvanceRecovery { get; set; }
    public decimal? OtherDeductions { get; set; }

    public string? Remarks { get; set; }
}

public class TdsOverrideDto
{
    public decimal TdsAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class PayslipDto
{
    public Guid TransactionId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string MonthYear { get; set; } = string.Empty;

    // Bank Details
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? BankIfsc { get; set; }

    // Statutory Numbers
    public string? Uan { get; set; }
    public string? PanNumber { get; set; }
    public string? EsiNumber { get; set; }

    // Attendance
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int LopDays { get; set; }

    // Earnings
    public List<PayslipLineItemDto> Earnings { get; set; } = new();
    public decimal TotalEarnings { get; set; }

    // Deductions
    public List<PayslipLineItemDto> Deductions { get; set; } = new();
    public decimal TotalDeductions { get; set; }

    // Net Pay
    public decimal NetPay { get; set; }
    public string NetPayInWords { get; set; } = string.Empty;

    // YTD Summary
    public decimal YtdGross { get; set; }
    public decimal YtdPf { get; set; }
    public decimal YtdPt { get; set; }
    public decimal YtdTds { get; set; }
}

public class PayslipLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
