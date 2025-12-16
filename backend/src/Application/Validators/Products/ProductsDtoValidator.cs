using Application.DTOs.Products;
using FluentValidation;

namespace Application.Validators.Products
{
    /// <summary>
    /// Validator for CreateProductsDto
    /// </summary>
    public class CreateProductsDtoValidator : AbstractValidator<CreateProductsDto>
    {
        public CreateProductsDtoValidator()
        {
RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Description cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

RuleFor(x => x.Sku)
                .MaximumLength(255).WithMessage("Sku cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Sku));

RuleFor(x => x.Category)
                .MaximumLength(255).WithMessage("Category cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Category));

RuleFor(x => x.Type)
                .MaximumLength(255).WithMessage("Type cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Type));

RuleFor(x => x.UnitPrice)
                .NotEmpty().WithMessage("UnitPrice is required")
                .GreaterThan(0).WithMessage("UnitPrice must be greater than 0");

RuleFor(x => x.Unit)
                .MaximumLength(255).WithMessage("Unit cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Unit));

}
    }

    /// <summary>
    /// Validator for UpdateProductsDto
    /// </summary>
    public class UpdateProductsDtoValidator : AbstractValidator<UpdateProductsDto>
    {
        public UpdateProductsDtoValidator()
        {

RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.Description)
                .MaximumLength(255).WithMessage("Description cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

RuleFor(x => x.Sku)
                .MaximumLength(255).WithMessage("Sku cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Sku));

RuleFor(x => x.Category)
                .MaximumLength(255).WithMessage("Category cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Category));

RuleFor(x => x.Type)
                .MaximumLength(255).WithMessage("Type cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Type));

RuleFor(x => x.UnitPrice)
                .NotEmpty().WithMessage("UnitPrice is required")
                .GreaterThan(0).WithMessage("UnitPrice must be greater than 0");

RuleFor(x => x.Unit)
                .MaximumLength(255).WithMessage("Unit cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Unit));

}
    }
}