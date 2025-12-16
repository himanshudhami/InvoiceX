namespace Application.DTOs.Payroll;

/// <summary>
/// DTO for payroll calculation line read operations
/// </summary>
public class PayrollCalculationLineDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public int LineSequence { get; set; }

    public string RuleCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal? BaseAmount { get; set; }
    public decimal? Rate { get; set; }
    public decimal ComputedAmount { get; set; }

    public string? ConfigVersion { get; set; }
    public string? ConfigSnapshot { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Formatted display string for the calculation
    /// </summary>
    public string CalculationDisplay => Rate.HasValue && BaseAmount.HasValue
        ? $"{BaseAmount:N2} × {Rate:N2}% = {ComputedAmount:N2}"
        : $"= {ComputedAmount:N2}";
}

/// <summary>
/// DTO for creating a calculation line (used internally during payroll processing)
/// </summary>
public class CreatePayrollCalculationLineDto
{
    public Guid TransactionId { get; set; }
    public string LineType { get; set; } = "earning";
    public int LineSequence { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? BaseAmount { get; set; }
    public decimal? Rate { get; set; }
    public decimal ComputedAmount { get; set; }
    public string? ConfigVersion { get; set; }
    public string? ConfigSnapshot { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Summary of calculation lines for a transaction
/// </summary>
public class PayrollCalculationSummaryDto
{
    public Guid TransactionId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string MonthYear { get; set; } = string.Empty;

    public List<PayrollCalculationLineDto> Earnings { get; set; } = new();
    public List<PayrollCalculationLineDto> Deductions { get; set; } = new();
    public List<PayrollCalculationLineDto> EmployerContributions { get; set; } = new();

    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalEmployerContributions { get; set; }
    public decimal NetPayable { get; set; }
}

/// <summary>
/// Grouped calculation breakdown for display
/// </summary>
public class CalculationBreakdownGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public List<CalculationLineItemDto> Items { get; set; } = new();
    public decimal GroupTotal { get; set; }
}

/// <summary>
/// Simplified line item for display
/// </summary>
public class CalculationLineItemDto
{
    public string RuleCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? BaseAmount { get; set; }
    public decimal? Rate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
