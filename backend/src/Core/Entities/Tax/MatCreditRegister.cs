namespace Core.Entities.Tax
{
    /// <summary>
    /// MAT Credit Register - tracks Minimum Alternate Tax credits per Section 115JAA.
    /// When MAT > Normal Tax, the difference becomes MAT Credit that can be carried forward for 15 years.
    /// </summary>
    public class MatCreditRegister
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // Financial year when MAT credit was created
        public string FinancialYear { get; set; } = string.Empty;
        public string AssessmentYear { get; set; } = string.Empty;

        // MAT computation for the year
        public decimal BookProfit { get; set; }
        public decimal MatRate { get; set; } = 15.00m;
        public decimal MatOnBookProfit { get; set; }
        public decimal MatSurcharge { get; set; }
        public decimal MatCess { get; set; }
        public decimal TotalMat { get; set; }

        // Normal tax for comparison
        public decimal NormalTax { get; set; }

        // MAT Credit created (MAT - Normal Tax, only if MAT > Normal Tax)
        public decimal MatCreditCreated { get; set; }

        // Utilization tracking
        public decimal MatCreditUtilized { get; set; }
        public decimal MatCreditBalance { get; set; }

        // Expiry (15 years from creation as per Section 115JAA)
        public string ExpiryYear { get; set; } = string.Empty;
        public bool IsExpired { get; set; }

        // Status: active, fully_utilized, expired, cancelled
        public string Status { get; set; } = "active";

        public string? Notes { get; set; }

        // Audit
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
