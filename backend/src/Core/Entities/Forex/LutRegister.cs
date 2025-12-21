namespace Core.Entities.Forex
{
    /// <summary>
    /// GST Letter of Undertaking register for zero-rated export supplies
    /// </summary>
    public class LutRegister
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // LUT details
        public string LutNumber { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;  // 2025-26
        public string Gstin { get; set; } = string.Empty;

        // Validity
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidTo { get; set; }

        // Filing details
        public DateOnly? FilingDate { get; set; }
        public string? Arn { get; set; }  // Application Reference Number

        // Status
        public string Status { get; set; } = "active";  // active, expired, superseded, cancelled

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Companies? Company { get; set; }

        /// <summary>
        /// Check if this LUT is valid for a given date
        /// </summary>
        public bool IsValidForDate(DateOnly date)
        {
            return Status == "active" && date >= ValidFrom && date <= ValidTo;
        }
    }
}
