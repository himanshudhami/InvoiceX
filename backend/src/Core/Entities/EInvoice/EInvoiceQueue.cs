using System.Text.Json;

namespace Core.Entities.EInvoice
{
    /// <summary>
    /// Queue for async e-invoice processing with retry support.
    /// Enables background processing and graceful error handling.
    /// </summary>
    public class EInvoiceQueue
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid InvoiceId { get; set; }

        /// <summary>
        /// Action type: generate_irn, cancel_irn, retry_failed
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Priority: 1 = highest, 10 = lowest
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Status: pending, processing, completed, failed, cancelled
        /// </summary>
        public string Status { get; set; } = EInvoiceQueueStatus.Pending;

        // Retry Logic
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public DateTime? NextRetryAt { get; set; }

        // Processing Info
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ProcessorId { get; set; } // Worker ID for distributed processing

        // Error Tracking
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        // Payload
        public JsonDocument? RequestPayload { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Queue status values
    /// </summary>
    public static class EInvoiceQueueStatus
    {
        public const string Pending = "pending";
        public const string Processing = "processing";
        public const string Completed = "completed";
        public const string Failed = "failed";
        public const string Cancelled = "cancelled";
    }

    /// <summary>
    /// Queue action types
    /// </summary>
    public static class EInvoiceQueueActions
    {
        public const string GenerateIrn = "generate_irn";
        public const string CancelIrn = "cancel_irn";
        public const string RetryFailed = "retry_failed";
        public const string GenerateEwayBill = "generate_ewaybill";
    }
}
