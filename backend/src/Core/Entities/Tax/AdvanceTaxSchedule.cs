namespace Core.Entities.Tax
{
    /// <summary>
    /// Quarterly payment schedule for advance tax.
    /// Per Section 211, corporates must pay in 4 installments:
    /// - Q1 (15 Jun): 15% cumulative
    /// - Q2 (15 Sep): 45% cumulative
    /// - Q3 (15 Dec): 75% cumulative
    /// - Q4 (15 Mar): 100% cumulative
    /// </summary>
    public class AdvanceTaxSchedule
    {
        public Guid Id { get; set; }

        public Guid AssessmentId { get; set; }

        // ==================== Quarter Details ====================

        /// <summary>
        /// Quarter number: 1 (Jun), 2 (Sep), 3 (Dec), 4 (Mar)
        /// </summary>
        public int Quarter { get; set; }

        /// <summary>
        /// Due date for this quarter (15th of the month)
        /// </summary>
        public DateOnly DueDate { get; set; }

        // ==================== Cumulative Requirements ====================

        /// <summary>
        /// Cumulative percentage due by this quarter (15, 45, 75, 100)
        /// </summary>
        public decimal CumulativePercentage { get; set; }

        /// <summary>
        /// Cumulative tax amount due by this quarter
        /// </summary>
        public decimal CumulativeTaxDue { get; set; }

        /// <summary>
        /// Tax payable in this specific quarter
        /// </summary>
        public decimal TaxPayableThisQuarter { get; set; }

        // ==================== Actual Payments ====================

        /// <summary>
        /// Tax actually paid in this quarter
        /// </summary>
        public decimal TaxPaidThisQuarter { get; set; }

        /// <summary>
        /// Cumulative tax paid up to and including this quarter
        /// </summary>
        public decimal CumulativeTaxPaid { get; set; }

        // ==================== Shortfall & Interest ====================

        /// <summary>
        /// Shortfall amount (cumulative due - cumulative paid)
        /// </summary>
        public decimal ShortfallAmount { get; set; }

        /// <summary>
        /// Interest u/s 234C for this quarter's deferment
        /// </summary>
        public decimal Interest234C { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Payment status: pending, partial, paid, overdue
        /// </summary>
        public string PaymentStatus { get; set; } = "pending";

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
