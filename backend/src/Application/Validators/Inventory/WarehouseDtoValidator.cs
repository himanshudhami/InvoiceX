using Application.DTOs.Inventory;
using FluentValidation;

namespace Application.Validators.Inventory
{
    /// <summary>
    /// Validator for CreateWarehouseDto
    /// </summary>
    public class CreateWarehouseDtoValidator : AbstractValidator<CreateWarehouseDto>
    {
        public CreateWarehouseDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

            RuleFor(x => x.Code)
                .MaximumLength(50).WithMessage("Code cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Code));

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.City));

            RuleFor(x => x.State)
                .MaximumLength(100).WithMessage("State cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.State));

            RuleFor(x => x.PinCode)
                .MaximumLength(20).WithMessage("PIN Code cannot exceed 20 characters")
                .Matches(@"^[0-9]{6}$").WithMessage("PIN Code must be 6 digits")
                .When(x => !string.IsNullOrEmpty(x.PinCode));
        }
    }

    /// <summary>
    /// Validator for UpdateWarehouseDto
    /// </summary>
    public class UpdateWarehouseDtoValidator : AbstractValidator<UpdateWarehouseDto>
    {
        public UpdateWarehouseDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

            RuleFor(x => x.Code)
                .MaximumLength(50).WithMessage("Code cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Code));

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.City));

            RuleFor(x => x.State)
                .MaximumLength(100).WithMessage("State cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.State));

            RuleFor(x => x.PinCode)
                .MaximumLength(20).WithMessage("PIN Code cannot exceed 20 characters")
                .Matches(@"^[0-9]{6}$").WithMessage("PIN Code must be 6 digits")
                .When(x => !string.IsNullOrEmpty(x.PinCode));
        }
    }
}
