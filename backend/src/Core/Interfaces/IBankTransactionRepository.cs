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
            Guid transactionId,
            string reconciledType,
            Guid reconciledId,
            string? reconciledBy = null,
            decimal? differenceAmount = null,
            string? differenceType = null,
            string? differenceNotes = null,
            string? tdsSection = null,
            Guid? adjustmentJournalId = null);
        Task UnreconcileTransactionAsync(Guid transactionId);

        // Bulk import operations
        Task<IEnumerable<BankTransaction>> BulkAddAsync(IEnumerable<BankTransaction> transactions);
        Task<IEnumerable<BankTransaction>> GetByImportBatchIdAsync(Guid batchId);
        Task DeleteByImportBatchIdAsync(Guid batchId);

        // Duplicate detection
        Task<bool> ExistsByHashAsync(string transactionHash, Guid bankAccountId);
        Task<IEnumerable<string>> GetExistingHashesAsync(Guid bankAccountId, IEnumerable<string> hashes);

        // Reconciliation suggestions - find potential matches for a transaction (credits)
        Task<IEnumerable<PaymentWithDetails>> GetReconciliationSuggestionsAsync(
            Guid transactionId, decimal tolerance = 0.01m, int maxResults = 10);

        // Search payments for credit reconciliation
        Task<IEnumerable<PaymentWithDetails>> SearchPaymentsAsync(
            Guid companyId,
            string? searchTerm = null,
            decimal? amountMin = null,
            decimal? amountMax = null,
            int maxResults = 20);

        // Summary queries
        Task<(int TotalCount, int ReconciledCount, decimal TotalCredits, decimal TotalDebits)>
            GetSummaryAsync(Guid bankAccountId, DateOnly? fromDate = null, DateOnly? toDate = null);

        // Debit reconciliation suggestions - search across outgoing payment tables
        Task<IEnumerable<OutgoingPaymentRecord>> GetDebitReconciliationCandidatesAsync(
            Guid companyId, decimal amount, DateOnly date, decimal amountTolerance, int dateTolerance, int maxResults);

        // Outgoing payments unified view
        Task<(IEnumerable<OutgoingPaymentRecord> Items, int TotalCount)> GetOutgoingPaymentsAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            bool? reconciled = null,
            List<string>? types = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            string? searchTerm = null);

        // Outgoing payments summary
        Task<OutgoingPaymentsSummary> GetOutgoingPaymentsSummaryAsync(
            Guid companyId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);

        // ==================== Reversal Pairing ====================

        /// <summary>
        /// Find potential original transactions for a reversal
        /// Searches for debit transactions with matching amount within date range
        /// </summary>
        Task<IEnumerable<BankTransaction>> FindPotentialOriginalsForReversalAsync(
            Guid reversalTransactionId,
            int maxDaysBack = 90,
            int maxResults = 10);

        /// <summary>
        /// Pair a reversal transaction with its original
        /// </summary>
        Task PairReversalAsync(
            Guid originalTransactionId,
            Guid reversalTransactionId,
            Guid? reversalJournalEntryId = null);

        /// <summary>
        /// Unpair a reversal from its original
        /// </summary>
        Task UnpairReversalAsync(Guid transactionId);

        /// <summary>
        /// Update the is_reversal_transaction flag
        /// </summary>
        Task UpdateReversalFlagAsync(Guid transactionId, bool isReversal);

        /// <summary>
        /// Get all unpaired reversal transactions
        /// </summary>
        Task<IEnumerable<BankTransaction>> GetUnpairedReversalsAsync(Guid? bankAccountId = null);

        // ==================== Journal Entry Linking (Hybrid Reconciliation) ====================

        /// <summary>
        /// Update the journal entry link for a bank transaction.
        /// Called during reconciliation to link bank txn to JE line.
        /// </summary>
        Task UpdateJournalEntryLinkAsync(
            Guid transactionId,
            Guid journalEntryId,
            Guid journalEntryLineId);

        /// <summary>
        /// Reconcile a bank transaction directly to a journal entry (for manual JEs without source documents).
        /// </summary>
        Task ReconcileToJournalAsync(
            Guid transactionId,
            Guid journalEntryId,
            Guid journalEntryLineId,
            Guid? adjustmentJournalId = null,
            string? reconciledBy = null,
            string? notes = null);

        /// <summary>
        /// Get reconciled transactions that don't have a JE link yet.
        /// Used for backfill migration.
        /// </summary>
        Task<IEnumerable<BankTransaction>> GetReconciledWithoutJeLinkAsync(Guid companyId);

        // ==================== Tally Migration ====================

        /// <summary>
        /// Get bank transaction by Tally voucher GUID for deduplication
        /// </summary>
        Task<BankTransaction?> GetByTallyGuidAsync(string tallyVoucherGuid);

        /// <summary>
        /// Get all bank transactions for a Tally migration batch
        /// </summary>
        Task<IEnumerable<BankTransaction>> GetByTallyBatchIdAsync(Guid batchId);

        /// <summary>
        /// Delete all bank transactions for a Tally migration batch (for rollback)
        /// </summary>
        Task DeleteByTallyBatchIdAsync(Guid batchId);
    }

    /// <summary>
    /// Unified record for outgoing payments across all expense types
    /// </summary>
    public class OutgoingPaymentRecord
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateOnly PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PayeeName { get; set; }
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public bool IsReconciled { get; set; }
        public Guid? BankTransactionId { get; set; }
        public DateTime? ReconciledAt { get; set; }
        public decimal? TdsAmount { get; set; }
        public string? TdsSection { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// Summary of outgoing payments
    /// </summary>
    public class OutgoingPaymentsSummary
    {
        public int TotalCount { get; set; }
        public int ReconciledCount { get; set; }
        public int UnreconciledCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ReconciledAmount { get; set; }
        public decimal UnreconciledAmount { get; set; }
        public Dictionary<string, (int Count, decimal Amount, int ReconciledCount)> ByType { get; set; } = new();
    }

    /// <summary>
    /// Payment with customer and invoice details for reconciliation suggestions
    /// </summary>
    public record PaymentWithDetails
    {
        public Guid Id { get; init; }
        public DateOnly PaymentDate { get; init; }
        public decimal Amount { get; init; }
        public string? ReferenceNumber { get; init; }
        public string? PaymentMethod { get; init; }
        public string? Notes { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerCompany { get; init; }
        public string? InvoiceNumber { get; init; }
    }
}
