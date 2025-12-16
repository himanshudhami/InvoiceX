using Application.DTOs.Payroll;
using Core.Common;

namespace Application.Services.Payroll
{
    /// <summary>
    /// Centralized validation service for tax declaration business rules
    /// </summary>
    public class TaxDeclarationValidationService : ITaxDeclarationValidationService
    {
        // Indian Tax Limits (FY 2024-25)
        private const decimal MAX_80C = 150000m;
        private const decimal MAX_80CCD_NPS = 50000m;
        private const decimal MAX_80D_SELF_FAMILY = 25000m;
        private const decimal MAX_80D_SELF_FAMILY_SENIOR = 50000m;
        private const decimal MAX_80D_PARENTS = 25000m;
        private const decimal MAX_80D_PARENTS_SENIOR = 50000m;
        private const decimal MAX_80D_PREVENTIVE = 5000m;
        private const decimal MAX_SECTION_24 = 200000m;
        private const decimal MAX_80TTA = 10000m;
        private const decimal HRA_PAN_THRESHOLD = 100000m;

        /// <summary>
        /// Validates tax declaration and returns capped values with warnings
        /// </summary>
        public Result<TaxDeclarationSummaryDto> ValidateAndCalculateSummary(
            CreateEmployeeTaxDeclarationDto dto,
            string? payrollTaxRegime = null)
        {
            var summary = new TaxDeclarationSummaryDto
            {
                FinancialYear = dto.FinancialYear,
                TaxRegime = dto.TaxRegime
            };

            // Calculate Section 80C
            var total80C = Calculate80CTotal(dto);
            summary.Section80CTotal = total80C;
            summary.Section80CAllowed = Math.Min(total80C, MAX_80C);
            summary.Section80CExcess = Math.Max(total80C - MAX_80C, 0);

            if (total80C > MAX_80C)
            {
                summary.Warnings.Add($"Section 80C total ({total80C:N0}) exceeds limit of {MAX_80C:N0}. Only {MAX_80C:N0} will be considered for tax calculation.");
            }

            // Calculate Section 80CCD(1B) NPS
            summary.Section80ccdTotal = dto.Sec80ccdNps;
            summary.Section80ccdAllowed = Math.Min(dto.Sec80ccdNps, MAX_80CCD_NPS);

            if (dto.Sec80ccdNps > MAX_80CCD_NPS)
            {
                summary.Warnings.Add($"NPS contribution ({dto.Sec80ccdNps:N0}) exceeds limit of {MAX_80CCD_NPS:N0}.");
            }

            // Calculate Section 80D
            var max80DSelfFamily = dto.Sec80dSelfSeniorCitizen ? MAX_80D_SELF_FAMILY_SENIOR : MAX_80D_SELF_FAMILY;
            var max80DParents = dto.Sec80dParentsSeniorCitizen ? MAX_80D_PARENTS_SENIOR : MAX_80D_PARENTS;

            summary.Section80DSelfFamilyAllowed = Math.Min(dto.Sec80dSelfFamily, max80DSelfFamily);
            summary.Section80DParentsAllowed = Math.Min(dto.Sec80dParents, max80DParents);
            summary.Section80DPreventiveAllowed = Math.Min(dto.Sec80dPreventiveCheckup, MAX_80D_PREVENTIVE);

            summary.Section80DTotal = summary.Section80DSelfFamilyAllowed +
                                      summary.Section80DParentsAllowed +
                                      summary.Section80DPreventiveAllowed;

            if (dto.Sec80dSelfFamily > max80DSelfFamily)
            {
                summary.Warnings.Add($"Self/Family health insurance ({dto.Sec80dSelfFamily:N0}) exceeds limit of {max80DSelfFamily:N0}.");
            }

            if (dto.Sec80dParents > max80DParents)
            {
                summary.Warnings.Add($"Parents health insurance ({dto.Sec80dParents:N0}) exceeds limit of {max80DParents:N0}.");
            }

            // Section 80E - No limit on education loan interest
            summary.Section80ETotal = dto.Sec80eEducationLoan;

            // Section 24 - Home Loan Interest
            summary.Section24Allowed = Math.Min(dto.Sec24HomeLoanInterest, MAX_SECTION_24);
            if (dto.Sec24HomeLoanInterest > MAX_SECTION_24)
            {
                summary.Warnings.Add($"Home loan interest ({dto.Sec24HomeLoanInterest:N0}) exceeds limit of {MAX_SECTION_24:N0}.");
            }

            // Section 80G - Donations (50% deduction for most)
            summary.Section80GAllowed = dto.Sec80gDonations;

            // Section 80TTA - Savings interest
            summary.Section80TTAAllowed = Math.Min(dto.Sec80ttaSavingsInterest, MAX_80TTA);

            // HRA validation
            summary.HraRentDeclared = dto.HraRentPaidAnnual;
            summary.RequiresPanForHra = dto.HraRentPaidAnnual > HRA_PAN_THRESHOLD;
            summary.HasValidLandlordPan = !string.IsNullOrEmpty(dto.HraLandlordPan) &&
                                          IsValidPanFormat(dto.HraLandlordPan);

            if (summary.RequiresPanForHra && !summary.HasValidLandlordPan)
            {
                summary.Errors.Add("Landlord PAN is mandatory when annual rent exceeds Rs. 1,00,000.");
            }

            // Calculate total allowed deductions
            summary.TotalAllowedDeductions = summary.Section80CAllowed +
                                              summary.Section80ccdAllowed +
                                              summary.Section80DTotal +
                                              summary.Section80ETotal +
                                              summary.Section24Allowed +
                                              summary.Section80GAllowed +
                                              summary.Section80TTAAllowed;

            // Tax regime consistency check
            if (!string.IsNullOrEmpty(payrollTaxRegime) &&
                !payrollTaxRegime.Equals(dto.TaxRegime, StringComparison.OrdinalIgnoreCase))
            {
                summary.Warnings.Add($"Tax regime mismatch: Declaration uses '{dto.TaxRegime}' but payroll is set to '{payrollTaxRegime}'. New regime will be used as default for TDS calculation.");
            }

            // New regime warning
            if (dto.TaxRegime.Equals("new", StringComparison.OrdinalIgnoreCase) &&
                summary.TotalAllowedDeductions > 0)
            {
                summary.Warnings.Add("Under New Tax Regime, most deductions are not applicable. Consider Old Tax Regime if you have significant deductions.");
            }

            // Return error if there are validation errors
            if (summary.Errors.Count > 0)
            {
                return Error.Validation(string.Join("; ", summary.Errors));
            }

            return Result<TaxDeclarationSummaryDto>.Success(summary);
        }

