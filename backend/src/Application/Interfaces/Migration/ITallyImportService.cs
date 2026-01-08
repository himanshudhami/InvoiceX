using Application.DTOs.Migration;
using Core.Common;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Main orchestrator for Tally import process
    /// </summary>
    public interface ITallyImportService
    {
        /// <summary>
        /// Upload and parse a Tally export file
        /// </summary>
        Task<Result<TallyUploadResponseDto>> UploadAndParseAsync(
            TallyUploadRequestDto request,
            Stream fileStream,
            string fileName,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get preview of parsed data for a batch
        /// </summary>
        Task<Result<TallyParsedDataDto>> GetParsedDataAsync(
            Guid batchId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configure mappings for a batch
        /// </summary>
        Task<Result<bool>> ConfigureMappingsAsync(
            TallyMappingConfigDto config,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Start the import process
        /// </summary>
        Task<Result<TallyImportResultDto>> StartImportAsync(
            TallyImportRequestDto request,
            Guid userId,
            IProgress<TallyImportProgressDto>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current import progress
        /// </summary>
        Task<Result<TallyImportProgressDto>> GetProgressAsync(
            Guid batchId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get import result/summary
        /// </summary>
        Task<Result<TallyImportResultDto>> GetResultAsync(
            Guid batchId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback an import batch
        /// </summary>
        Task<Result<bool>> RollbackAsync(
            TallyRollbackRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get list of all import batches for a company
        /// </summary>
        Task<Result<(IEnumerable<TallyBatchListItemDto> Items, int TotalCount)>> GetBatchesAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 20,
            string? status = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed batch information
        /// </summary>
        Task<Result<TallyBatchDetailDto>> GetBatchDetailAsync(
            Guid batchId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get import logs for a batch
        /// </summary>
        Task<Result<(IEnumerable<TallyImportErrorDto> Items, int TotalCount)>> GetLogsAsync(
            Guid batchId,
            int pageNumber = 1,
            int pageSize = 50,
            string? recordType = null,
            string? status = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service for rolling back Tally imports
    /// </summary>
    public interface ITallyRollbackService
    {
        /// <summary>
        /// Rollback all records imported in a batch
        /// </summary>
        Task<Result<TallyRollbackResultDto>> RollbackBatchAsync(
            Guid batchId,
            TallyRollbackRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a batch can be rolled back
        /// </summary>
        Task<Result<TallyRollbackPreviewDto>> PreviewRollbackAsync(
            Guid batchId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a rollback operation
    /// </summary>
    public class TallyRollbackResultDto
    {
        public Guid BatchId { get; set; }
        public bool Success { get; set; }
        public int MastersDeleted { get; set; }
        public int TransactionsDeleted { get; set; }
        public int JournalEntriesDeleted { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime RolledBackAt { get; set; }
    }

    /// <summary>
    /// Preview of what will be affected by rollback
    /// </summary>
    public class TallyRollbackPreviewDto
    {
        public Guid BatchId { get; set; }
        public bool CanRollback { get; set; }
        public string? BlockingReason { get; set; }

        // Counts of records that will be deleted
        public int CustomersCount { get; set; }
        public int VendorsCount { get; set; }
        public int AccountsCount { get; set; }
        public int StockItemsCount { get; set; }
        public int InvoicesCount { get; set; }
        public int PaymentsCount { get; set; }
        public int JournalEntriesCount { get; set; }

        // Dependencies that might be affected
        public int DependentTransactionsCount { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Service for validating Tally data before import
    /// </summary>
    public interface ITallyValidationService
    {
        /// <summary>
        /// Validate parsed Tally data
        /// </summary>
        Task<Result<TallyValidationResultDto>> ValidateAsync(
            Guid companyId,
            TallyParsedDataDto parsedData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check for duplicates
        /// </summary>
        Task<Result<TallyDuplicateCheckResultDto>> CheckDuplicatesAsync(
            Guid companyId,
            TallyParsedDataDto parsedData,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of validation
    /// </summary>
    public class TallyValidationResultDto
    {
        public bool IsValid { get; set; }
        public bool CanProceed { get; set; }
        public List<TallyValidationIssueDto> Issues { get; set; } = new();
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
    }

    /// <summary>
    /// Result of duplicate check
    /// </summary>
    public class TallyDuplicateCheckResultDto
    {
        public int DuplicateLedgers { get; set; }
        public int DuplicateStockItems { get; set; }
        public int DuplicateVouchers { get; set; }
        public List<TallyDuplicateItemDto> Duplicates { get; set; } = new();
    }

    /// <summary>
    /// A duplicate item found
    /// </summary>
    public class TallyDuplicateItemDto
    {
        public string RecordType { get; set; } = string.Empty;
        public string TallyGuid { get; set; } = string.Empty;
        public string TallyName { get; set; } = string.Empty;
        public Guid ExistingId { get; set; }
        public string ExistingName { get; set; } = string.Empty;
    }
}
