using Core.Entities.Gst;

namespace Core.Interfaces.Gst
{
    /// <summary>
    /// Repository interface for GSTR-2B operations
    /// </summary>
    public interface IGstr2bRepository
    {
        // ==================== Imports ====================

        Task<Gstr2bImport?> GetImportByIdAsync(Guid id);
        Task<Gstr2bImport?> GetImportByPeriodAsync(Guid companyId, string returnPeriod);
        Task<Gstr2bImport?> GetImportByHashAsync(Guid companyId, string returnPeriod, string fileHash);
        Task<IEnumerable<Gstr2bImport>> GetImportsByCompanyAsync(Guid companyId);
        Task<(IEnumerable<Gstr2bImport> Items, int TotalCount)> GetImportsPagedAsync(
            Guid companyId, int pageNumber, int pageSize, string? status = null);
        Task<Gstr2bImport> AddImportAsync(Gstr2bImport import);
        Task UpdateImportAsync(Gstr2bImport import);
        Task UpdateImportStatusAsync(Guid importId, string status, string? errorMessage = null);
        Task UpdateImportSummaryAsync(Guid importId, int total, int matched, int unmatched, int partial);
        Task DeleteImportAsync(Guid id);

        // ==================== Invoices ====================

        Task<Gstr2bInvoice?> GetInvoiceByIdAsync(Guid id);
        Task<IEnumerable<Gstr2bInvoice>> GetInvoicesByImportAsync(Guid importId);
        Task<(IEnumerable<Gstr2bInvoice> Items, int TotalCount)> GetInvoicesPagedAsync(
            Guid importId, int pageNumber, int pageSize,
            string? matchStatus = null, string? invoiceType = null, string? searchTerm = null);
        Task<IEnumerable<Gstr2bInvoice>> GetUnmatchedInvoicesAsync(Guid companyId, string returnPeriod);
        Task<IEnumerable<Gstr2bInvoice>> GetInvoicesByMatchStatusAsync(Guid importId, string matchStatus);
        Task<Gstr2bInvoice> AddInvoiceAsync(Gstr2bInvoice invoice);
        Task BulkInsertInvoicesAsync(IEnumerable<Gstr2bInvoice> invoices);
        Task UpdateInvoiceAsync(Gstr2bInvoice invoice);
        Task UpdateInvoiceMatchAsync(Guid invoiceId, string matchStatus, Guid? matchedVendorInvoiceId,
            int? matchConfidence, string? matchDetails, string? matchDiscrepancies);
        Task UpdateInvoiceActionAsync(Guid invoiceId, string actionStatus, Guid userId, string? notes);
        Task DeleteInvoicesByImportAsync(Guid importId);

        // ==================== Reconciliation Rules ====================

        Task<IEnumerable<Gstr2bReconciliationRule>> GetReconciliationRulesAsync(Guid? companyId = null);
        Task<Gstr2bReconciliationRule?> GetReconciliationRuleByCodeAsync(string ruleCode);

        // ==================== Summary Queries ====================

        Task<Gstr2bReconciliationSummary> GetReconciliationSummaryAsync(Guid companyId, string returnPeriod);
        Task<IEnumerable<Gstr2bSupplierSummary>> GetSupplierWiseSummaryAsync(Guid companyId, string returnPeriod);
        Task<Gstr2bItcSummary> GetItcSummaryAsync(Guid companyId, string returnPeriod);
    }

    /// <summary>
    /// Reconciliation summary DTO
    /// </summary>
    public class Gstr2bReconciliationSummary
    {
        public string ReturnPeriod { get; set; } = string.Empty;
        public int TotalInvoices { get; set; }
        public int MatchedInvoices { get; set; }
        public int PartialMatchInvoices { get; set; }
        public int UnmatchedInvoices { get; set; }
        public int AcceptedInvoices { get; set; }
        public int RejectedInvoices { get; set; }
        public int PendingReviewInvoices { get; set; }

        public decimal TotalTaxableValue { get; set; }
        public decimal MatchedTaxableValue { get; set; }
        public decimal UnmatchedTaxableValue { get; set; }

        public decimal TotalItcAvailable { get; set; }
        public decimal MatchedItc { get; set; }
        public decimal UnmatchedItc { get; set; }

        public decimal MatchPercentage => TotalInvoices > 0
            ? Math.Round((decimal)MatchedInvoices / TotalInvoices * 100, 2)
            : 0;
    }

    /// <summary>
    /// Supplier-wise summary
    /// </summary>
    public class Gstr2bSupplierSummary
    {
        public string SupplierGstin { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public int InvoiceCount { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public decimal TotalTaxableValue { get; set; }
        public decimal TotalItc { get; set; }
    }

    /// <summary>
    /// ITC summary for the period
    /// </summary>
    public class Gstr2bItcSummary
    {
        public string ReturnPeriod { get; set; } = string.Empty;

        // As per GSTR-2B
        public decimal Gstr2bItcIgst { get; set; }
        public decimal Gstr2bItcCgst { get; set; }
        public decimal Gstr2bItcSgst { get; set; }
        public decimal Gstr2bItcCess { get; set; }
        public decimal Gstr2bItcTotal { get; set; }

        // As per books (vendor invoices)
        public decimal BooksItcIgst { get; set; }
        public decimal BooksItcCgst { get; set; }
        public decimal BooksItcSgst { get; set; }
        public decimal BooksItcCess { get; set; }
        public decimal BooksItcTotal { get; set; }

        // Difference
        public decimal DifferenceIgst => Gstr2bItcIgst - BooksItcIgst;
        public decimal DifferenceCgst => Gstr2bItcCgst - BooksItcCgst;
        public decimal DifferenceSgst => Gstr2bItcSgst - BooksItcSgst;
        public decimal DifferenceCess => Gstr2bItcCess - BooksItcCess;
        public decimal DifferenceTotal => Gstr2bItcTotal - BooksItcTotal;
    }
}
