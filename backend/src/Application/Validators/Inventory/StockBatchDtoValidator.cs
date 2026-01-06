using Application.DTOs.Inventory;
using FluentValidation;

namespace Application.Validators.Inventory
{
    /// <summary>
    /// Validator for CreateStockBatchDto
    /// </summary>
    public class CreateStockBatchDtoValidator : AbstractValidator<CreateStockBatchDto>
    {
        public CreateStockBatchDtoValidator()
        {
            RuleFor(x => x.StockItemId)
                .NotEmpty().WithMessage("Stock item is required");

            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("Warehouse is required");

            RuleFor(x => x.BatchNumber)
                .NotEmpty().WithMessage("Batch number is required")
                .MaximumLength(100).WithMessage("Batch number cannot exceed 100 characters");

            RuleFor(x => x.ExpiryDate)
                .GreaterThan(x => x.ManufacturingDate)
                .WithMessage("Expiry date must be after manufacturing date")
                .When(x => x.ManufacturingDate.HasValue && x.ExpiryDate.HasValue);

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative");

            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Value cannot be negative");
        }
    }

    /// <summary>
    /// Validator for UpdateStockBatchDto
    /// </summary>
    public class UpdateStockBatchDtoValidator : AbstractValidator<UpdateStockBatchDto>
    {
        public UpdateStockBatchDtoValidator()
        {
            RuleFor(x => x.BatchNumber)
                .NotEmpty().WithMessage("Batch number is required")
                .MaximumLength(100).WithMessage("Batch number cannot exceed 100 characters");

            RuleFor(x => x.ExpiryDate)
                .GreaterThan(x => x.ManufacturingDate)
                .WithMessage("Expiry date must be after manufacturing date")
                .When(x => x.ManufacturingDate.HasValue && x.ExpiryDate.HasValue);
        }
    }
}
