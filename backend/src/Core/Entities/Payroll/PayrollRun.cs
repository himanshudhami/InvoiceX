namespace Core.Entities.Payroll
{
    /// <summary>
    /// Monthly payroll processing batch
    /// </summary>
    public class PayrollRun
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Period ====================

        /// <summary>
        /// Payroll month (1-12)
        /// </summary>
        public int PayrollMonth { get; set; }

        /// <summary>
        /// Payroll year
        /// </summary>
        public int PayrollYear { get; set; }

        /// <summary>
        /// Financial year (e.g., '2024-25')
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        // ==================== Status ====================

        /// <summary>
        /// Status: draft, processing, computed, approved, paid, cancelled
        /// </summary>
        public string Status { get; set; } = "draft";

        // ==================== Summary Totals ====================

        public int TotalEmployees { get; set; }
        public int TotalContractors { get; set; }
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNetSalary { get; set; }
        public decimal TotalEmployerPf { get; set; }
        public decimal TotalEmployerEsi { get; set; }

        /// <summary>
        /// Total cost to company (Gross + employer contributions)
        /// </summary>
        public decimal TotalEmployerCost { get; set; }

        // ==================== Workflow Audit ====================

        public string? ComputedBy { get; set; }
        public DateTime? ComputedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? PaidBy { get; set; }
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// Bank payment reference
        /// </summary>
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Payment mode (e.g., 'neft_batch', 'manual')
        /// </summary>
        public string? PaymentMode { get; set; }

        // ==================== Notes ====================

        public string? Remarks { get; set; }

        // ==================== Metadata ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation property
        public Companies? Company { get; set; }
        public ICollection<PayrollTransaction>? Transactions { get; set; }
    }
}
