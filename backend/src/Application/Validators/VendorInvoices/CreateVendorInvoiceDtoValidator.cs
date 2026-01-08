using Application.DTOs.VendorInvoices;
using FluentValidation;

namespace Application.Validators.VendorInvoices
{
    /// <summary>
    /// Validator for CreateVendorInvoiceDto
    /// </summary>
    public class CreateVendorInvoiceDtoValidator : AbstractValidator<CreateVendorInvoiceDto>
    {
        public CreateVendorInvoiceDtoValidator()
        {
            RuleFor(x => x.PartyId)
                .NotEmpty().WithMessage("Vendor ID is required");

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

            // Items validation
            RuleForEach(x => x.Items).SetValidator(new CreateVendorInvoiceItemDtoValidator())
                .When(x => x.Items != null && x.Items.Any());
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

    /// <summary>
    /// Validator for CreateVendorInvoiceItemDto
    /// </summary>
    public class CreateVendorInvoiceItemDtoValidator : AbstractValidator<CreateVendorInvoiceItemDto>
    {
        public CreateVendorInvoiceItemDtoValidator()
        {
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");

            RuleFor(x => x.LineTotal)
                .GreaterThanOrEqualTo(0).WithMessage("Line total cannot be negative");

            RuleFor(x => x.ItcCategory)
                .Must(BeValidItcCategory)
                .WithMessage("Invalid ITC category. Must be one of: capital_goods, inputs, input_services")
                .When(x => !string.IsNullOrEmpty(x.ItcCategory));
        }

        private bool BeValidItcCategory(string? category)
        {
            if (string.IsNullOrEmpty(category)) return true;
            var validCategories = new[] { "capital_goods", "inputs", "input_services" };
            return validCategories.Contains(category.ToLowerInvariant());
        }
    }
}
