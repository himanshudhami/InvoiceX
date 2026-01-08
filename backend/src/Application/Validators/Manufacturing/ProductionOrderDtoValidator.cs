using Application.DTOs.Manufacturing;
using FluentValidation;

namespace Application.Validators.Manufacturing;

public class CreateProductionOrderDtoValidator : AbstractValidator<CreateProductionOrderDto>
{
    public CreateProductionOrderDtoValidator()
    {
        RuleFor(x => x.BomId)
            .NotEmpty()
            .WithMessage("BOM is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("Warehouse is required");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0)
            .WithMessage("Planned quantity must be greater than 0");

        RuleFor(x => x.PlannedEndDate)
            .GreaterThanOrEqualTo(x => x.PlannedStartDate)
            .When(x => x.PlannedStartDate.HasValue && x.PlannedEndDate.HasValue)
            .WithMessage("Planned end date must be after planned start date");

        RuleForEach(x => x.Items)
            .SetValidator(new ProductionOrderItemDtoValidator())
            .When(x => x.Items != null && x.Items.Any());
    }
}

public class UpdateProductionOrderDtoValidator : AbstractValidator<UpdateProductionOrderDto>
{
    public UpdateProductionOrderDtoValidator()
    {
        RuleFor(x => x.BomId)
            .NotEmpty()
            .WithMessage("BOM is required");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("Warehouse is required");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0)
            .WithMessage("Planned quantity must be greater than 0");

        RuleFor(x => x.PlannedEndDate)
            .GreaterThanOrEqualTo(x => x.PlannedStartDate)
            .When(x => x.PlannedStartDate.HasValue && x.PlannedEndDate.HasValue)
            .WithMessage("Planned end date must be after planned start date");

        RuleForEach(x => x.Items)
            .SetValidator(new ProductionOrderItemDtoValidator())
            .When(x => x.Items != null && x.Items.Any());
    }
}

public class ProductionOrderItemDtoValidator : AbstractValidator<ProductionOrderItemDto>
{
    public ProductionOrderItemDtoValidator()
    {
        RuleFor(x => x.ComponentId)
            .NotEmpty()
            .WithMessage("Component is required");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0)
            .WithMessage("Planned quantity must be greater than 0");

        RuleFor(x => x.ConsumedQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Consumed quantity cannot be negative");
    }
}

public class CompleteProductionOrderDtoValidator : AbstractValidator<CompleteProductionOrderDto>
{
    public CompleteProductionOrderDtoValidator()
    {
        RuleFor(x => x.ActualQuantity)
            .GreaterThan(0)
            .WithMessage("Actual quantity must be greater than 0");
    }
}

public class ConsumeItemDtoValidator : AbstractValidator<ConsumeItemDto>
{
    public ConsumeItemDtoValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("Item is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");
    }
}
