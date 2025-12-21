using Core.Entities.Intercompany;

namespace Core.Interfaces.Intercompany
{
    /// <summary>
    /// Repository interface for intercompany transactions
    /// </summary>
    public interface IIntercompanyTransactionRepository
    {
        Task<IntercompanyTransaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<IntercompanyTransaction>> GetAllAsync();
        Task<(IEnumerable<IntercompanyTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<IntercompanyTransaction> AddAsync(IntercompanyTransaction entity);
        Task UpdateAsync(IntercompanyTransaction entity);
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Get transactions between two companies
        /// </summary>
        Task<IEnumerable<IntercompanyTransaction>> GetTransactionsBetweenCompaniesAsync(
            Guid companyId,
            Guid counterpartyId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);

        /// <summary>
        /// Get all transactions for a company
        /// </summary>
        Task<IEnumerable<IntercompanyTransaction>> GetByCompanyIdAsync(Guid companyId, string? financialYear = null);

        /// <summary>
        /// Get unreconciled transactions
        /// </summary>
        Task<IEnumerable<IntercompanyTransaction>> GetUnreconciledAsync(Guid? companyId = null);

        /// <summary>
        /// Get transaction by source document
        /// </summary>
        Task<IntercompanyTransaction?> GetBySourceAsync(string sourceType, Guid sourceId);

        /// <summary>
        /// Mark transactions as reconciled
        /// </summary>
        Task ReconcileTransactionsAsync(Guid transactionId, Guid counterpartyTransactionId, Guid reconciledBy);
    }
}
