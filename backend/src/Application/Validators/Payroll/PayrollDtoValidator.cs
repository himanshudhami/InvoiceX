using Application.DTOs.Payroll;
using FluentValidation;

namespace Application.Validators.Payroll;

public class CreateEmployeePayrollInfoDtoValidator : AbstractValidator<CreateEmployeePayrollInfoDto>
{
    public CreateEmployeePayrollInfoDtoValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId is required");
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");
        RuleFor(x => x.TaxRegime).Must(BeValidTaxRegime).WithMessage("TaxRegime must be 'old' or 'new'");
        RuleFor(x => x.PayrollType).Must(BeValidPayrollType).WithMessage("PayrollType must be 'employee' or 'contractor'");
        RuleFor(x => x.Uan).MaximumLength(12).WithMessage("UAN must not exceed 12 characters");
        RuleFor(x => x.PfAccountNumber).MaximumLength(22).WithMessage("PF Account Number must not exceed 22 characters");
        RuleFor(x => x.EsiNumber).MaximumLength(17).WithMessage("ESI Number must not exceed 17 characters");
        RuleFor(x => x.PanNumber).MaximumLength(10).Matches("^[A-Z]{5}[0-9]{4}[A-Z]$")
            .When(x => !string.IsNullOrEmpty(x.PanNumber))
            .WithMessage("PAN must be in valid format (e.g., ABCDE1234F)");
        RuleFor(x => x.BankIfsc).MaximumLength(11).Matches("^[A-Z]{4}0[A-Z0-9]{6}$")
            .When(x => !string.IsNullOrEmpty(x.BankIfsc))
            .WithMessage("IFSC must be in valid format (e.g., SBIN0001234)");
    }

    private bool BeValidTaxRegime(string regime) =>
        new[] { "old", "new" }.Contains(regime?.ToLowerInvariant());

    private bool BeValidPayrollType(string type) =>
        new[] { "employee", "contractor" }.Contains(type?.ToLowerInvariant());
}

public class UpdateEmployeePayrollInfoDtoValidator : AbstractValidator<UpdateEmployeePayrollInfoDto>
{
    public UpdateEmployeePayrollInfoDtoValidator()
    {
        RuleFor(x => x.TaxRegime).Must(BeValidTaxRegime)
            .When(x => !string.IsNullOrWhiteSpace(x.TaxRegime))
            .WithMessage("TaxRegime must be 'old' or 'new'");
        RuleFor(x => x.PayrollType).Must(BeValidPayrollType)
            .When(x => !string.IsNullOrWhiteSpace(x.PayrollType))
            .WithMessage("PayrollType must be 'employee' or 'contractor'");
        RuleFor(x => x.Uan).MaximumLength(12)
            .When(x => !string.IsNullOrEmpty(x.Uan))
            .WithMessage("UAN must not exceed 12 characters");
        RuleFor(x => x.PanNumber).MaximumLength(10).Matches("^[A-Z]{5}[0-9]{4}[A-Z]$")
            .When(x => !string.IsNullOrEmpty(x.PanNumber))
            .WithMessage("PAN must be in valid format (e.g., ABCDE1234F)");
        RuleFor(x => x.BankIfsc).MaximumLength(11).Matches("^[A-Z]{4}0[A-Z0-9]{6}$")
            .When(x => !string.IsNullOrEmpty(x.BankIfsc))
            .WithMessage("IFSC must be in valid format (e.g., SBIN0001234)");
    }

    private bool BeValidTaxRegime(string? regime) =>
        regime == null || new[] { "old", "new" }.Contains(regime.ToLowerInvariant());

    private bool BeValidPayrollType(string? type) =>
        type == null || new[] { "employee", "contractor" }.Contains(type.ToLowerInvariant());
}

