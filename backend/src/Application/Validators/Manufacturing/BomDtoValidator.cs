using Application.DTOs.Manufacturing;
using FluentValidation;

namespace Application.Validators.Manufacturing;

public class CreateBomDtoValidator : AbstractValidator<CreateBomDto>
{
    public CreateBomDtoValidator()
    {
        RuleFor(x => x.FinishedGoodId)
            .NotEmpty()
            .WithMessage("Finished good is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50)
            .WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Version)
            .MaximumLength(20)
            .WithMessage("Version cannot exceed 20 characters");

        RuleFor(x => x.OutputQuantity)
            .GreaterThan(0)
            .WithMessage("Output quantity must be greater than 0");

        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveFrom.HasValue && x.EffectiveTo.HasValue)
            .WithMessage("Effective to date must be after effective from date");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one component is required")
            .Must(items => items != null && items.Count > 0)
            .WithMessage("At least one component is required");

        RuleForEach(x => x.Items)
            .SetValidator(new BomItemDtoValidator())
            .When(x => x.Items != null && x.Items.Any());
    }
}

public class UpdateBomDtoValidator : AbstractValidator<UpdateBomDto>
{
    public UpdateBomDtoValidator()
    {
        RuleFor(x => x.FinishedGoodId)
            .NotEmpty()
            .WithMessage("Finished good is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50)
            .WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Version)
            .MaximumLength(20)
            .WithMessage("Version cannot exceed 20 characters");

        RuleFor(x => x.OutputQuantity)
            .GreaterThan(0)
            .WithMessage("Output quantity must be greater than 0");

        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveFrom.HasValue && x.EffectiveTo.HasValue)
            .WithMessage("Effective to date must be after effective from date");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one component is required")
            .Must(items => items != null && items.Count > 0)
            .WithMessage("At least one component is required");

        RuleForEach(x => x.Items)
            .SetValidator(new BomItemDtoValidator())
            .When(x => x.Items != null && x.Items.Any());
    }
}

public class BomItemDtoValidator : AbstractValidator<BomItemDto>
{
    public BomItemDtoValidator()
    {
        RuleFor(x => x.ComponentId)
            .NotEmpty()
            .WithMessage("Component is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.ScrapPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Scrap percentage must be between 0 and 100");

        RuleFor(x => x.Sequence)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sequence must be 0 or greater");
    }
}

public class CopyBomDtoValidator : AbstractValidator<CopyBomDto>
{
    public CopyBomDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50)
            .WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Version)
            .MaximumLength(20)
            .WithMessage("Version cannot exceed 20 characters");
    }
}
