namespace Core.Entities.Payroll
{
    /// <summary>
    /// Employee tax declaration for a financial year (80C, 80D, HRA exemption, etc.)
    /// </summary>
    public class EmployeeTaxDeclaration
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }

        /// <summary>
        /// Financial year (e.g., '2024-25')
        /// </summary>
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Tax regime choice: 'old' or 'new'
        /// </summary>
        public string TaxRegime { get; set; } = "new";

        // ==================== Section 80C (Max ₹1,50,000 combined) ====================

        public decimal Sec80cPpf { get; set; }
        public decimal Sec80cElss { get; set; }
        public decimal Sec80cLifeInsurance { get; set; }
        public decimal Sec80cHomeLoanPrincipal { get; set; }
        public decimal Sec80cChildrenTuition { get; set; }
        public decimal Sec80cNsc { get; set; }
        public decimal Sec80cSukanyaSamriddhi { get; set; }
        public decimal Sec80cFixedDeposit { get; set; }
        public decimal Sec80cOthers { get; set; }

        // ==================== Section 80CCD(1B) - NPS (Additional ₹50,000) ====================

        public decimal Sec80ccdNps { get; set; }

        // ==================== Section 80D - Health Insurance ====================

        /// <summary>
        /// Self & Family health insurance (max ₹25,000 or ₹50,000 if senior)
        /// </summary>
        public decimal Sec80dSelfFamily { get; set; }

        /// <summary>
        /// Parents health insurance (max ₹25,000 or ₹50,000 if senior)
        /// </summary>
        public decimal Sec80dParents { get; set; }

        /// <summary>
        /// Preventive health checkup (max ₹5,000, included in 80D limit)
        /// </summary>
        public decimal Sec80dPreventiveCheckup { get; set; }

        /// <summary>
        /// Whether self/spouse is a senior citizen (60+)
        /// </summary>
        public bool Sec80dSelfSeniorCitizen { get; set; }

        /// <summary>
        /// Whether parents are senior citizens
        /// </summary>
        public bool Sec80dParentsSeniorCitizen { get; set; }

        // ==================== Section 80E - Education Loan ====================

        /// <summary>
        /// Education loan interest (no limit)
        /// </summary>
        public decimal Sec80eEducationLoan { get; set; }

        // ==================== Section 24 - Home Loan Interest ====================

        /// <summary>
        /// Home loan interest (max ₹2,00,000)
        /// </summary>
        public decimal Sec24HomeLoanInterest { get; set; }

        // ==================== Section 80G - Donations ====================

        public decimal Sec80gDonations { get; set; }

        // ==================== Section 80TTA/TTB - Savings Interest ====================

        /// <summary>
        /// Savings account interest (max ₹10,000)
        /// </summary>
        public decimal Sec80ttaSavingsInterest { get; set; }

        // ==================== HRA Exemption Inputs ====================

        /// <summary>
        /// Annual rent paid for HRA exemption calculation
        /// </summary>
        public decimal HraRentPaidAnnual { get; set; }

        /// <summary>
        /// Whether residing in metro city (Mumbai, Delhi, Chennai, Kolkata)
        /// </summary>
        public bool HraMetroCity { get; set; }

        /// <summary>
        /// Landlord PAN (required if rent > ₹1L/year)
        /// </summary>
        public string? HraLandlordPan { get; set; }

        public string? HraLandlordName { get; set; }

        // ==================== Other Income ====================

        /// <summary>
        /// Other income to consider for TDS (rental, FD interest, etc.)
        /// </summary>
        public decimal OtherIncomeAnnual { get; set; }

        // ==================== Column 388A - Other TDS/TCS Credits ====================
        // Per CBDT Circular Feb 2025: Allows employees to declare TDS/TCS from other sources
        // to be adjusted against salary TDS

        /// <summary>
        /// TDS on interest income (FD, RD) - Section 194A
        /// </summary>
        public decimal OtherTdsInterest { get; set; }

        /// <summary>
        /// TDS on dividend income - Section 194
        /// </summary>
        public decimal OtherTdsDividend { get; set; }

        /// <summary>
        /// TDS on commission/brokerage - Section 194H
        /// </summary>
        public decimal OtherTdsCommission { get; set; }

        /// <summary>
        /// TDS on rental income - Section 194I
        /// </summary>
        public decimal OtherTdsRent { get; set; }

        /// <summary>
        /// TDS on professional/technical fees - Section 194J
        /// </summary>
        public decimal OtherTdsProfessional { get; set; }

        /// <summary>
        /// TDS from any other sources
        /// </summary>
        public decimal OtherTdsOthers { get; set; }

        /// <summary>
        /// TCS on foreign remittance under LRS - Section 206C(1G)
        /// </summary>
        public decimal TcsForeignRemittance { get; set; }

        /// <summary>
        /// TCS on overseas tour packages - Section 206C(1G)
        /// </summary>
        public decimal TcsOverseasTour { get; set; }

        /// <summary>
        /// TCS on motor vehicle purchase > 10 lakhs - Section 206C(1F)
        /// </summary>
        public decimal TcsVehiclePurchase { get; set; }

        /// <summary>
        /// TCS from any other sources
        /// </summary>
        public decimal TcsOthers { get; set; }

        /// <summary>
        /// JSON array with detailed breakdown: [{section, deductorTan, amount, certificateNo}]
        /// </summary>
        public string? OtherTdsTcsDetails { get; set; }

        // ==================== Previous Employer (if joined mid-year) ====================

        /// <summary>
        /// Income from previous employer in current FY
        /// </summary>
        public decimal PrevEmployerIncome { get; set; }

        /// <summary>
        /// TDS deducted by previous employer
        /// </summary>
        public decimal PrevEmployerTds { get; set; }

        /// <summary>
        /// PF deducted by previous employer
        /// </summary>
        public decimal PrevEmployerPf { get; set; }

        /// <summary>
        /// PT deducted by previous employer
        /// </summary>
        public decimal PrevEmployerPt { get; set; }

        // ==================== Workflow Status ====================

        /// <summary>
        /// Status: draft, submitted, verified, rejected, locked
        /// </summary>
        public string Status { get; set; } = "draft";

        public DateTime? SubmittedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? LockedAt { get; set; }

        // ==================== Rejection Workflow ====================

        /// <summary>
        /// Timestamp when declaration was rejected
        /// </summary>
        public DateTime? RejectedAt { get; set; }

        /// <summary>
        /// User who rejected the declaration
        /// </summary>
        public string? RejectedBy { get; set; }

        /// <summary>
        /// Reason for rejection
        /// </summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Number of times declaration has been revised after rejection
        /// </summary>
        public int RevisionCount { get; set; }

        /// <summary>
        /// Proof documents (JSON array of document references)
        /// </summary>
        public string? ProofDocuments { get; set; }

        // ==================== Metadata ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public Employees? Employee { get; set; }
    }
}
