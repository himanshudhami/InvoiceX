using Core.Entities.Ledger;

namespace Core.Interfaces.Ledger
{
    /// <summary>
    /// Repository interface for Journal Entry operations
    /// </summary>
    public interface IJournalEntryRepository
    {
        // ==================== Basic CRUD ====================

        Task<JournalEntry?> GetByIdAsync(Guid id);
        Task<JournalEntry?> GetByIdWithLinesAsync(Guid id);
        Task<IEnumerable<JournalEntry>> GetAllAsync();
        Task<(IEnumerable<JournalEntry> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<JournalEntry> AddAsync(JournalEntry entity);
        Task UpdateAsync(JournalEntry entity);
        Task DeleteAsync(Guid id);

        // ==================== Company-Specific Queries ====================

        /// <summary>
        /// Get all journal entries for a company
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get journal entry by number
        /// </summary>
        Task<JournalEntry?> GetByNumberAsync(Guid companyId, string journalNumber);

        /// <summary>
        /// Get entries for a specific period
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetByPeriodAsync(
            Guid companyId,
            string financialYear,
            int? periodMonth = null);

        /// <summary>
        /// Get entries by date range
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetByDateRangeAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate);

        // ==================== Source Tracking ====================

        /// <summary>
        /// Get journal entries by source (invoice, payment, etc.)
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetBySourceAsync(
            Guid companyId,
            string sourceType,
            Guid sourceId);

        /// <summary>
        /// Get journal entries by source type and ID (without company filter)
        /// Used for payroll posting where company is derived from source
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetBySourceAsync(
            string sourceType,
            Guid sourceId);

        /// <summary>
        /// Check if a source already has journal entries
        /// </summary>
        Task<bool> HasEntriesForSourceAsync(string sourceType, Guid sourceId);

        // ==================== Idempotency ====================

        /// <summary>
        /// Get journal entry by idempotency key
        /// Used to prevent duplicate entries for the same event
        /// </summary>
        Task<JournalEntry?> GetByIdempotencyKeyAsync(string idempotencyKey);

        // ==================== Status Operations ====================

        /// <summary>
        /// Post a draft journal entry
        /// </summary>
        Task PostAsync(Guid id, Guid postedBy);

        /// <summary>
        /// Get unposted (draft) entries for a company
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetDraftEntriesAsync(Guid companyId);

        // ==================== Reversal ====================

        /// <summary>
        /// Create a reversal entry for an existing journal
        /// </summary>
        Task<JournalEntry> CreateReversalAsync(Guid originalId, Guid createdBy, string? reason = null);

        /// <summary>
        /// Check if an entry can be reversed
        /// </summary>
        Task<bool> CanReverseAsync(Guid id);

        // ==================== Number Generation ====================

        /// <summary>
        /// Generate next journal number for a company/period
        /// </summary>
        Task<string> GenerateNextNumberAsync(Guid companyId, string financialYear);

        // ==================== Lines ====================

        /// <summary>
        /// Get lines for a journal entry
        /// </summary>
        Task<IEnumerable<JournalEntryLine>> GetLinesAsync(Guid journalEntryId);

        /// <summary>
        /// Add lines to a journal entry
        /// </summary>
        Task AddLinesAsync(Guid journalEntryId, IEnumerable<JournalEntryLine> lines);

        /// <summary>
        /// Update lines (for draft entries only)
        /// </summary>
        Task UpdateLinesAsync(Guid journalEntryId, IEnumerable<JournalEntryLine> lines);

        // ==================== Balance Queries ====================

        /// <summary>
        /// Get the balance for a specific account as of a date.
        /// Calculates sum of debits - sum of credits from posted journal entries.
        /// Used for BRS generation to get book balance from ledger perspective.
        /// </summary>
        Task<decimal> GetAccountBalanceAsync(Guid companyId, Guid accountId, DateOnly asOfDate);
    }
}
