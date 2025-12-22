using Core.Entities.Ledger;

namespace Core.Interfaces.Ledger
{
    /// <summary>
    /// Repository interface for GST Input Tax Credit tracking.
    /// Supports GSTR-3B filing and GSTR-2B reconciliation.
    /// </summary>
    public interface IGstInputCreditRepository
    {
        // Basic CRUD
        Task<GstInputCredit?> GetByIdAsync(Guid id);
        Task<IEnumerable<GstInputCredit>> GetAllAsync();
        Task<GstInputCredit> AddAsync(GstInputCredit entity);
        Task UpdateAsync(GstInputCredit entity);
        Task DeleteAsync(Guid id);

        // Company-specific queries
        Task<IEnumerable<GstInputCredit>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<GstInputCredit>> GetByReturnPeriodAsync(Guid companyId, string returnPeriod);
        Task<IEnumerable<GstInputCredit>> GetByFinancialYearAsync(Guid companyId, string financialYear);

        // Source document queries
        Task<GstInputCredit?> GetBySourceAsync(string sourceType, Guid sourceId);
        Task<IEnumerable<GstInputCredit>> GetBySourceTypeAsync(Guid companyId, string sourceType);

        // Status-based queries
        Task<IEnumerable<GstInputCredit>> GetPendingAsync(Guid companyId);
        Task<IEnumerable<GstInputCredit>> GetClaimedAsync(Guid companyId, string? returnPeriod = null);
        Task<IEnumerable<GstInputCredit>> GetUnmatchedAsync(Guid companyId);

        // Summary queries
        Task<(decimal TotalCgst, decimal TotalSgst, decimal TotalIgst, decimal TotalCess)> GetItcSummaryAsync(
            Guid companyId, string returnPeriod);
        Task<(decimal TotalCgst, decimal TotalSgst, decimal TotalIgst, decimal TotalCess)> GetPendingItcSummaryAsync(
            Guid companyId);

        // Bulk operations
        Task MarkAsClaimedAsync(IEnumerable<Guid> ids, string returnPeriod, string claimedBy);
        Task MarkAsMatchedAsync(Guid id, DateTime matchDate);
        Task ReverseItcAsync(Guid id, decimal amount, string reason);

        // Paged query
        Task<(IEnumerable<GstInputCredit> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
    }
}
