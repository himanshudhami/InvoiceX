using Application.DTOs.Inventory;
using FluentValidation;

namespace Application.Validators.Inventory
{
    /// <summary>
    /// Validator for CreateStockMovementDto
    /// </summary>
    public class CreateStockMovementDtoValidator : AbstractValidator<CreateStockMovementDto>
    {
        public CreateStockMovementDtoValidator()
        {
            RuleFor(x => x.StockItemId)
                .NotEmpty().WithMessage("Stock item is required");

            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("Warehouse is required");

            RuleFor(x => x.MovementDate)
                .NotEmpty().WithMessage("Movement date is required");

            RuleFor(x => x.MovementType)
                .NotEmpty().WithMessage("Movement type is required")
                .Must(BeValidMovementType)
                .WithMessage("Invalid movement type. Must be one of: purchase, sale, transfer_in, transfer_out, adjustment, opening");

            RuleFor(x => x.Quantity)
                .NotEqual(0).WithMessage("Quantity cannot be zero");

            RuleFor(x => x.Rate)
                .GreaterThanOrEqualTo(0).WithMessage("Rate cannot be negative")
                .When(x => x.Rate.HasValue);

            RuleFor(x => x.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Value cannot be negative")
                .When(x => x.Value.HasValue);

            RuleFor(x => x.SourceType)
                .Must(BeValidSourceType)
                .WithMessage("Invalid source type. Must be one of: sales_invoice, purchase_invoice, stock_journal, stock_transfer")
                .When(x => !string.IsNullOrEmpty(x.SourceType));

            RuleFor(x => x.SourceId)
                .NotEmpty()
                .WithMessage("Source ID is required when source type is specified")
                .When(x => !string.IsNullOrEmpty(x.SourceType));
        }

        private bool BeValidMovementType(string type)
        {
            var validTypes = new[] { "purchase", "sale", "transfer_in", "transfer_out", "adjustment", "opening" };
            return validTypes.Contains(type.ToLowerInvariant());
        }

        private bool BeValidSourceType(string? sourceType)
        {
            if (string.IsNullOrEmpty(sourceType)) return true;
            var validTypes = new[] { "sales_invoice", "purchase_invoice", "stock_journal", "stock_transfer", "delivery_note", "receipt_note" };
            return validTypes.Contains(sourceType.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Validator for UpdateStockMovementDto
    /// </summary>
    public class UpdateStockMovementDtoValidator : AbstractValidator<UpdateStockMovementDto>
    {
        public UpdateStockMovementDtoValidator()
        {
            RuleFor(x => x.MovementDate)
                .NotEmpty().WithMessage("Movement date is required");

            RuleFor(x => x.MovementType)
                .NotEmpty().WithMessage("Movement type is required")
                .Must(BeValidMovementType)
                .WithMessage("Invalid movement type. Must be one of: purchase, sale, transfer_in, transfer_out, adjustment, opening");

            RuleFor(x => x.Quantity)
                .NotEqual(0).WithMessage("Quantity cannot be zero");

            RuleFor(x => x.Rate)
                .GreaterThanOrEqualTo(0).WithMessage("Rate cannot be negative")
                .When(x => x.Rate.HasValue);
        }

        private bool BeValidMovementType(string type)
        {
            var validTypes = new[] { "purchase", "sale", "transfer_in", "transfer_out", "adjustment", "opening" };
            return validTypes.Contains(type.ToLowerInvariant());
        }
    }
}
