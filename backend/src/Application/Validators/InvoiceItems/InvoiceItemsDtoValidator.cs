using Application.DTOs.InvoiceItems;
using FluentValidation;

namespace Application.Validators.InvoiceItems
{
    /// <summary>
    /// Validator for CreateInvoiceItemsDto
    /// </summary>
    public class CreateInvoiceItemsDtoValidator : AbstractValidator<CreateInvoiceItemsDto>
    {
        public CreateInvoiceItemsDtoValidator()
        {
RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(255).WithMessage("Description cannot exceed 255 characters");

RuleFor(x => x.Quantity)
                .NotEmpty().WithMessage("Quantity is required")
;

RuleFor(x => x.UnitPrice)
                .NotEmpty().WithMessage("UnitPrice is required")
;

RuleFor(x => x.LineTotal)
                .NotEmpty().WithMessage("LineTotal is required")
;

}
    }

    /// <summary>
    /// Validator for UpdateInvoiceItemsDto
    /// </summary>
    public class UpdateInvoiceItemsDtoValidator : AbstractValidator<UpdateInvoiceItemsDto>
    {
        public UpdateInvoiceItemsDtoValidator()
        {

RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(255).WithMessage("Description cannot exceed 255 characters");

RuleFor(x => x.Quantity)
                .NotEmpty().WithMessage("Quantity is required")
;

RuleFor(x => x.UnitPrice)
                .NotEmpty().WithMessage("UnitPrice is required")
;

RuleFor(x => x.LineTotal)
                .NotEmpty().WithMessage("LineTotal is required")
;

}
    }
}