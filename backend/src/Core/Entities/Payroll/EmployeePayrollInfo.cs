namespace Core.Entities.Payroll
{
    /// <summary>
    /// Employee payroll-specific information (separate from main employee record)
    /// </summary>
    public class EmployeePayrollInfo
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Statutory Numbers ====================

        /// <summary>
        /// Universal Account Number for PF
        /// </summary>
        public string? Uan { get; set; }

        /// <summary>
        /// PF Account Number
        /// </summary>
        public string? PfAccountNumber { get; set; }

        /// <summary>
        /// ESI IP Number
        /// </summary>
        public string? EsiNumber { get; set; }

        /// <summary>
        /// PAN Number
        /// </summary>
        public string? PanNumber { get; set; }

        // ==================== Statutory Applicability ====================

        /// <summary>
        /// Whether PF is applicable for this employee
        /// </summary>
        public bool IsPfApplicable { get; set; } = true;

        /// <summary>
        /// Whether ESI is applicable for this employee
        /// </summary>
        public bool IsEsiApplicable { get; set; } = false;

        /// <summary>
        /// Whether PT is applicable for this employee
        /// </summary>
        public bool IsPtApplicable { get; set; } = true;

        /// <summary>
        /// For restricted_pf mode: if true, employee PF is calculated on statutory minimum
        /// instead of full wage (only applicable for employees earning above PF ceiling)
        /// </summary>
        public bool OptedForRestrictedPf { get; set; } = false;

        // ==================== Tax Information ====================

        /// <summary>
        /// Tax regime: 'old' or 'new'
        /// </summary>
        public string TaxRegime { get; set; } = "new";

        /// <summary>
        /// Tax residency status: 'resident', 'non_resident', 'rnor' (Resident but Not Ordinarily Resident)
        /// </summary>
        public string ResidentialStatus { get; set; } = "resident";

        /// <summary>
        /// Date of birth for senior citizen tax benefits (60+ for senior, 80+ for super senior)
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Date from which current tax regime applies (for audit trail)
        /// </summary>
        public DateTime? TaxRegimeEffectiveFrom { get; set; }

        /// <summary>
        /// State where employee works - determines professional tax slab to apply
        /// </summary>
        public string? WorkState { get; set; }

        // ==================== Payroll Type ====================

        /// <summary>
        /// Type of payroll: 'employee' or 'contractor'
        /// </summary>
        public string PayrollType { get; set; } = "employee";

        // ==================== Bank Details ====================

        /// <summary>
        /// Bank account for salary credit
        /// </summary>
        public string? BankAccountNumber { get; set; }

        public string? BankName { get; set; }

        public string? BankIfsc { get; set; }

        // ==================== Employment Dates ====================

        /// <summary>
        /// Date of joining
        /// </summary>
        public DateTime? DateOfJoining { get; set; }

        /// <summary>
        /// Date of leaving (termination/resignation)
        /// </summary>
        public DateTime? DateOfLeaving { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Whether payroll is active for this employee
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ==================== Metadata ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Employees? Employee { get; set; }
        public Companies? Company { get; set; }
    }
}
