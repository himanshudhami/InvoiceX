using Application.DTOs.Inventory;
using FluentValidation;

namespace Application.Validators.Inventory
{
    /// <summary>
    /// Validator for CreateStockItemDto
    /// </summary>
    public class CreateStockItemDtoValidator : AbstractValidator<CreateStockItemDto>
    {
        public CreateStockItemDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

            RuleFor(x => x.Sku)
                .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Sku));

            RuleFor(x => x.BaseUnitId)
                .NotEmpty().WithMessage("Base unit is required");

            RuleFor(x => x.HsnSacCode)
                .MaximumLength(20).WithMessage("HSN/SAC Code cannot exceed 20 characters")
                .Matches(@"^[0-9]{4,8}$").WithMessage("HSN/SAC Code must be 4-8 digits")
                .When(x => !string.IsNullOrEmpty(x.HsnSacCode));

            RuleFor(x => x.GstRate)
                .InclusiveBetween(0, 100).WithMessage("GST rate must be between 0 and 100");

            RuleFor(x => x.OpeningQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Opening quantity cannot be negative");

            RuleFor(x => x.OpeningValue)
                .GreaterThanOrEqualTo(0).WithMessage("Opening value cannot be negative");

            RuleFor(x => x.ReorderLevel)
                .GreaterThanOrEqualTo(0).WithMessage("Reorder level cannot be negative")
                .When(x => x.ReorderLevel.HasValue);

            RuleFor(x => x.ReorderQuantity)
                .GreaterThan(0).WithMessage("Reorder quantity must be greater than 0")
                .When(x => x.ReorderQuantity.HasValue);

            RuleFor(x => x.ValuationMethod)
                .Must(BeValidValuationMethod)
                .WithMessage("Invalid valuation method. Must be one of: fifo, lifo, weighted_avg");

            RuleForEach(x => x.UnitConversions)
                .SetValidator(new UnitConversionDtoValidator())
                .When(x => x.UnitConversions != null && x.UnitConversions.Any());
        }

        private bool BeValidValuationMethod(string method)
        {
            var validMethods = new[] { "fifo", "lifo", "weighted_avg" };
            return validMethods.Contains(method.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Validator for UpdateStockItemDto
    /// </summary>
    public class UpdateStockItemDtoValidator : AbstractValidator<UpdateStockItemDto>
    {
        public UpdateStockItemDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

            RuleFor(x => x.Sku)
                .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Sku));

            RuleFor(x => x.BaseUnitId)
                .NotEmpty().WithMessage("Base unit is required");

            RuleFor(x => x.HsnSacCode)
                .MaximumLength(20).WithMessage("HSN/SAC Code cannot exceed 20 characters")
                .Matches(@"^[0-9]{4,8}$").WithMessage("HSN/SAC Code must be 4-8 digits")
                .When(x => !string.IsNullOrEmpty(x.HsnSacCode));

            RuleFor(x => x.GstRate)
                .InclusiveBetween(0, 100).WithMessage("GST rate must be between 0 and 100");

            RuleFor(x => x.ReorderLevel)
                .GreaterThanOrEqualTo(0).WithMessage("Reorder level cannot be negative")
                .When(x => x.ReorderLevel.HasValue);

            RuleFor(x => x.ReorderQuantity)
                .GreaterThan(0).WithMessage("Reorder quantity must be greater than 0")
                .When(x => x.ReorderQuantity.HasValue);

            RuleFor(x => x.ValuationMethod)
                .Must(BeValidValuationMethod)
                .WithMessage("Invalid valuation method. Must be one of: fifo, lifo, weighted_avg");

            RuleForEach(x => x.UnitConversions)
                .SetValidator(new UnitConversionDtoValidator())
                .When(x => x.UnitConversions != null && x.UnitConversions.Any());
        }

        private bool BeValidValuationMethod(string method)
        {
            var validMethods = new[] { "fifo", "lifo", "weighted_avg" };
            return validMethods.Contains(method.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Validator for UnitConversionDto
    /// </summary>
    public class UnitConversionDtoValidator : AbstractValidator<UnitConversionDto>
    {
        public UnitConversionDtoValidator()
        {
            RuleFor(x => x.FromUnitId)
                .NotEmpty().WithMessage("From unit is required");

            RuleFor(x => x.ToUnitId)
                .NotEmpty().WithMessage("To unit is required")
                .NotEqual(x => x.FromUnitId).WithMessage("From unit and To unit must be different");

            RuleFor(x => x.ConversionFactor)
                .GreaterThan(0).WithMessage("Conversion factor must be greater than 0");
        }
    }
}
