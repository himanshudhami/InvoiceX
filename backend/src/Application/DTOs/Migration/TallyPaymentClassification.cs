namespace Application.DTOs.Migration
{
    /// <summary>
    /// Classification of Tally Payment vouchers for routing to appropriate handlers
    /// </summary>
    public enum TallyPaymentType
    {
        /// <summary>Sundry Creditors - existing vendor payment flow</summary>
        Vendor,

        /// <summary>CONSULTANTS group - contractor payments (this plan)</summary>
        Contractor,

        /// <summary>EPF/ESI/TDS/PT statutory remittances (Plan 2)</summary>
        Statutory,

        /// <summary>Salary Payable entries</summary>
        Salary,

        /// <summary>EMI/Loan payments</summary>
        LoanEmi,

        /// <summary>Bank service charges</summary>
        BankCharge,

        /// <summary>Contra-like internal transfers</summary>
        InternalTransfer,

        /// <summary>Fallback - import as journal entry</summary>
        Other
    }

    /// <summary>
    /// Result of classifying a Tally Payment voucher
    /// </summary>
    public class TallyPaymentClassificationResult
    {
        /// <summary>
        /// The determined payment type
        /// </summary>
        public TallyPaymentType Type { get; set; }

        /// <summary>
        /// The payee/target ledger name (e.g., contractor name)
        /// </summary>
        public string? TargetLedgerName { get; set; }

        /// <summary>
        /// The payee/target ledger GUID from Tally
        /// </summary>
        public string? TargetLedgerGuid { get; set; }

        /// <summary>
        /// The Tally parent group (e.g., 'CONSULTANTS', 'Sundry Creditors')
        /// </summary>
        public string? ParentGroup { get; set; }

        /// <summary>
        /// Payment amount (absolute value)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Optional: Resolved party ID if found
        /// </summary>
        public Guid? PartyId { get; set; }

        /// <summary>
        /// Classification confidence/reason for debugging
        /// </summary>
        public string? ClassificationReason { get; set; }
    }
}
