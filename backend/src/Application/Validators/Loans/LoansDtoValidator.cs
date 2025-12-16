using Application.DTOs.Loans;
using FluentValidation;

namespace Application.Validators.Loans;

public class CreateLoanDtoValidator : AbstractValidator<CreateLoanDto>
{
    public CreateLoanDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");
        RuleFor(x => x.LoanName).NotEmpty().MaximumLength(200).WithMessage("LoanName is required and must not exceed 200 characters");
        RuleFor(x => x.LenderName).NotEmpty().MaximumLength(200).WithMessage("LenderName is required and must not exceed 200 characters");
        RuleFor(x => x.LoanType).Must(BeValidLoanType).WithMessage("LoanType must be 'secured', 'unsecured', or 'asset_financing'");
        RuleFor(x => x.PrincipalAmount).GreaterThan(0).WithMessage("PrincipalAmount must be greater than 0");
        RuleFor(x => x.InterestRate).InclusiveBetween(0, 100).WithMessage("InterestRate must be between 0 and 100");
        RuleFor(x => x.LoanStartDate).NotEmpty().WithMessage("LoanStartDate is required");
        RuleFor(x => x.TenureMonths).GreaterThan(0).LessThanOrEqualTo(360).WithMessage("TenureMonths must be between 1 and 360");
        RuleFor(x => x.InterestType).Must(BeValidInterestType).WithMessage("InterestType must be 'fixed', 'floating', or 'reducing'");
        RuleFor(x => x.CompoundingFrequency).Must(BeValidCompoundingFrequency).WithMessage("CompoundingFrequency must be 'monthly', 'quarterly', or 'annually'");
    }

    private bool BeValidLoanType(string type) =>
        new[] { "secured", "unsecured", "asset_financing" }.Contains(type?.ToLowerInvariant());

    private bool BeValidInterestType(string type) =>
        new[] { "fixed", "floating", "reducing" }.Contains(type?.ToLowerInvariant());

    private bool BeValidCompoundingFrequency(string frequency) =>
        new[] { "monthly", "quarterly", "annually" }.Contains(frequency?.ToLowerInvariant());
}

public class UpdateLoanDtoValidator : AbstractValidator<UpdateLoanDto>
{
    public UpdateLoanDtoValidator()
    {
        RuleFor(x => x.LoanName).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.LoanName))
            .WithMessage("LoanName must not exceed 200 characters");
        RuleFor(x => x.LenderName).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.LenderName))
            .WithMessage("LenderName must not exceed 200 characters");
        RuleFor(x => x.LoanType).Must(BeValidLoanType).When(x => !string.IsNullOrWhiteSpace(x.LoanType))
            .WithMessage("LoanType must be 'secured', 'unsecured', or 'asset_financing'");
        RuleFor(x => x.PrincipalAmount).GreaterThan(0).When(x => x.PrincipalAmount.HasValue)
            .WithMessage("PrincipalAmount must be greater than 0");
        RuleFor(x => x.InterestRate).InclusiveBetween(0, 100).When(x => x.InterestRate.HasValue)
            .WithMessage("InterestRate must be between 0 and 100");
        RuleFor(x => x.TenureMonths).GreaterThan(0).LessThanOrEqualTo(360).When(x => x.TenureMonths.HasValue)
            .WithMessage("TenureMonths must be between 1 and 360");
        RuleFor(x => x.InterestType).Must(BeValidInterestType).When(x => !string.IsNullOrWhiteSpace(x.InterestType))
            .WithMessage("InterestType must be 'fixed', 'floating', or 'reducing'");
        RuleFor(x => x.CompoundingFrequency).Must(BeValidCompoundingFrequency).When(x => !string.IsNullOrWhiteSpace(x.CompoundingFrequency))
            .WithMessage("CompoundingFrequency must be 'monthly', 'quarterly', or 'annually'");
        RuleFor(x => x.Status).Must(BeValidStatus).When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be 'active', 'closed', 'foreclosed', or 'defaulted'");
    }

    private bool BeValidLoanType(string? type) =>
        new[] { "secured", "unsecured", "asset_financing" }.Contains(type?.ToLowerInvariant());

    private bool BeValidInterestType(string? type) =>
        new[] { "fixed", "floating", "reducing" }.Contains(type?.ToLowerInvariant());

    private bool BeValidCompoundingFrequency(string? frequency) =>
        new[] { "monthly", "quarterly", "annually" }.Contains(frequency?.ToLowerInvariant());

    private bool BeValidStatus(string? status) =>
        new[] { "active", "closed", "foreclosed", "defaulted" }.Contains(status?.ToLowerInvariant());
}

public class CreateEmiPaymentDtoValidator : AbstractValidator<CreateEmiPaymentDto>
{
    public CreateEmiPaymentDtoValidator()
    {
        RuleFor(x => x.PaymentDate).NotEmpty().WithMessage("PaymentDate is required");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");
        RuleFor(x => x.PrincipalAmount).GreaterThanOrEqualTo(0).WithMessage("PrincipalAmount must be greater than or equal to 0");
        RuleFor(x => x.InterestAmount).GreaterThanOrEqualTo(0).WithMessage("InterestAmount must be greater than or equal to 0");
        RuleFor(x => x).Must(HaveValidAmounts).WithMessage("PrincipalAmount + InterestAmount must equal Amount");
        RuleFor(x => x.PaymentMethod).Must(BeValidPaymentMethod).When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod))
            .WithMessage("PaymentMethod must be 'bank_transfer', 'cheque', 'cash', 'online', or 'other'");
        RuleFor(x => x.EmiNumber).GreaterThan(0).When(x => x.EmiNumber.HasValue)
            .WithMessage("EmiNumber must be greater than 0");
    }

    private bool HaveValidAmounts(CreateEmiPaymentDto dto)
    {
        var sum = dto.PrincipalAmount + dto.InterestAmount;
        return Math.Abs(sum - dto.Amount) < 0.01m; // Allow small rounding differences
    }

    private bool BeValidPaymentMethod(string method) =>
        new[] { "bank_transfer", "cheque", "cash", "online", "other" }.Contains(method?.ToLowerInvariant());
}

public class PrepaymentDtoValidator : AbstractValidator<PrepaymentDto>
{
    public PrepaymentDtoValidator()
    {
        RuleFor(x => x.PrepaymentDate).NotEmpty().WithMessage("PrepaymentDate is required");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");
        RuleFor(x => x.PaymentMethod).Must(BeValidPaymentMethod).When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod))
            .WithMessage("PaymentMethod must be 'bank_transfer', 'cheque', 'cash', 'online', or 'other'");
    }

    private bool BeValidPaymentMethod(string method) =>
        new[] { "bank_transfer", "cheque", "cash", "online", "other" }.Contains(method?.ToLowerInvariant());
}





