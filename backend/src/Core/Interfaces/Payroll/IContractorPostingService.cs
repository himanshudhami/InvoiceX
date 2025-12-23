using Core.Entities.Ledger;

namespace Core.Interfaces.Payroll
{
    /// <summary>
    /// Service interface for posting contractor payment journal entries.
    ///
    /// Implements two-stage journal model following Indian accounting standards:
    /// 1. Accrual (on approval) - expense recognition with TDS liability
    /// 2. Disbursement (on payment) - contractor payable settlement
    ///
    /// TDS Sections supported:
    /// - 194C: Contractor payments (1% individuals/HUF, 2% others)
    /// - 194J: Professional/Technical fees (10%)
    ///
    /// Journal Entry Structure (as per CA best practices):
    ///
    /// ACCRUAL (On Approval):
    /// Dr. Professional Fees (5550)           [Gross Amount]
    /// Dr. Input GST (1210)                   [GST Amount, if applicable]
    ///     Cr. TDS Payable - 194J/194C (2213/2214)  [TDS Amount]
    ///     Cr. Contractor Payable (2101)            [Net Payable]
    ///
    /// DISBURSEMENT (On Payment):
    /// Dr. Contractor Payable (2101)          [Net Payable]
    ///     Cr. Bank Account (1112)            [Net Payable]
    ///
    /// Note: TDS is deducted on base amount only (excluding GST) as per Indian tax law.
    /// </summary>
    public interface IContractorPostingService
    {
        /// <summary>
        /// Posts accrual journal entry when contractor payment is approved.
        /// Records expense recognition with TDS and contractor payable liabilities.
        /// </summary>
        /// <param name="contractorPaymentId">ID of the approved contractor payment</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Created journal entry or existing entry if already posted (idempotent)</returns>
        Task<JournalEntry?> PostAccrualAsync(Guid contractorPaymentId, Guid? postedBy = null);

        /// <summary>
        /// Posts disbursement journal entry when contractor is paid.
        /// Clears contractor payable liability, credits bank account.
        /// </summary>
        /// <param name="contractorPaymentId">ID of the paid contractor payment</param>
        /// <param name="bankAccountId">Bank account used for payment</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Created journal entry or existing entry if already posted (idempotent)</returns>
        Task<JournalEntry?> PostDisbursementAsync(
            Guid contractorPaymentId,
            Guid bankAccountId,
            Guid? postedBy = null);

        /// <summary>
        /// Reverses a contractor payment journal entry.
        /// Creates a reversal entry with opposite debits/credits.
        /// </summary>
        /// <param name="journalEntryId">ID of the journal entry to reverse</param>
        /// <param name="reversedBy">User ID who triggered the reversal</param>
        /// <param name="reason">Reason for reversal</param>
        /// <returns>Created reversal entry or null if failed</returns>
        Task<JournalEntry?> ReverseEntryAsync(
            Guid journalEntryId,
            Guid reversedBy,
            string reason);

        /// <summary>
        /// Checks if accrual entry exists for contractor payment
        /// </summary>
        Task<bool> HasAccrualEntryAsync(Guid contractorPaymentId);

        /// <summary>
        /// Checks if disbursement entry exists for contractor payment
        /// </summary>
        Task<bool> HasDisbursementEntryAsync(Guid contractorPaymentId);

        /// <summary>
        /// Gets all journal entries related to a contractor payment
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetPaymentEntriesAsync(Guid contractorPaymentId);

        /// <summary>
        /// Gets contractor payment posting summary for a company and period
        /// </summary>
        Task<ContractorPostingSummary> GetPostingSummaryAsync(
            Guid companyId,
            int paymentMonth,
            int paymentYear);
    }

    /// <summary>
    /// Summary of contractor payment journal postings for a period
    /// </summary>
    public class ContractorPostingSummary
    {
        public int PaymentMonth { get; set; }
        public int PaymentYear { get; set; }
        public int TotalPayments { get; set; }
        public int PaymentsWithAccrual { get; set; }
        public int PaymentsWithDisbursement { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalTdsAmount { get; set; }
        public decimal TotalGstAmount { get; set; }
        public decimal TotalNetPayable { get; set; }
    }
}
