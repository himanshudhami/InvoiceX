using Application.DTOs.Gst;
using Core.Common;

namespace Application.Interfaces.Gst
{
    /// <summary>
    /// Service interface for GSTR-2B ingestion and reconciliation
    /// </summary>
    public interface IGstr2bService
    {
        // ==================== Import ====================

        /// <summary>
        /// Import GSTR-2B JSON data
        /// </summary>
        Task<Result<Gstr2bImportDto>> ImportGstr2bAsync(Guid companyId, string returnPeriod, string jsonData, string? fileName, Guid userId);

        /// <summary>
        /// Get import by ID
        /// </summary>
        Task<Result<Gstr2bImportDto>> GetImportByIdAsync(Guid importId);

        /// <summary>
        /// Get import by period
        /// </summary>
        Task<Result<Gstr2bImportDto>> GetImportByPeriodAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get imports for a company (paged)
        /// </summary>
        Task<Result<(IEnumerable<Gstr2bImportDto> Items, int TotalCount)>> GetImportsAsync(
            Guid companyId, int pageNumber = 1, int pageSize = 12, string? status = null);

        /// <summary>
        /// Delete an import and all its invoices
        /// </summary>
        Task<Result> DeleteImportAsync(Guid importId);

        // ==================== Reconciliation ====================

        /// <summary>
        /// Run reconciliation on an import
        /// </summary>
        Task<Result<Gstr2bReconciliationSummaryDto>> RunReconciliationAsync(Guid importId, bool force = false);

        /// <summary>
        /// Get reconciliation summary
        /// </summary>
        Task<Result<Gstr2bReconciliationSummaryDto>> GetReconciliationSummaryAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get supplier-wise summary
        /// </summary>
        Task<Result<IEnumerable<Gstr2bSupplierSummaryDto>>> GetSupplierSummaryAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get ITC comparison (GSTR-2B vs Books)
        /// </summary>
        Task<Result<Gstr2bItcComparisonDto>> GetItcComparisonAsync(Guid companyId, string returnPeriod);

        // ==================== Invoices ====================

        /// <summary>
        /// Get invoices for an import (paged)
        /// </summary>
        Task<Result<(IEnumerable<Gstr2bInvoiceListItemDto> Items, int TotalCount)>> GetInvoicesAsync(
            Guid importId, int pageNumber = 1, int pageSize = 50,
            string? matchStatus = null, string? invoiceType = null, string? searchTerm = null);

        /// <summary>
        /// Get invoice details
        /// </summary>
        Task<Result<Gstr2bInvoiceDto>> GetInvoiceByIdAsync(Guid invoiceId);

        /// <summary>
        /// Get unmatched invoices for a period
        /// </summary>
        Task<Result<IEnumerable<Gstr2bInvoiceListItemDto>>> GetUnmatchedInvoicesAsync(Guid companyId, string returnPeriod);

        // ==================== Actions ====================

        /// <summary>
        /// Accept a mismatch (user confirms the GSTR-2B data)
        /// </summary>
        Task<Result> AcceptMismatchAsync(Guid invoiceId, Guid userId, string? notes = null);

        /// <summary>
        /// Reject an invoice (user marks as invalid/not for ITC)
        /// </summary>
        Task<Result> RejectInvoiceAsync(Guid invoiceId, Guid userId, string reason);

        /// <summary>
        /// Manually match a GSTR-2B invoice to a vendor invoice
        /// </summary>
        Task<Result> ManualMatchAsync(Guid gstr2bInvoiceId, Guid vendorInvoiceId, Guid userId, string? notes = null);

        /// <summary>
        /// Reset action (undo accept/reject)
        /// </summary>
        Task<Result> ResetActionAsync(Guid invoiceId);
    }
}
