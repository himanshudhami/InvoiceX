using Application.DTOs.VendorPayments;
using FluentValidation;

namespace Application.Validators.VendorPayments
{
    /// <summary>
    /// Validator for UpdateVendorPaymentDto
    /// </summary>
    public class UpdateVendorPaymentDtoValidator : AbstractValidator<UpdateVendorPaymentDto>
    {
        public UpdateVendorPaymentDtoValidator()
        {
            RuleFor(x => x.PaymentDate)
                .NotEmpty().WithMessage("Payment date is required");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0");

            RuleFor(x => x.GrossAmount)
                .GreaterThan(0).WithMessage("Gross amount must be greater than 0")
                .When(x => x.GrossAmount.HasValue);

            // Payment method validation
            RuleFor(x => x.PaymentMethod)
                .Must(BeValidPaymentMethod)
                .WithMessage("Invalid payment method. Must be one of: bank_transfer, cheque, cash, neft, rtgs, upi, demand_draft")
                .When(x => !string.IsNullOrEmpty(x.PaymentMethod));

            // Payment type validation
            RuleFor(x => x.PaymentType)
                .Must(BeValidPaymentType)
                .WithMessage("Invalid payment type. Must be one of: bill_payment, advance_paid, expense_reimbursement, refund_paid")
                .When(x => !string.IsNullOrEmpty(x.PaymentType));

            // Status validation
            RuleFor(x => x.Status)
                .Must(BeValidStatus)
                .WithMessage("Invalid status. Must be one of: draft, pending_approval, approved, processed, cancelled")
                .When(x => !string.IsNullOrEmpty(x.Status));

            // TDS validation
            RuleFor(x => x.TdsSection)
                .Must(BeValidTdsSection)
                .WithMessage("Invalid TDS section")
                .When(x => x.TdsApplicable);

            RuleFor(x => x.TdsRate)
                .InclusiveBetween(0, 100)
                .WithMessage("TDS rate must be between 0 and 100")
                .When(x => x.TdsRate.HasValue);

            RuleFor(x => x.TdsAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("TDS amount cannot be negative")
                .When(x => x.TdsAmount.HasValue);

            // Cheque validation
            RuleFor(x => x.ChequeNumber)
                .NotEmpty()
                .WithMessage("Cheque number is required for cheque payments")
                .When(x => x.PaymentMethod?.ToLowerInvariant() == "cheque");
        }

        private bool BeValidPaymentMethod(string? method)
        {
            if (string.IsNullOrEmpty(method)) return true;
            var validMethods = new[] { "bank_transfer", "cheque", "cash", "neft", "rtgs", "upi", "demand_draft", "online" };
            return validMethods.Contains(method.ToLowerInvariant());
        }

        private bool BeValidPaymentType(string? type)
        {
            if (string.IsNullOrEmpty(type)) return true;
            var validTypes = new[] { "bill_payment", "advance_paid", "expense_reimbursement", "refund_paid" };
            return validTypes.Contains(type.ToLowerInvariant());
        }

        private bool BeValidStatus(string? status)
        {
            if (string.IsNullOrEmpty(status)) return true;
            var validStatuses = new[] { "draft", "pending_approval", "approved", "processed", "cancelled", "failed" };
            return validStatuses.Contains(status.ToLowerInvariant());
        }

        private bool BeValidTdsSection(string? section)
        {
            if (string.IsNullOrEmpty(section)) return true;
            var validSections = new[] { "194C", "194J", "194H", "194I", "194A", "194Q", "194IA", "194IB", "194M", "194O" };
            return validSections.Contains(section.ToUpperInvariant());
        }
    }
}
