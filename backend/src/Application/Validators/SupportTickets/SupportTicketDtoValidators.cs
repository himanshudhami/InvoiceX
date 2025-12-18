using Application.DTOs.SupportTickets;
using FluentValidation;

namespace Application.Validators.SupportTickets;

public class CreateSupportTicketDtoValidator : AbstractValidator<CreateSupportTicketDto>
{
    private static readonly string[] ValidCategories = { "payroll", "leave", "it", "hr", "assets", "general" };
    private static readonly string[] ValidPriorities = { "low", "medium", "high", "urgent" };

    public CreateSupportTicketDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Category).Must(c => ValidCategories.Contains(c))
            .WithMessage("Category must be one of: " + string.Join(", ", ValidCategories));
        RuleFor(x => x.Priority).Must(p => ValidPriorities.Contains(p))
            .WithMessage("Priority must be one of: " + string.Join(", ", ValidPriorities));
    }
}

public class UpdateSupportTicketDtoValidator : AbstractValidator<UpdateSupportTicketDto>
{
    private static readonly string[] ValidCategories = { "payroll", "leave", "it", "hr", "assets", "general" };
    private static readonly string[] ValidPriorities = { "low", "medium", "high", "urgent" };
    private static readonly string[] ValidStatuses = { "open", "in_progress", "waiting_on_employee", "resolved", "closed" };

    public UpdateSupportTicketDtoValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Category).Must(c => ValidCategories.Contains(c))
            .WithMessage("Category must be one of: " + string.Join(", ", ValidCategories));
        RuleFor(x => x.Priority).Must(p => ValidPriorities.Contains(p))
            .WithMessage("Priority must be one of: " + string.Join(", ", ValidPriorities));
        RuleFor(x => x.Status).Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be one of: " + string.Join(", ", ValidStatuses));
    }
}

public class CreateTicketMessageDtoValidator : AbstractValidator<CreateTicketMessageDto>
{
    public CreateTicketMessageDtoValidator()
    {
        RuleFor(x => x.Message).NotEmpty();
    }
}

public class CreateFaqDtoValidator : AbstractValidator<CreateFaqDto>
{
    public CreateFaqDtoValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Question).NotEmpty();
        RuleFor(x => x.Answer).NotEmpty();
    }
}

public class UpdateFaqDtoValidator : AbstractValidator<UpdateFaqDto>
{
    public UpdateFaqDtoValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Question).NotEmpty();
        RuleFor(x => x.Answer).NotEmpty();
    }
}
