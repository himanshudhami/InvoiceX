using System.Globalization;
using System.Text.Json;
using Application.DTOs.Payroll;
using Core.Entities.Payroll;
using Core.Interfaces.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Result of payroll calculation containing transaction and audit lines
/// </summary>
public class PayrollCalculationResult
{
    public PayrollTransaction Transaction { get; set; } = null!;
    public List<PayrollCalculationLine> CalculationLines { get; set; } = new();
}

/// <summary>
/// Main service that orchestrates all payroll calculations for an employee
/// </summary>
public class PayrollCalculationService
{
    private readonly PfCalculationService _pfService;
    private readonly EsiCalculationService _esiService;
    private readonly ProfessionalTaxCalculationService _ptService;
    private readonly TdsCalculationService _tdsService;
    private readonly ICompanyStatutoryConfigRepository _companyConfigRepository;
    private readonly IEmployeeTaxDeclarationRepository _taxDeclarationRepository;
    private readonly IPayrollTransactionRepository _payrollTransactionRepository;
    private readonly IEsiEligibilityRepository? _esiEligibilityRepository;

    private const string CONFIG_VERSION = "FY_2024-25_v1";

    public PayrollCalculationService(
        PfCalculationService pfService,
        EsiCalculationService esiService,
        ProfessionalTaxCalculationService ptService,
        TdsCalculationService tdsService,
        ICompanyStatutoryConfigRepository companyConfigRepository,
        IEmployeeTaxDeclarationRepository taxDeclarationRepository,
        IPayrollTransactionRepository payrollTransactionRepository,
        IEsiEligibilityRepository? esiEligibilityRepository = null)
    {
        _pfService = pfService;
        _esiService = esiService;
        _ptService = ptService;
        _tdsService = tdsService;
        _companyConfigRepository = companyConfigRepository;
        _taxDeclarationRepository = taxDeclarationRepository;
        _payrollTransactionRepository = payrollTransactionRepository;
        _esiEligibilityRepository = esiEligibilityRepository;
    }

    /// <summary>
    /// Calculate complete payroll for an employee for a given month
    /// Returns transaction and calculation lines for auditability
    /// </summary>
    public async Task<PayrollCalculationResult> CalculateEmployeePayrollAsync(
        EmployeePayrollInfo payrollInfo,
        EmployeeSalaryStructure salaryStructure,
        int payrollMonth,
        int payrollYear,
        int workingDays,
        int presentDays,
        decimal ltaPaid = 0,
        decimal bonusPaid = 0,
        decimal arrears = 0,
        decimal reimbursements = 0,
        decimal incentives = 0,
        decimal otherEarnings = 0,
        decimal loanRecovery = 0,
        decimal advanceRecovery = 0,
        decimal otherDeductions = 0)
    {
        var result = new PayrollCalculationResult();
        var lines = new List<PayrollCalculationLine>();
        int lineSeq = 0;

        // Get company statutory config
        var companyConfig = await _companyConfigRepository.GetByCompanyIdAsync(payrollInfo.CompanyId);
        if (companyConfig == null)
        {
            throw new InvalidOperationException($"No statutory configuration found for company {payrollInfo.CompanyId}");
        }

        var transaction = new PayrollTransaction
        {
            EmployeeId = payrollInfo.EmployeeId,
            SalaryStructureId = salaryStructure.Id,
            PayrollMonth = payrollMonth,
            PayrollYear = payrollYear,
            PayrollType = payrollInfo.PayrollType,
            WorkingDays = workingDays,
            PresentDays = presentDays,
            LopDays = workingDays - presentDays
        };

        // Calculate proration factor
        var prorationFactor = workingDays > 0 ? (decimal)presentDays / workingDays : 1m;

        // Calculate prorated earnings with audit lines
        transaction.BasicEarned = Math.Round(salaryStructure.BasicSalary * prorationFactor, 2, MidpointRounding.AwayFromZero);
        if (transaction.BasicEarned > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "BASIC_EARNED", "Basic Salary", salaryStructure.BasicSalary, prorationFactor * 100, transaction.BasicEarned));