public class CreateEmployeeSalaryStructureDtoValidator : AbstractValidator<CreateEmployeeSalaryStructureDto>
{
    public CreateEmployeeSalaryStructureDtoValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId is required");
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");
        RuleFor(x => x.EffectiveFrom).NotEmpty().WithMessage("EffectiveFrom date is required");
        RuleFor(x => x.AnnualCtc).GreaterThan(0).WithMessage("Annual CTC must be greater than 0");
        RuleFor(x => x.BasicSalary).GreaterThan(0).WithMessage("Basic salary must be greater than 0");
        RuleFor(x => x.Hra).GreaterThanOrEqualTo(0).WithMessage("HRA must be greater than or equal to 0");
        RuleFor(x => x.DearnessAllowance).GreaterThanOrEqualTo(0).WithMessage("DA must be greater than or equal to 0");
        RuleFor(x => x.ConveyanceAllowance).GreaterThanOrEqualTo(0).WithMessage("Conveyance must be greater than or equal to 0");
        RuleFor(x => x.MedicalAllowance).GreaterThanOrEqualTo(0).WithMessage("Medical allowance must be greater than or equal to 0");
        RuleFor(x => x.SpecialAllowance).GreaterThanOrEqualTo(0).WithMessage("Special allowance must be greater than or equal to 0");
        RuleFor(x => x.OtherAllowances).GreaterThanOrEqualTo(0).WithMessage("Other allowances must be greater than or equal to 0");
        RuleFor(x => x.LtaAnnual).GreaterThanOrEqualTo(0).WithMessage("LTA must be greater than or equal to 0");
        RuleFor(x => x.BonusAnnual).GreaterThanOrEqualTo(0).WithMessage("Bonus must be greater than or equal to 0");
        RuleFor(x => x.PfEmployerMonthly).GreaterThanOrEqualTo(0).WithMessage("PF employer must be greater than or equal to 0");
        RuleFor(x => x.EsiEmployerMonthly).GreaterThanOrEqualTo(0).WithMessage("ESI employer must be greater than or equal to 0");
        RuleFor(x => x.GratuityMonthly).GreaterThanOrEqualTo(0).WithMessage("Gratuity must be greater than or equal to 0");
    }
}

public class CreatePayrollRunDtoValidator : AbstractValidator<CreatePayrollRunDto>
{
    public CreatePayrollRunDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");
        RuleFor(x => x.PayrollMonth).InclusiveBetween(1, 12).WithMessage("PayrollMonth must be between 1 and 12");
        RuleFor(x => x.PayrollYear).InclusiveBetween(2000, 2100).WithMessage("PayrollYear must be between 2000 and 2100");
    }
}

public class UpdatePayrollRunDtoValidator : AbstractValidator<UpdatePayrollRunDto>
{
    public UpdatePayrollRunDtoValidator()
    {
        RuleFor(x => x.Status).Must(BeValidStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be 'draft', 'processing', 'computed', 'approved', 'paid', or 'cancelled'");
        RuleFor(x => x.PaymentMode).Must(BeValidPaymentMode)
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentMode))
            .WithMessage("PaymentMode must be 'neft_batch', 'imps', 'upi', or 'manual'");
    }

    private bool BeValidStatus(string? status) =>
        status == null || new[] { "draft", "processing", "computed", "approved", "paid", "cancelled" }.Contains(status.ToLowerInvariant());

    private bool BeValidPaymentMode(string? mode) =>
        mode == null || new[] { "neft_batch", "imps", "upi", "manual" }.Contains(mode.ToLowerInvariant());
}

public class CreateContractorPaymentDtoValidator : AbstractValidator<CreateContractorPaymentDto>
{
    public CreateContractorPaymentDtoValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId is required");
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");
        RuleFor(x => x.PaymentMonth).InclusiveBetween(1, 12).WithMessage("PaymentMonth must be between 1 and 12");
        RuleFor(x => x.PaymentYear).InclusiveBetween(2000, 2100).WithMessage("PaymentYear must be between 2000 and 2100");
        RuleFor(x => x.GrossAmount).GreaterThan(0).WithMessage("GrossAmount must be greater than 0");
        RuleFor(x => x.TdsRate).InclusiveBetween(0, 100).WithMessage("TdsRate must be between 0 and 100");
        RuleFor(x => x.GstRate).InclusiveBetween(0, 28).When(x => x.GstApplicable).WithMessage("GstRate must be between 0 and 28");
        RuleFor(x => x.OtherDeductions).GreaterThanOrEqualTo(0).WithMessage("OtherDeductions must be greater than or equal to 0");
        RuleFor(x => x.InvoiceNumber).MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.InvoiceNumber))
            .WithMessage("InvoiceNumber must not exceed 50 characters");
    }
}

