namespace Core.Entities.Tax
{
    /// <summary>
    /// Form 16 - TDS Certificate for Salary (Section 192).
    /// Issued by employer to employee certifying TDS deducted and deposited.
    ///
    /// Part A: Summary of TDS deducted and deposited (quarterly basis)
    /// Part B: Detailed salary computation and tax calculation
    ///
    /// Legal Requirements:
    /// - Must be issued by June 15 following the FY end
    /// - Format prescribed by CBDT
    /// - Unique certificate number per employee per FY
    /// </summary>
    public class Form16
    {
        public Guid Id { get; set; }

        // ==================== Company & Employee Linking ====================

        public Guid CompanyId { get; set; }
        public Guid EmployeeId { get; set; }

        /// <summary>
        /// Indian financial year in '2024-25' format
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Unique certificate number: COMPANY_TAN/FY/SERIAL
        /// </summary>
        public string CertificateNumber { get; set; } = string.Empty;

        // ==================== Part A - Deductor Details ====================

        /// <summary>
        /// Tax Deduction Account Number of employer
        /// </summary>
        public string Tan { get; set; } = string.Empty;

        /// <summary>
        /// PAN of the employer
        /// </summary>
        public string DeductorPan { get; set; } = string.Empty;

        public string DeductorName { get; set; } = string.Empty;
        public string DeductorAddress { get; set; } = string.Empty;
        public string DeductorCity { get; set; } = string.Empty;
        public string DeductorState { get; set; } = string.Empty;
        public string DeductorPincode { get; set; } = string.Empty;
        public string? DeductorEmail { get; set; }
        public string? DeductorPhone { get; set; }

        // ==================== Part A - Employee Details ====================

