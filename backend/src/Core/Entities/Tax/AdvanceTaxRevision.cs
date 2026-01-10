namespace Core.Entities.Tax
{
    /// <summary>
    /// Tracks quarterly revisions to advance tax assessments.
    /// CAs revise estimates each quarter as actuals become clearer.
    /// </summary>
    public class AdvanceTaxRevision
    {
        public Guid Id { get; set; }
        public Guid AssessmentId { get; set; }

        // Revision context
        public int RevisionNumber { get; set; }
        public int RevisionQuarter { get; set; }
        public DateOnly RevisionDate { get; set; }

        // Snapshot BEFORE revision
        public decimal PreviousProjectedRevenue { get; set; }
        public decimal PreviousProjectedExpenses { get; set; }
        public decimal PreviousTaxableIncome { get; set; }
        public decimal PreviousTotalTaxLiability { get; set; }
        public decimal PreviousNetTaxPayable { get; set; }

        // Snapshot AFTER revision
        public decimal RevisedProjectedRevenue { get; set; }
        public decimal RevisedProjectedExpenses { get; set; }
        public decimal RevisedTaxableIncome { get; set; }
        public decimal RevisedTotalTaxLiability { get; set; }
        public decimal RevisedNetTaxPayable { get; set; }

        // Variance (computed in DB, but also available here)
        public decimal RevenueVariance { get; set; }
        public decimal ExpenseVariance { get; set; }
        public decimal TaxableIncomeVariance { get; set; }
        public decimal TaxLiabilityVariance { get; set; }
        public decimal NetPayableVariance { get; set; }

        // Reason and notes
        public string? RevisionReason { get; set; }
        public string? Notes { get; set; }

        // Audit
        public Guid? RevisedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
