using Core.Entities.Ledger;

namespace Core.Interfaces.Payroll
{
    /// <summary>
    /// Service interface for posting payroll-related journal entries
    /// Implements three-stage journal model:
    /// 1. Accrual (on approval) - expense recognition
    /// 2. Disbursement (on payment) - salary payment to employees
    /// 3. Statutory Remittance (on challan) - TDS/PF/ESI/PT payment to government
    /// </summary>
    public interface IPayrollPostingService
    {
        /// <summary>
        /// Posts accrual journal entry when payroll is approved
        /// Creates expense recognition entries with all statutory liabilities
        /// </summary>
        /// <param name="payrollRunId">ID of the approved payroll run</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Created journal entry or null if already exists or invalid</returns>
        Task<JournalEntry?> PostAccrualAsync(Guid payrollRunId, Guid? postedBy = null);

        /// <summary>
        /// Posts disbursement journal entry when salaries are paid
        /// Clears net salary payable, credits bank account
        /// </summary>
        /// <param name="payrollRunId">ID of the paid payroll run</param>
        /// <param name="bankAccountId">Bank account used for payment</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Created journal entry or null if already exists or invalid</returns>
        Task<JournalEntry?> PostDisbursementAsync(
            Guid payrollRunId,
            Guid bankAccountId,
            Guid? postedBy = null);

        /// <summary>
        /// Posts statutory payment journal entry (TDS/PF/ESI/PT challan)
        /// Clears statutory payable, credits bank account
        /// </summary>
        /// <param name="statutoryPaymentId">ID of the statutory payment record</param>
        /// <param name="bankAccountId">Bank account used for payment (optional)</param>
        /// <param name="postedBy">User ID who triggered the posting</param>
        /// <returns>Created journal entry or null if already exists or invalid</returns>
        Task<JournalEntry?> PostStatutoryPaymentAsync(
            Guid statutoryPaymentId,
            Guid? bankAccountId = null,
            Guid? postedBy = null);

        /// <summary>
        /// Reverses a payroll journal entry (for corrections)
        /// Creates a reversal entry with opposite debits/credits
        /// </summary>
        /// <param name="journalEntryId">ID of the journal entry to reverse</param>
        /// <param name="reversedBy">User ID who triggered the reversal</param>
        /// <param name="reason">Reason for reversal</param>
        /// <returns>Created reversal entry or null if failed</returns>
        Task<JournalEntry?> ReversePayrollEntryAsync(
            Guid journalEntryId,
            Guid reversedBy,
            string reason);

        /// <summary>
        /// Checks if accrual entry exists for payroll run
        /// </summary>
        Task<bool> HasAccrualEntryAsync(Guid payrollRunId);

        /// <summary>
        /// Checks if disbursement entry exists for payroll run
        /// </summary>
        Task<bool> HasDisbursementEntryAsync(Guid payrollRunId);

        /// <summary>
        /// Gets all journal entries related to a payroll run
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetPayrollEntriesAsync(Guid payrollRunId);

        /// <summary>
        /// Gets payroll posting summary for a company and period
        /// </summary>
        Task<PayrollPostingSummary?> GetPostingSummaryAsync(
            Guid companyId,
            int payrollMonth,
            int payrollYear);
    }

    /// <summary>
    /// Summary of payroll journal postings for a period
    /// </summary>
    public class PayrollPostingSummary
    {
        public Guid PayrollRunId { get; set; }
        public int PayrollMonth { get; set; }
        public int PayrollYear { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string PayrollStatus { get; set; } = string.Empty;

        // Accrual entry info
        public bool HasAccrualEntry { get; set; }
        public Guid? AccrualJournalEntryId { get; set; }
        public string? AccrualJournalNumber { get; set; }
        public DateOnly? AccrualDate { get; set; }

        // Disbursement entry info
        public bool HasDisbursementEntry { get; set; }
        public Guid? DisbursementJournalEntryId { get; set; }
        public string? DisbursementJournalNumber { get; set; }
        public DateOnly? DisbursementDate { get; set; }

        // Amounts
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetSalary { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalEmployerCost { get; set; }
    }
}