        public string EmployeePan { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeAddress { get; set; } = string.Empty;
        public string EmployeeCity { get; set; } = string.Empty;
        public string EmployeeState { get; set; } = string.Empty;
        public string EmployeePincode { get; set; } = string.Empty;
        public string? EmployeeEmail { get; set; }

        // ==================== Part A - Employment Period ====================

        public DateOnly PeriodFrom { get; set; }
        public DateOnly PeriodTo { get; set; }

        // ==================== Part A - Quarterly TDS Summary ====================

        public decimal Q1TdsDeducted { get; set; }
        public decimal Q1TdsDeposited { get; set; }
        public string? Q1ChallanDetails { get; set; }  // JSON: [{challanNo, bsrCode, depositDate, amount}]

        public decimal Q2TdsDeducted { get; set; }
        public decimal Q2TdsDeposited { get; set; }
        public string? Q2ChallanDetails { get; set; }

        public decimal Q3TdsDeducted { get; set; }
        public decimal Q3TdsDeposited { get; set; }
        public string? Q3ChallanDetails { get; set; }

        public decimal Q4TdsDeducted { get; set; }
        public decimal Q4TdsDeposited { get; set; }
        public string? Q4ChallanDetails { get; set; }

        public decimal TotalTdsDeducted { get; set; }
        public decimal TotalTdsDeposited { get; set; }

        // ==================== Part B - Salary Details (Section 17) ====================

        /// <summary>
        /// Salary as per Section 17(1)
        /// </summary>
        public decimal GrossSalary { get; set; }

        /// <summary>
        /// Value of perquisites under Section 17(2)
        /// </summary>
        public decimal Perquisites { get; set; }

        /// <summary>
        /// Profits in lieu of salary under Section 17(3)
        /// </summary>
        public decimal ProfitsInLieu { get; set; }

        public decimal TotalSalary { get; set; }

        // ==================== Part B - Exemptions (Section 10) ====================

        /// <summary>
        /// HRA exemption under Section 10(13A)
        /// </summary>
        public decimal HraExemption { get; set; }

        /// <summary>
        /// Leave Travel Allowance under Section 10(5)
        /// </summary>
        public decimal LtaExemption { get; set; }

        /// <summary>
        /// Other exemptions under Section 10
        /// </summary>
        public decimal OtherExemptions { get; set; }

        public decimal TotalExemptions { get; set; }

        // ==================== Part B - Deductions (Chapter VI-A) ====================

        /// <summary>
        /// Standard deduction (Section 16(ia)) - ₹75,000 for new regime, ₹50,000 for old
        /// </summary>
        public decimal StandardDeduction { get; set; }

        /// <summary>
        /// Entertainment allowance (Section 16(ii)) - Government employees only
        /// </summary>
        public decimal EntertainmentAllowance { get; set; }

        /// <summary>
        /// Professional tax (Section 16(iii))
        /// </summary>
        public decimal ProfessionalTax { get; set; }

        /// <summary>
        /// Section 80C investments (PPF, ELSS, LIC, etc.) - Max ₹1,50,000
        /// </summary>
        public decimal Section80C { get; set; }

        /// <summary>
        /// Section 80CCC - Pension contribution
        /// </summary>
        public decimal Section80CCC { get; set; }

        /// <summary>
        /// Section 80CCD(1) - NPS employee contribution - Max 10% of salary
        /// </summary>
        public decimal Section80CCD1 { get; set; }

        /// <summary>
        /// Section 80CCD(1B) - Additional NPS contribution - Max ₹50,000
        /// </summary>
        public decimal Section80CCD1B { get; set; }

        /// <summary>
        /// Section 80CCD(2) - Employer NPS contribution - Max 10%/14% of salary
        /// </summary>
        public decimal Section80CCD2 { get; set; }

        /// <summary>
        /// Section 80D - Health insurance premium
        /// </summary>
        public decimal Section80D { get; set; }

        /// <summary>
        /// Section 80E - Education loan interest
        /// </summary>
        public decimal Section80E { get; set; }

        /// <summary>
        /// Section 80G - Donations
        /// </summary>
        public decimal Section80G { get; set; }

        /// <summary>
        /// Section 80TTA/80TTB - Savings interest deduction
        /// </summary>
        public decimal Section80TTA { get; set; }

        /// <summary>
        /// Section 24(b) - Home loan interest - Max ₹2,00,000
        /// </summary>
        public decimal Section24 { get; set; }

        /// <summary>
        /// Other Chapter VI-A deductions
        /// </summary>
        public decimal OtherDeductions { get; set; }

        public decimal TotalDeductions { get; set; }

        // ==================== Part B - Tax Computation ====================

        /// <summary>
        /// Tax regime: 'old' or 'new'
        /// </summary>
        public string TaxRegime { get; set; } = "new";

        public decimal TaxableIncome { get; set; }

        /// <summary>
        /// Tax on total income as per slab rates
        /// </summary>
        public decimal TaxOnIncome { get; set; }

        /// <summary>
        /// Rebate under Section 87A
        /// </summary>
        public decimal Rebate87A { get; set; }

        /// <summary>
        /// Tax after rebate
        /// </summary>
        public decimal TaxAfterRebate { get; set; }

        /// <summary>
        /// Surcharge (applicable for income > 50L)
        /// </summary>
        public decimal Surcharge { get; set; }

        /// <summary>
        /// Health and Education Cess (4%)
        /// </summary>
        public decimal Cess { get; set; }

        /// <summary>
        /// Total tax liability
        /// </summary>
        public decimal TotalTaxLiability { get; set; }

        /// <summary>
        /// Relief under Section 89 (for arrears)
        /// </summary>
        public decimal Relief89 { get; set; }

        /// <summary>
        /// Net tax payable after relief
        /// </summary>
        public decimal NetTaxPayable { get; set; }

        // ==================== Part B - Other Income ====================

        /// <summary>
        /// Income from previous employer (if applicable)
        /// </summary>
        public decimal PreviousEmployerIncome { get; set; }

        /// <summary>
        /// TDS by previous employer
        /// </summary>
        public decimal PreviousEmployerTds { get; set; }

        /// <summary>
        /// Other income declared by employee
        /// </summary>
        public decimal OtherIncome { get; set; }

        // ==================== Verification & Signature ====================

        public string? VerifiedByName { get; set; }
        public string? VerifiedByDesignation { get; set; }
        public string? VerifiedByPan { get; set; }
        public string? Place { get; set; }
        public DateOnly? SignatureDate { get; set; }

        // ==================== Status & Workflow ====================

        /// <summary>
        /// Status: draft, generated, verified, issued, cancelled
        /// </summary>
        public string Status { get; set; } = "draft";

        public DateTime? GeneratedAt { get; set; }
        public Guid? GeneratedBy { get; set; }

        public DateTime? VerifiedAt { get; set; }
        public Guid? VerifiedBy { get; set; }

        public DateTime? IssuedAt { get; set; }
        public Guid? IssuedBy { get; set; }

        /// <summary>
        /// Path to generated PDF file
        /// </summary>
        public string? PdfPath { get; set; }

        /// <summary>
        /// Detailed salary breakdown JSON for Part B computation
        /// </summary>
        public string? SalaryBreakdownJson { get; set; }

        /// <summary>
        /// Tax computation breakdown JSON
        /// </summary>
        public string? TaxComputationJson { get; set; }

        // ==================== Audit Fields ====================

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        // ==================== Navigation Properties ====================

        public Companies? Company { get; set; }
        public Employees? Employee { get; set; }
    }

    /// <summary>
    /// Form 16 status constants
    /// </summary>
    public static class Form16Status
    {
        public const string Draft = "draft";
        public const string Generated = "generated";
        public const string Verified = "verified";
        public const string Issued = "issued";
        public const string Cancelled = "cancelled";
    }
}
