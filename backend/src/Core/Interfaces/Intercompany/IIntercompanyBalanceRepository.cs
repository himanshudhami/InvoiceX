using Core.Entities.Intercompany;

namespace Core.Interfaces.Intercompany
{
    /// <summary>
    /// Repository interface for intercompany balances
    /// </summary>
    public interface IIntercompanyBalanceRepository
    {
        Task<IntercompanyBalance?> GetByIdAsync(Guid id);
        Task<IEnumerable<IntercompanyBalance>> GetAllAsync();
        Task<(IEnumerable<IntercompanyBalance> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<IntercompanyBalance> AddAsync(IntercompanyBalance entity);
        Task UpdateAsync(IntercompanyBalance entity);
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Get balance between two companies as of a date
        /// </summary>
        Task<IntercompanyBalance?> GetBalanceAsync(Guid fromCompanyId, Guid toCompanyId, DateOnly asOfDate);

        /// <summary>
        /// Get latest balance between two companies
        /// </summary>
        Task<IntercompanyBalance?> GetLatestBalanceAsync(Guid fromCompanyId, Guid toCompanyId);

        /// <summary>
        /// Get all balances for a company
        /// </summary>
        Task<IEnumerable<IntercompanyBalance>> GetBalancesForCompanyAsync(Guid companyId, DateOnly? asOfDate = null);

        /// <summary>
        /// Get or create balance record
        /// </summary>
        Task<IntercompanyBalance> GetOrCreateBalanceAsync(Guid fromCompanyId, Guid toCompanyId, DateOnly asOfDate, string financialYear);

        /// <summary>
        /// Update balance after a transaction
        /// </summary>
        Task UpdateBalanceAsync(Guid fromCompanyId, Guid toCompanyId, DateOnly transactionDate, decimal amount, bool isDebit);
    }
}
