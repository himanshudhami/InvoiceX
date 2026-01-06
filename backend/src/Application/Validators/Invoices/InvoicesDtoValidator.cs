using Application.DTOs.Invoices;
using FluentValidation;
using System;

namespace Application.Validators.Invoices
{
    /// <summary>
    /// Validator for CreateInvoicesDto
    /// </summary>
    public class CreateInvoicesDtoValidator : AbstractValidator<CreateInvoicesDto>
    {
        public CreateInvoicesDtoValidator()
        {
RuleFor(x => x.InvoiceNumber)
                .NotEmpty().WithMessage("InvoiceNumber is required")
                .MaximumLength(255).WithMessage("InvoiceNumber cannot exceed 255 characters");

RuleFor(x => x.Status)
                .MaximumLength(255).WithMessage("Status cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Status));

RuleFor(x => x.Subtotal)
                .NotEmpty().WithMessage("Subtotal is required")
                .GreaterThan(0).WithMessage("Subtotal must be greater than 0");

RuleFor(x => x.TotalAmount)
                .NotEmpty().WithMessage("TotalAmount is required")
                .GreaterThan(0).WithMessage("TotalAmount must be greater than 0");

RuleFor(x => x.Currency)
                .MaximumLength(255).WithMessage("Currency cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Currency));

RuleFor(x => x.ExchangeRate)
                .GreaterThan(0).WithMessage("ExchangeRate must be greater than 0")
                .When(x => !string.IsNullOrEmpty(x.Currency) &&
                           !x.Currency.Equals("INR", StringComparison.OrdinalIgnoreCase));

RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

RuleFor(x => x.Terms)
                .MaximumLength(255).WithMessage("Terms cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Terms));

RuleFor(x => x.PoNumber)
                .MaximumLength(255).WithMessage("PoNumber cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.PoNumber));

RuleFor(x => x.ProjectName)
                .MaximumLength(255).WithMessage("ProjectName cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.ProjectName));

}
    }

    /// <summary>
    /// Validator for UpdateInvoicesDto
    /// </summary>
    public class UpdateInvoicesDtoValidator : AbstractValidator<UpdateInvoicesDto>
    {
        public UpdateInvoicesDtoValidator()
        {

RuleFor(x => x.InvoiceNumber)
                .NotEmpty().WithMessage("InvoiceNumber is required")
                .MaximumLength(255).WithMessage("InvoiceNumber cannot exceed 255 characters");

RuleFor(x => x.Status)
                .MaximumLength(255).WithMessage("Status cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Status));

RuleFor(x => x.Subtotal)
                .NotEmpty().WithMessage("Subtotal is required")
                .GreaterThan(0).WithMessage("Subtotal must be greater than 0");

RuleFor(x => x.TotalAmount)
                .NotEmpty().WithMessage("TotalAmount is required")
                .GreaterThan(0).WithMessage("TotalAmount must be greater than 0");

RuleFor(x => x.Currency)
                .MaximumLength(255).WithMessage("Currency cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Currency));

RuleFor(x => x.ExchangeRate)
                .GreaterThan(0).WithMessage("ExchangeRate must be greater than 0")
                .When(x => !string.IsNullOrEmpty(x.Currency) &&
                           !x.Currency.Equals("INR", StringComparison.OrdinalIgnoreCase));

RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

RuleFor(x => x.Terms)
                .MaximumLength(255).WithMessage("Terms cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Terms));

RuleFor(x => x.PoNumber)
                .MaximumLength(255).WithMessage("PoNumber cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.PoNumber));

RuleFor(x => x.ProjectName)
                .MaximumLength(255).WithMessage("ProjectName cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.ProjectName));

}
    }
}
