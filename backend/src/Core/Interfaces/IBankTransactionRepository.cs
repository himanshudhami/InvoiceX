using Core.Entities;

namespace Core.Interfaces
{
    public interface IBankTransactionRepository
    {
        Task<BankTransaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<BankTransaction>> GetAllAsync();
        Task<(IEnumerable<BankTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<BankTransaction> AddAsync(BankTransaction entity);
        Task UpdateAsync(BankTransaction entity);
        Task DeleteAsync(Guid id);

        // Bank account specific queries
        Task<IEnumerable<BankTransaction>> GetByBankAccountIdAsync(Guid bankAccountId);
        Task<IEnumerable<BankTransaction>> GetByDateRangeAsync(
            Guid bankAccountId, DateOnly fromDate, DateOnly toDate);

        // Reconciliation queries
        Task<IEnumerable<BankTransaction>> GetUnreconciledAsync(Guid? bankAccountId = null);
        Task<IEnumerable<BankTransaction>> GetReconciledAsync(Guid bankAccountId);

        // Reconciliation operations
        Task ReconcileTransactionAsync(
            Guid transactionId, string reconciledType, Guid reconciledId, string? reconciledBy = null);
        Task UnreconcileTransactionAsync(Guid transactionId);

        // Bulk import operations
        Task<IEnumerable<BankTransaction>> BulkAddAsync(IEnumerable<BankTransaction> transactions);
        Task<IEnumerable<BankTransaction>> GetByImportBatchIdAsync(Guid batchId);
        Task DeleteByImportBatchIdAsync(Guid batchId);

        // Duplicate detection
        Task<bool> ExistsByHashAsync(string transactionHash, Guid bankAccountId);
        Task<IEnumerable<string>> GetExistingHashesAsync(Guid bankAccountId, IEnumerable<string> hashes);

        // Reconciliation suggestions - find potential matches for a transaction
        Task<IEnumerable<Payments>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 0.01m, int maxResults = 10);

        // Summary queries
        Task<(int TotalCount, int ReconciledCount, decimal TotalCredits, decimal TotalDebits)>
            GetSummaryAsync(Guid bankAccountId, DateOnly? fromDate = null, DateOnly? toDate = null);
    }
}
