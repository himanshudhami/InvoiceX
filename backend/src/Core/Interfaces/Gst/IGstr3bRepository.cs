using Core.Entities.Gst;

namespace Core.Interfaces.Gst
{
    /// <summary>
    /// Repository interface for GSTR-3B filings
    /// </summary>
    public interface IGstr3bRepository
    {
        // ==================== Filing CRUD ====================

        Task<Gstr3bFiling?> GetByIdAsync(Guid id);
        Task<Gstr3bFiling?> GetByPeriodAsync(Guid companyId, string returnPeriod);
        Task<IEnumerable<Gstr3bFiling>> GetByCompanyAsync(Guid companyId, string? financialYear = null);
        Task<Gstr3bFiling> AddAsync(Gstr3bFiling filing);
        Task UpdateAsync(Gstr3bFiling filing);
        Task DeleteAsync(Guid id);

        // ==================== Line Items ====================

        Task<IEnumerable<Gstr3bLineItem>> GetLineItemsAsync(Guid filingId);
        Task<IEnumerable<Gstr3bLineItem>> GetLineItemsByTableAsync(Guid filingId, string tableCode);
        Task BulkInsertLineItemsAsync(IEnumerable<Gstr3bLineItem> lineItems);
        Task DeleteLineItemsByFilingAsync(Guid filingId);

        // ==================== Source Documents ====================

        Task<IEnumerable<Gstr3bSourceDocument>> GetSourceDocumentsAsync(Guid lineItemId);
        Task BulkInsertSourceDocumentsAsync(IEnumerable<Gstr3bSourceDocument> documents);
        Task DeleteSourceDocumentsByFilingAsync(Guid filingId);

        // ==================== Status Updates ====================

        Task UpdateStatusAsync(Guid id, string status);
        Task MarkAsGeneratedAsync(Guid id, Guid generatedBy);
        Task MarkAsReviewedAsync(Guid id, Guid reviewedBy);
        Task MarkAsFiledAsync(Guid id, string arn, DateTime filingDate, Guid filedBy);

        // ==================== Table Summary Updates ====================

        Task UpdateTableSummaryAsync(Guid id, string tableColumn, string jsonData);

        // ==================== Queries ====================

        /// <summary>
        /// Check if filing exists for period
        /// </summary>
        Task<bool> ExistsForPeriodAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get filing history with pagination
        /// </summary>
        Task<(IEnumerable<Gstr3bFiling> Items, int TotalCount)> GetFilingHistoryAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null);

        /// <summary>
        /// Get previous period filing for variance calculation
        /// </summary>
        Task<Gstr3bFiling?> GetPreviousPeriodFilingAsync(Guid companyId, string currentPeriod);
    }
}
