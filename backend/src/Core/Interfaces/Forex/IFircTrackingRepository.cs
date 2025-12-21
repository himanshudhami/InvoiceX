using Core.Entities.Forex;

namespace Core.Interfaces.Forex
{
    public interface IFircTrackingRepository
    {
        Task<FircTracking?> GetByIdAsync(Guid id);
        Task<IEnumerable<FircTracking>> GetAllAsync();
        Task<(IEnumerable<FircTracking> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<FircTracking> AddAsync(FircTracking entity);
        Task UpdateAsync(FircTracking entity);
        Task DeleteAsync(Guid id);

        // Company-specific queries
        Task<IEnumerable<FircTracking>> GetByCompanyIdAsync(Guid companyId);
        Task<FircTracking?> GetByFircNumberAsync(string fircNumber);

        // Payment linking
        Task<FircTracking?> GetByPaymentIdAsync(Guid paymentId);
        Task<IEnumerable<FircTracking>> GetUnlinkedAsync(Guid companyId);

        // EDPMS tracking
        Task<IEnumerable<FircTracking>> GetPendingEdpmsReportingAsync(Guid companyId);
        Task MarkEdpmsReportedAsync(Guid id, DateOnly reportDate, string? reference);

        // Invoice links
        Task AddInvoiceLinkAsync(FircInvoiceLink link);
        Task<IEnumerable<FircInvoiceLink>> GetInvoiceLinksAsync(Guid fircId);
        Task RemoveInvoiceLinkAsync(Guid fircId, Guid invoiceId);
    }
}
