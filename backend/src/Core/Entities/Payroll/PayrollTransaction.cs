namespace Core.Entities.Payroll
{
    /// <summary>
    /// Individual employee salary record for a payroll run
    /// </summary>
    public class PayrollTransaction
    {
        public Guid Id { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? SalaryStructureId { get; set; }

        // ==================== Period ====================

        public int PayrollMonth { get; set; }
        public int PayrollYear { get; set; }

        // ==================== Employee Type ====================

        /// <summary>
        /// Payroll type: 'employee' or 'contractor'
        /// </summary>
        public string PayrollType { get; set; } = "employee";

        // ==================== Attendance ====================

        public int WorkingDays { get; set; } = 30;
        public int PresentDays { get; set; } = 30;

        /// <summary>
        /// Loss of Pay days
        /// </summary>
        public int LopDays { get; set; } = 0;

        // ==================== EARNINGS ====================

        // Fixed Components
        public decimal BasicEarned { get; set; }
        public decimal HraEarned { get; set; }
        public decimal DaEarned { get; set; }
        public decimal ConveyanceEarned { get; set; }
        public decimal MedicalEarned { get; set; }
        public decimal SpecialAllowanceEarned { get; set; }
        public decimal OtherAllowancesEarned { get; set; }

        // Variable/One-time Earnings
        public decimal LtaPaid { get; set; }
        public decimal BonusPaid { get; set; }
        public decimal Arrears { get; set; }
        public decimal Reimbursements { get; set; }
        public decimal Incentives { get; set; }
        public decimal OtherEarnings { get; set; }

        /// <summary>
        /// Gross earnings (sum of all earnings)
        /// </summary>
        public decimal GrossEarnings { get; set; }

        // ==================== DEDUCTIONS ====================

        // Statutory Deductions
        public decimal PfEmployee { get; set; }
        public decimal EsiEmployee { get; set; }
        public decimal ProfessionalTax { get; set; }
        public decimal TdsDeducted { get; set; }

        // Other Deductions
        public decimal LoanRecovery { get; set; }
        public decimal AdvanceRecovery { get; set; }
        public decimal OtherDeductions { get; set; }

        /// <summary>
        /// Total deductions
        /// </summary>
        public decimal TotalDeductions { get; set; }

        // ==================== NET PAY ====================

        public decimal NetPayable { get; set; }

        // ==================== EMPLOYER CONTRIBUTIONS ====================

        public decimal PfEmployer { get; set; }
        public decimal PfAdminCharges { get; set; }
        public decimal PfEdli { get; set; }
        public decimal EsiEmployer { get; set; }
        public decimal GratuityProvision { get; set; }
        public decimal TotalEmployerCost { get; set; }

        // ==================== TDS CALCULATION ====================

        /// <summary>
        /// TDS calculation breakdown (JSON)
        /// </summary>
        public string? TdsCalculation { get; set; }

        /// <summary>
        /// HR overridden TDS amount (if manually adjusted)
        /// </summary>
        public decimal? TdsHrOverride { get; set; }

        public string? TdsOverrideReason { get; set; }

        // ==================== PAYMENT INFO ====================

        /// <summary>
        /// Status: computed, approved, paid, cancelled, on_hold
        /// </summary>
        public string Status { get; set; } = "computed";

        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public string? BankAccount { get; set; }

        // ==================== Notes ====================

        public string? Remarks { get; set; }

        // ==================== Metadata ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public PayrollRun? PayrollRun { get; set; }
        public Employees? Employee { get; set; }
        public EmployeeSalaryStructure? SalaryStructure { get; set; }
    }
}