public class UpdateContractorPaymentDtoValidator : AbstractValidator<UpdateContractorPaymentDto>
{
    public UpdateContractorPaymentDtoValidator()
    {
        RuleFor(x => x.GrossAmount).GreaterThan(0)
            .When(x => x.GrossAmount.HasValue)
            .WithMessage("GrossAmount must be greater than 0");
        RuleFor(x => x.TdsRate).InclusiveBetween(0, 100)
            .When(x => x.TdsRate.HasValue)
            .WithMessage("TdsRate must be between 0 and 100");
        RuleFor(x => x.GstRate).InclusiveBetween(0, 28)
            .When(x => x.GstRate.HasValue)
            .WithMessage("GstRate must be between 0 and 28");
        RuleFor(x => x.OtherDeductions).GreaterThanOrEqualTo(0)
            .When(x => x.OtherDeductions.HasValue)
            .WithMessage("OtherDeductions must be greater than or equal to 0");
        RuleFor(x => x.Status).Must(BeValidStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be 'pending', 'approved', 'paid', or 'cancelled'");
        RuleFor(x => x.PaymentMethod).Must(BeValidPaymentMethod)
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod))
            .WithMessage("PaymentMethod must be 'neft', 'imps', 'upi', 'cheque', or 'cash'");
    }

    private bool BeValidStatus(string? status) =>
        status == null || new[] { "pending", "approved", "paid", "cancelled" }.Contains(status.ToLowerInvariant());

    private bool BeValidPaymentMethod(string? method) =>
        method == null || new[] { "neft", "imps", "upi", "cheque", "cash" }.Contains(method.ToLowerInvariant());
}

public class CreateCompanyStatutoryConfigDtoValidator : AbstractValidator<CreateCompanyStatutoryConfigDto>
{
    public CreateCompanyStatutoryConfigDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");

        // PF validation
        When(x => x.PfEnabled, () =>
        {
            RuleFor(x => x.PfEmployeeRate).InclusiveBetween(0, 12).WithMessage("PF Employee Rate must be between 0 and 12");
            RuleFor(x => x.PfEmployerRate).InclusiveBetween(0, 12).WithMessage("PF Employer Rate must be between 0 and 12");
            RuleFor(x => x.PfWageCeiling).GreaterThanOrEqualTo(0).WithMessage("PF Wage Ceiling must be greater than or equal to 0");
        });

        // ESI validation
        When(x => x.EsiEnabled, () =>
        {
            RuleFor(x => x.EsiEmployeeRate).InclusiveBetween(0, 5).WithMessage("ESI Employee Rate must be between 0 and 5");
            RuleFor(x => x.EsiEmployerRate).InclusiveBetween(0, 5).WithMessage("ESI Employer Rate must be between 0 and 5");
            RuleFor(x => x.EsiWageCeiling).GreaterThan(0).WithMessage("ESI Wage Ceiling must be greater than 0");
        });

        // PT validation
        When(x => x.PtEnabled, () =>
        {
            RuleFor(x => x.PtState).NotEmpty().WithMessage("PT State is required when PT is enabled");
        });
    }
}

public class TdsOverrideDtoValidator : AbstractValidator<TdsOverrideDto>
{
    public TdsOverrideDtoValidator()
    {
        RuleFor(x => x.TdsAmount).GreaterThanOrEqualTo(0).WithMessage("TDS Amount must be greater than or equal to 0");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("Reason is required and must not exceed 500 characters");
    }
}

public class CreateEmployeeTaxDeclarationDtoValidator : AbstractValidator<CreateEmployeeTaxDeclarationDto>
{
    private const decimal MAX_80C = 150000m;
    private const decimal MAX_80CCD = 50000m;
    private const decimal MAX_24_HOME_LOAN = 200000m;
    private const decimal MAX_80TTA = 10000m;

    public CreateEmployeeTaxDeclarationDtoValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId is required");
        RuleFor(x => x.FinancialYear).NotEmpty().Matches("^\\d{4}-\\d{2}$")
            .WithMessage("FinancialYear must be in format 'YYYY-YY' (e.g., 2024-25)");
        RuleFor(x => x.TaxRegime).Must(BeValidTaxRegime).WithMessage("TaxRegime must be 'old' or 'new'");

