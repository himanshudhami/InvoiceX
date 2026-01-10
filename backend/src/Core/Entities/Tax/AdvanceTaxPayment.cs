namespace Core.Entities.Tax
{
    /// <summary>
    /// Individual advance tax payment record with challan details.
    /// </summary>
    public class AdvanceTaxPayment
    {
        public Guid Id { get; set; }

        public Guid AssessmentId { get; set; }

        /// <summary>
        /// Optional link to specific quarter schedule
        /// </summary>
        public Guid? ScheduleId { get; set; }

        // ==================== Payment Details ====================

        public DateOnly PaymentDate { get; set; }
        public decimal Amount { get; set; }

        // ==================== Challan Details ====================

        /// <summary>
        /// Challan serial number
        /// </summary>
        public string? ChallanNumber { get; set; }

        /// <summary>
        /// BSR Code of the bank branch
        /// </summary>
        public string? BsrCode { get; set; }

        /// <summary>
        /// Challan Identification Number (CIN)
        /// </summary>
        public string? Cin { get; set; }

        // ==================== Bank & Accounting ====================

        public Guid? BankAccountId { get; set; }
        public Guid? JournalEntryId { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Status: pending, completed, failed
        /// </summary>
        public string Status { get; set; } = "completed";

        public string? Notes { get; set; }

        // ==================== Audit ====================

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