        transaction.HraEarned = Math.Round(salaryStructure.Hra * prorationFactor, 2, MidpointRounding.AwayFromZero);
        if (transaction.HraEarned > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "HRA_EARNED", "House Rent Allowance", salaryStructure.Hra, prorationFactor * 100, transaction.HraEarned));

        transaction.DaEarned = Math.Round(salaryStructure.DearnessAllowance * prorationFactor, 2, MidpointRounding.AwayFromZero);
        if (transaction.DaEarned > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "DA_EARNED", "Dearness Allowance", salaryStructure.DearnessAllowance, prorationFactor * 100, transaction.DaEarned));

        transaction.ConveyanceEarned = Math.Round(salaryStructure.ConveyanceAllowance * prorationFactor, 2, MidpointRounding.AwayFromZero);
        if (transaction.ConveyanceEarned > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "CONVEYANCE_EARNED", "Conveyance Allowance", salaryStructure.ConveyanceAllowance, prorationFactor * 100, transaction.ConveyanceEarned));

        transaction.MedicalEarned = Math.Round(salaryStructure.MedicalAllowance * prorationFactor, 2, MidpointRounding.AwayFromZero);
        if (transaction.MedicalEarned > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "MEDICAL_EARNED", "Medical Allowance", salaryStructure.MedicalAllowance, prorationFactor * 100, transaction.MedicalEarned));

        transaction.SpecialAllowanceEarned = Math.Round(salaryStructure.SpecialAllowance * prorationFactor, 2, MidpointRounding.AwayFromZero);
        if (transaction.SpecialAllowanceEarned > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "SPECIAL_ALLOWANCE_EARNED", "Special Allowance", salaryStructure.SpecialAllowance, prorationFactor * 100, transaction.SpecialAllowanceEarned));

        transaction.OtherAllowancesEarned = Math.Round(salaryStructure.OtherAllowances * prorationFactor, 2, MidpointRounding.AwayFromZero);
        if (transaction.OtherAllowancesEarned > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "OTHER_ALLOWANCES_EARNED", "Other Allowances", salaryStructure.OtherAllowances, prorationFactor * 100, transaction.OtherAllowancesEarned));

        // Variable earnings (not prorated)
        transaction.LtaPaid = ltaPaid;
        if (ltaPaid > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "LTA_PAID", "Leave Travel Allowance", null, null, ltaPaid));

        transaction.BonusPaid = bonusPaid;
        if (bonusPaid > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "BONUS_PAID", "Bonus", null, null, bonusPaid));

        transaction.Arrears = arrears;
        if (arrears > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "ARREARS", "Arrears", null, null, arrears));

        transaction.Reimbursements = reimbursements;
        if (reimbursements > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "REIMBURSEMENTS", "Reimbursements", null, null, reimbursements));

        transaction.Incentives = incentives;
        if (incentives > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "INCENTIVES", "Incentives", null, null, incentives));

        transaction.OtherEarnings = otherEarnings;
        if (otherEarnings > 0)
            lines.Add(CreateLine("earning", ++lineSeq, "OTHER_EARNINGS", "Other Earnings", null, null, otherEarnings));

        // Calculate gross earnings
        transaction.GrossEarnings = transaction.BasicEarned + transaction.HraEarned + transaction.DaEarned +
                                    transaction.ConveyanceEarned + transaction.MedicalEarned +
                                    transaction.SpecialAllowanceEarned + transaction.OtherAllowancesEarned +
                                    transaction.LtaPaid + transaction.BonusPaid + transaction.Arrears +
                                    transaction.Reimbursements + transaction.Incentives + transaction.OtherEarnings;

        // Calculate PF with audit lines
        if (payrollInfo.IsPfApplicable && companyConfig.PfEnabled)
        {
            // Calculate PF wage base - include special allowance if configured
            var pfWage = salaryStructure.BasicSalary + salaryStructure.DearnessAllowance;
            if (companyConfig.PfIncludeSpecialAllowance)
            {
                pfWage += salaryStructure.SpecialAllowance;
            }
            var pfWageBase = Math.Min(pfWage, companyConfig.PfWageCeiling);

            var pfResult = _pfService.CalculateProrated(
                salaryStructure.BasicSalary,
                salaryStructure.DearnessAllowance,
                salaryStructure.SpecialAllowance,
                companyConfig.PfIncludeSpecialAllowance,
                companyConfig.PfWageCeiling,
                companyConfig.PfEmployeeRate,
                companyConfig.PfEmployerRate,
                true,
                workingDays,
                presentDays);

            transaction.PfEmployee = pfResult.EmployeeContribution;
            transaction.PfEmployer = pfResult.EmployerContribution;
            transaction.PfAdminCharges = pfResult.AdminCharges;
            transaction.PfEdli = pfResult.EdliCharges;

            if (transaction.PfEmployee > 0)
                lines.Add(CreateLine("deduction", ++lineSeq, "PF_EMPLOYEE_12", "PF Employee Contribution", pfWageBase * prorationFactor, companyConfig.PfEmployeeRate, transaction.PfEmployee,
                    JsonSerializer.Serialize(new { wage_ceiling = companyConfig.PfWageCeiling, rate = companyConfig.PfEmployeeRate, include_special_allowance = companyConfig.PfIncludeSpecialAllowance })));

            if (transaction.PfEmployer > 0)
                lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_EMPLOYER_12", "PF Employer Contribution", pfWageBase * prorationFactor, companyConfig.PfEmployerRate, transaction.PfEmployer));

            if (transaction.PfAdminCharges > 0)
                lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_ADMIN_CHARGES", "PF Admin Charges", pfWageBase * prorationFactor, 0.5m, transaction.PfAdminCharges));

            if (transaction.PfEdli > 0)
                lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_EDLI", "PF EDLI", pfWageBase * prorationFactor, 0.5m, transaction.PfEdli));
        }

        // Calculate ESI with audit lines
        // ESI 6-month rule: If eligible at start of contribution period (Apr-Sep or Oct-Mar),
        // must contribute until period end even if salary crosses ceiling mid-period
        if (payrollInfo.IsEsiApplicable && companyConfig.EsiEnabled)
        {
            // Check ESI eligibility using 6-month period rule if repository is available
            bool isEsiEligibleForPeriod = salaryStructure.MonthlyGross <= companyConfig.EsiGrossCeiling;

            if (_esiEligibilityRepository != null)
            {
                // Use repository to check/track eligibility based on 6-month rule
                isEsiEligibleForPeriod = await _esiEligibilityRepository.IsEligibleForPeriodAsync(
                    payrollInfo.EmployeeId,
                    payrollInfo.CompanyId,
                    salaryStructure.MonthlyGross,
                    payrollMonth,
                    payrollYear,
                    companyConfig.EsiGrossCeiling);

                // Ensure eligibility period is tracked for audit
                await _esiEligibilityRepository.EnsureEligibilityPeriodAsync(
                    payrollInfo.EmployeeId,
                    payrollInfo.CompanyId,
                    salaryStructure.MonthlyGross,
                    payrollMonth,
                    payrollYear,
                    companyConfig.EsiGrossCeiling);
            }

            if (isEsiEligibleForPeriod)
            {
                var esiResult = _esiService.CalculateProrated(
                    salaryStructure.MonthlyGross,
                    companyConfig.EsiGrossCeiling,
                    companyConfig.EsiEmployeeRate,
                    companyConfig.EsiEmployerRate,
                    true, // Force applicable since we've already determined eligibility
                    workingDays,
                    presentDays);

                transaction.EsiEmployee = esiResult.EmployeeContribution;
                transaction.EsiEmployer = esiResult.EmployerContribution;

                if (transaction.EsiEmployee > 0)
                    lines.Add(CreateLine("deduction", ++lineSeq, "ESI_EMPLOYEE_075", "ESI Employee Contribution", transaction.GrossEarnings, companyConfig.EsiEmployeeRate, transaction.EsiEmployee,
                        JsonSerializer.Serialize(new { gross_ceiling = companyConfig.EsiGrossCeiling, rate = companyConfig.EsiEmployeeRate, six_month_rule = _esiEligibilityRepository != null })));

                if (transaction.EsiEmployer > 0)
                    lines.Add(CreateLine("employer_contribution", ++lineSeq, "ESI_EMPLOYER_325", "ESI Employer Contribution", transaction.GrossEarnings, companyConfig.EsiEmployerRate, transaction.EsiEmployer));
            }
        }

        // Calculate PT with audit lines
        // Use employee's work state if available, otherwise fall back to company's configured state
        if (payrollInfo.IsPtApplicable && companyConfig.PtEnabled)
        {
            var ptState = payrollInfo.WorkState ?? companyConfig.PtState ?? "";
            var ptResult = await _ptService.CalculateAsync(
                transaction.GrossEarnings,
                ptState,
                payrollMonth,
                true);

            transaction.ProfessionalTax = ptResult.TaxAmount;

            if (transaction.ProfessionalTax > 0)
                lines.Add(CreateLine("statutory", ++lineSeq, $"PT_{(ptState.Length > 0 ? ptState : "UNKNOWN").ToUpperInvariant().Replace(" ", "_")}",
                    $"Professional Tax ({ptState})", transaction.GrossEarnings, null, transaction.ProfessionalTax,
                    JsonSerializer.Serialize(new { state = ptState, employee_work_state = payrollInfo.WorkState, company_default_state = companyConfig.PtState, month = payrollMonth })));
        }

        // Calculate TDS with audit lines
        var tdsResult = await CalculateTdsForEmployeeAsync(
            payrollInfo,
            salaryStructure,
            payrollMonth,
            payrollYear,
            transaction.GrossEarnings);

        transaction.TdsDeducted = tdsResult.MonthlyTds;
        transaction.TdsCalculation = System.Text.Json.JsonSerializer.Serialize(tdsResult);

        if (transaction.TdsDeducted > 0)
            lines.Add(CreateLine("statutory", ++lineSeq, "TDS_192", "TDS under Section 192", tdsResult.TaxableIncome, null, transaction.TdsDeducted,
                JsonSerializer.Serialize(new { regime = payrollInfo.TaxRegime, annual_tax = tdsResult.TotalTaxLiability, remaining_months = tdsResult.RemainingMonths })));

        // Other deductions with audit lines
        transaction.LoanRecovery = loanRecovery;
        if (loanRecovery > 0)
            lines.Add(CreateLine("deduction", ++lineSeq, "LOAN_RECOVERY", "Loan Recovery", null, null, loanRecovery));

        transaction.AdvanceRecovery = advanceRecovery;
        if (advanceRecovery > 0)
            lines.Add(CreateLine("deduction", ++lineSeq, "ADVANCE_RECOVERY", "Advance Recovery", null, null, advanceRecovery));

        transaction.OtherDeductions = otherDeductions;
        if (otherDeductions > 0)
            lines.Add(CreateLine("deduction", ++lineSeq, "OTHER_DEDUCTIONS", "Other Deductions", null, null, otherDeductions));

        // Calculate total deductions
        transaction.TotalDeductions = transaction.PfEmployee + transaction.EsiEmployee +
                                      transaction.ProfessionalTax + transaction.TdsDeducted +
                                      transaction.LoanRecovery + transaction.AdvanceRecovery +
                                      transaction.OtherDeductions;

        // Calculate net payable
        transaction.NetPayable = transaction.GrossEarnings - transaction.TotalDeductions;

        // Gratuity provision (4.81% of basic)
        transaction.GratuityProvision = Math.Round(transaction.BasicEarned * 0.0481m, 0, MidpointRounding.AwayFromZero);
        if (transaction.GratuityProvision > 0)
            lines.Add(CreateLine("employer_contribution", ++lineSeq, "GRATUITY_PROVISION", "Gratuity Provision", transaction.BasicEarned, 4.81m, transaction.GratuityProvision));

        // Total employer cost
        transaction.TotalEmployerCost = transaction.GrossEarnings + transaction.PfEmployer +
                                        transaction.PfAdminCharges + transaction.PfEdli +
                                        transaction.EsiEmployer + transaction.GratuityProvision;

        transaction.Status = "computed";
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;

        result.Transaction = transaction;
        result.CalculationLines = lines;

        return result;
    }

    /// <summary>
    /// Helper to create a calculation line
    /// </summary>
    private PayrollCalculationLine CreateLine(string lineType, int seq, string ruleCode, string description,
        decimal? baseAmount, decimal? rate, decimal computedAmount, string? configSnapshot = null)
    {
        return new PayrollCalculationLine
        {
            LineType = lineType,
            LineSequence = seq,
            RuleCode = ruleCode,
            Description = description,
            BaseAmount = baseAmount,
            Rate = rate,
            ComputedAmount = computedAmount,
            ConfigVersion = CONFIG_VERSION,
            ConfigSnapshot = configSnapshot,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Calculate TDS for an employee considering YTD and declarations
    /// </summary>
    private async Task<TaxCalculationDto> CalculateTdsForEmployeeAsync(
        EmployeePayrollInfo payrollInfo,
        EmployeeSalaryStructure salaryStructure,
        int payrollMonth,
        int payrollYear,
        decimal currentMonthGross)
    {
        var financialYear = GetFinancialYear(payrollMonth, payrollYear);
        var remainingMonths = GetRemainingMonthsInFy(payrollMonth, payrollYear);

        // Get YTD summary
        var ytdSummary = await _payrollTransactionRepository.GetYtdSummaryAsync(payrollInfo.EmployeeId, financialYear);
        var ytdGross = ytdSummary.TryGetValue("YtdGross", out var g) ? g : 0;
        var ytdTds = ytdSummary.TryGetValue("YtdTds", out var t) ? t : 0;

        // Project annual income
        var projectedAnnualIncome = ytdGross + currentMonthGross + (salaryStructure.MonthlyGross * (remainingMonths - 1));
        var annualBasic = salaryStructure.BasicSalary * 12;
        var annualHra = salaryStructure.Hra * 12;

        // Get tax declaration
        var declaration = await _taxDeclarationRepository.GetByEmployeeAndYearAsync(payrollInfo.EmployeeId, financialYear);

        EmployeeTaxDeclarationDto? declarationDto = null;
        if (declaration != null)
        {
            declarationDto = MapToDeclarationDto(declaration);
        }

        return await _tdsService.CalculateAsync(
            payrollInfo.EmployeeId,
            financialYear,
            payrollInfo.TaxRegime,
            projectedAnnualIncome,
            annualBasic,
            annualHra,
            declarationDto?.OtherIncomeAnnual ?? 0,
            declarationDto?.PrevEmployerIncome ?? 0,
            declarationDto?.PrevEmployerTds ?? 0,
            ytdTds,
            remainingMonths,
            declarationDto,
            payrollInfo.DateOfBirth);
    }

    /// <summary>
    /// Calculate contractor payment (simplified - just gross and TDS)
    /// </summary>
    public ContractorPayment CalculateContractorPayment(
        Guid employeeId,
        Guid companyId,
        int paymentMonth,
        int paymentYear,
        decimal grossAmount,
        decimal tdsRate = 10.0m,
        bool gstApplicable = false,
        decimal gstRate = 18.0m,
        decimal otherDeductions = 0,
        string? invoiceNumber = null,
        string? contractReference = null,
        string? description = null)
    {
        var payment = new ContractorPayment
        {
            EmployeeId = employeeId,
            CompanyId = companyId,
            PaymentMonth = paymentMonth,
            PaymentYear = paymentYear,
            InvoiceNumber = invoiceNumber,
            ContractReference = contractReference,
            GrossAmount = grossAmount,
            TdsRate = tdsRate,
            OtherDeductions = otherDeductions,
            GstApplicable = gstApplicable,
            GstRate = gstRate,
            Description = description,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calculate TDS
        payment.TdsAmount = _tdsService.CalculateContractorTds(grossAmount, tdsRate);

        // Calculate GST if applicable
        if (gstApplicable)
        {
            payment.GstAmount = Math.Round(grossAmount * gstRate / 100, 2, MidpointRounding.AwayFromZero);
            payment.TotalInvoiceAmount = grossAmount + payment.GstAmount;
        }
        else
        {
            payment.TotalInvoiceAmount = grossAmount;
        }

        // Calculate net payable - use TotalInvoiceAmount to include GST in vendor payment
        // TDS is deducted from the invoice total, so contractor receives: InvoiceTotal - TDS
        payment.NetPayable = (payment.TotalInvoiceAmount ?? grossAmount) - payment.TdsAmount - otherDeductions;

        return payment;
    }

    private string GetFinancialYear(int month, int year)
    {
        // Indian FY: April to March
        if (month >= 4)
        {
            return $"{year}-{(year + 1) % 100:D2}";
        }
        else
        {
            return $"{year - 1}-{year % 100:D2}";
        }
    }

    private int GetRemainingMonthsInFy(int currentMonth, int currentYear)
    {
        // FY ends in March
        if (currentMonth >= 4)
        {
            return 12 - currentMonth + 3; // April to Dec remaining + Jan to March
        }
        else
        {
            return 3 - currentMonth + 1; // Months remaining in Jan-March
        }
    }

    private EmployeeTaxDeclarationDto MapToDeclarationDto(EmployeeTaxDeclaration declaration)
    {
        return new EmployeeTaxDeclarationDto
        {
            Id = declaration.Id,
            EmployeeId = declaration.EmployeeId,
            FinancialYear = declaration.FinancialYear,
            TaxRegime = declaration.TaxRegime,
            Sec80cPpf = declaration.Sec80cPpf,
            Sec80cElss = declaration.Sec80cElss,
            Sec80cLifeInsurance = declaration.Sec80cLifeInsurance,
            Sec80cHomeLoanPrincipal = declaration.Sec80cHomeLoanPrincipal,
            Sec80cChildrenTuition = declaration.Sec80cChildrenTuition,
            Sec80cNsc = declaration.Sec80cNsc,
            Sec80cSukanyaSamriddhi = declaration.Sec80cSukanyaSamriddhi,
            Sec80cFixedDeposit = declaration.Sec80cFixedDeposit,
            Sec80cOthers = declaration.Sec80cOthers,
            Sec80ccdNps = declaration.Sec80ccdNps,
            Sec80dSelfFamily = declaration.Sec80dSelfFamily,
            Sec80dParents = declaration.Sec80dParents,
            Sec80dPreventiveCheckup = declaration.Sec80dPreventiveCheckup,
            Sec80dSelfSeniorCitizen = declaration.Sec80dSelfSeniorCitizen,
            Sec80dParentsSeniorCitizen = declaration.Sec80dParentsSeniorCitizen,
            Sec80eEducationLoan = declaration.Sec80eEducationLoan,
            Sec24HomeLoanInterest = declaration.Sec24HomeLoanInterest,
            Sec80gDonations = declaration.Sec80gDonations,
            Sec80ttaSavingsInterest = declaration.Sec80ttaSavingsInterest,
            HraRentPaidAnnual = declaration.HraRentPaidAnnual,
            HraMetroCity = declaration.HraMetroCity,
            HraLandlordPan = declaration.HraLandlordPan,
            HraLandlordName = declaration.HraLandlordName,
            OtherIncomeAnnual = declaration.OtherIncomeAnnual,
            PrevEmployerIncome = declaration.PrevEmployerIncome,
            PrevEmployerTds = declaration.PrevEmployerTds,
            PrevEmployerPf = declaration.PrevEmployerPf,
            PrevEmployerPt = declaration.PrevEmployerPt,
            Status = declaration.Status
        };
    }
}
