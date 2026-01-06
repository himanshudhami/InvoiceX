using Application.DTOs.Vendors;
using FluentValidation;

namespace Application.Validators.Vendors
{
    /// <summary>
    /// Validator for CreateVendorsDto
    /// </summary>
    public class CreateVendorsDtoValidator : AbstractValidator<CreateVendorsDto>
    {
        public CreateVendorsDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

            RuleFor(x => x.CompanyName)
                .MaximumLength(255).WithMessage("Company name cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.CompanyName));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            // GSTIN validation (15 character Indian GST Number)
            RuleFor(x => x.Gstin)
                .Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
                .WithMessage("Invalid GSTIN format. Must be 15 characters in format: 22AAAAA0000A1Z5")
                .When(x => !string.IsNullOrEmpty(x.Gstin));

            // PAN validation (10 character)
            RuleFor(x => x.PanNumber)
                .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$")
                .WithMessage("Invalid PAN format. Must be 10 characters in format: AAAAA0000A")
                .When(x => !string.IsNullOrEmpty(x.PanNumber));

            // TAN validation (10 character)
            RuleFor(x => x.TanNumber)
                .Matches(@"^[A-Z]{4}[0-9]{5}[A-Z]{1}$")
                .WithMessage("Invalid TAN format. Must be 10 characters in format: AAAA00000A")
                .When(x => !string.IsNullOrEmpty(x.TanNumber));

            // TDS Section validation
            RuleFor(x => x.DefaultTdsSection)
                .Must(BeValidTdsSection)
                .WithMessage("Invalid TDS Section. Must be one of: 194C, 194J, 194H, 194I, 194A, 194Q")
                .When(x => !string.IsNullOrEmpty(x.DefaultTdsSection));

            // TDS Rate validation
            RuleFor(x => x.DefaultTdsRate)
                .InclusiveBetween(0, 100)
                .WithMessage("TDS Rate must be between 0 and 100")
                .When(x => x.DefaultTdsRate.HasValue);

            RuleFor(x => x.LowerTdsRate)
                .InclusiveBetween(0, 100)
                .WithMessage("Lower TDS Rate must be between 0 and 100")
                .When(x => x.LowerTdsRate.HasValue);

            // MSME Category validation
            RuleFor(x => x.MsmeCategory)
                .Must(BeValidMsmeCategory)
                .WithMessage("Invalid MSME Category. Must be one of: micro, small, medium")
                .When(x => !string.IsNullOrEmpty(x.MsmeCategory));

            // Vendor Type validation
            RuleFor(x => x.VendorType)
                .Must(BeValidVendorType)
                .WithMessage("Invalid Vendor Type. Must be one of: b2b, b2c, import, rcm_applicable")
                .When(x => !string.IsNullOrEmpty(x.VendorType));

            // IFSC Code validation (11 character)
            RuleFor(x => x.BankIfscCode)
                .Matches(@"^[A-Z]{4}0[A-Z0-9]{6}$")
                .WithMessage("Invalid IFSC Code format. Must be 11 characters")
                .When(x => !string.IsNullOrEmpty(x.BankIfscCode));

            // GST State Code validation
            RuleFor(x => x.GstStateCode)
                .Matches(@"^[0-9]{2}$")
                .WithMessage("GST State Code must be 2 digits")
                .When(x => !string.IsNullOrEmpty(x.GstStateCode));

            // Credit Limit validation
            RuleFor(x => x.CreditLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Credit limit must be non-negative")
                .When(x => x.CreditLimit.HasValue);

            // Payment Terms validation
            RuleFor(x => x.PaymentTerms)
                .InclusiveBetween(0, 365)
                .WithMessage("Payment terms must be between 0 and 365 days")
                .When(x => x.PaymentTerms.HasValue);

            // Business rule: TDS Section requires PAN
            RuleFor(x => x.PanNumber)
                .NotEmpty()
                .WithMessage("PAN is required when TDS is applicable")
                .When(x => x.TdsApplicable);
        }

        private bool BeValidTdsSection(string? section)
        {
            if (string.IsNullOrEmpty(section)) return true;
            var validSections = new[] { "194C", "194J", "194H", "194I", "194A", "194Q", "194IA", "194IB", "194M", "194O" };
            return validSections.Contains(section.ToUpperInvariant());
        }

        private bool BeValidMsmeCategory(string? category)
        {
            if (string.IsNullOrEmpty(category)) return true;
            var validCategories = new[] { "micro", "small", "medium" };
            return validCategories.Contains(category.ToLowerInvariant());
        }

        private bool BeValidVendorType(string? vendorType)
        {
            if (string.IsNullOrEmpty(vendorType)) return true;
            var validTypes = new[] { "b2b", "b2c", "import", "rcm_applicable" };
            return validTypes.Contains(vendorType.ToLowerInvariant());
        }
    }
}
