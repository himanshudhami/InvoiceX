using Application.DTOs.EmployeeDocuments;
using FluentValidation;

namespace Application.Validators.EmployeeDocuments;

public class CreateEmployeeDocumentDtoValidator : AbstractValidator<CreateEmployeeDocumentDto>
{
    private static readonly string[] ValidDocumentTypes = {
        "offer_letter", "appointment_letter", "form16", "form12bb",
        "salary_certificate", "experience_certificate", "relieving_letter",
        "policy", "handbook", "nda", "agreement", "other"
    };

    public CreateEmployeeDocumentDtoValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().When(x => !x.IsCompanyWide);
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.DocumentType).Must(t => ValidDocumentTypes.Contains(t))
            .WithMessage("Document type must be one of: " + string.Join(", ", ValidDocumentTypes));
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileUrl).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
    }
}

public class UpdateEmployeeDocumentDtoValidator : AbstractValidator<UpdateEmployeeDocumentDto>
{
    private static readonly string[] ValidDocumentTypes = {
        "offer_letter", "appointment_letter", "form16", "form12bb",
        "salary_certificate", "experience_certificate", "relieving_letter",
        "policy", "handbook", "nda", "agreement", "other"
    };

    public UpdateEmployeeDocumentDtoValidator()
    {
        RuleFor(x => x.DocumentType).Must(t => ValidDocumentTypes.Contains(t))
            .WithMessage("Document type must be one of: " + string.Join(", ", ValidDocumentTypes));
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileUrl).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
    }
}

public class CreateDocumentRequestDtoValidator : AbstractValidator<CreateDocumentRequestDto>
{
    private static readonly string[] ValidDocumentTypes = {
        "offer_letter", "appointment_letter", "form16", "form12bb",
        "salary_certificate", "experience_certificate", "relieving_letter",
        "policy", "handbook", "nda", "agreement", "other"
    };

    public CreateDocumentRequestDtoValidator()
    {
        RuleFor(x => x.DocumentType).Must(t => ValidDocumentTypes.Contains(t))
            .WithMessage("Document type must be one of: " + string.Join(", ", ValidDocumentTypes));
    }
}

public class UpdateDocumentRequestDtoValidator : AbstractValidator<UpdateDocumentRequestDto>
{
    private static readonly string[] ValidStatuses = { "pending", "processing", "completed", "rejected" };

    public UpdateDocumentRequestDtoValidator()
    {
        RuleFor(x => x.Status).Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be one of: " + string.Join(", ", ValidStatuses));
        RuleFor(x => x.RejectionReason).NotEmpty()
            .When(x => x.Status == "rejected")
            .WithMessage("Rejection reason is required when rejecting a request");
    }
}