        /// <summary>
        /// Validates that declaration can be used for payroll
        /// </summary>
        public bool IsDeclarationUsableForPayroll(string? status)
        {
            if (string.IsNullOrEmpty(status)) return false;
            var normalizedStatus = status.ToLowerInvariant();
            return normalizedStatus is "submitted" or "verified" or "locked";
        }

        /// <summary>
        /// Get effective tax regime (defaults to new if not set)
        /// </summary>
        public string GetEffectiveTaxRegime(string? payrollRegime, string? declarationRegime)
        {
            // User preference: default to new regime
            if (string.IsNullOrEmpty(payrollRegime) && string.IsNullOrEmpty(declarationRegime))
                return "new";

            // Payroll setting takes precedence if set
            if (!string.IsNullOrEmpty(payrollRegime))
                return payrollRegime;

            return declarationRegime ?? "new";
        }

        /// <summary>
        /// Calculate capped deductions for TDS calculation
        /// </summary>
        public TaxDeclarationSummaryDto CalculateCappedDeductions(CreateEmployeeTaxDeclarationDto dto)
        {
            var result = ValidateAndCalculateSummary(dto);
            return result.IsSuccess ? result.Value! : new TaxDeclarationSummaryDto();
        }

        private decimal Calculate80CTotal(CreateEmployeeTaxDeclarationDto dto)
        {
            return dto.Sec80cPpf +
                   dto.Sec80cElss +
                   dto.Sec80cLifeInsurance +
                   dto.Sec80cHomeLoanPrincipal +
                   dto.Sec80cChildrenTuition +
                   dto.Sec80cNsc +
                   dto.Sec80cSukanyaSamriddhi +
                   dto.Sec80cFixedDeposit +
                   dto.Sec80cOthers;
        }

        private static bool IsValidPanFormat(string pan)
        {
            if (string.IsNullOrEmpty(pan) || pan.Length != 10)
                return false;

            // PAN format: 5 letters + 4 digits + 1 letter (e.g., ABCDE1234F)
            return System.Text.RegularExpressions.Regex.IsMatch(
                pan.ToUpperInvariant(),
                @"^[A-Z]{5}[0-9]{4}[A-Z]$");
        }
    }

    /// <summary>
    /// Interface for tax declaration validation service
    /// </summary>
    public interface ITaxDeclarationValidationService
    {
        Result<TaxDeclarationSummaryDto> ValidateAndCalculateSummary(
            CreateEmployeeTaxDeclarationDto dto,
            string? payrollTaxRegime = null);

        bool IsDeclarationUsableForPayroll(string? status);
        string GetEffectiveTaxRegime(string? payrollRegime, string? declarationRegime);
        TaxDeclarationSummaryDto CalculateCappedDeductions(CreateEmployeeTaxDeclarationDto dto);
    }
}
