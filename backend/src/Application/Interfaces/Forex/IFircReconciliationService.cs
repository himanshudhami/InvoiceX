using Application.DTOs.Forex;
using Core.Common;
using Core.Entities.Forex;

namespace Application.Interfaces.Forex
{
    /// <summary>
    /// Service interface for FIRC (Foreign Inward Remittance Certificate) reconciliation
    /// Handles FEMA/RBI compliance for export receivables
    /// </summary>
    public interface IFircReconciliationService
    {
        // ==================== CRUD Operations ====================

        /// <summary>
        /// Get FIRC by ID
        /// </summary>
        Task<Result<FircTracking>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get paginated FIRCs with filtering
        /// </summary>
        Task<Result<(IEnumerable<FircTracking> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            string? searchTerm = null,
            string? status = null,
            bool? edpmsReported = null);

        /// <summary>
        /// Create a new FIRC entry
        /// </summary>
        Task<Result<FircTracking>> CreateAsync(CreateFircDto dto);

        /// <summary>
        /// Update an existing FIRC
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateFircDto dto);

        /// <summary>
        /// Delete a FIRC
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        // ==================== Payment Linking ====================

        /// <summary>
        /// Link a FIRC to a payment
        /// </summary>
        Task<Result> LinkToPaymentAsync(Guid fircId, Guid paymentId);

        /// <summary>
        /// Unlink a FIRC from its payment
        /// </summary>
        Task<Result> UnlinkFromPaymentAsync(Guid fircId);

        /// <summary>
        /// Get unlinked FIRCs for a company (FIRCs not yet matched to payments)
        /// </summary>
        Task<Result<IEnumerable<FircTracking>>> GetUnlinkedAsync(Guid companyId);

        /// <summary>
        /// Auto-match FIRCs with payments based on amount and date
        /// </summary>
        Task<Result<FircAutoMatchResultDto>> AutoMatchFircsAsync(
            Guid companyId,
            decimal amountTolerance = 0.01m,
            int dateTolerance = 5);

        // ==================== Invoice Linking ====================

        /// <summary>
        /// Link a FIRC to invoices (one FIRC can cover multiple invoices)
        /// </summary>
        Task<Result> LinkToInvoicesAsync(Guid fircId, IEnumerable<FircInvoiceAllocationDto> allocations);

        /// <summary>
        /// Remove an invoice link from a FIRC
        /// </summary>
        Task<Result> RemoveInvoiceLinkAsync(Guid fircId, Guid invoiceId);

        /// <summary>
        /// Get invoices linked to a FIRC
        /// </summary>
        Task<Result<IEnumerable<FircInvoiceLink>>> GetInvoiceLinksAsync(Guid fircId);

        // ==================== EDPMS Compliance ====================

        /// <summary>
        /// Get FIRCs pending EDPMS reporting
        /// </summary>
        Task<Result<IEnumerable<FircTracking>>> GetPendingEdpmsReportingAsync(Guid companyId);

        /// <summary>
        /// Mark a FIRC as reported to EDPMS
        /// </summary>
        Task<Result> MarkEdpmsReportedAsync(Guid fircId, DateOnly reportDate, string? reference);

        /// <summary>
        /// Get EDPMS compliance summary for a company
        /// </summary>
        Task<Result<EdpmsComplianceSummaryDto>> GetEdpmsComplianceSummaryAsync(Guid companyId);

        // ==================== Realization Tracking (9-month FEMA) ====================

        /// <summary>
        /// Get invoices approaching realization deadline
        /// </summary>
        Task<Result<IEnumerable<RealizationAlertDto>>> GetRealizationAlertsAsync(
            Guid companyId,
            int alertDaysBeforeDeadline = 30);

        /// <summary>
        /// Get export receivables with realization status
        /// </summary>
        Task<Result<IEnumerable<RealizationStatusDto>>> GetRealizationStatusAsync(
            Guid companyId,
            DateOnly? asOfDate = null);

        /// <summary>
        /// Get realization summary for a company
        /// </summary>
        Task<Result<RealizationSummaryDto>> GetRealizationSummaryAsync(
            Guid companyId,
            DateOnly? asOfDate = null);

        // ==================== Reconciliation Reports ====================

        /// <summary>
        /// Get FIRC reconciliation report matching FIRCs with bank credits and invoices
        /// </summary>
        Task<Result<FircReconciliationReportDto>> GetReconciliationReportAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate);

        /// <summary>
        /// Validate FIRC data for completeness and compliance
        /// </summary>
        Task<Result<FircValidationResultDto>> ValidateFircAsync(Guid fircId);
    }
}
