using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BankTransactions
{
    /// <summary>
    /// Data transfer object for importing bank transactions from CSV
    /// </summary>
    public class ImportBankTransactionDto
    {
        /// <summary>
        /// Date of the transaction
        /// </summary>
        [Required(ErrorMessage = "Transaction date is required")]
        public DateOnly TransactionDate { get; set; }

        /// <summary>
        /// Value date (settlement date)
        /// </summary>
        public DateOnly? ValueDate { get; set; }

        /// <summary>
        /// Transaction description/narration from bank statement
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Bank reference number
        /// </summary>
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Cheque number if applicable
        /// </summary>
        public string? ChequeNumber { get; set; }

        /// <summary>
        /// Transaction type: credit or debit
        /// </summary>
        [Required(ErrorMessage = "Transaction type is required")]
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Transaction amount (always positive)
        /// </summary>
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Balance after this transaction
        /// </summary>
        public decimal? BalanceAfter { get; set; }

        /// <summary>
        /// Raw data from CSV row (for audit trail)
        /// </summary>
        public string? RawData { get; set; }
    }

    /// <summary>
    /// Request for importing bank transactions
    /// </summary>
    public class ImportBankTransactionsRequest
    {
        /// <summary>
        /// Bank account to import transactions into
        /// </summary>
        [Required(ErrorMessage = "Bank account ID is required")]
        public Guid BankAccountId { get; set; }

        /// <summary>
        /// Transactions to import
        /// </summary>
        [Required(ErrorMessage = "Transactions are required")]
        public List<ImportBankTransactionDto> Transactions { get; set; } = new();

        /// <summary>
        /// Whether to skip duplicates (based on transaction hash)
        /// </summary>
        public bool SkipDuplicates { get; set; } = true;
    }

    /// <summary>
    /// Result of import operation
    /// </summary>
    public class ImportBankTransactionsResult
    {
        /// <summary>
        /// Number of transactions successfully imported
        /// </summary>
        public int ImportedCount { get; set; }

        /// <summary>
        /// Number of duplicate transactions skipped
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Number of transactions that failed to import
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Import batch ID for tracking
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Error messages for failed imports
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}
