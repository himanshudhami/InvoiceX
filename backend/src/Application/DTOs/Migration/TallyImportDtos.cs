using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Migration
{
    /// <summary>
    /// Request to upload and parse a Tally export file
    /// </summary>
    public class TallyUploadRequestDto
    {
        [Required]
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Import type: 'full' for initial migration, 'incremental' for sync
        /// </summary>
        public string ImportType { get; set; } = "full";

        /// <summary>
        /// File content as base64 (for API upload)
        /// </summary>
        public string? FileContent { get; set; }

        /// <summary>
        /// Original file name
        /// </summary>
        public string? FileName { get; set; }
    }

    /// <summary>
    /// Response after file upload and parsing
    /// </summary>
    public class TallyUploadResponseDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Parsed data preview
        public TallyParsedDataDto? ParsedData { get; set; }

        // Counts
        public int TotalLedgers { get; set; }
        public int TotalStockItems { get; set; }
        public int TotalVouchers { get; set; }
        public int TotalCostCenters { get; set; }

        // Validation
        public List<TallyValidationIssueDto> ValidationIssues { get; set; } = new();
        public bool CanProceed { get; set; }
    }

    /// <summary>
    /// Request to configure mappings before import
    /// </summary>
    public class TallyMappingConfigDto
    {
        public Guid BatchId { get; set; }

        /// <summary>
        /// Ledger group mappings (overrides)
        /// </summary>
        public List<TallyGroupMappingDto> GroupMappings { get; set; } = new();

        /// <summary>
        /// Specific ledger mappings (overrides group mapping)
        /// </summary>
        public List<TallyLedgerMappingDto> LedgerMappings { get; set; } = new();

        /// <summary>
        /// Cost category to tag group mappings
        /// </summary>
        public List<TallyCostCategoryMappingDto> CostCategoryMappings { get; set; } = new();

        /// <summary>
        /// Whether to create suspense accounts for unmapped data
        /// </summary>
        public bool CreateSuspenseAccounts { get; set; } = true;

        /// <summary>
        /// Whether to skip unmapped records instead of mapping to suspense
        /// </summary>
        public bool SkipUnmapped { get; set; } = false;
    }

    /// <summary>
    /// Mapping for a Tally ledger group
    /// </summary>
    public class TallyGroupMappingDto
    {
        public string TallyGroupName { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public string? TargetAccountType { get; set; }
        public Guid? TargetAccountId { get; set; }
    }

    /// <summary>
    /// Mapping for a specific Tally ledger
    /// </summary>
    public class TallyLedgerMappingDto
    {
        public string TallyLedgerName { get; set; } = string.Empty;
        public string TallyGroupName { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public Guid? TargetAccountId { get; set; }
        public Guid? TargetCustomerId { get; set; }
        public Guid? TargetVendorId { get; set; }
    }

    /// <summary>
    /// Mapping for Tally cost category to tag group
    /// </summary>
    public class TallyCostCategoryMappingDto
    {
        public string TallyCostCategoryName { get; set; } = string.Empty;
        public string TargetTagGroup { get; set; } = "cost_center";
    }

    /// <summary>
    /// Request to start the import process
    /// </summary>
    public class TallyImportRequestDto
    {
        [Required]
        public Guid BatchId { get; set; }

        /// <summary>
        /// Optional: Only import specific record types
        /// </summary>
        public List<string>? RecordTypes { get; set; }

        /// <summary>
        /// Optional: Date range filter for vouchers
        /// </summary>
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        /// <summary>
        /// Whether to create GL journal entries for vouchers
        /// </summary>
        public bool CreateJournalEntries { get; set; } = true;

        /// <summary>
        /// Whether to update stock quantities
        /// </summary>
        public bool UpdateStockQuantities { get; set; } = true;
    }

    /// <summary>
    /// Progress update during import
    /// </summary>
    public class TallyImportProgressDto
    {
        public Guid BatchId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CurrentPhase { get; set; } = string.Empty; // masters, opening_balances, vouchers

        // Master progress
        public int TotalMasters { get; set; }
        public int ProcessedMasters { get; set; }
        public int SuccessfulMasters { get; set; }
        public int FailedMasters { get; set; }

        // Voucher progress
        public int TotalVouchers { get; set; }
        public int ProcessedVouchers { get; set; }
        public int SuccessfulVouchers { get; set; }
        public int FailedVouchers { get; set; }

        // Overall
        public int PercentComplete { get; set; }
        public string? CurrentItem { get; set; }
        public string? LastError { get; set; }

        // Timing
        public DateTime? StartedAt { get; set; }
        public int ElapsedSeconds { get; set; }
        public int? EstimatedRemainingSeconds { get; set; }
    }

    /// <summary>
    /// Final import result
    /// </summary>
    public class TallyImportResultDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool Success { get; set; }

        // Counts
        public int TotalRecords { get; set; }
        public int ImportedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int FailedRecords { get; set; }
        public int SuspenseRecords { get; set; }

        // Breakdown by type
        public TallyImportCountsDto Ledgers { get; set; } = new();
        public TallyImportCountsDto StockItems { get; set; } = new();
        public TallyImportCountsDto StockGroups { get; set; } = new();
        public TallyImportCountsDto Godowns { get; set; } = new();
        public TallyImportCountsDto CostCenters { get; set; } = new();
        public TallyImportCountsDto Vouchers { get; set; } = new();

        // Voucher breakdown
        public Dictionary<string, int> VoucherCountsByType { get; set; } = new();

        // Suspense details
        public decimal SuspenseTotalAmount { get; set; }
        public List<TallySuspenseItemDto> SuspenseItems { get; set; } = new();

        // Errors
        public List<TallyImportErrorDto> Errors { get; set; } = new();

        // Timing
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public int DurationSeconds { get; set; }

        // Amount reconciliation
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal Imbalance { get; set; }
    }

    /// <summary>
    /// Import counts for a record type
    /// </summary>
    public class TallyImportCountsDto
    {
        public int Total { get; set; }
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public int Suspense { get; set; }
    }

    /// <summary>
    /// Item mapped to suspense account
    /// </summary>
    public class TallySuspenseItemDto
    {
        public string RecordType { get; set; } = string.Empty;
        public string TallyName { get; set; } = string.Empty;
        public string? TallyGroupName { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? SuspenseAccountId { get; set; }
    }

    /// <summary>
    /// Import error detail
    /// </summary>
    public class TallyImportErrorDto
    {
        public string RecordType { get; set; } = string.Empty;
        public string? TallyGuid { get; set; }
        public string? TallyName { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// Request to rollback an import batch
    /// </summary>
    public class TallyRollbackRequestDto
    {
        [Required]
        public Guid BatchId { get; set; }

        /// <summary>
        /// Whether to delete masters created in this batch
        /// </summary>
        public bool DeleteMasters { get; set; } = true;

        /// <summary>
        /// Whether to delete transactions created in this batch
        /// </summary>
        public bool DeleteTransactions { get; set; } = true;

        /// <summary>
        /// Reason for rollback
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Batch list item for history view
    /// </summary>
    public class TallyBatchListItemDto
    {
        public Guid Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ImportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TallyCompanyName { get; set; }
        public string? SourceFileName { get; set; }
        public DateOnly? TallyFromDate { get; set; }
        public DateOnly? TallyToDate { get; set; }
        public int ImportedLedgers { get; set; }
        public int ImportedVouchers { get; set; }
        public DateTime? ImportCompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Detailed batch info
    /// </summary>
    public class TallyBatchDetailDto
    {
        public Guid Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ImportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Source
        public string? SourceFileName { get; set; }
        public long? SourceFileSize { get; set; }
        public string SourceFormat { get; set; } = string.Empty;

        // Tally Company
        public string? TallyCompanyName { get; set; }
        public string? TallyCompanyGuid { get; set; }
        public DateOnly? TallyFromDate { get; set; }
        public DateOnly? TallyToDate { get; set; }

        // All counts
        public TallyImportCountsDto Ledgers { get; set; } = new();
        public TallyImportCountsDto StockItems { get; set; } = new();
        public TallyImportCountsDto StockGroups { get; set; } = new();
        public TallyImportCountsDto Godowns { get; set; } = new();
        public TallyImportCountsDto Units { get; set; } = new();
        public TallyImportCountsDto CostCenters { get; set; } = new();
        public TallyImportCountsDto Vouchers { get; set; } = new();

        // Suspense
        public int SuspenseEntriesCreated { get; set; }
        public decimal SuspenseTotalAmount { get; set; }

        // Timing
        public DateTime? UploadStartedAt { get; set; }
        public DateTime? ParsingCompletedAt { get; set; }
        public DateTime? ImportStartedAt { get; set; }
        public DateTime? ImportCompletedAt { get; set; }

        // Error
        public string? ErrorMessage { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
    }
}
