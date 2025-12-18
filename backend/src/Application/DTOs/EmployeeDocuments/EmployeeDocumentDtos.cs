namespace Application.DTOs.EmployeeDocuments;

public class CreateEmployeeDocumentDto
{
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int? FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? FinancialYear { get; set; }
    public bool IsCompanyWide { get; set; }
}

public class UpdateEmployeeDocumentDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int? FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? FinancialYear { get; set; }
    public bool IsCompanyWide { get; set; }
}

public class EmployeeDocumentSummaryDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int? FileSize { get; set; }
    public string? FinancialYear { get; set; }
    public bool IsCompanyWide { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmployeeDocumentDetailDto : EmployeeDocumentSummaryDto
{
    public string? Description { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string? EmployeeName { get; set; }
}

// Document Request DTOs
public class CreateDocumentRequestDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string? Purpose { get; set; }
}

public class UpdateDocumentRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public Guid? DocumentId { get; set; }
}

public class DocumentRequestSummaryDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
