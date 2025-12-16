namespace Core.Entities.Payroll
{
    /// <summary>
    /// Employee salary structure (CTC breakdown) - effective dated for salary revisions
    /// </summary>
    public class EmployeeSalaryStructure
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Effective Dating ====================

        /// <summary>
        /// Date from which this structure is effective
        /// </summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>
        /// Date until which this structure is effective (null = current)
        /// </summary>
        public DateTime? EffectiveTo { get; set; }

        // ==================== Annual CTC ====================

        /// <summary>
        /// Total annual Cost-To-Company
        /// </summary>
        public decimal AnnualCtc { get; set; }

        // ==================== Monthly Fixed Components ====================

        /// <summary>
        /// Basic salary - base for PF calculation
        /// </summary>
        public decimal BasicSalary { get; set; }

        /// <summary>
        /// House Rent Allowance
        /// </summary>
        public decimal Hra { get; set; }

        /// <summary>
        /// Dearness Allowance (if applicable)
        /// </summary>
        public decimal DearnessAllowance { get; set; }

        /// <summary>
        /// Conveyance/Transport Allowance
        /// </summary>
        public decimal ConveyanceAllowance { get; set; }

        /// <summary>
        /// Medical Allowance
        /// </summary>
        public decimal MedicalAllowance { get; set; }

        /// <summary>
        /// Special Allowance (balancing figure)
        /// </summary>
        public decimal SpecialAllowance { get; set; }

        /// <summary>
        /// Other fixed allowances
        /// </summary>
        public decimal OtherAllowances { get; set; }

        // ==================== Annual Components ====================

        /// <summary>
        /// Leave Travel Allowance (annual)
        /// </summary>
        public decimal LtaAnnual { get; set; }

        /// <summary>
        /// Bonus (statutory/performance) annual
        /// </summary>
        public decimal BonusAnnual { get; set; }

        // ==================== Employer Contributions (Part of CTC) ====================

        /// <summary>
        /// Employer PF contribution (monthly)
        /// </summary>
        public decimal PfEmployerMonthly { get; set; }

        /// <summary>
        /// Employer ESI contribution (monthly)
        /// </summary>
        public decimal EsiEmployerMonthly { get; set; }

        /// <summary>
        /// Gratuity provision (monthly)
        /// </summary>
        public decimal GratuityMonthly { get; set; }

        // ==================== Calculated ====================

        /// <summary>
        /// Monthly gross salary (sum of all earnings)
        /// </summary>
        public decimal MonthlyGross { get; set; }

        // ==================== Metadata ====================

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Reason for revision (e.g., 'Annual Increment', 'Promotion')
        /// </summary>
        public string? RevisionReason { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public Employees? Employee { get; set; }
        public Companies? Company { get; set; }
    }
}
