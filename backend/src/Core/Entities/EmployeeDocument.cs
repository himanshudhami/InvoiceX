namespace Core.Entities;

public class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public string DocumentType { get; set; } = string.Empty; // offer_letter, form16, policy, certificate, etc.
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int? FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? FinancialYear { get; set; } // For Form 16, tax docs
    public bool IsCompanyWide { get; set; } // Policies visible to all
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public string? EmployeeName { get; set; }
}

public class DocumentRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string Status { get; set; } = "pending"; // pending, processing, completed, rejected
    public Guid? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? DocumentId { get; set; } // Created document
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public string? EmployeeName { get; set; }
}
