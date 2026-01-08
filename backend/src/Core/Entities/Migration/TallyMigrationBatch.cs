namespace Core.Entities.Migration
{
    /// <summary>
    /// Tracks a Tally import batch with status, counts, and timing information.
    /// Each batch represents one import session from a Tally export file.
    /// </summary>
    public class TallyMigrationBatch
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Batch Identification ====================

        /// <summary>
        /// Unique batch number within company (e.g., "TALLY-2024-001")
        /// </summary>
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// Type of import: 'full' for initial migration, 'incremental' for sync
        /// </summary>
        public string ImportType { get; set; } = "full";

        // ==================== Source File Info ====================

        /// <summary>
        /// Original filename uploaded by user
        /// </summary>
        public string? SourceFileName { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long? SourceFileSize { get; set; }

        /// <summary>
        /// File format: 'xml' or 'json'
        /// </summary>
        public string SourceFormat { get; set; } = "xml";

        /// <summary>
        /// SHA-256 checksum for duplicate detection
        /// </summary>
        public string? SourceChecksum { get; set; }

        // ==================== Tally Company Info ====================

        /// <summary>
        /// Company name extracted from Tally export
        /// </summary>
        public string? TallyCompanyName { get; set; }

        /// <summary>
        /// Company GUID from Tally
        /// </summary>
        public string? TallyCompanyGuid { get; set; }

        /// <summary>
        /// Start date of data in export
        /// </summary>
        public DateOnly? TallyFromDate { get; set; }

        /// <summary>
        /// End date of data in export
        /// </summary>
        public DateOnly? TallyToDate { get; set; }

        /// <summary>
        /// Tally financial year (e.g., "2024-25")
        /// </summary>
        public string? TallyFinancialYear { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Current status of the batch.
        /// Values: pending, uploading, parsing, validating, preview, mapping,
        ///         importing, posting, completed, failed, rolled_back, cancelled
        /// </summary>
        public string Status { get; set; } = "pending";

        // ==================== Master Counts ====================

        public int TotalLedgers { get; set; }
        public int ImportedLedgers { get; set; }
        public int SkippedLedgers { get; set; }
        public int FailedLedgers { get; set; }

        public int TotalStockItems { get; set; }
        public int ImportedStockItems { get; set; }
        public int SkippedStockItems { get; set; }
        public int FailedStockItems { get; set; }

        public int TotalCostCenters { get; set; }
        public int ImportedCostCenters { get; set; }
        public int SkippedCostCenters { get; set; }
        public int FailedCostCenters { get; set; }

        public int TotalGodowns { get; set; }
        public int ImportedGodowns { get; set; }

        public int TotalUnits { get; set; }
        public int ImportedUnits { get; set; }

        public int TotalStockGroups { get; set; }
        public int ImportedStockGroups { get; set; }

        // ==================== Voucher Counts ====================

        public int TotalVouchers { get; set; }
        public int ImportedVouchers { get; set; }
        public int SkippedVouchers { get; set; }
        public int FailedVouchers { get; set; }

        /// <summary>
        /// Voucher counts by type (JSONB).
        /// Example: {"sales": 234, "purchase": 189, "receipt": 78}
        /// </summary>
        public string VoucherCounts { get; set; } = "{}";

        // ==================== Suspense Tracking ====================

        /// <summary>
        /// Number of entries mapped to suspense accounts
        /// </summary>
        public int SuspenseEntriesCreated { get; set; }

        /// <summary>
        /// Total amount in suspense accounts
        /// </summary>
        public decimal SuspenseTotalAmount { get; set; }

        // ==================== Timing ====================

        public DateTime? UploadStartedAt { get; set; }
        public DateTime? ParsingStartedAt { get; set; }
        public DateTime? ParsingCompletedAt { get; set; }
        public DateTime? ValidationStartedAt { get; set; }
        public DateTime? ValidationCompletedAt { get; set; }
        public DateTime? ImportStartedAt { get; set; }
        public DateTime? ImportCompletedAt { get; set; }

        // ==================== Error Info ====================

        /// <summary>
        /// Human-readable error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Structured error details (JSONB)
        /// </summary>
        public string? ErrorDetails { get; set; }

        // ==================== Mapping Config ====================

        /// <summary>
        /// Snapshot of mapping configuration used (JSONB)
        /// </summary>
        public string MappingConfig { get; set; } = "{}";

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ==================== Navigation ====================

        public Companies? Company { get; set; }
        public User? CreatedByUser { get; set; }
        public ICollection<TallyMigrationLog>? Logs { get; set; }
    }
}
