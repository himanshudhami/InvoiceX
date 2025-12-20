namespace Core.Entities.FileStorage
{
    /// <summary>
    /// Represents an audit log entry for document-related actions.
    /// Used for compliance, security monitoring, and troubleshooting.
    /// </summary>
    public class DocumentAuditLog
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Company context for the action
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Reference to employee_documents if applicable
        /// </summary>
        public Guid? DocumentId { get; set; }

        /// <summary>
        /// Reference to file_storage record
        /// </summary>
        public Guid? FileStorageId { get; set; }

        /// <summary>
        /// Type of action performed: upload, download, view, delete, update, share, restore
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// User who performed the action
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// IP address of the actor (IPv4 or IPv6)
        /// </summary>
        public string? ActorIp { get; set; }

        /// <summary>
        /// Browser/client user agent string
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Additional context as JSON (filename, file size, target employee, etc.)
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Timestamp of the action
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // Navigation properties (populated by service layer)
        public string? ActorName { get; set; }
        public string? FileName { get; set; }
    }

    /// <summary>
    /// Constants for audit log action types
    /// </summary>
    public static class AuditActions
    {
        public const string Upload = "upload";
        public const string Download = "download";
        public const string View = "view";
        public const string Delete = "delete";
        public const string Update = "update";
        public const string Share = "share";
        public const string Restore = "restore";

        public static readonly string[] All = new[]
        {
            Upload, Download, View, Delete, Update, Share, Restore
        };

        public static bool IsValid(string action)
        {
            return All.Contains(action.ToLowerInvariant());
        }
    }
}
