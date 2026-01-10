namespace Core.Entities.Gst
{
    /// <summary>
    /// GSTR-2B Import entity - tracks each upload/import of GSTR-2B data
    /// </summary>
    public class Gstr2bImport
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;  // Format: 'Jan-2025'
        public string Gstin { get; set; } = string.Empty;

        // Import source
        public string ImportSource { get; set; } = Gstr2bImportSource.FileUpload;
        public string? FileName { get; set; }
        public string? FileHash { get; set; }

        // Import status
        public string ImportStatus { get; set; } = Gstr2bImportStatus.Pending;
        public string? ErrorMessage { get; set; }

        // Summary counts
        public int TotalInvoices { get; set; }
        public int MatchedInvoices { get; set; }
        public int UnmatchedInvoices { get; set; }
        public int PartiallyMatchedInvoices { get; set; }

        // ITC Summary
        public decimal TotalItcIgst { get; set; }
        public decimal TotalItcCgst { get; set; }
        public decimal TotalItcSgst { get; set; }
        public decimal TotalItcCess { get; set; }
        public decimal MatchedItcAmount { get; set; }

        // Raw data
        public string? RawJson { get; set; }

        // Audit
        public Guid? ImportedBy { get; set; }
        public DateTime? ImportedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Computed properties
        public decimal TotalItcAmount => TotalItcIgst + TotalItcCgst + TotalItcSgst + TotalItcCess;

        // Navigation
        public ICollection<Gstr2bInvoice>? Invoices { get; set; }
    }

    /// <summary>
    /// Import source types
    /// </summary>
    public static class Gstr2bImportSource
    {
        public const string FileUpload = "file_upload";
        public const string Api = "api";
        public const string Manual = "manual";
    }

    /// <summary>
    /// Import status types
    /// </summary>
    public static class Gstr2bImportStatus
    {
        public const string Pending = "pending";
        public const string Processing = "processing";
        public const string Completed = "completed";
        public const string Failed = "failed";
    }
}
