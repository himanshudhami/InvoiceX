using System.Text.Json;

namespace Core.Entities.EInvoice
{
    /// <summary>
    /// Immutable audit log for all e-invoice API interactions.
    /// Used for compliance, debugging, and reconciliation.
    /// </summary>
    public class EInvoiceAuditLog
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid? InvoiceId { get; set; }

        /// <summary>
        /// Action type: generate_irn, cancel_irn, get_irn_by_docno, get_ewaybill, auth
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        public DateTime RequestTimestamp { get; set; }

        // Request/Response (stored as JSON for flexibility)
        public JsonDocument? RequestPayload { get; set; }
        public string? RequestHash { get; set; } // SHA256 for integrity

        /// <summary>
        /// Response status: success, error, timeout
        /// </summary>
        public string? ResponseStatus { get; set; }
        public JsonDocument? ResponsePayload { get; set; }
        public int? ResponseTimeMs { get; set; }

        // IRN Details (denormalized for quick lookup)
        public string? Irn { get; set; }
        public string? AckNumber { get; set; }
        public DateTime? AckDate { get; set; }

        // Error Tracking
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        // Metadata
        public string? GspProvider { get; set; }
        public string? Environment { get; set; }
        public string? ApiVersion { get; set; }
        public Guid? UserId { get; set; }
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// E-invoice action types for audit logging
    /// </summary>
    public static class EInvoiceActionTypes
    {
        public const string GenerateIrn = "generate_irn";
        public const string CancelIrn = "cancel_irn";
        public const string GetIrnByDocNo = "get_irn_by_docno";
        public const string GetEwayBill = "get_ewaybill";
        public const string GenerateEwayBill = "generate_ewaybill";
        public const string Auth = "auth";
        public const string RefreshToken = "refresh_token";
        public const string GetGstinDetails = "get_gstin_details";
    }

    /// <summary>
    /// Response status values
    /// </summary>
    public static class EInvoiceResponseStatus
    {
        public const string Success = "success";
        public const string Error = "error";
        public const string Timeout = "timeout";
        public const string Pending = "pending";
    }
}
