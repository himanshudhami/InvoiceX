namespace Core.Entities.Payroll
{
    /// <summary>
    /// Tracks ESI eligibility periods for 6-month rule compliance.
    /// ESI contribution periods: April-September (Period 1) and October-March (Period 2)
    /// Once eligible at start of period, employee must contribute until period ends,
    /// even if salary crosses the ceiling mid-period.
    /// </summary>
    public class EsiEligibilityPeriod
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid CompanyId { get; set; }

        /// <summary>
        /// First day of contribution period (April 1 or October 1)
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Last day of contribution period (September 30 or March 31)
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// ESI contribution period: 'apr_sep' or 'oct_mar'
        /// </summary>
        public string ContributionPeriod { get; set; } = string.Empty;

        /// <summary>
        /// Financial year e.g., '2024-25'
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Gross salary when period started (used to determine initial eligibility)
        /// </summary>
        public decimal InitialGrossSalary { get; set; }

        /// <summary>
        /// Whether gross was within ESI ceiling at period start
        /// </summary>
        public bool WasEligibleAtStart { get; set; }

        /// <summary>
        /// Current status of this eligibility period
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date when gross exceeded ESI ceiling (if applicable)
        /// Employee still contributes until period end
        /// </summary>
        public DateTime? CrossedCeilingDate { get; set; }

        /// <summary>
        /// Gross salary when ceiling was crossed
        /// </summary>
        public decimal? CrossedCeilingGross { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
