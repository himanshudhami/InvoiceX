using Application.DTOs.Payments;
using FluentValidation;

namespace Application.Validators.Payments
{
    /// <summary>
    /// Validator for CreatePaymentsDto
    /// </summary>
    public class CreatePaymentsDtoValidator : AbstractValidator<CreatePaymentsDto>
    {
        public CreatePaymentsDtoValidator()
        {
RuleFor(x => x.Amount)
                .NotEmpty().WithMessage("Amount is required")
                .GreaterThan(0).WithMessage("Amount must be greater than 0");

RuleFor(x => x.PaymentMethod)
                .MaximumLength(255).WithMessage("PaymentMethod cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.PaymentMethod));

RuleFor(x => x.ReferenceNumber)
                .MaximumLength(255).WithMessage("ReferenceNumber cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.ReferenceNumber));

RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

}
    }

    /// <summary>
    /// Validator for UpdatePaymentsDto
    /// </summary>
    public class UpdatePaymentsDtoValidator : AbstractValidator<UpdatePaymentsDto>
    {
        public UpdatePaymentsDtoValidator()
        {

RuleFor(x => x.Amount)
                .NotEmpty().WithMessage("Amount is required")
                .GreaterThan(0).WithMessage("Amount must be greater than 0");

RuleFor(x => x.PaymentMethod)
                .MaximumLength(255).WithMessage("PaymentMethod cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.PaymentMethod));

RuleFor(x => x.ReferenceNumber)
                .MaximumLength(255).WithMessage("ReferenceNumber cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.ReferenceNumber));

RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

}
    }
}