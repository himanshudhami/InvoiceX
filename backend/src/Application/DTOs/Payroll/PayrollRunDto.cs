namespace Application.DTOs.Payroll;

public class PayrollRunDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public int TotalEmployees { get; set; }
    public int TotalContractors { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal TotalEmployerPf { get; set; }
    public decimal TotalEmployerEsi { get; set; }
    public decimal TotalEmployerCost { get; set; }
    public string? ComputedBy { get; set; }
    public DateTime? ComputedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMode { get; set; }
    public string? Remarks { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public string? CompanyName { get; set; }
    public string MonthName => new DateTime(PayrollYear, PayrollMonth, 1).ToString("MMMM");
}

public class CreatePayrollRunDto
{
    public Guid CompanyId { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public string? Remarks { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdatePayrollRunDto
{
    public string? Status { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMode { get; set; }
    public string? Remarks { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Bank account ID for disbursement journal entry auto-posting
    /// </summary>
    public Guid? BankAccountId { get; set; }
}

/// <summary>
/// Result DTO for payroll approval operation
/// </summary>
public class PayrollApprovalResultDto
{
    public Guid PayrollRunId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? AccrualJournalEntryId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result DTO for payroll payment operation
/// </summary>
public class PayrollPaymentResultDto
{
    public Guid PayrollRunId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? DisbursementJournalEntryId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ProcessPayrollDto
{
    public Guid CompanyId { get; set; }
    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }
    public bool IncludeContractors { get; set; } = true;
    public string? ProcessedBy { get; set; }
}

public class PayrollRunSummaryDto
{
    public Guid PayrollRunId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int TotalContractors { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalEmployerCost { get; set; }

    // Statutory breakdown
    public decimal TotalPfEmployee { get; set; }
    public decimal TotalPfEmployer { get; set; }
    public decimal TotalEsiEmployee { get; set; }
    public decimal TotalEsiEmployer { get; set; }
    public decimal TotalPt { get; set; }
    public decimal TotalTds { get; set; }
}
