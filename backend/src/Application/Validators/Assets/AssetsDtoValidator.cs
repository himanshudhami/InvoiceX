using Application.DTOs.Assets;
using FluentValidation;

namespace Application.Validators.Assets;

public class CreateAssetDtoValidator : AbstractValidator<CreateAssetDto>
{
    public CreateAssetDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required");
        RuleFor(x => x.AssetType).NotEmpty().Must(BeValidType).WithMessage("AssetType must be IT_Asset, Fixed_Asset, or Intangible_Asset");
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.AssetTag).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DepreciationMethod).Must(BeValidDepMethod).When(x => !string.IsNullOrWhiteSpace(x.DepreciationMethod));
        RuleFor(x => x.PurchaseType).Must(BeValidPurchaseType).When(x => !string.IsNullOrWhiteSpace(x.PurchaseType));
    }

    private bool BeValidType(string type) =>
        new[] { "IT_Asset", "Fixed_Asset", "Intangible_Asset" }.Contains(type);

    private bool BeValidDepMethod(string? method) =>
        string.IsNullOrWhiteSpace(method) || new[] { "none", "straight_line", "double_declining", "sum_of_years_digits" }.Contains(method);

    private bool BeValidPurchaseType(string type) =>
        new[] { "capex", "opex" }.Contains(type);
}

public class UpdateAssetDtoValidator : AbstractValidator<UpdateAssetDto>
{
    public UpdateAssetDtoValidator()
    {
        RuleFor(x => x.AssetTag).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AssetType).Must(BeValidType).When(x => !string.IsNullOrWhiteSpace(x.AssetType));
        RuleFor(x => x.Status).Must(BeValidStatus).When(x => !string.IsNullOrWhiteSpace(x.Status));
        RuleFor(x => x.DepreciationMethod).Must(BeValidDepMethod).When(x => !string.IsNullOrWhiteSpace(x.DepreciationMethod));
        RuleFor(x => x.PurchaseType).Must(BeValidPurchaseType).When(x => !string.IsNullOrWhiteSpace(x.PurchaseType));
    }

    private bool BeValidType(string type) =>
        new[] { "IT_Asset", "Fixed_Asset", "Intangible_Asset" }.Contains(type);

    private bool BeValidStatus(string status) =>
        new[] { "available", "assigned", "maintenance", "retired", "reserved", "lost" }.Contains(status);

    private bool BeValidDepMethod(string? method) =>
        string.IsNullOrWhiteSpace(method) || new[] { "none", "straight_line", "double_declining", "sum_of_years_digits" }.Contains(method);

    private bool BeValidPurchaseType(string type) =>
        new[] { "capex", "opex" }.Contains(type);
}

public class CreateAssetAssignmentDtoValidator : AbstractValidator<CreateAssetAssignmentDto>
{
    public CreateAssetAssignmentDtoValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TargetType).Must(t => t == "employee" || t == "company");
        When(x => x.TargetType == "employee", () =>
        {
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId required when assigning to employee");
        });
    }
}

public class ReturnAssetAssignmentDtoValidator : AbstractValidator<ReturnAssetAssignmentDto>
{
    public ReturnAssetAssignmentDtoValidator()
    {
        // nothing required; both optional
    }
}

public class CreateAssetMaintenanceDtoValidator : AbstractValidator<CreateAssetMaintenanceDto>
{
    public CreateAssetMaintenanceDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(BeValidMaintenanceStatus).WithMessage("Invalid maintenance status");
    }

    private bool BeValidMaintenanceStatus(string status) =>
        new[] { "open", "in_progress", "resolved", "closed" }.Contains(status);
}

public class UpdateAssetMaintenanceDtoValidator : AbstractValidator<UpdateAssetMaintenanceDto>
{
    public UpdateAssetMaintenanceDtoValidator()
    {
        RuleFor(x => x.Status).Must(BeValidMaintenanceStatus).When(x => !string.IsNullOrWhiteSpace(x.Status));
    }

    private bool BeValidMaintenanceStatus(string status) =>
        new[] { "open", "in_progress", "resolved", "closed" }.Contains(status);
}

public class CreateAssetDocumentDtoValidator : AbstractValidator<CreateAssetDocumentDto>
{
    public CreateAssetDocumentDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Url).NotEmpty();
    }
}

public class CreateAssetDisposalDtoValidator : AbstractValidator<CreateAssetDisposalDto>
{
    public CreateAssetDisposalDtoValidator()
    {
        RuleFor(x => x.Method).Must(BeValidMethod).WithMessage("Method must be sold, retired, recycled, donated, or lost");
    }

    private bool BeValidMethod(string method) =>
        new[] { "sold", "retired", "recycled", "donated", "lost" }.Contains(method);
}




