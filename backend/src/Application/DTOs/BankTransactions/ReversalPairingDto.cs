namespace Application.DTOs.BankTransactions
{
    /// <summary>
    /// Request to pair a reversal transaction with its original
    /// </summary>
    public class PairReversalRequest
    {
        /// <summary>
        /// The reversal transaction ID (the credit that came back)
        /// </summary>
        public Guid ReversalTransactionId { get; set; }

        /// <summary>
        /// The original transaction ID (the failed debit)
        /// </summary>
        public Guid OriginalTransactionId { get; set; }

        /// <summary>
        /// Whether the original was already posted to ledger
        /// If true, a reversal journal entry will be created
        /// </summary>
        public bool OriginalWasPostedToLedger { get; set; }

        /// <summary>
        /// If original was posted, the journal entry ID to reverse
        /// </summary>
        public Guid? OriginalJournalEntryId { get; set; }

        /// <summary>
        /// Notes about the reversal
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// User performing the pairing
        /// </summary>
        public string? PairedBy { get; set; }
    }

    /// <summary>
    /// Result of pairing transactions
    /// </summary>
    public class PairReversalResult
    {
        public bool Success { get; set; }
        public Guid OriginalTransactionId { get; set; }
        public Guid ReversalTransactionId { get; set; }
        public Guid? ReversalJournalEntryId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Suggested original transaction for a reversal
    /// </summary>
    public class ReversalMatchSuggestion
    {
        public Guid TransactionId { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Match score (0-100) based on amount, date proximity, and description similarity
        /// </summary>
        public int MatchScore { get; set; }

        /// <summary>
        /// Explanation of why this is a match
        /// </summary>
        public string MatchReason { get; set; } = string.Empty;

        /// <summary>
        /// Whether this transaction was already reconciled (posted to ledger)
        /// </summary>
        public bool IsReconciled { get; set; }

        /// <summary>
        /// If reconciled, the type and ID of the linked record
        /// </summary>
        public string? ReconciledType { get; set; }
        public Guid? ReconciledId { get; set; }
    }

    /// <summary>
    /// Information about a detected reversal transaction
    /// </summary>
    public class ReversalDetectionResult
    {
        /// <summary>
        /// Whether this transaction appears to be a reversal
        /// </summary>
        public bool IsReversal { get; set; }

        /// <summary>
        /// The pattern that was detected (e.g., "REV-", "REVERSAL", "R-")
        /// </summary>
        public string? DetectedPattern { get; set; }

        /// <summary>
        /// Confidence level (0-100)
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// The extracted original reference if found in description
        /// </summary>
        public string? ExtractedOriginalReference { get; set; }

        /// <summary>
        /// Suggested original transactions to pair with
        /// </summary>
        public List<ReversalMatchSuggestion> SuggestedOriginals { get; set; } = new();
    }
}
