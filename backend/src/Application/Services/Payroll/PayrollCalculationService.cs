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

    /// <summary>
    /// The Tax Rule Pack ID used for TDS calculation (if using Rule Packs)
    /// </summary>
    public Guid? RulePackId { get; set; }

    /// <summary>
    /// The Tax Rate Provider name used (e.g., "Hybrid(RulePack->Legacy)", "Legacy")
    /// </summary>
    public string? TaxRateProviderName { get; set; }
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
    private readonly CalculationRuleEngine? _ruleEngine;

    private const string CONFIG_VERSION = "FY_2024-25_v2"; // Updated for rules engine integration

    public PayrollCalculationService(
        PfCalculationService pfService,
        EsiCalculationService esiService,
        ProfessionalTaxCalculationService ptService,
        TdsCalculationService tdsService,
        ICompanyStatutoryConfigRepository companyConfigRepository,
        IEmployeeTaxDeclarationRepository taxDeclarationRepository,
        IPayrollTransactionRepository payrollTransactionRepository,
        IEsiEligibilityRepository? esiEligibilityRepository = null,
        CalculationRuleEngine? ruleEngine = null)
    {
        _pfService = pfService;
        _esiService = esiService;
        _ptService = ptService;
        _tdsService = tdsService;
        _companyConfigRepository = companyConfigRepository;
        _taxDeclarationRepository = taxDeclarationRepository;
        _payrollTransactionRepository = payrollTransactionRepository;
        _esiEligibilityRepository = esiEligibilityRepository;
        _ruleEngine = ruleEngine;
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

        // Build variables dictionary for rules engine
        var ruleVariables = BuildRuleVariables(salaryStructure, transaction, workingDays, presentDays, prorationFactor, companyConfig, payrollMonth, payrollYear);

        // Calculate PF with audit lines
        if (payrollInfo.IsPfApplicable && companyConfig.PfEnabled)
        {
            // Try to use rules engine first
            var pfCalculatedViaRules = false;

            if (_ruleEngine != null)
            {
                // Calculate PF Employee using rules engine
                var pfEmployeeResult = await _ruleEngine.CalculateComponentAsync(
                    payrollInfo.CompanyId, "PF_EMPLOYEE", ruleVariables);

                if (pfEmployeeResult.Success && pfEmployeeResult.RuleUsed != null)
                {
                    transaction.PfEmployee = Math.Round(pfEmployeeResult.Result, 0, MidpointRounding.AwayFromZero);
                    pfCalculatedViaRules = true;

                    if (transaction.PfEmployee > 0)
                        lines.Add(CreateLine("deduction", ++lineSeq, "PF_EMPLOYEE", "PF Employee Contribution",
                            ruleVariables.GetValueOrDefault("pf_wage", 0), null, transaction.PfEmployee,
                            JsonSerializer.Serialize(new {
                                source = "rules_engine",
                                rule_name = pfEmployeeResult.RuleUsed.Name,
                                rule_id = pfEmployeeResult.RuleUsed.Id,
                                steps = pfEmployeeResult.Steps.Select(s => new { s.Description, s.Expression, s.Value })
                            })));
                }

                // Calculate PF Employer using rules engine
                var pfEmployerResult = await _ruleEngine.CalculateComponentAsync(
                    payrollInfo.CompanyId, "PF_EMPLOYER", ruleVariables);

                if (pfEmployerResult.Success && pfEmployerResult.RuleUsed != null)
                {
                    transaction.PfEmployer = Math.Round(pfEmployerResult.Result, 0, MidpointRounding.AwayFromZero);

                    if (transaction.PfEmployer > 0)
                        lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_EMPLOYER", "PF Employer Contribution",
                            ruleVariables.GetValueOrDefault("pf_wage", 0), null, transaction.PfEmployer,
                            JsonSerializer.Serialize(new {
                                source = "rules_engine",
                                rule_name = pfEmployerResult.RuleUsed.Name,
                                rule_id = pfEmployerResult.RuleUsed.Id
                            })));
                }

                // Admin charges and EDLI still use standard calculation (0.5% each on capped wage)
                if (pfCalculatedViaRules)
                {
                    var cappedPfWage = Math.Min(ruleVariables.GetValueOrDefault("pf_wage", 0), 15000m) * prorationFactor;
                    transaction.PfAdminCharges = Math.Round(cappedPfWage * 0.5m / 100m, 0, MidpointRounding.AwayFromZero);
                    transaction.PfEdli = Math.Round(cappedPfWage * 0.5m / 100m, 0, MidpointRounding.AwayFromZero);

                    if (transaction.PfAdminCharges > 0)
                        lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_ADMIN_CHARGES", "PF Admin Charges", cappedPfWage, 0.5m, transaction.PfAdminCharges));

                    if (transaction.PfEdli > 0)
                        lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_EDLI", "PF EDLI", cappedPfWage, 0.5m, transaction.PfEdli));
                }
            }

            // Fall back to legacy calculation if rules engine not used
            if (!pfCalculatedViaRules)
            {
                // Get PF calculation mode from company config (default: ceiling_based)
                var pfCalculationMode = companyConfig.PfCalculationMode ?? "ceiling_based";
                var restrictedPfMaxWage = companyConfig.RestrictedPfMaxWage > 0 ? companyConfig.RestrictedPfMaxWage : 15000m;

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
                    presentDays,
                    pfCalculationMode,
                    restrictedPfMaxWage,
                    payrollInfo.OptedForRestrictedPf);

                transaction.PfEmployee = pfResult.EmployeeContribution;
                transaction.PfEmployer = pfResult.EmployerContribution;
                transaction.PfAdminCharges = pfResult.AdminCharges;
                transaction.PfEdli = pfResult.EdliCharges;

                // Use PfWageBase from result for audit (already includes mode-specific calculation)
                var auditPfWageBase = pfResult.PfWageBase * prorationFactor;

                if (transaction.PfEmployee > 0)
                    lines.Add(CreateLine("deduction", ++lineSeq, "PF_EMPLOYEE_12", "PF Employee Contribution", auditPfWageBase, companyConfig.PfEmployeeRate, transaction.PfEmployee,
                        JsonSerializer.Serialize(new {
                            source = "legacy",
                            calculation_mode = pfCalculationMode,
                            wage_ceiling = companyConfig.PfWageCeiling,
                            actual_pf_wage = pfResult.ActualPfWage,
                            pf_wage_base = pfResult.PfWageBase,
                            ceiling_applied = pfResult.CeilingApplied,
                            rate = companyConfig.PfEmployeeRate,
                            include_special_allowance = companyConfig.PfIncludeSpecialAllowance,
                            opted_for_restricted_pf = payrollInfo.OptedForRestrictedPf
                        })));

                if (transaction.PfEmployer > 0)
                    lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_EMPLOYER_12", "PF Employer Contribution", auditPfWageBase, companyConfig.PfEmployerRate, transaction.PfEmployer,
                        JsonSerializer.Serialize(new { source = "legacy", calculation_mode = pfCalculationMode })));

                if (transaction.PfAdminCharges > 0)
                    lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_ADMIN_CHARGES", "PF Admin Charges", auditPfWageBase, 0.5m, transaction.PfAdminCharges));

                if (transaction.PfEdli > 0)
                    lines.Add(CreateLine("employer_contribution", ++lineSeq, "PF_EDLI", "PF EDLI", auditPfWageBase, 0.5m, transaction.PfEdli));
            }
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
                var esiCalculatedViaRules = false;

                // Try rules engine first for ESI
                if (_ruleEngine != null)
                {
                    // Calculate ESI Employee using rules engine
                    var esiEmployeeResult = await _ruleEngine.CalculateComponentAsync(
                        payrollInfo.CompanyId, "ESI_EMPLOYEE", ruleVariables);

                    if (esiEmployeeResult.Success && esiEmployeeResult.RuleUsed != null)
                    {
                        transaction.EsiEmployee = Math.Round(esiEmployeeResult.Result, 0, MidpointRounding.AwayFromZero);
                        esiCalculatedViaRules = true;

                        if (transaction.EsiEmployee > 0)
                            lines.Add(CreateLine("deduction", ++lineSeq, "ESI_EMPLOYEE", "ESI Employee Contribution",
                                transaction.GrossEarnings, null, transaction.EsiEmployee,
                                JsonSerializer.Serialize(new {
                                    source = "rules_engine",
                                    rule_name = esiEmployeeResult.RuleUsed.Name,
                                    rule_id = esiEmployeeResult.RuleUsed.Id,
                                    six_month_rule = _esiEligibilityRepository != null
                                })));
                    }

                    // Calculate ESI Employer using rules engine
                    var esiEmployerResult = await _ruleEngine.CalculateComponentAsync(
                        payrollInfo.CompanyId, "ESI_EMPLOYER", ruleVariables);

                    if (esiEmployerResult.Success && esiEmployerResult.RuleUsed != null)
                    {
                        transaction.EsiEmployer = Math.Round(esiEmployerResult.Result, 0, MidpointRounding.AwayFromZero);

                        if (transaction.EsiEmployer > 0)
                            lines.Add(CreateLine("employer_contribution", ++lineSeq, "ESI_EMPLOYER", "ESI Employer Contribution",
                                transaction.GrossEarnings, null, transaction.EsiEmployer,
                                JsonSerializer.Serialize(new {
                                    source = "rules_engine",
                                    rule_name = esiEmployerResult.RuleUsed.Name,
                                    rule_id = esiEmployerResult.RuleUsed.Id
                                })));
                    }
                }

                // Fall back to legacy calculation if rules engine not used
                if (!esiCalculatedViaRules)
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
                            JsonSerializer.Serialize(new { source = "legacy", gross_ceiling = companyConfig.EsiGrossCeiling, rate = companyConfig.EsiEmployeeRate, six_month_rule = _esiEligibilityRepository != null })));

                    if (transaction.EsiEmployer > 0)
                        lines.Add(CreateLine("employer_contribution", ++lineSeq, "ESI_EMPLOYER_325", "ESI Employer Contribution", transaction.GrossEarnings, companyConfig.EsiEmployerRate, transaction.EsiEmployer,
                            JsonSerializer.Serialize(new { source = "legacy" })));
                }
            }
        }

        // Calculate PT with audit lines
        // Use employee's work state if available, otherwise fall back to company's configured state
        if (payrollInfo.IsPtApplicable && companyConfig.PtEnabled)
        {
            var ptState = payrollInfo.WorkState ?? companyConfig.PtState ?? "";
            var ptCalculatedViaRules = false;

            // Try rules engine first for PT
            if (_ruleEngine != null)
            {
                // PT rules are typically named PT (generic) or can include state in conditions
                var ptResult = await _ruleEngine.CalculateComponentAsync(
                    payrollInfo.CompanyId, "PT", ruleVariables);

                if (ptResult.Success && ptResult.RuleUsed != null)
                {
                    transaction.ProfessionalTax = Math.Round(ptResult.Result, 0, MidpointRounding.AwayFromZero);
                    ptCalculatedViaRules = true;

                    if (transaction.ProfessionalTax > 0)
                        lines.Add(CreateLine("statutory", ++lineSeq, $"PT_{(ptState.Length > 0 ? ptState : "UNKNOWN").ToUpperInvariant().Replace(" ", "_")}",
                            $"Professional Tax ({ptState})", transaction.GrossEarnings, null, transaction.ProfessionalTax,
                            JsonSerializer.Serialize(new {
                                source = "rules_engine",
                                rule_name = ptResult.RuleUsed.Name,
                                rule_id = ptResult.RuleUsed.Id,
                                state = ptState,
                                employee_work_state = payrollInfo.WorkState,
                                company_default_state = companyConfig.PtState,
                                month = payrollMonth,
                                steps = ptResult.Steps.Select(s => new { s.Description, s.Expression, s.Value })
                            })));
                }
            }

            // Fall back to legacy PT service if rules not used
            if (!ptCalculatedViaRules)
            {
                var ptResult = await _ptService.CalculateAsync(
                    transaction.GrossEarnings,
                    ptState,
                    payrollMonth,
                    true);

                transaction.ProfessionalTax = ptResult.TaxAmount;

                if (transaction.ProfessionalTax > 0)
                    lines.Add(CreateLine("statutory", ++lineSeq, $"PT_{(ptState.Length > 0 ? ptState : "UNKNOWN").ToUpperInvariant().Replace(" ", "_")}",
                        $"Professional Tax ({ptState})", transaction.GrossEarnings, null, transaction.ProfessionalTax,
                        JsonSerializer.Serialize(new { source = "legacy", state = ptState, employee_work_state = payrollInfo.WorkState, company_default_state = companyConfig.PtState, month = payrollMonth })));
            }
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

        // Gratuity provision (only if enabled in company config)
        if (companyConfig.GratuityEnabled)
        {
            var gratuityCalculatedViaRules = false;

            // Try rules engine first for Gratuity
            if (_ruleEngine != null)
            {
                var gratuityResult = await _ruleEngine.CalculateComponentAsync(
                    payrollInfo.CompanyId, "GRATUITY", ruleVariables);

                if (gratuityResult.Success && gratuityResult.RuleUsed != null)
                {
                    transaction.GratuityProvision = Math.Round(gratuityResult.Result, 0, MidpointRounding.AwayFromZero);
                    gratuityCalculatedViaRules = true;

                    if (transaction.GratuityProvision > 0)
                        lines.Add(CreateLine("employer_contribution", ++lineSeq, "GRATUITY_PROVISION", "Gratuity Provision",
                            transaction.BasicEarned, null, transaction.GratuityProvision,
                            JsonSerializer.Serialize(new {
                                source = "rules_engine",
                                rule_name = gratuityResult.RuleUsed.Name,
                                rule_id = gratuityResult.RuleUsed.Id
                            })));
                }
            }

            // Fall back to legacy calculation if rules not used
            if (!gratuityCalculatedViaRules)
            {
                var gratuityRate = companyConfig.GratuityRate > 0 ? companyConfig.GratuityRate : 4.81m;
                transaction.GratuityProvision = Math.Round(transaction.BasicEarned * (gratuityRate / 100m), 0, MidpointRounding.AwayFromZero);
                if (transaction.GratuityProvision > 0)
                    lines.Add(CreateLine("employer_contribution", ++lineSeq, "GRATUITY_PROVISION", "Gratuity Provision", transaction.BasicEarned, gratuityRate, transaction.GratuityProvision,
                        JsonSerializer.Serialize(new { source = "legacy", rate = gratuityRate })));
            }
        }
        else
        {
            transaction.GratuityProvision = 0;
        }

        // Total employer cost
        transaction.TotalEmployerCost = transaction.GrossEarnings + transaction.PfEmployer +
                                        transaction.PfAdminCharges + transaction.PfEdli +
                                        transaction.EsiEmployer + transaction.GratuityProvision;

        transaction.Status = "computed";
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;

        result.Transaction = transaction;
        result.CalculationLines = lines;

        // Track which tax rate provider/rule pack was used for TDS calculation
        var financialYear = GetFinancialYear(payrollMonth, payrollYear);
        result.RulePackId = await _tdsService.GetActiveRulePackIdAsync(financialYear);
        result.TaxRateProviderName = _tdsService.ProviderName;

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
        // Formula: YTD (actual past) + Current month (actual) + Future months (projected at standard rate)
        // remainingMonths represents months AFTER current month (e.g., 11 for April = May→March)
        var projectedAnnualIncome = ytdGross + currentMonthGross + (salaryStructure.MonthlyGross * remainingMonths);

        // For TDS distribution, we need to include the current month
        // Per Section 192 and Rule 26B, TDS should be spread over all months INCLUDING the current month
        // e.g., for April (1st month of FY), TDS should be divided by 12, not 11
        var tdsDistributionMonths = remainingMonths + 1;
        var annualBasic = salaryStructure.BasicSalary * 12;
        var annualHra = salaryStructure.Hra * 12;

        // Get tax declaration
        var declaration = await _taxDeclarationRepository.GetByEmployeeAndYearAsync(payrollInfo.EmployeeId, financialYear);

        // CRITICAL FIX: Only use declarations that are submitted, verified, or locked
        // Draft declarations should NOT be used for TDS calculation
        EmployeeTaxDeclarationDto? declarationDto = null;
        if (declaration != null && IsDeclarationUsableForPayroll(declaration.Status))
        {
            declarationDto = MapToDeclarationDto(declaration);

            // Log warning if tax regime mismatch between declaration and payroll info
            if (!string.IsNullOrEmpty(declaration.TaxRegime) &&
                !string.IsNullOrEmpty(payrollInfo.TaxRegime) &&
                declaration.TaxRegime.ToLowerInvariant() != payrollInfo.TaxRegime.ToLowerInvariant())
            {
                // TODO: Add proper logging - regime mismatch detected
                // Declaration regime: {declaration.TaxRegime}, Payroll info regime: {payrollInfo.TaxRegime}
                // Using payroll info regime as authoritative for TDS calculation
            }
        }

        // Default to NEW regime if not set (user preference)
        var effectiveRegime = string.IsNullOrEmpty(payrollInfo.TaxRegime) ? "new" : payrollInfo.TaxRegime;

        return await _tdsService.CalculateAsync(
            payrollInfo.EmployeeId,
            financialYear,
            effectiveRegime,
            projectedAnnualIncome,
            annualBasic,
            annualHra,
            declarationDto?.OtherIncomeAnnual ?? 0,
            declarationDto?.PrevEmployerIncome ?? 0,
            declarationDto?.PrevEmployerTds ?? 0,
            ytdTds,
            tdsDistributionMonths,  // Use tdsDistributionMonths (includes current month) for TDS division
            declarationDto,
            payrollInfo.DateOfBirth);
    }

    /// <summary>
    /// Check if a declaration status allows it to be used for payroll TDS calculation
    /// Only submitted, verified, or locked declarations should be used
    /// </summary>
    private static bool IsDeclarationUsableForPayroll(string? status)
    {
        if (string.IsNullOrEmpty(status))
            return false;

        var normalizedStatus = status.ToLowerInvariant();
        return normalizedStatus is "submitted" or "verified" or "locked";
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

    /// <summary>
    /// Build the variables dictionary for the rules engine.
    /// These variables can be referenced in formula expressions.
    /// </summary>
    private Dictionary<string, decimal> BuildRuleVariables(
        EmployeeSalaryStructure salaryStructure,
        PayrollTransaction transaction,
        int workingDays,
        int presentDays,
        decimal prorationFactor,
        CompanyStatutoryConfig companyConfig,
        int payrollMonth,
        int payrollYear)
    {
        // Calculate PF wage (Basic + DA, optionally + Special Allowance)
        var pfWage = salaryStructure.BasicSalary + salaryStructure.DearnessAllowance;
        if (companyConfig.PfIncludeSpecialAllowance)
        {
            pfWage += salaryStructure.SpecialAllowance;
        }

        return new Dictionary<string, decimal>
        {
            // Basic salary components (monthly values)
            ["basic"] = salaryStructure.BasicSalary,
            ["hra"] = salaryStructure.Hra,
            ["da"] = salaryStructure.DearnessAllowance,
            ["conveyance"] = salaryStructure.ConveyanceAllowance,
            ["medical"] = salaryStructure.MedicalAllowance,
            ["special_allowance"] = salaryStructure.SpecialAllowance,
            ["other_allowances"] = salaryStructure.OtherAllowances,

            // Gross and CTC
            ["monthly_gross"] = salaryStructure.MonthlyGross,
            ["annual_ctc"] = salaryStructure.AnnualCtc,

            // PF wage base (for PF calculations)
            ["pf_wage"] = pfWage,
            ["pf_wage_ceiling"] = companyConfig.PfWageCeiling,

            // Prorated/earned values (after attendance adjustment)
            ["basic_earned"] = transaction.BasicEarned,
            ["hra_earned"] = transaction.HraEarned,
            ["da_earned"] = transaction.DaEarned,
            ["gross_earnings"] = transaction.GrossEarnings,

            // Attendance variables
            ["working_days"] = workingDays,
            ["present_days"] = presentDays,
            ["lop_days"] = workingDays - presentDays,
            ["proration_factor"] = prorationFactor,

            // Payroll period (for month-specific calculations like PT February adjustment)
            ["payroll_month"] = payrollMonth,
            ["payroll_year"] = payrollYear,

            // Variable pay (not prorated)
            ["bonus"] = transaction.BonusPaid,
            ["arrears"] = transaction.Arrears,
            ["incentives"] = transaction.Incentives,
            ["reimbursements"] = transaction.Reimbursements,
            ["lta"] = transaction.LtaPaid,

            // Statutory ceilings and rates for reference
            ["esi_ceiling"] = companyConfig.EsiGrossCeiling,
            ["pf_employee_rate"] = companyConfig.PfEmployeeRate,
            ["pf_employer_rate"] = companyConfig.PfEmployerRate,
            ["esi_employee_rate"] = companyConfig.EsiEmployeeRate,
            ["esi_employer_rate"] = companyConfig.EsiEmployerRate,
        };
    }
}
