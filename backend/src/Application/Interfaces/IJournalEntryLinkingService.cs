using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Result of a backfill operation for JE links
    /// </summary>
    public class BackfillResult
    {
        public int LinkedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public List<BackfillFailure> FailedItems { get; set; } = new();
    }

    /// <summary>
    /// Details of a failed backfill item
    /// </summary>
    public class BackfillFailure
    {
        public Guid TransactionId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service for linking bank transactions to journal entries
    /// Enables hybrid reconciliation (source documents + JE lines)
    /// </summary>
    public interface IJournalEntryLinkingService
    {
        /// <summary>
        /// Find the JE and JE line for a source document that affects a specific bank account.
        /// Used during reconciliation to auto-link the JE.
        /// </summary>
        /// <param name="sourceType">Type of source document (payment, expense_claim, salary, etc.)</param>
        /// <param name="sourceId">ID of the source document</param>
        /// <param name="bankAccountId">Bank account ID to find the relevant JE line for</param>
        /// <returns>Tuple of (JournalEntryId, JournalEntryLineId) if found, null if not found</returns>
        Task<Result<(Guid JournalEntryId, Guid JournalEntryLineId)?>> FindBankJournalEntryLineAsync(
            string sourceType,
            Guid sourceId,
            Guid bankAccountId);

        /// <summary>
        /// Auto-link a bank transaction to its JE (if source document is reconciled and JE exists)
        /// Called after reconciliation to source document.
        /// </summary>
        /// <param name="bankTransactionId">Bank transaction ID to update</param>
        Task<Result> AutoLinkJournalEntryAsync(Guid bankTransactionId);

        /// <summary>
        /// Backfill JE links for all existing reconciled transactions in a company.
        /// Used as a migration step for existing data.
        /// </summary>
        /// <param name="companyId">Company ID to backfill</param>
        /// <returns>Summary of backfill results</returns>
        Task<Result<BackfillResult>> BackfillJournalEntryLinksAsync(Guid companyId);

        /// <summary>
        /// Get unlinked reconciled transactions (reconciled but no JE link).
        /// Useful for audit/reporting.
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <returns>Count and list of unlinked transaction IDs</returns>
        Task<Result<(int Count, IEnumerable<Guid> TransactionIds)>> GetUnlinkedTransactionsAsync(Guid companyId);
    }
}
