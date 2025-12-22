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
}
