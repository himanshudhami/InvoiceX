using System.Text.Json;
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Entities.Migration;
using Core.Interfaces.Migration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Main orchestrator for the Tally import process
    /// </summary>
    public class TallyImportOrchestrator : ITallyImportService
    {
        private readonly ILogger<TallyImportOrchestrator> _logger;
        private readonly ITallyParserFactory _parserFactory;
        private readonly ITallyMasterMappingService _masterMappingService;
        private readonly ITallyVoucherMappingService _voucherMappingService;
        private readonly ITallyValidationService _validationService;
        private readonly ITallyRollbackService _rollbackService;
        private readonly ITallyMigrationBatchRepository _batchRepository;
        private readonly ITallyMigrationLogRepository _logRepository;
        private readonly ITallyFieldMappingRepository _mappingRepository;

        // In-memory cache for parsed data (in production, use Redis or similar)
        private static readonly Dictionary<Guid, TallyParsedDataDto> _parsedDataCache = new();
        private static readonly object _cacheLock = new();

        public TallyImportOrchestrator(
            ILogger<TallyImportOrchestrator> logger,
            ITallyParserFactory parserFactory,
            ITallyMasterMappingService masterMappingService,
            ITallyVoucherMappingService voucherMappingService,
            ITallyValidationService validationService,
            ITallyRollbackService rollbackService,
            ITallyMigrationBatchRepository batchRepository,
            ITallyMigrationLogRepository logRepository,
            ITallyFieldMappingRepository mappingRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
            _masterMappingService = masterMappingService ?? throw new ArgumentNullException(nameof(masterMappingService));
            _voucherMappingService = voucherMappingService ?? throw new ArgumentNullException(nameof(voucherMappingService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _rollbackService = rollbackService ?? throw new ArgumentNullException(nameof(rollbackService));
            _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
        }

        public async Task<Result<TallyUploadResponseDto>> UploadAndParseAsync(
            TallyUploadRequestDto request,
            Stream fileStream,
            string fileName,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Tally file upload for company {CompanyId}: {FileName}",
                request.CompanyId, fileName);

            try
            {
                // Create batch record
                var batch = new TallyMigrationBatch
                {
                    Id = Guid.NewGuid(),
                    CompanyId = request.CompanyId,
                    BatchNumber = GenerateBatchNumber(),
                    ImportType = request.ImportType,
                    SourceFileName = fileName,
                    SourceFileSize = fileStream.Length,
                    SourceFormat = Path.GetExtension(fileName).TrimStart('.').ToLower(),
                    Status = "parsing",
                    UploadStartedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _batchRepository.AddAsync(batch);

                // Parse the file
                var parseResult = await _parserFactory.ParseAsync(fileStream, fileName);
                if (parseResult.IsFailure)
                {
                    batch.Status = "failed";
                    batch.ErrorMessage = parseResult.Error!.Message;
                    await _batchRepository.UpdateAsync(batch);
                    return parseResult.Error;
                }

                var parsedData = parseResult.Value!;
                parsedData.FileName = fileName;
                parsedData.FileSize = fileStream.Length;

                // Update batch with parsed info
                batch.Status = "preview";
                batch.ParsingCompletedAt = DateTime.UtcNow;
                batch.TallyCompanyName = parsedData.Masters.TallyCompanyName;
                batch.TallyCompanyGuid = parsedData.Masters.TallyCompanyGuid;

                if (parsedData.Vouchers.MinDate.HasValue)
                    batch.TallyFromDate = parsedData.Vouchers.MinDate.Value;
                if (parsedData.Vouchers.MaxDate.HasValue)
                    batch.TallyToDate = parsedData.Vouchers.MaxDate.Value;

                // Set counts
                batch.TotalLedgers = parsedData.Masters.Ledgers.Count;
                batch.TotalStockItems = parsedData.Masters.StockItems.Count;
                batch.TotalStockGroups = parsedData.Masters.StockGroups.Count;
                batch.TotalGodowns = parsedData.Masters.Godowns.Count;
                batch.TotalUnits = parsedData.Masters.Units.Count;
                batch.TotalCostCenters = parsedData.Masters.CostCenters.Count;
                batch.TotalVouchers = parsedData.Vouchers.Vouchers.Count;

                await _batchRepository.UpdateAsync(batch);

                // Validate
                var validationResult = await _validationService.ValidateAsync(request.CompanyId, parsedData, cancellationToken);
                var canProceed = validationResult.IsSuccess && validationResult.Value!.CanProceed;

                // Cache parsed data for later import
                lock (_cacheLock)
                {
                    _parsedDataCache[batch.Id] = parsedData;
                }

                // Build response
                var response = new TallyUploadResponseDto
                {
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    Status = batch.Status,
                    ParsedData = parsedData,
                    TotalLedgers = batch.TotalLedgers,
                    TotalStockItems = batch.TotalStockItems,
                    TotalVouchers = batch.TotalVouchers,
                    TotalCostCenters = batch.TotalCostCenters,
                    ValidationIssues = parsedData.ValidationIssues,
                    CanProceed = canProceed
                };

                _logger.LogInformation("Tally file parsed successfully: Batch {BatchId}, {Ledgers} ledgers, {Vouchers} vouchers",
                    batch.Id, batch.TotalLedgers, batch.TotalVouchers);

                return Result<TallyUploadResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload and parse Tally file {FileName}", fileName);
                return Error.Internal($"Failed to process file: {ex.Message}");
            }
        }

        public async Task<Result<TallyParsedDataDto>> GetParsedDataAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            // Check cache first
            lock (_cacheLock)
            {
                if (_parsedDataCache.TryGetValue(batchId, out var cachedData))
                {
                    return Result<TallyParsedDataDto>.Success(cachedData);
                }
            }

            // Load from batch record (would need to store raw data or re-parse)
            var batch = await _batchRepository.GetByIdAsync(batchId);
            if (batch == null)
            {
                return Error.NotFound($"Batch {batchId} not found");
            }

            // If not in cache and not stored, return error
            return Error.NotFound("Parsed data not available. Please re-upload the file.");
        }

        public async Task<Result<bool>> ConfigureMappingsAsync(
            TallyMappingConfigDto config,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var batch = await _batchRepository.GetByIdAsync(config.BatchId);
                if (batch == null)
                {
                    return Error.NotFound($"Batch {config.BatchId} not found");
                }

                // Save group mappings
                foreach (var groupMapping in config.GroupMappings)
                {
                    var mapping = new TallyFieldMapping
                    {
                        CompanyId = batch.CompanyId,
                        MappingType = "ledger_group",
                        TallyGroupName = groupMapping.TallyGroupName,
                        TallyName = "", // Must be empty string, not null, for unique constraint
                        TargetEntity = groupMapping.TargetEntity,
                        TargetAccountId = groupMapping.TargetAccountId,
                        TargetAccountType = groupMapping.TargetAccountType,
                        Priority = 10,
                        IsActive = true,
                        IsSystemDefault = false
                    };
                    await _mappingRepository.AddAsync(mapping);
                }

                // Save ledger-specific mappings
                foreach (var ledgerMapping in config.LedgerMappings)
                {
                    var mapping = new TallyFieldMapping
                    {
                        CompanyId = batch.CompanyId,
                        MappingType = "ledger",
                        TallyGroupName = ledgerMapping.TallyGroupName,
                        TallyName = ledgerMapping.TallyLedgerName,
                        TargetEntity = ledgerMapping.TargetEntity,
                        TargetAccountId = ledgerMapping.TargetAccountId ?? ledgerMapping.TargetCustomerId ?? ledgerMapping.TargetVendorId,
                        Priority = 5, // Higher priority than group mappings
                        IsActive = true,
                        IsSystemDefault = false
                    };
                    await _mappingRepository.AddAsync(mapping);
                }

                // Save cost category mappings
                foreach (var costCategoryMapping in config.CostCategoryMappings)
                {
                    var mapping = new TallyFieldMapping
                    {
                        CompanyId = batch.CompanyId,
                        MappingType = "cost_category",
                        TallyGroupName = costCategoryMapping.TallyCostCategoryName,
                        TallyName = "", // Must be empty string, not null, for unique constraint
                        TargetEntity = "tags",
                        TargetTagGroup = costCategoryMapping.TargetTagGroup,
                        Priority = 10,
                        IsActive = true,
                        IsSystemDefault = false
                    };
                    await _mappingRepository.AddAsync(mapping);
                }

                // Update batch status
                batch.Status = "mapping";
                await _batchRepository.UpdateAsync(batch);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure mappings for batch {BatchId}", config.BatchId);
                return Error.Internal($"Failed to save mappings: {ex.Message}");
            }
        }

        public async Task<Result<TallyImportResultDto>> StartImportAsync(
            TallyImportRequestDto request,
            Guid userId,
            IProgress<TallyImportProgressDto>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var batch = await _batchRepository.GetByIdAsync(request.BatchId);
            if (batch == null)
            {
                return Error.NotFound($"Batch {request.BatchId} not found");
            }

            // Get parsed data from cache
            TallyParsedDataDto? parsedData;
            lock (_cacheLock)
            {
                _parsedDataCache.TryGetValue(request.BatchId, out parsedData);
            }

            if (parsedData == null)
            {
                return Error.NotFound("Parsed data not available. Please re-upload the file.");
            }

            try
            {
                // Update batch status
                batch.Status = "importing";
                batch.ImportStartedAt = DateTime.UtcNow;
                await _batchRepository.UpdateAsync(batch);

                var result = new TallyImportResultDto
                {
                    BatchId = batch.Id,
                    BatchNumber = batch.BatchNumber,
                    StartedAt = batch.ImportStartedAt ?? DateTime.UtcNow
                };

                // Report initial progress
                progress?.Report(new TallyImportProgressDto
                {
                    BatchId = batch.Id,
                    Status = "importing",
                    CurrentPhase = "masters",
                    PercentComplete = 0,
                    StartedAt = batch.ImportStartedAt
                });

                // Phase 1: Import Masters
                _logger.LogInformation("Starting master import for batch {BatchId}", batch.Id);
                var masterResult = await _masterMappingService.ImportMastersAsync(
                    batch.Id,
                    batch.CompanyId,
                    parsedData.Masters,
                    null,
                    cancellationToken);

                if (masterResult.IsFailure)
                {
                    batch.Status = "failed";
                    batch.ErrorMessage = masterResult.Error!.Message;
                    await _batchRepository.UpdateAsync(batch);
                    return masterResult.Error;
                }

                var masters = masterResult.Value!;
                result.Ledgers = masters.Ledgers;
                result.StockItems = masters.StockItems;
                result.StockGroups = masters.StockGroups;
                result.Godowns = masters.Godowns;
                result.CostCenters = masters.CostCenters;

                // Update batch counts
                batch.ImportedLedgers = masters.Ledgers.Imported;
                batch.ImportedStockItems = masters.StockItems.Imported;
                batch.ImportedStockGroups = masters.StockGroups.Imported;
                batch.ImportedGodowns = masters.Godowns.Imported;
                batch.ImportedCostCenters = masters.CostCenters.Imported;
                batch.ImportedUnits = masters.Units.Imported;
                batch.SuspenseEntriesCreated = masters.TotalSuspense;
                await _batchRepository.UpdateAsync(batch);

                progress?.Report(new TallyImportProgressDto
                {
                    BatchId = batch.Id,
                    Status = "importing",
                    CurrentPhase = "vouchers",
                    TotalMasters = parsedData.Masters.Ledgers.Count + parsedData.Masters.StockItems.Count,
                    ProcessedMasters = masters.TotalImported,
                    SuccessfulMasters = masters.TotalImported,
                    FailedMasters = masters.TotalFailed,
                    PercentComplete = 30
                });

                // Phase 2: Import Vouchers
                _logger.LogInformation("Starting voucher import for batch {BatchId}", batch.Id);
                var voucherResult = await _voucherMappingService.ImportVouchersAsync(
                    batch.Id,
                    batch.CompanyId,
                    parsedData.Vouchers,
                    request,
                    progress,
                    cancellationToken);

                if (voucherResult.IsFailure)
                {
                    batch.Status = "failed";
                    batch.ErrorMessage = voucherResult.Error!.Message;
                    await _batchRepository.UpdateAsync(batch);
                    return voucherResult.Error;
                }

                var vouchers = voucherResult.Value!;
                result.Vouchers = new TallyImportCountsDto
                {
                    Total = parsedData.Vouchers.Vouchers.Count,
                    Imported = vouchers.TotalImported,
                    Failed = vouchers.TotalFailed,
                    Skipped = vouchers.TotalSkipped
                };
                result.VoucherCountsByType = vouchers.ByVoucherType;
                result.TotalDebitAmount = vouchers.TotalDebitAmount;
                result.TotalCreditAmount = vouchers.TotalCreditAmount;
                result.Imbalance = Math.Abs(vouchers.TotalDebitAmount - vouchers.TotalCreditAmount);

                // Update batch
                batch.ImportedVouchers = vouchers.TotalImported;
                batch.FailedVouchers = vouchers.TotalFailed;

                // Finalize
                batch.Status = "completed";
                batch.ImportCompletedAt = DateTime.UtcNow;
                await _batchRepository.UpdateAsync(batch);

                // Calculate totals
                result.TotalRecords = masters.TotalImported + vouchers.TotalImported;
                result.ImportedRecords = masters.TotalImported + vouchers.TotalImported;
                result.SkippedRecords = vouchers.TotalSkipped;
                result.FailedRecords = masters.TotalFailed + vouchers.TotalFailed;
                result.SuspenseRecords = masters.TotalSuspense;
                result.Status = "completed";
                result.Success = true;
                result.CompletedAt = DateTime.UtcNow;
                result.DurationSeconds = (int)(result.CompletedAt - result.StartedAt).TotalSeconds;

                // Clear cache
                lock (_cacheLock)
                {
                    _parsedDataCache.Remove(batch.Id);
                }

                progress?.Report(new TallyImportProgressDto
                {
                    BatchId = batch.Id,
                    Status = "completed",
                    CurrentPhase = "completed",
                    PercentComplete = 100,
                    ProcessedVouchers = vouchers.TotalImported,
                    SuccessfulVouchers = vouchers.TotalImported,
                    FailedVouchers = vouchers.TotalFailed
                });

                _logger.LogInformation("Import completed for batch {BatchId}: {Records} records imported",
                    batch.Id, result.ImportedRecords);

                return Result<TallyImportResultDto>.Success(result);
            }
            catch (OperationCanceledException)
            {
                batch.Status = "cancelled";
                await _batchRepository.UpdateAsync(batch);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed for batch {BatchId}", batch.Id);
                batch.Status = "failed";
                batch.ErrorMessage = ex.Message;
                await _batchRepository.UpdateAsync(batch);
                return Error.Internal($"Import failed: {ex.Message}");
            }
        }

        public async Task<Result<TallyImportProgressDto>> GetProgressAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            var batch = await _batchRepository.GetByIdAsync(batchId);
            if (batch == null)
            {
                return Error.NotFound($"Batch {batchId} not found");
            }

            var counts = await _logRepository.GetCountsByStatusAsync(batchId);
            var successCount = counts.GetValueOrDefault("success", 0);
            var failedCount = counts.GetValueOrDefault("failed", 0);
            var skippedCount = counts.GetValueOrDefault("skipped", 0);
            var suspenseCount = counts.GetValueOrDefault("mapped_to_suspense", 0);

            var totalExpected = batch.TotalLedgers + batch.TotalStockItems + batch.TotalVouchers;
            var processed = successCount + failedCount + skippedCount + suspenseCount;

            var progress = new TallyImportProgressDto
            {
                BatchId = batch.Id,
                Status = batch.Status,
                CurrentPhase = DeterminePhase(batch),
                TotalMasters = batch.TotalLedgers + batch.TotalStockItems + batch.TotalStockGroups,
                ProcessedMasters = batch.ImportedLedgers + batch.ImportedStockItems + batch.ImportedStockGroups,
                SuccessfulMasters = batch.ImportedLedgers + batch.ImportedStockItems,
                FailedMasters = failedCount,
                TotalVouchers = batch.TotalVouchers,
                ProcessedVouchers = batch.ImportedVouchers + batch.FailedVouchers,
                SuccessfulVouchers = batch.ImportedVouchers,
                FailedVouchers = batch.FailedVouchers,
                PercentComplete = totalExpected > 0 ? (processed * 100 / totalExpected) : 0,
                StartedAt = batch.ImportStartedAt,
                ElapsedSeconds = batch.ImportStartedAt.HasValue
                    ? (int)(DateTime.UtcNow - batch.ImportStartedAt.Value).TotalSeconds
                    : 0
            };

            // Estimate remaining time based on progress
            if (progress.ElapsedSeconds > 0 && progress.PercentComplete > 0)
            {
                var estimatedTotal = progress.ElapsedSeconds * 100 / progress.PercentComplete;
                progress.EstimatedRemainingSeconds = (int)(estimatedTotal - progress.ElapsedSeconds);
            }

            return Result<TallyImportProgressDto>.Success(progress);
        }

        public async Task<Result<TallyImportResultDto>> GetResultAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            var batch = await _batchRepository.GetByIdAsync(batchId);
            if (batch == null)
            {
                return Error.NotFound($"Batch {batchId} not found");
            }

            var countsByType = await _logRepository.GetCountsByRecordTypeAsync(batchId);
            var countsByStatus = await _logRepository.GetCountsByStatusAsync(batchId);

            var result = new TallyImportResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                Success = batch.Status == "completed",
                TotalRecords = batch.TotalLedgers + batch.TotalStockItems + batch.TotalVouchers,
                ImportedRecords = batch.ImportedLedgers + batch.ImportedStockItems + batch.ImportedVouchers,
                FailedRecords = countsByStatus.GetValueOrDefault("failed", 0),
                SkippedRecords = countsByStatus.GetValueOrDefault("skipped", 0),
                SuspenseRecords = countsByStatus.GetValueOrDefault("mapped_to_suspense", 0),
                Ledgers = new TallyImportCountsDto
                {
                    Total = batch.TotalLedgers,
                    Imported = batch.ImportedLedgers
                },
                StockItems = new TallyImportCountsDto
                {
                    Total = batch.TotalStockItems,
                    Imported = batch.ImportedStockItems
                },
                StockGroups = new TallyImportCountsDto
                {
                    Total = batch.TotalStockGroups,
                    Imported = batch.ImportedStockGroups
                },
                Godowns = new TallyImportCountsDto
                {
                    Total = batch.TotalGodowns,
                    Imported = batch.ImportedGodowns
                },
                CostCenters = new TallyImportCountsDto
                {
                    Total = batch.TotalCostCenters,
                    Imported = batch.ImportedCostCenters
                },
                Vouchers = new TallyImportCountsDto
                {
                    Total = batch.TotalVouchers,
                    Imported = batch.ImportedVouchers,
                    Failed = batch.FailedVouchers
                },
                StartedAt = batch.ImportStartedAt ?? batch.CreatedAt,
                CompletedAt = batch.ImportCompletedAt ?? DateTime.UtcNow,
                DurationSeconds = batch.ImportStartedAt.HasValue && batch.ImportCompletedAt.HasValue
                    ? (int)(batch.ImportCompletedAt.Value - batch.ImportStartedAt.Value).TotalSeconds
                    : 0
            };

            // Get errors
            var errors = await _logRepository.GetFailedByBatchIdAsync(batchId);
            result.Errors = errors.Select(e => new TallyImportErrorDto
            {
                RecordType = e.RecordType,
                TallyGuid = e.TallyGuid,
                TallyName = e.TallyName,
                ErrorMessage = e.ErrorMessage ?? "Unknown error"
            }).ToList();

            // Get suspense items
            var suspenseItems = await _logRepository.GetSuspenseByBatchIdAsync(batchId);
            result.SuspenseItems = suspenseItems.Select(s => new TallySuspenseItemDto
            {
                RecordType = s.RecordType,
                TallyName = s.TallyName ?? string.Empty,
                Amount = s.TallyAmount ?? 0,
                Reason = s.ErrorMessage ?? "Unmapped"
            }).ToList();

            return Result<TallyImportResultDto>.Success(result);
        }

        public async Task<Result<bool>> RollbackAsync(
            TallyRollbackRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var result = await _rollbackService.RollbackBatchAsync(request.BatchId, request, cancellationToken);
            return result.IsSuccess
                ? Result<bool>.Success(result.Value!.Success)
                : result.Error!;
        }

        public async Task<Result<(IEnumerable<TallyBatchListItemDto> Items, int TotalCount)>> GetBatchesAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 20,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            var (batches, totalCount) = await _batchRepository.GetPagedAsync(
                companyId, pageNumber, pageSize, status);

            var items = batches.Select(b => new TallyBatchListItemDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNumber,
                ImportType = b.ImportType,
                Status = b.Status,
                TallyCompanyName = b.TallyCompanyName,
                SourceFileName = b.SourceFileName,
                TallyFromDate = b.TallyFromDate,
                TallyToDate = b.TallyToDate,
                ImportedLedgers = b.ImportedLedgers,
                ImportedVouchers = b.ImportedVouchers,
                ImportCompletedAt = b.ImportCompletedAt,
                CreatedAt = b.CreatedAt,
                ErrorMessage = b.ErrorMessage
            }).ToList();

            return Result<(IEnumerable<TallyBatchListItemDto>, int)>.Success((items, totalCount));
        }

        public async Task<Result<TallyBatchDetailDto>> GetBatchDetailAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            var batch = await _batchRepository.GetByIdAsync(batchId);
            if (batch == null)
            {
                return Error.NotFound($"Batch {batchId} not found");
            }

            var detail = new TallyBatchDetailDto
            {
                Id = batch.Id,
                BatchNumber = batch.BatchNumber,
                ImportType = batch.ImportType,
                Status = batch.Status,
                SourceFileName = batch.SourceFileName,
                SourceFileSize = batch.SourceFileSize,
                SourceFormat = batch.SourceFormat,
                TallyCompanyName = batch.TallyCompanyName,
                TallyCompanyGuid = batch.TallyCompanyGuid,
                TallyFromDate = batch.TallyFromDate,
                TallyToDate = batch.TallyToDate,
                Ledgers = new TallyImportCountsDto { Total = batch.TotalLedgers, Imported = batch.ImportedLedgers },
                StockItems = new TallyImportCountsDto { Total = batch.TotalStockItems, Imported = batch.ImportedStockItems },
                StockGroups = new TallyImportCountsDto { Total = batch.TotalStockGroups, Imported = batch.ImportedStockGroups },
                Godowns = new TallyImportCountsDto { Total = batch.TotalGodowns, Imported = batch.ImportedGodowns },
                Units = new TallyImportCountsDto { Total = batch.TotalUnits, Imported = batch.ImportedUnits },
                CostCenters = new TallyImportCountsDto { Total = batch.TotalCostCenters, Imported = batch.ImportedCostCenters },
                Vouchers = new TallyImportCountsDto { Total = batch.TotalVouchers, Imported = batch.ImportedVouchers, Failed = batch.FailedVouchers },
                SuspenseEntriesCreated = batch.SuspenseEntriesCreated,
                SuspenseTotalAmount = batch.SuspenseTotalAmount,
                UploadStartedAt = batch.UploadStartedAt,
                ParsingCompletedAt = batch.ParsingCompletedAt,
                ImportStartedAt = batch.ImportStartedAt,
                ImportCompletedAt = batch.ImportCompletedAt,
                ErrorMessage = batch.ErrorMessage,
                CreatedAt = batch.CreatedAt
            };

            return Result<TallyBatchDetailDto>.Success(detail);
        }

        public async Task<Result<(IEnumerable<TallyImportErrorDto> Items, int TotalCount)>> GetLogsAsync(
            Guid batchId,
            int pageNumber = 1,
            int pageSize = 50,
            string? recordType = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            var (logs, totalCount) = await _logRepository.GetPagedByBatchIdAsync(
                batchId, pageNumber, pageSize, recordType, status);

            var items = logs.Select(l => new TallyImportErrorDto
            {
                RecordType = l.RecordType,
                TallyGuid = l.TallyGuid,
                TallyName = l.TallyName,
                ErrorCode = l.ErrorCode ?? string.Empty,
                ErrorMessage = l.ErrorMessage ?? string.Empty
            }).ToList();

            return Result<(IEnumerable<TallyImportErrorDto>, int)>.Success((items, totalCount));
        }

        private static string GenerateBatchNumber()
        {
            return $"TALLY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        private static string DeterminePhase(TallyMigrationBatch batch)
        {
            return batch.Status switch
            {
                "parsing" => "parsing",
                "preview" => "awaiting_configuration",
                "mapping" => "ready",
                "importing" when batch.ImportedVouchers == 0 => "masters",
                "importing" => "vouchers",
                "completed" => "completed",
                "failed" => "failed",
                "rolled_back" => "rolled_back",
                _ => batch.Status
            };
        }
    }
}
