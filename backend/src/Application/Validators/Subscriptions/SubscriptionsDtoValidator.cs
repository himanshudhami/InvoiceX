using Application.DTOs.Subscriptions;
using FluentValidation;

namespace Application.Validators.Subscriptions;

public class CreateSubscriptionDtoValidator : AbstractValidator<CreateSubscriptionDto>
{
    public CreateSubscriptionDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(BeValidStatus);
        RuleFor(x => x.RenewalPeriod).Must(BeValidPeriod);
        RuleFor(x => x.Currency).MaximumLength(10).When(x => !string.IsNullOrWhiteSpace(x.Currency));
    }

    private bool BeValidStatus(string status) =>
        new[] { "trial", "active", "on_hold", "expired", "cancelled" }.Contains(status);

    private bool BeValidPeriod(string period) =>
        new[] { "monthly", "quarterly", "yearly", "custom" }.Contains(period);
}

public class UpdateSubscriptionDtoValidator : AbstractValidator<UpdateSubscriptionDto>
{
    public UpdateSubscriptionDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
        {
            RuleFor(x => x.Status).Must(s => new[] { "trial", "active", "on_hold", "expired", "cancelled" }.Contains(s!));
        });
        When(x => !string.IsNullOrWhiteSpace(x.RenewalPeriod), () =>
        {
            RuleFor(x => x.RenewalPeriod).Must(p => new[] { "monthly", "quarterly", "yearly", "custom" }.Contains(p!));
        });
    }
}

public class CreateSubscriptionAssignmentDtoValidator : AbstractValidator<CreateSubscriptionAssignmentDto>
{
    public CreateSubscriptionAssignmentDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TargetType).Must(t => t == "employee" || t == "company");
        When(x => x.TargetType == "employee", () =>
        {
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId required when assigning to employee");
        });
    }
}

public class RevokeSubscriptionAssignmentDtoValidator : AbstractValidator<RevokeSubscriptionAssignmentDto>
{
    public RevokeSubscriptionAssignmentDtoValidator()
    {
        // optional
    }
}




