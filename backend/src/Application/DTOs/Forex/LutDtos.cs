namespace Application.DTOs.Forex
{
    /// <summary>
    /// DTO for creating a new LUT entry
    /// </summary>
    public class CreateLutDto
    {
        public Guid CompanyId { get; set; }
        public required string LutNumber { get; set; }
        public required string FinancialYear { get; set; }
        public required string Gstin { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }
        public DateOnly? FilingDate { get; set; }
        public string? Arn { get; set; }
        public string? Notes { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// DTO for updating a LUT entry
    /// </summary>
    public class UpdateLutDto
    {
        public string? LutNumber { get; set; }
        public DateOnly? FilingDate { get; set; }
        public string? Arn { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Result of LUT validation for an invoice
    /// </summary>
    public class LutValidationResultDto
    {
        public bool IsValid { get; set; }
        public Guid? LutId { get; set; }
        public string? LutNumber { get; set; }
        public string? FinancialYear { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public string? Message { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// LUT utilization report
    /// </summary>
    public class LutUtilizationReportDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // LUT details
        public Guid? LutId { get; set; }
        public string? LutNumber { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public string? LutStatus { get; set; }

        // Export summary
        public int TotalExportInvoices { get; set; }
        public decimal TotalExportValueForeign { get; set; }
        public decimal TotalExportValueInr { get; set; }
        public string PrimaryCurrency { get; set; } = "USD";

        // Breakdown by currency
        public Dictionary<string, CurrencyExportSummaryDto> CurrencyBreakdown { get; set; } = new();

        // Monthly breakdown
        public List<MonthlyExportSummaryDto> MonthlyBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Export summary by currency
    /// </summary>
    public class CurrencyExportSummaryDto
    {
        public string Currency { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountInr { get; set; }
    }

    /// <summary>
    /// Monthly export summary
    /// </summary>
    public class MonthlyExportSummaryDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalAmountForeign { get; set; }
        public decimal TotalAmountInr { get; set; }
    }

    /// <summary>
    /// LUT compliance summary
    /// </summary>
    public class LutComplianceSummaryDto
    {
        public Guid CompanyId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Current LUT status
        public bool HasActiveLut { get; set; }
        public Guid? ActiveLutId { get; set; }
        public string? ActiveLutNumber { get; set; }
        public DateOnly? ActiveLutExpiry { get; set; }
        public int? DaysUntilExpiry { get; set; }

        // Historical summary
        public int TotalLutsIssued { get; set; }
        public int ActiveLuts { get; set; }
        public int ExpiredLuts { get; set; }
        public int CancelledLuts { get; set; }

        // Compliance status
        public bool IsCompliant { get; set; }
        public List<string> ComplianceIssues { get; set; } = new();

        // Export coverage
        public int ExportInvoicesUnderLut { get; set; }
        public int ExportInvoicesWithoutLut { get; set; }
        public decimal ExportValueUnderLut { get; set; }
        public decimal ExportValueWithoutLut { get; set; }
    }

    /// <summary>
    /// LUT expiry alert
    /// </summary>
    public class LutExpiryAlertDto
    {
        public Guid LutId { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string LutNumber { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public DateOnly ValidTo { get; set; }
        public int DaysUntilExpiry { get; set; }
        public string AlertLevel { get; set; } = "normal";  // normal, warning, critical, expired
    }
}
