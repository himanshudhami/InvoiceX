using Application.DTOs.Inventory;
using FluentValidation;

namespace Application.Validators.Inventory
{
    /// <summary>
    /// Validator for CreateStockGroupDto
    /// </summary>
    public class CreateStockGroupDtoValidator : AbstractValidator<CreateStockGroupDto>
    {
        public CreateStockGroupDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");
        }
    }

    /// <summary>
    /// Validator for UpdateStockGroupDto
    /// </summary>
    public class UpdateStockGroupDtoValidator : AbstractValidator<UpdateStockGroupDto>
    {
        public UpdateStockGroupDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");
        }
    }
}
