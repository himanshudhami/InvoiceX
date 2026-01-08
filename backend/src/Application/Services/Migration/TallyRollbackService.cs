using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Interfaces;
using Core.Interfaces.Inventory;
using Core.Interfaces.Ledger;
using Core.Interfaces.Migration;
using Core.Interfaces.Tags;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Service for rolling back Tally imports
    /// </summary>
    public class TallyRollbackService : ITallyRollbackService
    {
        private readonly ILogger<TallyRollbackService> _logger;
        private readonly ITallyMigrationBatchRepository _batchRepository;
        private readonly ITallyMigrationLogRepository _logRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly IVendorsRepository _vendorsRepository;
        private readonly IChartOfAccountRepository _coaRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IStockGroupRepository _stockGroupRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IUnitOfMeasureRepository _unitRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly IVendorInvoicesRepository _vendorInvoiceRepository;
        private readonly IVendorPaymentsRepository _vendorPaymentRepository;
        private readonly IJournalEntryRepository _journalRepository;

        public TallyRollbackService(
            ILogger<TallyRollbackService> logger,
            ITallyMigrationBatchRepository batchRepository,
            ITallyMigrationLogRepository logRepository,
            ICustomersRepository customersRepository,
            IVendorsRepository vendorsRepository,
            IChartOfAccountRepository coaRepository,
            IBankAccountRepository bankAccountRepository,
            IStockItemRepository stockItemRepository,
            IStockGroupRepository stockGroupRepository,
            IWarehouseRepository warehouseRepository,
            IUnitOfMeasureRepository unitRepository,
            ITagRepository tagRepository,
            IInvoicesRepository invoicesRepository,
            IPaymentsRepository paymentsRepository,
            IVendorInvoicesRepository vendorInvoiceRepository,
            IVendorPaymentsRepository vendorPaymentRepository,
            IJournalEntryRepository journalRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
            _vendorsRepository = vendorsRepository ?? throw new ArgumentNullException(nameof(vendorsRepository));
            _coaRepository = coaRepository ?? throw new ArgumentNullException(nameof(coaRepository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
            _stockItemRepository = stockItemRepository ?? throw new ArgumentNullException(nameof(stockItemRepository));
            _stockGroupRepository = stockGroupRepository ?? throw new ArgumentNullException(nameof(stockGroupRepository));
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _vendorInvoiceRepository = vendorInvoiceRepository ?? throw new ArgumentNullException(nameof(vendorInvoiceRepository));
            _vendorPaymentRepository = vendorPaymentRepository ?? throw new ArgumentNullException(nameof(vendorPaymentRepository));
            _journalRepository = journalRepository ?? throw new ArgumentNullException(nameof(journalRepository));
        }

        public async Task<Result<TallyRollbackPreviewDto>> PreviewRollbackAsync(
            Guid batchId,
            CancellationToken cancellationToken = default)
        {
            var batch = await _batchRepository.GetByIdAsync(batchId);
            if (batch == null)
            {
                return Error.NotFound($"Batch {batchId} not found");
            }

            var preview = new TallyRollbackPreviewDto
            {
                BatchId = batchId,
                CanRollback = true
            };

            // Check batch status
            if (batch.Status == "rolled_back")
            {
                preview.CanRollback = false;
                preview.BlockingReason = "This batch has already been rolled back";
                return Result<TallyRollbackPreviewDto>.Success(preview);
            }

            if (batch.Status == "importing")
            {
                preview.CanRollback = false;
                preview.BlockingReason = "Cannot rollback while import is in progress";
                return Result<TallyRollbackPreviewDto>.Success(preview);
            }

            // Get logs grouped by entity type
            var logs = await _logRepository.GetByBatchIdAsync(batchId);
            var successfulLogs = logs.Where(l => l.Status == "success" && l.TargetId.HasValue).ToList();

            // Count by entity type
            foreach (var log in successfulLogs)
            {
                switch (log.TargetEntity?.ToLower())
                {
                    case "customers":
                        preview.CustomersCount++;
                        break;
                    case "vendors":
                        preview.VendorsCount++;
                        break;
                    case "chart_of_accounts":
                        preview.AccountsCount++;
                        break;
                    case "stock_items":
                        preview.StockItemsCount++;
                        break;
                    case "invoices":
                        preview.InvoicesCount++;
                        break;
                    case "payments":
                        preview.PaymentsCount++;
                        break;
                    case "journal_entries":
                        preview.JournalEntriesCount++;
                        break;
                }
            }

            // Check for dependent transactions
            // For example, if invoices were created and payments were applied
            if (preview.InvoicesCount > 0 || preview.CustomersCount > 0)
            {
                // Check if there are any payments not from this batch against imported invoices
                var dependentCount = await CountDependentTransactionsAsync(batchId, successfulLogs);
                if (dependentCount > 0)
                {
                    preview.DependentTransactionsCount = dependentCount;
                    preview.Warnings.Add($"Warning: {dependentCount} transactions created after this import reference imported records");
                }
            }

            return Result<TallyRollbackPreviewDto>.Success(preview);
        }

        public async Task<Result<TallyRollbackResultDto>> RollbackBatchAsync(
            Guid batchId,
            TallyRollbackRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var batch = await _batchRepository.GetByIdAsync(batchId);
            if (batch == null)
            {
                return Error.NotFound($"Batch {batchId} not found");
            }

            // Preview first to check if rollback is allowed
            var previewResult = await PreviewRollbackAsync(batchId, cancellationToken);
            if (previewResult.IsFailure)
            {
                return previewResult.Error!;
            }

            var preview = previewResult.Value!;
            if (!preview.CanRollback)
            {
                return Error.Validation(preview.BlockingReason ?? "Rollback is not allowed");
            }

            var result = new TallyRollbackResultDto
            {
                BatchId = batchId
            };
            var errors = new List<string>();

            try
            {
                _logger.LogInformation("Starting rollback for batch {BatchId}", batchId);

                // Get all successful imports in reverse order (delete children before parents)
                var logs = (await _logRepository.GetByBatchIdAsync(batchId))
                    .Where(l => l.Status == "success" && l.TargetId.HasValue)
                    .OrderByDescending(l => l.ProcessingOrder)
                    .ToList();

                // Delete transactions first (they depend on masters)
                if (request.DeleteTransactions)
                {
                    foreach (var log in logs.Where(l => IsTransactionEntity(l.TargetEntity)))
                    {
                        try
                        {
                            await DeleteEntity(log.TargetEntity!, log.TargetId!.Value);
                            result.TransactionsDeleted++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Failed to delete {log.TargetEntity} {log.TargetId}: {ex.Message}");
                        }
                    }

                    // Delete journal entries
                    foreach (var log in logs.Where(l => l.TargetEntity == "journal_entries"))
                    {
                        try
                        {
                            await _journalRepository.DeleteAsync(log.TargetId!.Value);
                            result.JournalEntriesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Failed to delete journal entry {log.TargetId}: {ex.Message}");
                        }
                    }
                }

                // Delete masters (in reverse order to handle dependencies)
                if (request.DeleteMasters)
                {
                    foreach (var log in logs.Where(l => IsMasterEntity(l.TargetEntity)))
                    {
                        try
                        {
                            await DeleteEntity(log.TargetEntity!, log.TargetId!.Value);
                            result.MastersDeleted++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Failed to delete {log.TargetEntity} {log.TargetId}: {ex.Message}");
                        }
                    }
                }

                // Update batch status
                batch.Status = "rolled_back";
                batch.ErrorMessage = request.Reason;
                await _batchRepository.UpdateAsync(batch);

                // Delete migration logs for this batch
                await _logRepository.DeleteByBatchIdAsync(batchId);

                result.Success = errors.Count == 0;
                result.Errors = errors;
                result.RolledBackAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Rollback completed for batch {BatchId}: {Masters} masters, {Transactions} transactions, {Journals} journals deleted",
                    batchId, result.MastersDeleted, result.TransactionsDeleted, result.JournalEntriesDeleted);

                return Result<TallyRollbackResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback failed for batch {BatchId}", batchId);
                result.Success = false;
                result.Errors.Add($"Rollback failed: {ex.Message}");
                return Result<TallyRollbackResultDto>.Success(result);
            }
        }

        private async Task DeleteEntity(string entityType, Guid entityId)
        {
            switch (entityType.ToLower())
            {
                case "customers":
                    await _customersRepository.DeleteAsync(entityId);
                    break;
                case "vendors":
                    await _vendorsRepository.DeleteAsync(entityId);
                    break;
                case "chart_of_accounts":
                    await _coaRepository.DeleteAsync(entityId);
                    break;
                case "bank_accounts":
                    await _bankAccountRepository.DeleteAsync(entityId);
                    break;
                case "stock_items":
                    await _stockItemRepository.DeleteAsync(entityId);
                    break;
                case "stock_groups":
                    await _stockGroupRepository.DeleteAsync(entityId);
                    break;
                case "warehouses":
                    await _warehouseRepository.DeleteAsync(entityId);
                    break;
                case "units":
                    await _unitRepository.DeleteAsync(entityId);
                    break;
                case "tags":
                    await _tagRepository.DeleteAsync(entityId);
                    break;
                case "invoices":
                    await _invoicesRepository.DeleteAsync(entityId);
                    break;
                case "payments":
                    await _paymentsRepository.DeleteAsync(entityId);
                    break;
                case "vendor_invoices":
                    await _vendorInvoiceRepository.DeleteAsync(entityId);
                    break;
                case "vendor_payments":
                    await _vendorPaymentRepository.DeleteAsync(entityId);
                    break;
                case "journal_entries":
                    await _journalRepository.DeleteAsync(entityId);
                    break;
            }
        }

        private static bool IsMasterEntity(string? entityType)
        {
            if (string.IsNullOrEmpty(entityType)) return false;

            return entityType.ToLower() switch
            {
                "customers" or "vendors" or "chart_of_accounts" or "bank_accounts" or
                "stock_items" or "stock_groups" or "warehouses" or "units" or "tags" => true,
                _ => false
            };
        }

        private static bool IsTransactionEntity(string? entityType)
        {
            if (string.IsNullOrEmpty(entityType)) return false;

            return entityType.ToLower() switch
            {
                "invoices" or "payments" or "vendor_invoices" or "vendor_payments" or
                "stock_movements" or "stock_transfers" => true,
                _ => false
            };
        }

        private async Task<int> CountDependentTransactionsAsync(Guid batchId, List<Core.Entities.Migration.TallyMigrationLog> logs)
        {
            // This would check for transactions created after the import that reference imported records
            // For simplicity, returning 0 - a full implementation would query each entity type
            return 0;
        }
    }
}
