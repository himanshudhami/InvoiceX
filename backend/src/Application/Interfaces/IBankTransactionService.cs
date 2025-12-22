using Application.DTOs.BankTransactions;
using Core.Entities;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for BankTransaction CRUD operations
    /// For reconciliation, use IReconciliationService
    /// For import, use IBankStatementImportService
    /// For BRS, use IBrsService
    /// For reversals, use IReversalDetectionService
    /// For outgoing payments, use IOutgoingPaymentsService
    /// </summary>
    public interface IBankTransactionService
    {
        /// <summary>
        /// Get bank transaction by ID
        /// </summary>
        Task<Result<BankTransaction>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all bank transactions
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetAllAsync();

        /// <summary>
        /// Get paginated bank transactions with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<BankTransaction> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new bank transaction
        /// </summary>
        Task<Result<BankTransaction>> CreateAsync(CreateBankTransactionDto dto);

        /// <summary>
        /// Update an existing bank transaction
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateBankTransactionDto dto);

        /// <summary>
        /// Delete a bank transaction by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// Get transactions for a specific bank account
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetByBankAccountIdAsync(Guid bankAccountId);

        /// <summary>
        /// Get transactions within a date range for a specific bank account
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetByDateRangeAsync(
            Guid bankAccountId, DateOnly fromDate, DateOnly toDate);

        /// <summary>
        /// Get unreconciled transactions, optionally filtered by bank account
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetUnreconciledAsync(Guid? bankAccountId = null);

        /// <summary>
        /// Get summary statistics for a bank account
        /// </summary>
        Task<Result<BankTransactionSummaryDto>> GetSummaryAsync(
            Guid bankAccountId, DateOnly? fromDate = null, DateOnly? toDate = null);
    }

    // ==================== DTOs that stay in this file for backwards compatibility ====================

    /// <summary>
    /// Request to pair a reversal with its original
    /// </summary>
    public class PairReversalRequestDto
    {
        public Guid ReversalTransactionId { get; set; }
        public Guid OriginalTransactionId { get; set; }
        public bool OriginalWasPostedToLedger { get; set; }
        public Guid? OriginalJournalEntryId { get; set; }
        public string? Notes { get; set; }
        public string? PairedBy { get; set; }
    }

    /// <summary>
    /// Result of pairing reversal
    /// </summary>
    public class PairReversalResultDto
    {
        public bool Success { get; set; }
        public Guid OriginalTransactionId { get; set; }
        public Guid ReversalTransactionId { get; set; }
        public Guid? ReversalJournalEntryId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of reversal detection
    /// </summary>
    public class ReversalDetectionResultDto
    {
        public bool IsReversal { get; set; }
        public string? DetectedPattern { get; set; }
        public int Confidence { get; set; }
        public string? ExtractedOriginalReference { get; set; }
        public List<ReversalMatchSuggestionDto> SuggestedOriginals { get; set; } = new();
    }

    /// <summary>
    /// Suggested original transaction for a reversal
    /// </summary>
    public class ReversalMatchSuggestionDto
    {
        public Guid TransactionId { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public int MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public bool IsReconciled { get; set; }
        public string? ReconciledType { get; set; }
        public Guid? ReconciledId { get; set; }
    }

    /// <summary>
    /// Summary statistics for bank transactions
    /// </summary>
    public class BankTransactionSummaryDto
    {
        public int TotalCount { get; set; }
        public int ReconciledCount { get; set; }
        public int UnreconciledCount => TotalCount - ReconciledCount;
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal NetAmount => TotalCredits - TotalDebits;
        public double ReconciliationPercentage => TotalCount > 0 ? Math.Round((double)ReconciledCount / TotalCount * 100, 2) : 0;
    }

    /// <summary>
    /// Result of auto-reconciliation process
    /// </summary>
    public class AutoReconcileResultDto
    {
        public int TransactionsProcessed { get; set; }
        public int TransactionsReconciled { get; set; }
        public int TransactionsSkipped { get; set; }
        public decimal TotalAmountReconciled { get; set; }
        public List<AutoReconcileMatchDto> Matches { get; set; } = new();
    }

    public class AutoReconcileMatchDto
    {
        public Guid BankTransactionId { get; set; }
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public int MatchScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bank Reconciliation Statement
    /// </summary>
    public class BankReconciliationStatementDto
    {
        public Guid BankAccountId { get; set; }
        public string BankAccountName { get; set; } = string.Empty;
        public DateOnly AsOfDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public decimal BankStatementBalance { get; set; }
        public decimal DepositsInTransit { get; set; }
        public List<BrsItemDto> DepositsInTransitItems { get; set; } = new();
        public decimal OutstandingCheques { get; set; }
        public List<BrsItemDto> OutstandingChequeItems { get; set; } = new();
        public decimal AdjustedBankBalance { get; set; }
        public decimal BookBalance { get; set; }
        public decimal BankCreditsNotInBooks { get; set; }
        public List<BrsItemDto> BankCreditsNotInBooksItems { get; set; } = new();
        public decimal BankDebitsNotInBooks { get; set; }
        public List<BrsItemDto> BankDebitsNotInBooksItems { get; set; } = new();
        public decimal AdjustedBookBalance { get; set; }
        public decimal Difference => AdjustedBankBalance - AdjustedBookBalance;
        public bool IsReconciled => Math.Abs(Difference) < 0.01m;
        public int TotalTransactions { get; set; }
        public int ReconciledTransactions { get; set; }
        public int UnreconciledTransactions { get; set; }
    }

    public class BrsItemDto
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Enhanced BRS with journal entry perspective for CA compliance
    /// Includes TDS summary and audit metrics
    /// </summary>
    public class EnhancedBrsReportDto : BankReconciliationStatementDto
    {
        // ==================== Ledger Perspective ====================

        /// <summary>
        /// Book balance calculated from journal entries (not bank transactions)
        /// This is the "true" book balance per ledger
        /// </summary>
        public decimal LedgerBalance { get; set; }

        /// <summary>
        /// Whether the bank account is linked to a ledger account
        /// </summary>
        public bool HasLedgerLink { get; set; }

        /// <summary>
        /// Linked ledger account ID (if any)
        /// </summary>
        public Guid? LinkedAccountId { get; set; }

        /// <summary>
        /// Linked ledger account name (for display)
        /// </summary>
        public string? LinkedAccountName { get; set; }

        // ==================== TDS Summary ====================

        /// <summary>
        /// TDS deductions by section (for compliance reporting)
        /// </summary>
        public List<TdsSummaryItemDto> TdsSummary { get; set; } = new();

        /// <summary>
        /// Total TDS deducted across all sections
        /// </summary>
        public decimal TotalTdsDeducted { get; set; }

        // ==================== Audit Metrics ====================

        /// <summary>
        /// Count of reconciled transactions without JE link
        /// Should be 0 for full compliance
        /// </summary>
        public int UnlinkedJeCount { get; set; }

        /// <summary>
        /// IDs of transactions without JE link (for remediation)
        /// </summary>
        public List<Guid> UnlinkedJeTransactionIds { get; set; } = new();

        /// <summary>
        /// Count of transactions reconciled directly to journal entries
        /// (manual JE reconciliation without source documents)
        /// </summary>
        public int DirectJeReconciliationCount { get; set; }

        // ==================== Date Range ====================

        /// <summary>
        /// Start of period covered by this BRS
        /// </summary>
        public DateOnly? PeriodStart { get; set; }

        /// <summary>
        /// End of period (same as AsOfDate)
        /// </summary>
        public DateOnly PeriodEnd => AsOfDate;

        // ==================== Differences Analysis ====================

        /// <summary>
        /// Difference between bank balance and ledger balance
        /// Used for BRS reconciliation
        /// </summary>
        public decimal BankToLedgerDifference => BankStatementBalance - LedgerBalance;

        /// <summary>
        /// Summary of difference types (bank_interest, bank_charges, etc.)
        /// </summary>
        public List<DifferenceTypeSummaryDto> DifferenceTypeSummary { get; set; } = new();
    }

    /// <summary>
    /// TDS summary by section for compliance reporting
    /// </summary>
    public class TdsSummaryItemDto
    {
        /// <summary>
        /// TDS section (e.g., 194C, 194J, 194A)
        /// </summary>
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable section description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Number of transactions with this TDS section
        /// </summary>
        public int TransactionCount { get; set; }

        /// <summary>
        /// Total TDS amount for this section
        /// </summary>
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// Summary of reconciliation difference types
    /// </summary>
    public class DifferenceTypeSummaryDto
    {
        /// <summary>
        /// Type of difference (bank_interest, bank_charges, tds_deducted, etc.)
        /// </summary>
        public string DifferenceType { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Number of occurrences
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Total amount (can be positive or negative)
        /// </summary>
        public decimal TotalAmount { get; set; }
    }
}
