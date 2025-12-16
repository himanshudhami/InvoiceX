namespace Application.DTOs.Payroll;

public class EmployeeTaxDeclarationDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string TaxRegime { get; set; } = "new";

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

    // Other Sections
    public decimal Sec80eEducationLoan { get; set; }
    public decimal Sec24HomeLoanInterest { get; set; }
    public decimal Sec80gDonations { get; set; }
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
    public string Status { get; set; } = "draft";
    public DateTime? SubmittedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? ProofDocuments { get; set; }

    // Rejection workflow
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public int RevisionCount { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Calculated totals
    public decimal Total80cDeduction { get; set; }
    public decimal Total80dDeduction { get; set; }
    public decimal TotalDeductions { get; set; }

    // Navigation
    public string? EmployeeName { get; set; }
}

public class CreateEmployeeTaxDeclarationDto
{
    public Guid EmployeeId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string TaxRegime { get; set; } = "new";

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

    // Other Sections
    public decimal Sec80eEducationLoan { get; set; }
    public decimal Sec24HomeLoanInterest { get; set; }
    public decimal Sec80gDonations { get; set; }
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
}

public class UpdateEmployeeTaxDeclarationDto
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

    // Other Sections
    public decimal? Sec80eEducationLoan { get; set; }
    public decimal? Sec24HomeLoanInterest { get; set; }
    public decimal? Sec80gDonations { get; set; }
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

    public string? ProofDocuments { get; set; }
}

/// <summary>
/// DTO for rejecting a tax declaration
/// </summary>
public class RejectDeclarationDto
{
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

/// <summary>
/// DTO for tax declaration history/audit trail entries
/// </summary>
public class DeclarationHistoryDto
{
    public Guid Id { get; set; }
    public Guid DeclarationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? PreviousValues { get; set; }
    public string? NewValues { get; set; }
    public string? RejectionReason { get; set; }
    public string? RejectionComments { get; set; }
}

/// <summary>
/// DTO for tax declaration summary with capped deduction amounts
/// </summary>
public class TaxDeclarationSummaryDto
{
    public Guid DeclarationId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string TaxRegime { get; set; } = string.Empty;

    // Section 80C
    public decimal Section80CTotal { get; set; }
    public decimal Section80CAllowed { get; set; }
    public decimal Section80CExcess { get; set; }

    // Section 80CCD(1B)
    public decimal Section80ccdTotal { get; set; }
    public decimal Section80ccdAllowed { get; set; }

    // Section 80D
    public decimal Section80DSelfFamilyAllowed { get; set; }
    public decimal Section80DParentsAllowed { get; set; }
    public decimal Section80DPreventiveAllowed { get; set; }
    public decimal Section80DTotal { get; set; }

    // Other Sections
    public decimal Section80ETotal { get; set; }
    public decimal Section24Allowed { get; set; }
    public decimal Section80GAllowed { get; set; }
    public decimal Section80TTAAllowed { get; set; }

    // HRA
    public decimal HraRentDeclared { get; set; }
    public bool RequiresPanForHra { get; set; }
    public bool HasValidLandlordPan { get; set; }

    // Grand Total
    public decimal TotalAllowedDeductions { get; set; }

    // Validation Status
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
