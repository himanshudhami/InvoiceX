using Application.DTOs.Inventory;
using FluentValidation;

namespace Application.Validators.Inventory
{
    /// <summary>
    /// Validator for CreateUnitOfMeasureDto
    /// </summary>
    public class CreateUnitOfMeasureDtoValidator : AbstractValidator<CreateUnitOfMeasureDto>
    {
        public CreateUnitOfMeasureDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Symbol)
                .NotEmpty().WithMessage("Symbol is required")
                .MaximumLength(20).WithMessage("Symbol cannot exceed 20 characters");

            RuleFor(x => x.DecimalPlaces)
                .InclusiveBetween(0, 6).WithMessage("Decimal places must be between 0 and 6");
        }
    }

    /// <summary>
    /// Validator for UpdateUnitOfMeasureDto
    /// </summary>
    public class UpdateUnitOfMeasureDtoValidator : AbstractValidator<UpdateUnitOfMeasureDto>
    {
        public UpdateUnitOfMeasureDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Symbol)
                .NotEmpty().WithMessage("Symbol is required")
                .MaximumLength(20).WithMessage("Symbol cannot exceed 20 characters");

            RuleFor(x => x.DecimalPlaces)
                .InclusiveBetween(0, 6).WithMessage("Decimal places must be between 0 and 6");
        }
    }
}
