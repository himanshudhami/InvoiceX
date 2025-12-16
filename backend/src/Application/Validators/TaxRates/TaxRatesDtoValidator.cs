using Application.DTOs.TaxRates;
using FluentValidation;

namespace Application.Validators.TaxRates
{
    /// <summary>
    /// Validator for CreateTaxRatesDto
    /// </summary>
    public class CreateTaxRatesDtoValidator : AbstractValidator<CreateTaxRatesDto>
    {
        public CreateTaxRatesDtoValidator()
        {
RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.Rate)
                .NotEmpty().WithMessage("Rate is required")
                .GreaterThan(0).WithMessage("Rate must be greater than 0");

}
    }

    /// <summary>
    /// Validator for UpdateTaxRatesDto
    /// </summary>
    public class UpdateTaxRatesDtoValidator : AbstractValidator<UpdateTaxRatesDto>
    {
        public UpdateTaxRatesDtoValidator()
        {

RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.Rate)
                .NotEmpty().WithMessage("Rate is required")
                .GreaterThan(0).WithMessage("Rate must be greater than 0");

}
    }
}