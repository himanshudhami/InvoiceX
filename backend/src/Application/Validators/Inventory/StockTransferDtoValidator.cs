using Application.DTOs.Inventory;
using FluentValidation;

namespace Application.Validators.Inventory
{
    /// <summary>
    /// Validator for CreateStockTransferDto
    /// </summary>
    public class CreateStockTransferDtoValidator : AbstractValidator<CreateStockTransferDto>
    {
        public CreateStockTransferDtoValidator()
        {
            RuleFor(x => x.TransferDate)
                .NotEmpty().WithMessage("Transfer date is required");

            RuleFor(x => x.FromWarehouseId)
                .NotEmpty().WithMessage("Source warehouse is required");

            RuleFor(x => x.ToWarehouseId)
                .NotEmpty().WithMessage("Destination warehouse is required")
                .NotEqual(x => x.FromWarehouseId)
                .WithMessage("Source and destination warehouses must be different");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one item is required")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("At least one item is required");

            RuleForEach(x => x.Items)
                .SetValidator(new StockTransferItemDtoValidator())
                .When(x => x.Items != null && x.Items.Any());
        }
    }

    /// <summary>
    /// Validator for UpdateStockTransferDto
    /// </summary>
    public class UpdateStockTransferDtoValidator : AbstractValidator<UpdateStockTransferDto>
    {
        public UpdateStockTransferDtoValidator()
        {
            RuleFor(x => x.TransferDate)
                .NotEmpty().WithMessage("Transfer date is required");

            RuleFor(x => x.FromWarehouseId)
                .NotEmpty().WithMessage("Source warehouse is required");

            RuleFor(x => x.ToWarehouseId)
                .NotEmpty().WithMessage("Destination warehouse is required")
                .NotEqual(x => x.FromWarehouseId)
                .WithMessage("Source and destination warehouses must be different");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one item is required")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("At least one item is required");

            RuleForEach(x => x.Items)
                .SetValidator(new StockTransferItemDtoValidator())
                .When(x => x.Items != null && x.Items.Any());
        }
    }

    /// <summary>
    /// Validator for StockTransferItemDto
    /// </summary>
    public class StockTransferItemDtoValidator : AbstractValidator<StockTransferItemDto>
    {
        public StockTransferItemDtoValidator()
        {
            RuleFor(x => x.StockItemId)
                .NotEmpty().WithMessage("Stock item is required");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            RuleFor(x => x.Rate)
                .GreaterThanOrEqualTo(0).WithMessage("Rate cannot be negative")
                .When(x => x.Rate.HasValue);

            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Value cannot be negative")
                .When(x => x.Value.HasValue);

            RuleFor(x => x.ReceivedQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Received quantity cannot be negative")
                .LessThanOrEqualTo(x => x.Quantity).WithMessage("Received quantity cannot exceed transfer quantity")
                .When(x => x.ReceivedQuantity.HasValue);
        }
    }
}
