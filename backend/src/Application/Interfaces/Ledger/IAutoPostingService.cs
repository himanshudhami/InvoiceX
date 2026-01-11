using Core.Entities.Ledger;

namespace Application.Interfaces.Ledger
{
    /// <summary>
    /// Service for automatically posting journal entries based on business events
    /// </summary>
    public interface IAutoPostingService
    {
        /// <summary>
        /// Create and post a journal entry for an invoice finalization
        /// </summary>
        Task<JournalEntry?> PostInvoiceAsync(
            Guid invoiceId,
            Guid? postedBy = null,
            bool autoPost = true);

        /// <summary>
        /// Create and post a journal entry for a payment receipt
        /// </summary>
        Task<JournalEntry?> PostPaymentAsync(
            Guid paymentId,
            Guid? postedBy = null,
            bool autoPost = true);

        // Note: Payroll posting uses dedicated IPayrollPostingService
        // Note: Expense claim posting uses dedicated IExpensePostingService
        // Note: Contractor payment posting uses dedicated IContractorPostingService

        /// <summary>
        /// Create and post a journal entry for an expense
        /// </summary>
        Task<JournalEntry?> PostExpenseAsync(
            Guid expenseId,
            Guid? postedBy = null,
            bool autoPost = true);

        /// <summary>
        /// Create and post a journal entry for a vendor invoice (purchase)
        /// </summary>
        Task<JournalEntry?> PostVendorInvoiceAsync(
            Guid vendorInvoiceId,
            Guid? postedBy = null,
            bool autoPost = true);

        /// <summary>
        /// Create and post a journal entry for a vendor payment (outgoing)
        /// </summary>
        Task<JournalEntry?> PostVendorPaymentAsync(
            Guid vendorPaymentId,
            Guid? postedBy = null,
            bool autoPost = true);

        /// <summary>
        /// Create and post a journal entry for a loan EMI payment
        /// </summary>
        Task<JournalEntry?> PostLoanPaymentAsync(
            Guid loanTransactionId,
            Guid loanId,
            Guid? postedBy = null,
            bool autoPost = true);

        /// <summary>
        /// Create and post a journal entry for a loan prepayment
        /// </summary>
        Task<JournalEntry?> PostLoanPrepaymentAsync(
            Guid loanTransactionId,
            Guid loanId,
            Guid? postedBy = null,
            bool autoPost = true);

        /// <summary>
        /// Create and post a journal entry for a generic source
        /// </summary>
        Task<JournalEntry?> PostFromSourceAsync(
            string sourceType,
            Guid sourceId,
            Dictionary<string, object> sourceData,
            Guid? postedBy = null,
            bool autoPost = true);

        /// <summary>
        /// Reverse an existing journal entry
        /// </summary>
        Task<JournalEntry?> ReverseEntryAsync(Guid journalEntryId, Guid reversedBy, string? reason = null);

        /// <summary>
        /// Check if auto-posting is enabled for a company
        /// </summary>
        Task<bool> IsAutoPostingEnabledAsync(Guid companyId);
    }
}
