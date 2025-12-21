using Core.Entities.Forex;

namespace Core.Interfaces.Forex
{
    public interface IForexTransactionRepository
    {
        Task<ForexTransaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<ForexTransaction>> GetAllAsync();
        Task<(IEnumerable<ForexTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<ForexTransaction> AddAsync(ForexTransaction entity);
        Task UpdateAsync(ForexTransaction entity);
        Task DeleteAsync(Guid id);

        // Source-specific queries
        Task<ForexTransaction?> GetBySourceAsync(string sourceType, Guid sourceId);
        Task<IEnumerable<ForexTransaction>> GetBySourceTypeAsync(Guid companyId, string sourceType);

        // Booking/Settlement queries
        Task<IEnumerable<ForexTransaction>> GetUnpostedAsync(Guid? companyId = null);
        Task<IEnumerable<ForexTransaction>> GetBookingsForSettlementAsync(
            Guid companyId, string currency, decimal? amount = null);

        // Revaluation queries
        Task<IEnumerable<ForexTransaction>> GetOutstandingBookingsAsync(
            Guid companyId, string currency, DateOnly asOfDate);

        // Gain/Loss summary
        Task<(decimal RealizedGain, decimal UnrealizedGain)> GetGainLossSummaryAsync(
            Guid companyId, string financialYear);

        // Bulk operations
        Task<IEnumerable<ForexTransaction>> BulkAddAsync(IEnumerable<ForexTransaction> transactions);
        Task MarkAsPostedAsync(Guid id, Guid journalEntryId);
    }
}