        // 80C validations
        RuleFor(x => x.Sec80cPpf).GreaterThanOrEqualTo(0).WithMessage("PPF must be >= 0");
        RuleFor(x => x.Sec80cElss).GreaterThanOrEqualTo(0).WithMessage("ELSS must be >= 0");
        RuleFor(x => x.Sec80cLifeInsurance).GreaterThanOrEqualTo(0).WithMessage("Life Insurance must be >= 0");
        RuleFor(x => x.Sec80cHomeLoanPrincipal).GreaterThanOrEqualTo(0).WithMessage("Home Loan Principal must be >= 0");
        RuleFor(x => x.Sec80cChildrenTuition).GreaterThanOrEqualTo(0).WithMessage("Children Tuition must be >= 0");
        RuleFor(x => x.Sec80cNsc).GreaterThanOrEqualTo(0).WithMessage("NSC must be >= 0");
        RuleFor(x => x.Sec80cSukanyaSamriddhi).GreaterThanOrEqualTo(0).WithMessage("Sukanya Samriddhi must be >= 0");
        RuleFor(x => x.Sec80cFixedDeposit).GreaterThanOrEqualTo(0).WithMessage("Fixed Deposit must be >= 0");
        RuleFor(x => x.Sec80cOthers).GreaterThanOrEqualTo(0).WithMessage("80C Others must be >= 0");

        // 80CCD(1B) validation
        RuleFor(x => x.Sec80ccdNps).GreaterThanOrEqualTo(0).LessThanOrEqualTo(MAX_80CCD)
            .WithMessage($"NPS (80CCD) must be between 0 and {MAX_80CCD}");

        // 80D validations
        RuleFor(x => x.Sec80dSelfFamily).GreaterThanOrEqualTo(0).WithMessage("Self/Family health insurance must be >= 0");
        RuleFor(x => x.Sec80dParents).GreaterThanOrEqualTo(0).WithMessage("Parents health insurance must be >= 0");
        RuleFor(x => x.Sec80dPreventiveCheckup).GreaterThanOrEqualTo(0).LessThanOrEqualTo(5000)
            .WithMessage("Preventive checkup must be between 0 and 5000");

        // Other sections
        RuleFor(x => x.Sec80eEducationLoan).GreaterThanOrEqualTo(0).WithMessage("Education loan interest must be >= 0");
        RuleFor(x => x.Sec24HomeLoanInterest).GreaterThanOrEqualTo(0).WithMessage("Home loan interest must be >= 0");
        RuleFor(x => x.Sec80gDonations).GreaterThanOrEqualTo(0).WithMessage("Donations must be >= 0");
        RuleFor(x => x.Sec80ttaSavingsInterest).GreaterThanOrEqualTo(0).LessThanOrEqualTo(MAX_80TTA)
            .WithMessage($"Savings interest must be between 0 and {MAX_80TTA}");

        // HRA validation
        RuleFor(x => x.HraRentPaidAnnual).GreaterThanOrEqualTo(0).WithMessage("Rent paid must be >= 0");
        RuleFor(x => x.HraLandlordPan).Matches("^[A-Z]{5}[0-9]{4}[A-Z]$")
            .When(x => !string.IsNullOrEmpty(x.HraLandlordPan) && x.HraRentPaidAnnual > 100000)
            .WithMessage("Landlord PAN is required and must be valid when rent > ₹1 lakh");

        // Previous employer
        RuleFor(x => x.PrevEmployerIncome).GreaterThanOrEqualTo(0).WithMessage("Previous employer income must be >= 0");
        RuleFor(x => x.PrevEmployerTds).GreaterThanOrEqualTo(0).WithMessage("Previous employer TDS must be >= 0");
        RuleFor(x => x.PrevEmployerPf).GreaterThanOrEqualTo(0).WithMessage("Previous employer PF must be >= 0");
        RuleFor(x => x.PrevEmployerPt).GreaterThanOrEqualTo(0).WithMessage("Previous employer PT must be >= 0");

        // Other income
        RuleFor(x => x.OtherIncomeAnnual).GreaterThanOrEqualTo(0).WithMessage("Other income must be >= 0");
    }

    private bool BeValidTaxRegime(string regime) =>
        new[] { "old", "new" }.Contains(regime?.ToLowerInvariant());
}

public class ProcessPayrollDtoValidator : AbstractValidator<ProcessPayrollDto>
{
    public ProcessPayrollDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");
        RuleFor(x => x.PayrollMonth).InclusiveBetween(1, 12).WithMessage("PayrollMonth must be between 1 and 12");
        RuleFor(x => x.PayrollYear).InclusiveBetween(2000, 2100).WithMessage("PayrollYear must be between 2000 and 2100");
    }
}
