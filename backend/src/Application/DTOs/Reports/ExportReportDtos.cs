namespace Application.DTOs.Reports
{
    // ==================== Receivables Ageing ====================

    /// <summary>
    /// Export receivables ageing report
    /// </summary>
    public class ExportReceivablesAgeingReportDto
    {
        public Guid CompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Summary in primary currency (USD)
        public string PrimaryCurrency { get; set; } = "USD";
        public decimal TotalReceivablesForeign { get; set; }
        public decimal TotalReceivablesInr { get; set; }

        // Ageing buckets in foreign currency
        public decimal Current { get; set; }          // 0-30 days
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Days91To180 { get; set; }
        public decimal Days181To270 { get; set; }     // Approaching FEMA deadline
        public decimal Over270Days { get; set; }       // FEMA overdue

        // Ageing buckets in INR
        public decimal CurrentInr { get; set; }
        public decimal Days31To60Inr { get; set; }
        public decimal Days61To90Inr { get; set; }
        public decimal Days91To180Inr { get; set; }
        public decimal Days181To270Inr { get; set; }
        public decimal Over270DaysInr { get; set; }

        // Invoice counts per bucket
        public int CurrentCount { get; set; }
        public int Days31To60Count { get; set; }
        public int Days61To90Count { get; set; }
        public int Days91To180Count { get; set; }
        public int Days181To270Count { get; set; }
        public int Over270DaysCount { get; set; }

        // Currency breakdown
        public Dictionary<string, CurrencyAgeingDto> CurrencyBreakdown { get; set; } = new();

        // Detailed items
        public List<AgeingInvoiceDto> Invoices { get; set; } = new();
    }

    /// <summary>
    /// Ageing by currency
    /// </summary>
    public class CurrencyAgeingDto
    {
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountInr { get; set; }
        public int InvoiceCount { get; set; }
        public decimal Current { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90Days { get; set; }
    }

    /// <summary>
    /// Individual invoice in ageing report
    /// </summary>
    public class AgeingInvoiceDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public DateOnly DueDate { get; set; }
        public int DaysOutstanding { get; set; }
        public string AgeingBucket { get; set; } = string.Empty;
        public Guid PartyId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal InvoiceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal OutstandingAmountInr { get; set; }
        public DateOnly FemaDeadline { get; set; }
        public int DaysToFemaDeadline { get; set; }
        public bool IsFemaOverdue { get; set; }
    }

    /// <summary>
    /// Customer-wise export receivables
    /// </summary>
    public class CustomerExportReceivableDto
    {
        public Guid PartyId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Country { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalOutstandingInr { get; set; }
        public string PrimaryCurrency { get; set; } = "USD";
        public int OldestInvoiceDays { get; set; }
        public int FemaOverdueCount { get; set; }
        public decimal FemaOverdueAmount { get; set; }
    }

    // ==================== Forex Reports ====================

    /// <summary>
    /// Forex gain/loss report
    /// </summary>
    public class ForexGainLossReportDto
    {
        public Guid CompanyId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Realized gains/losses (from settled transactions)
        public decimal RealizedGainTotal { get; set; }
        public decimal RealizedLossTotal { get; set; }
        public decimal NetRealizedGainLoss => RealizedGainTotal - RealizedLossTotal;

        // Unrealized gains/losses (from open positions)
        public decimal UnrealizedGainTotal { get; set; }
        public decimal UnrealizedLossTotal { get; set; }
        public decimal NetUnrealizedGainLoss => UnrealizedGainTotal - UnrealizedLossTotal;

        // Total
        public decimal TotalGainLoss => NetRealizedGainLoss + NetUnrealizedGainLoss;

        // Transaction counts
        public int RealizedTransactionCount { get; set; }
        public int UnrealizedPositionCount { get; set; }

        // Detailed transactions
        public List<ForexTransactionDetailDto> RealizedTransactions { get; set; } = new();
        public List<ForexPositionDetailDto> UnrealizedPositions { get; set; } = new();

        // Monthly trend
        public List<MonthlyForexSummaryDto> MonthlyTrend { get; set; } = new();
    }

    /// <summary>
    /// Individual forex transaction detail
    /// </summary>
    public class ForexTransactionDetailDto
    {
        public Guid TransactionId { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;  // booking, settlement
        public string DocumentNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public decimal BookingRate { get; set; }
        public decimal SettlementRate { get; set; }
        public decimal BookingAmountInr { get; set; }
        public decimal SettlementAmountInr { get; set; }
        public decimal GainLoss { get; set; }
        public bool IsGain => GainLoss > 0;
    }

    /// <summary>
    /// Open forex position
    /// </summary>
    public class ForexPositionDetailDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal OutstandingForeign { get; set; }
        public decimal BookingRate { get; set; }
        public decimal CurrentRate { get; set; }
        public decimal BookingAmountInr { get; set; }
        public decimal CurrentAmountInr { get; set; }
        public decimal UnrealizedGainLoss { get; set; }
    }

    /// <summary>
    /// Monthly forex summary
    /// </summary>
    public class MonthlyForexSummaryDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal RealizedGainLoss { get; set; }
        public decimal UnrealizedGainLoss { get; set; }
        public decimal TotalGainLoss { get; set; }
        public int TransactionCount { get; set; }
    }

    /// <summary>
    /// Unrealized forex position summary
    /// </summary>
    public class UnrealizedForexPositionDto
    {
        public Guid CompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public decimal CurrentExchangeRate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Position summary
        public decimal TotalOpenPositionForeign { get; set; }
        public decimal TotalOpenPositionInrAtBooking { get; set; }
        public decimal TotalOpenPositionInrAtCurrent { get; set; }
        public decimal TotalUnrealizedGainLoss { get; set; }

        // By currency
        public Dictionary<string, CurrencyForexPositionDto> CurrencyBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Forex position by currency
    /// </summary>
    public class CurrencyForexPositionDto
    {
        public string Currency { get; set; } = string.Empty;
        public decimal OpenAmount { get; set; }
        public decimal AvgBookingRate { get; set; }
        public decimal CurrentRate { get; set; }
        public decimal BookingValueInr { get; set; }
        public decimal CurrentValueInr { get; set; }
        public decimal UnrealizedGainLoss { get; set; }
    }

    // ==================== FEMA Compliance ====================

    /// <summary>
    /// FEMA compliance dashboard
    /// </summary>
    public class FemaComplianceDashboardDto
    {
        public Guid CompanyId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Overall status
        public string OverallStatus { get; set; } = "compliant";  // compliant, warning, critical, non_compliant
        public int ComplianceScore { get; set; }  // 0-100

        // Export receivables summary
        public decimal TotalExportReceivables { get; set; }
        public string PrimaryCurrency { get; set; } = "USD";
        public int TotalOpenInvoices { get; set; }

        // Realization status
        public int FullyRealizedCount { get; set; }
        public int PartiallyRealizedCount { get; set; }
        public int PendingRealizationCount { get; set; }
        public int OverdueCount { get; set; }

        public decimal FullyRealizedAmount { get; set; }
        public decimal PartiallyRealizedAmount { get; set; }
        public decimal PendingRealizationAmount { get; set; }
        public decimal OverdueAmount { get; set; }

        // FIRC status
        public int FircsReceived { get; set; }
        public int FircsPending { get; set; }
        public decimal FircsCoverage { get; set; }  // Percentage

        // EDPMS status
        public int EdpmsReported { get; set; }
        public int EdpmsPending { get; set; }

        // LUT status
        public bool HasActiveLut { get; set; }
        public string? ActiveLutNumber { get; set; }
        public int? DaysToLutExpiry { get; set; }

        // Alerts
        public int CriticalAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public List<FemaViolationAlertDto> TopAlerts { get; set; } = new();

        // Trend
        public List<MonthlyComplianceTrendDto> ComplianceTrend { get; set; } = new();
    }

    /// <summary>
    /// FEMA violation alert
    /// </summary>
    public class FemaViolationAlertDto
    {
        public string AlertType { get; set; } = string.Empty;  // overdue, approaching_deadline, missing_firc, edpms_pending, lut_expiring
        public string Severity { get; set; } = "info";  // info, warning, critical
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }  // invoice, firc, lut
        public string? DocumentNumber { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public int? DaysOverdue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Monthly compliance trend
    /// </summary>
    public class MonthlyComplianceTrendDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int ComplianceScore { get; set; }
        public decimal ExportValue { get; set; }
        public decimal RealizedValue { get; set; }
        public double RealizationRate { get; set; }
    }

    // ==================== Export Realization ====================

    /// <summary>
    /// Export realization report
    /// </summary>
    public class ExportRealizationReportDto
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Summary
        public int TotalExportInvoices { get; set; }
        public decimal TotalExportValue { get; set; }
        public decimal TotalRealizedValue { get; set; }
        public decimal TotalPendingValue { get; set; }
        public string PrimaryCurrency { get; set; } = "USD";

        // Realization metrics
        public double RealizationPercentage { get; set; }
        public double AvgRealizationDays { get; set; }
        public decimal AvgExchangeRate { get; set; }

        // By status
        public List<RealizationStatusSummaryDto> ByStatus { get; set; } = new();

        // By customer
        public List<CustomerRealizationDto> ByCustomer { get; set; } = new();

        // Monthly breakdown
        public List<MonthlyRealizationDto> MonthlyBreakdown { get; set; } = new();

        // At-risk invoices (approaching or past deadline)
        public List<AtRiskInvoiceDto> AtRiskInvoices { get; set; } = new();
    }

    /// <summary>
    /// Realization summary by status
    /// </summary>
    public class RealizationStatusSummaryDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInr { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Customer realization summary
    /// </summary>
    public class CustomerRealizationDto
    {
        public Guid PartyId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalExportValue { get; set; }
        public decimal RealizedValue { get; set; }
        public decimal PendingValue { get; set; }
        public double RealizationPercentage { get; set; }
        public int AvgRealizationDays { get; set; }
    }

    /// <summary>
    /// Monthly realization data
    /// </summary>
    public class MonthlyRealizationDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal InvoicedAmount { get; set; }
        public decimal RealizedAmount { get; set; }
        public double RealizationPercentage { get; set; }
    }

    /// <summary>
    /// At-risk invoice
    /// </summary>
    public class AtRiskInvoiceDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public DateOnly FemaDeadline { get; set; }
        public int DaysToDeadline { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal OutstandingAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;  // low, medium, high, critical
    }

    /// <summary>
    /// Monthly realization trend
    /// </summary>
    public class MonthlyRealizationTrendDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Invoiced { get; set; }
        public decimal Realized { get; set; }
        public decimal Outstanding { get; set; }
        public double RealizationRate { get; set; }
        public int AvgDaysToRealize { get; set; }
    }

    // ==================== Combined Dashboard ====================

    /// <summary>
    /// Comprehensive export dashboard
    /// </summary>
    public class ExportDashboardDto
    {
        public Guid CompanyId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Key metrics
        public decimal TotalExportRevenueFy { get; set; }
        public decimal TotalExportReceivables { get; set; }
        public decimal TotalRealizedFy { get; set; }
        public string PrimaryCurrency { get; set; } = "USD";

        // Forex summary
        public decimal NetForexGainLossFy { get; set; }
        public decimal UnrealizedForexPosition { get; set; }

        // Compliance summary
        public int FemaComplianceScore { get; set; }
        public bool HasActiveLut { get; set; }
        public int PendingFircs { get; set; }
        public int OverdueInvoices { get; set; }

        // Quick stats
        public int TotalCustomers { get; set; }
        public int TotalInvoicesFy { get; set; }
        public double AvgRealizationDays { get; set; }
        public decimal AvgExchangeRate { get; set; }

        // Alerts count
        public int CriticalAlerts { get; set; }
        public int WarningAlerts { get; set; }

        // Charts data
        public List<MonthlyRealizationTrendDto> RealizationTrend { get; set; } = new();
        public List<MonthlyForexSummaryDto> ForexTrend { get; set; } = new();
        public Dictionary<string, decimal> ReceivablesByCustomer { get; set; } = new();
        public Dictionary<string, decimal> ReceivablesByCurrency { get; set; } = new();
    }
}
