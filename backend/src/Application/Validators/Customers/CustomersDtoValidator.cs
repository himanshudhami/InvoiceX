using Application.DTOs.Customers;
using FluentValidation;

namespace Application.Validators.Customers
{
    /// <summary>
    /// Validator for CreateCustomersDto
    /// </summary>
    public class CreateCustomersDtoValidator : AbstractValidator<CreateCustomersDto>
    {
        public CreateCustomersDtoValidator()
        {
RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.CompanyName)
                .MaximumLength(255).WithMessage("CompanyName cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.CompanyName));

RuleFor(x => x.Email)
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

RuleFor(x => x.Phone)
                .MaximumLength(255).WithMessage("Phone cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

RuleFor(x => x.AddressLine1)
                .MaximumLength(255).WithMessage("AddressLine1 cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.AddressLine1));

RuleFor(x => x.AddressLine2)
                .MaximumLength(255).WithMessage("AddressLine2 cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.AddressLine2));

RuleFor(x => x.City)
                .MaximumLength(255).WithMessage("City cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.City));

RuleFor(x => x.State)
                .MaximumLength(255).WithMessage("State cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.State));

RuleFor(x => x.ZipCode)
                .MaximumLength(255).WithMessage("ZipCode cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.ZipCode));

RuleFor(x => x.Country)
                .MaximumLength(255).WithMessage("Country cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Country));

RuleFor(x => x.TaxNumber)
                .MaximumLength(255).WithMessage("TaxNumber cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.TaxNumber));

RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

}
    }

    /// <summary>
    /// Validator for UpdateCustomersDto
    /// </summary>
    public class UpdateCustomersDtoValidator : AbstractValidator<UpdateCustomersDto>
    {
        public UpdateCustomersDtoValidator()
        {

RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.CompanyName)
                .MaximumLength(255).WithMessage("CompanyName cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.CompanyName));

RuleFor(x => x.Email)
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

RuleFor(x => x.Phone)
                .MaximumLength(255).WithMessage("Phone cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

RuleFor(x => x.AddressLine1)
                .MaximumLength(255).WithMessage("AddressLine1 cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.AddressLine1));

RuleFor(x => x.AddressLine2)
                .MaximumLength(255).WithMessage("AddressLine2 cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.AddressLine2));

RuleFor(x => x.City)
                .MaximumLength(255).WithMessage("City cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.City));

RuleFor(x => x.State)
                .MaximumLength(255).WithMessage("State cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.State));

RuleFor(x => x.ZipCode)
                .MaximumLength(255).WithMessage("ZipCode cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.ZipCode));

RuleFor(x => x.Country)
                .MaximumLength(255).WithMessage("Country cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Country));

RuleFor(x => x.TaxNumber)
                .MaximumLength(255).WithMessage("TaxNumber cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.TaxNumber));

RuleFor(x => x.Notes)
                .MaximumLength(255).WithMessage("Notes cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));

}
    }
}