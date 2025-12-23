using Core.Entities.Ledger;

namespace Core.Interfaces.Expense
{
    /// <summary>
    /// Service interface for posting expense claim journal entries.
    ///
    /// Implements single-stage journal model following Indian accounting standards:
    /// - On reimbursement: Expense recognition with employee payable
    ///
    /// GST Treatment (as per GST Act 2017):
    /// - Intra-state: CGST + SGST on base amount
    /// - Inter-state: IGST on base amount
    /// - ITC (Input Tax Credit) eligible if proper invoice available
    ///
    /// Journal Entry Structure (as per CA best practices):
    ///
    /// ON REIMBURSEMENT (Without GST):
    /// Dr. Expense Account (category GL)        [Total Amount]
    ///     Cr. Employee Reimbursement Payable (2102)  [Total Amount]
    ///
    /// ON REIMBURSEMENT (With GST - Intra-State):
    /// Dr. Expense Account (category GL)        [Base Amount]
    /// Dr. CGST Input (1141)                    [CGST Amount]
    /// Dr. SGST Input (1142)                    [SGST Amount]
    ///     Cr. Employee Reimbursement Payable (2102)  [Total Amount]
    ///
    /// ON REIMBURSEMENT (With GST - Inter-State):
    /// Dr. Expense Account (category GL)        [Base Amount]
    /// Dr. IGST Input (1143)                    [IGST Amount]
    ///     Cr. Employee Reimbursement Payable (2102)  [Total Amount]
    ///
    /// Note: GST amounts are recorded as Input Tax Credit if ITC eligible.
    /// </summary>
    public interface IExpensePostingService
    {
        /// <summary>
        /// Posts journal entry when expense claim is reimbursed.
        /// Records expense and employee payable with GST treatment.
        /// </summary>
        /// <param name="expenseClaimId">ID of the reimbursed expense claim</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Created journal entry or existing entry if already posted (idempotent)</returns>
        Task<JournalEntry?> PostReimbursementAsync(Guid expenseClaimId, Guid? postedBy = null);

        /// <summary>
        /// Reverses an expense claim journal entry.
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
        /// Checks if journal entry exists for expense claim
        /// </summary>
        Task<bool> HasJournalEntryAsync(Guid expenseClaimId);

        /// <summary>
        /// Gets all journal entries related to an expense claim
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetClaimEntriesAsync(Guid expenseClaimId);

        /// <summary>
        /// Gets expense posting summary for a company and period
        /// </summary>
        Task<ExpensePostingSummary> GetPostingSummaryAsync(
            Guid companyId,
            int month,
            int year);
    }

    /// <summary>
    /// Summary of expense claim journal postings for a period
    /// </summary>
    public class ExpensePostingSummary
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalClaims { get; set; }
        public int ClaimsWithJournalEntry { get; set; }
        public decimal TotalExpenseAmount { get; set; }
        public decimal TotalGstAmount { get; set; }
        public decimal TotalReimbursementAmount { get; set; }
    }
}
