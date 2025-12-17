namespace Application.DTOs.Portal
{
    // ==================== Profile DTOs ====================

    /// <summary>
    /// Employee's own profile information (limited view)
    /// </summary>
    public class EmployeeProfileDto
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public DateTime? HireDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string Country { get; set; } = "India";
        public string? Company { get; set; }
        public Guid? CompanyId { get; set; }

        // Masked sensitive info
        public string? MaskedPanNumber { get; set; }
        public string? MaskedBankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? IfscCode { get; set; }
    }

    // ==================== Payslip DTOs ====================

    /// <summary>
    /// Payslip summary for list view
    /// </summary>
    public class PayslipSummaryDto
    {
        public Guid Id { get; set; }
        public int SalaryMonth { get; set; }
        public int SalaryYear { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal GrossSalary { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
    }

    /// <summary>
    /// Full payslip details
    /// </summary>
    public class PayslipDetailDto
    {
        public Guid Id { get; set; }
        public int SalaryMonth { get; set; }
        public int SalaryYear { get; set; }
        public string MonthName { get; set; } = string.Empty;

        // Earnings
        public decimal BasicSalary { get; set; }
        public decimal Hra { get; set; }
        public decimal Conveyance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal SpecialAllowance { get; set; }
        public decimal Lta { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal GrossSalary { get; set; }

        // Deductions
        public decimal PfEmployee { get; set; }
        public decimal PfEmployer { get; set; }
        public decimal Pt { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal TotalDeductions { get; set; }

        // Net
        public decimal NetSalary { get; set; }

        // Payment
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public string? Remarks { get; set; }

        // Employee context
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? Company { get; set; }
        public string? MaskedBankAccountNumber { get; set; }
        public string? BankName { get; set; }
    }

    // ==================== Asset DTOs ====================

    /// <summary>
    /// Asset assigned to employee
    /// </summary>
    public class MyAssetDto
    {
        public Guid AssignmentId { get; set; }
        public Guid AssetId { get; set; }
        public string AssetTag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public DateTime AssignedOn { get; set; }
        public string? ConditionOut { get; set; }
        public string? Notes { get; set; }
        public DateTime? WarrantyExpiration { get; set; }
    }

    /// <summary>
    /// Request for a new asset
    /// </summary>
    public class AssetRequestDto
    {
        public string AssetType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Justification { get; set; } = string.Empty;
        public string? PreferredSpecifications { get; set; }
        public string Priority { get; set; } = "normal"; // low, normal, high, urgent
    }

    /// <summary>
    /// Asset request status
    /// </summary>
    public class AssetRequestResponseDto
    {
        public Guid Id { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Justification { get; set; } = string.Empty;
        public string? PreferredSpecifications { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public string? ProcessingNotes { get; set; }
        public Guid? AssignedAssetId { get; set; }
    }

    // ==================== Tax Declaration DTOs ====================

    /// <summary>
    /// Tax declaration summary for list
    /// </summary>
    public class TaxDeclarationSummaryDto
    {
        public Guid Id { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string TaxRegime { get; set; } = string.Empty;
        public decimal Total80CDeductions { get; set; }
        public decimal Total80DDeductions { get; set; }
        public decimal TotalOtherDeductions { get; set; }
        public decimal GrandTotalDeductions { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public int RevisionCount { get; set; }
    }

    /// <summary>
    /// Full tax declaration for viewing/editing
    /// </summary>
    public class TaxDeclarationDetailDto
    {
        public Guid Id { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string TaxRegime { get; set; } = string.Empty;

        // Section 80C
        public decimal Sec80cPpf { get; set; }
        public decimal Sec80cElss { get; set; }
        public decimal Sec80cLifeInsurance { get; set; }
        public decimal Sec80cHomeLoanPrincipal { get; set; }
        public decimal Sec80cChildrenTuition { get; set; }
        public decimal Sec80cNsc { get; set; }
        public decimal Sec80cSukanyaSamriddhi { get; set; }
        public decimal Sec80cFixedDeposit { get; set; }
        public decimal Sec80cOthers { get; set; }

        // Section 80CCD(1B)
        public decimal Sec80ccdNps { get; set; }

        // Section 80D
        public decimal Sec80dSelfFamily { get; set; }
        public decimal Sec80dParents { get; set; }
        public decimal Sec80dPreventiveCheckup { get; set; }
        public bool Sec80dSelfSeniorCitizen { get; set; }
        public bool Sec80dParentsSeniorCitizen { get; set; }

        // Section 80E
        public decimal Sec80eEducationLoan { get; set; }

        // Section 24
        public decimal Sec24HomeLoanInterest { get; set; }

        // Section 80G
        public decimal Sec80gDonations { get; set; }

        // Section 80TTA
        public decimal Sec80ttaSavingsInterest { get; set; }

        // HRA
        public decimal HraRentPaidAnnual { get; set; }
        public bool HraMetroCity { get; set; }
        public string? HraLandlordPan { get; set; }
        public string? HraLandlordName { get; set; }

        // Other Income
        public decimal OtherIncomeAnnual { get; set; }

        // Previous Employer
        public decimal PrevEmployerIncome { get; set; }
        public decimal PrevEmployerTds { get; set; }
        public decimal PrevEmployerPf { get; set; }
        public decimal PrevEmployerPt { get; set; }

        // Status
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectedBy { get; set; }
        public string? RejectionReason { get; set; }
        public int RevisionCount { get; set; }
        public string? ProofDocuments { get; set; }

        // Calculated summaries
        public decimal Total80CDeductions { get; set; }
        public decimal Total80DDeductions { get; set; }
        public decimal TotalOtherDeductions { get; set; }
        public decimal GrandTotalDeductions { get; set; }
    }

    /// <summary>
    /// Update tax declaration request
    /// </summary>
    public class UpdateTaxDeclarationDto
    {
        public string? TaxRegime { get; set; }

        // Section 80C
        public decimal? Sec80cPpf { get; set; }
        public decimal? Sec80cElss { get; set; }
        public decimal? Sec80cLifeInsurance { get; set; }
        public decimal? Sec80cHomeLoanPrincipal { get; set; }
        public decimal? Sec80cChildrenTuition { get; set; }
        public decimal? Sec80cNsc { get; set; }
        public decimal? Sec80cSukanyaSamriddhi { get; set; }
        public decimal? Sec80cFixedDeposit { get; set; }
        public decimal? Sec80cOthers { get; set; }

        // Section 80CCD(1B)
        public decimal? Sec80ccdNps { get; set; }

        // Section 80D
        public decimal? Sec80dSelfFamily { get; set; }
        public decimal? Sec80dParents { get; set; }
        public decimal? Sec80dPreventiveCheckup { get; set; }
        public bool? Sec80dSelfSeniorCitizen { get; set; }
        public bool? Sec80dParentsSeniorCitizen { get; set; }

        // Section 80E
        public decimal? Sec80eEducationLoan { get; set; }

        // Section 24
        public decimal? Sec24HomeLoanInterest { get; set; }

        // Section 80G
        public decimal? Sec80gDonations { get; set; }

        // Section 80TTA
        public decimal? Sec80ttaSavingsInterest { get; set; }

        // HRA
        public decimal? HraRentPaidAnnual { get; set; }
        public bool? HraMetroCity { get; set; }
        public string? HraLandlordPan { get; set; }
        public string? HraLandlordName { get; set; }

        // Other Income
        public decimal? OtherIncomeAnnual { get; set; }

        // Previous Employer
        public decimal? PrevEmployerIncome { get; set; }
        public decimal? PrevEmployerTds { get; set; }
        public decimal? PrevEmployerPf { get; set; }
        public decimal? PrevEmployerPt { get; set; }

        // Proof documents
        public string? ProofDocuments { get; set; }
    }

    // ==================== Subscription DTOs ====================

    /// <summary>
    /// Subscription assigned to employee
    /// </summary>
    public class MySubscriptionDto
    {
        public Guid AssignmentId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Vendor { get; set; }
        public DateTime AssignedOn { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }

    // ==================== Dashboard/Summary DTOs ====================

    /// <summary>
    /// Portal dashboard data
    /// </summary>
    public class PortalDashboardDto
    {
        public EmployeeProfileDto Profile { get; set; } = null!;
        public PayslipSummaryDto? LatestPayslip { get; set; }
        public int AssignedAssetsCount { get; set; }
        public int ActiveSubscriptionsCount { get; set; }
        public TaxDeclarationSummaryDto? CurrentTaxDeclaration { get; set; }
        public List<QuickActionDto> QuickActions { get; set; } = new();
        public List<NotificationDto> Notifications { get; set; } = new();
    }

    /// <summary>
    /// Quick action item for dashboard
    /// </summary>
    public class QuickActionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // view_payslip, submit_declaration, etc.
        public string? ActionUrl { get; set; }
        public bool IsUrgent { get; set; }
    }

    /// <summary>
    /// Notification for employee
    /// </summary>
    public class NotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // info, success, warning, error
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
