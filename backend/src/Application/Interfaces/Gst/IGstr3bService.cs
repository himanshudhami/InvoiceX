using Application.DTOs.Gst;
using Core.Common;
using Core.Entities.Gst;

namespace Application.Interfaces.Gst
{
    /// <summary>
    /// Service interface for GSTR-3B filing pack generation
    /// </summary>
    public interface IGstr3bService
    {
        // ==================== Filing Generation ====================

        /// <summary>
        /// Generate complete GSTR-3B filing pack for a period
        /// </summary>
        Task<Result<Gstr3bFilingDto>> GenerateFilingPackAsync(Guid companyId, string returnPeriod, Guid userId, bool regenerate = false);

        /// <summary>
        /// Get filing by ID
        /// </summary>
        Task<Result<Gstr3bFilingDto>> GetFilingByIdAsync(Guid filingId);

        /// <summary>
        /// Get filing for a specific period
        /// </summary>
        Task<Result<Gstr3bFilingDto>> GetFilingByPeriodAsync(Guid companyId, string returnPeriod);

        // ==================== Table Builders ====================

        /// <summary>
        /// Build Table 3.1 - Outward supplies (from invoices/GSTR-1 data)
        /// </summary>
        Task<Result<Gstr3bTable31Dto>> BuildTable31Async(Guid companyId, string returnPeriod);

        /// <summary>
        /// Build Table 4 - ITC (from vendor invoices + RCM + blocked ITC)
        /// </summary>
        Task<Result<Gstr3bTable4Dto>> BuildTable4Async(Guid companyId, string returnPeriod);

        /// <summary>
        /// Build Table 5 - Exempt, nil-rated, non-GST supplies
        /// </summary>
        Task<Result<Gstr3bTable5Dto>> BuildTable5Async(Guid companyId, string returnPeriod);

        // ==================== Drill-down ====================

        /// <summary>
        /// Get line items for a specific table
        /// </summary>
        Task<Result<IEnumerable<Gstr3bLineItemDto>>> GetLineItemsAsync(Guid filingId, string? tableCode = null);

        /// <summary>
        /// Get source documents for a line item (drill-down)
        /// </summary>
        Task<Result<IEnumerable<Gstr3bSourceDocumentDto>>> GetSourceDocumentsAsync(Guid lineItemId);

        // ==================== Variance ====================

        /// <summary>
        /// Get variance from previous period
        /// </summary>
        Task<Result<Gstr3bVarianceSummaryDto>> GetVarianceAsync(Guid companyId, string currentPeriod);

        // ==================== Filing Workflow ====================

        /// <summary>
        /// Mark filing as reviewed
        /// </summary>
        Task<Result> MarkAsReviewedAsync(Guid filingId, Guid userId, string? notes = null);

        /// <summary>
        /// Mark filing as filed (with ARN from GSTN)
        /// </summary>
        Task<Result> MarkAsFiledAsync(Guid filingId, string arn, DateTime filingDate, Guid userId);

        // ==================== History ====================

        /// <summary>
        /// Get filing history for a company
        /// </summary>
        Task<Result<(IEnumerable<Gstr3bFilingHistoryDto> Items, int TotalCount)>> GetFilingHistoryAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 12,
            string? financialYear = null,
            string? status = null);

        // ==================== Export ====================

        /// <summary>
        /// Export filing to JSON (GSTN format)
        /// </summary>
        Task<Result<string>> ExportToJsonAsync(Guid filingId);
    }
}
