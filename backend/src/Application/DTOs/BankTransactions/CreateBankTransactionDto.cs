using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BankTransactions
{
    /// <summary>
    /// Data transfer object for creating a bank transaction
    /// </summary>
    public class CreateBankTransactionDto
    {
        /// <summary>
        /// Bank account this transaction belongs to
        /// </summary>
        [Required(ErrorMessage = "Bank account ID is required")]
        public Guid BankAccountId { get; set; }

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
        /// Transaction description/narration from bank
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Bank reference number (UTR, NEFT ref, etc.)
        /// </summary>
        [StringLength(255, ErrorMessage = "Reference number cannot exceed 255 characters")]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Cheque number if applicable
        /// </summary>
        [StringLength(50, ErrorMessage = "Cheque number cannot exceed 50 characters")]
        public string? ChequeNumber { get; set; }

        /// <summary>
        /// Transaction type: credit (money in) or debit (money out)
        /// </summary>
        [Required(ErrorMessage = "Transaction type is required")]
        [StringLength(20, ErrorMessage = "Transaction type cannot exceed 20 characters")]
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
        /// Category: customer_payment, vendor_payment, salary, tax, bank_charges, transfer, other
        /// </summary>
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string? Category { get; set; }
    }
}
