using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BankTransactions
{
    /// <summary>
    /// Data transfer object for updating a bank transaction
    /// </summary>
    public class UpdateBankTransactionDto
    {
        /// <summary>
        /// Date of the transaction
        /// </summary>
        public DateOnly? TransactionDate { get; set; }

        /// <summary>
        /// Value date (settlement date)
        /// </summary>
        public DateOnly? ValueDate { get; set; }

        /// <summary>
        /// Transaction description/narration
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Bank reference number
        /// </summary>
        [StringLength(255, ErrorMessage = "Reference number cannot exceed 255 characters")]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Cheque number if applicable
        /// </summary>
        [StringLength(50, ErrorMessage = "Cheque number cannot exceed 50 characters")]
        public string? ChequeNumber { get; set; }

        /// <summary>
        /// Transaction type: credit or debit
        /// </summary>
        [StringLength(20, ErrorMessage = "Transaction type cannot exceed 20 characters")]
        public string? TransactionType { get; set; }

        /// <summary>
        /// Transaction amount
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// Balance after this transaction
        /// </summary>
        public decimal? BalanceAfter { get; set; }

        /// <summary>
        /// Category
        /// </summary>
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string? Category { get; set; }
    }
}
