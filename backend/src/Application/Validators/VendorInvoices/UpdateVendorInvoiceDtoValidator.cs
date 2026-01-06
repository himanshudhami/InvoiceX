using Application.DTOs.VendorInvoices;
using FluentValidation;

namespace Application.Validators.VendorInvoices
{
    /// <summary>
    /// Validator for UpdateVendorInvoiceDto
    /// </summary>
    public class UpdateVendorInvoiceDtoValidator : AbstractValidator<UpdateVendorInvoiceDto>
    {
        public UpdateVendorInvoiceDtoValidator()
        {
            RuleFor(x => x.InvoiceNumber)
                .NotEmpty().WithMessage("Invoice number is required")
                .MaximumLength(100).WithMessage("Invoice number cannot exceed 100 characters");

            RuleFor(x => x.InvoiceDate)
                .NotEmpty().WithMessage("Invoice date is required");

            RuleFor(x => x.DueDate)
                .NotEmpty().WithMessage("Due date is required")
                .Must((dto, dueDate) => dueDate >= dto.InvoiceDate)
                .WithMessage("Due date must be on or after invoice date");

            RuleFor(x => x.TotalAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Total amount cannot be negative");

            RuleFor(x => x.Subtotal)
                .GreaterThanOrEqualTo(0).WithMessage("Subtotal cannot be negative");

            // Invoice type validation
            RuleFor(x => x.InvoiceType)
                .Must(BeValidInvoiceType)
                .WithMessage("Invalid invoice type. Must be one of: purchase_b2b, purchase_import, purchase_rcm, purchase_sez")
                .When(x => !string.IsNullOrEmpty(x.InvoiceType));

            // Supply type validation
            RuleFor(x => x.SupplyType)
                .Must(BeValidSupplyType)
                .WithMessage("Invalid supply type. Must be one of: intra_state, inter_state, import")
                .When(x => !string.IsNullOrEmpty(x.SupplyType));

            // Place of supply validation (Indian state codes)
            RuleFor(x => x.PlaceOfSupply)
                .Matches(@"^[0-9]{2}$")
                .WithMessage("Place of supply must be a 2-digit state code")
                .When(x => !string.IsNullOrEmpty(x.PlaceOfSupply));

            // TDS validation
            RuleFor(x => x.TdsSection)
                .Must(BeValidTdsSection)
                .WithMessage("Invalid TDS section")
                .When(x => x.TdsApplicable);

            RuleFor(x => x.TdsRate)
                .InclusiveBetween(0, 100)
                .WithMessage("TDS rate must be between 0 and 100")
                .When(x => x.TdsRate.HasValue);

            // GST totals validation
            RuleFor(x => x.TotalCgst)
                .GreaterThanOrEqualTo(0).WithMessage("CGST cannot be negative");

            RuleFor(x => x.TotalSgst)
                .GreaterThanOrEqualTo(0).WithMessage("SGST cannot be negative");

            RuleFor(x => x.TotalIgst)
                .GreaterThanOrEqualTo(0).WithMessage("IGST cannot be negative");

            // Status validation
            RuleFor(x => x.Status)
                .Must(BeValidStatus)
                .WithMessage("Invalid status. Must be one of: draft, pending_approval, approved, partially_paid, paid, cancelled")
                .When(x => !string.IsNullOrEmpty(x.Status));
        }

        private bool BeValidInvoiceType(string? type)
        {
            if (string.IsNullOrEmpty(type)) return true;
            var validTypes = new[] { "purchase_b2b", "purchase_import", "purchase_rcm", "purchase_sez" };
            return validTypes.Contains(type.ToLowerInvariant());
        }

        private bool BeValidSupplyType(string? type)
        {
            if (string.IsNullOrEmpty(type)) return true;
            var validTypes = new[] { "intra_state", "inter_state", "import" };
            return validTypes.Contains(type.ToLowerInvariant());
        }

        private bool BeValidTdsSection(string? section)
        {
            if (string.IsNullOrEmpty(section)) return true;
            var validSections = new[] { "194C", "194J", "194H", "194I", "194A", "194Q", "194IA", "194IB", "194M", "194O" };
            return validSections.Contains(section.ToUpperInvariant());
        }

        private bool BeValidStatus(string? status)
        {
            if (string.IsNullOrEmpty(status)) return true;
            var validStatuses = new[] { "draft", "pending_approval", "approved", "partially_paid", "paid", "cancelled" };
            return validStatuses.Contains(status.ToLowerInvariant());
        }
    }
}
