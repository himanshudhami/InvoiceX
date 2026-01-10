namespace Core.Entities.Audit
{
    /// <summary>
    /// MCA-compliant audit trail entry for entity CRUD operations.
    /// Captures before/after values for compliance and troubleshooting.
    /// </summary>
    public class AuditTrail
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Company context for multi-tenant isolation
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Type of entity: invoice, payment, vendor, journal_entry, etc.
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Primary key of the audited entity
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Human-readable display name (e.g., "INV-2024-0001", "John Doe")
        /// </summary>
        public string? EntityDisplayName { get; set; }

        /// <summary>
        /// Operation type: create, update, delete
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// JSON snapshot of entity state before change (NULL for create)
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// JSON snapshot of entity state after change (NULL for delete)
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// Array of field names that were modified (for updates)
        /// </summary>
        public string[]? ChangedFields { get; set; }

        /// <summary>
        /// User who performed the action
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// Denormalized actor name for reports
        /// </summary>
        public string? ActorName { get; set; }

        /// <summary>
        /// Denormalized actor email for reports
        /// </summary>
        public string? ActorEmail { get; set; }

        /// <summary>
        /// IP address of the actor (IPv4 or IPv6)
        /// </summary>
        public string? ActorIp { get; set; }

        /// <summary>
        /// Browser/client user agent string
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Request correlation ID for distributed tracing
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// API endpoint called
        /// </summary>
        public string? RequestPath { get; set; }

        /// <summary>
        /// HTTP method: GET, POST, PUT, DELETE
        /// </summary>
        public string? RequestMethod { get; set; }

        /// <summary>
        /// Timestamp of the action (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// SHA256 hash for tamper detection (MCA compliance)
        /// </summary>
        public string? Checksum { get; set; }
    }

    /// <summary>
    /// Constants for audit operation types
    /// </summary>
    public static class AuditOperations
    {
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";

        public static readonly string[] All = { Create, Update, Delete };

        public static bool IsValid(string operation)
        {
            return All.Contains(operation.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Entity type constants for audit trail
    /// </summary>
    public static class AuditEntityTypes
    {
        public const string Invoice = "invoice";
        public const string Payment = "payment";
        public const string Vendor = "vendor";
        public const string VendorInvoice = "vendor_invoice";
        public const string VendorPayment = "vendor_payment";
        public const string JournalEntry = "journal_entry";
        public const string Customer = "customer";
        public const string BankAccount = "bank_account";
        public const string BankTransaction = "bank_transaction";
        public const string Employee = "employee";
        public const string Asset = "asset";
        public const string ChartOfAccount = "chart_of_account";
        public const string Party = "party";
        public const string CreditNote = "credit_note";
        public const string Quote = "quote";
        public const string Product = "product";
        public const string ExpenseClaim = "expense_claim";
        public const string PayrollRun = "payroll_run";

        public static readonly string[] All =
        {
            Invoice, Payment, Vendor, VendorInvoice, VendorPayment,
            JournalEntry, Customer, BankAccount, BankTransaction,
            Employee, Asset, ChartOfAccount, Party, CreditNote,
            Quote, Product, ExpenseClaim, PayrollRun
        };
    }
}
