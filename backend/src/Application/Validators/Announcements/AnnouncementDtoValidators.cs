using Application.DTOs.Announcements;
using FluentValidation;

namespace Application.Validators.Announcements;

public class CreateAnnouncementDtoValidator : AbstractValidator<CreateAnnouncementDto>
{
    private static readonly string[] ValidCategories = { "general", "hr", "policy", "event", "celebration" };
    private static readonly string[] ValidPriorities = { "low", "normal", "high", "urgent" };

    public CreateAnnouncementDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Category).Must(c => ValidCategories.Contains(c))
            .WithMessage("Category must be one of: " + string.Join(", ", ValidCategories));
        RuleFor(x => x.Priority).Must(p => ValidPriorities.Contains(p))
            .WithMessage("Priority must be one of: " + string.Join(", ", ValidPriorities));
    }
}

public class UpdateAnnouncementDtoValidator : AbstractValidator<UpdateAnnouncementDto>
{
    private static readonly string[] ValidCategories = { "general", "hr", "policy", "event", "celebration" };
    private static readonly string[] ValidPriorities = { "low", "normal", "high", "urgent" };

    public UpdateAnnouncementDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Category).Must(c => ValidCategories.Contains(c))
            .WithMessage("Category must be one of: " + string.Join(", ", ValidCategories));
        RuleFor(x => x.Priority).Must(p => ValidPriorities.Contains(p))
            .WithMessage("Priority must be one of: " + string.Join(", ", ValidPriorities));
    }
}
