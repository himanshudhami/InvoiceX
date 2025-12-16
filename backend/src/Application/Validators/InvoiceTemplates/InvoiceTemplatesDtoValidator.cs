using Application.DTOs.InvoiceTemplates;
using FluentValidation;

namespace Application.Validators.InvoiceTemplates
{
    /// <summary>
    /// Validator for CreateInvoiceTemplatesDto
    /// </summary>
    public class CreateInvoiceTemplatesDtoValidator : AbstractValidator<CreateInvoiceTemplatesDto>
    {
        public CreateInvoiceTemplatesDtoValidator()
        {
RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.TemplateData)
                .NotEmpty().WithMessage("TemplateData is required")
                .MaximumLength(255).WithMessage("TemplateData cannot exceed 255 characters");

}
    }

    /// <summary>
    /// Validator for UpdateInvoiceTemplatesDto
    /// </summary>
    public class UpdateInvoiceTemplatesDtoValidator : AbstractValidator<UpdateInvoiceTemplatesDto>
    {
        public UpdateInvoiceTemplatesDtoValidator()
        {

RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");

RuleFor(x => x.TemplateData)
                .NotEmpty().WithMessage("TemplateData is required")
                .MaximumLength(255).WithMessage("TemplateData cannot exceed 255 characters");

}
    }
}