namespace Core.Entities.Tax
{
    /// <summary>
    /// Tracks individual MAT credit utilizations across assessment years.
    /// When normal tax exceeds MAT, available MAT credits can be utilized.
    /// </summary>
    public class MatCreditUtilization
    {
        public Guid Id { get; set; }
        public Guid MatCreditId { get; set; }

        // Year in which credit is being utilized
        public string UtilizationYear { get; set; } = string.Empty;
        public Guid? AssessmentId { get; set; }

        // Amount utilized from this credit entry
        public decimal AmountUtilized { get; set; }

        // Balance after this utilization
        public decimal BalanceAfter { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
