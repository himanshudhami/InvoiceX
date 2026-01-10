using Application.DTOs.CreditNotes;
using FluentValidation;

namespace Application.Validators
{
    public class UpdateCreditNotesDtoValidator : AbstractValidator<UpdateCreditNotesDto>
    {
        private static readonly string[] ValidReasons = new[]
        {
            "goods_returned",
            "post_sale_discount",
            "deficiency_in_services",
            "excess_amount_charged",
            "excess_tax_charged",
            "change_in_pos",
            "export_refund",
            "other"
        };

        public UpdateCreditNotesDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Credit note ID is required");

            RuleFor(x => x.CreditNoteNumber)
                .NotEmpty().WithMessage("Credit note number is required")
                .MaximumLength(50).WithMessage("Credit note number cannot exceed 50 characters");

            RuleFor(x => x.CreditNoteDate)
                .NotEmpty().WithMessage("Credit note date is required");

            RuleFor(x => x.OriginalInvoiceId)
                .NotEmpty().WithMessage("Original invoice ID is required");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .Must(r => ValidReasons.Contains(r))
                .WithMessage($"Reason must be one of: {string.Join(", ", ValidReasons)}");

            RuleFor(x => x.ReasonDescription)
                .NotEmpty()
                .When(x => x.Reason == "other")
                .WithMessage("Reason description is required when reason is 'other'");

            RuleFor(x => x.Subtotal)
                .GreaterThanOrEqualTo(0).WithMessage("Subtotal cannot be negative");

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0).WithMessage("Total amount must be greater than 0");

            RuleFor(x => x.Status)
                .Must(s => s == null || new[] { "draft", "issued", "cancelled" }.Contains(s))
                .WithMessage("Status must be draft, issued, or cancelled");

            RuleFor(x => x.EInvoiceStatus)
                .Must(s => s == null || new[] { "not_applicable", "pending", "generated", "cancelled", "error" }.Contains(s))
                .WithMessage("E-invoice status must be not_applicable, pending, generated, cancelled, or error");
        }
    }
}
