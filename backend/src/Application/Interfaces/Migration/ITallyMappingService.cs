using Application.DTOs.Migration;
using Core.Common;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Service for mapping Tally masters to our entities
    /// </summary>
    public interface ITallyMasterMappingService
    {
        /// <summary>
        /// Import all masters from parsed Tally data
        /// </summary>
        Task<Result<TallyMasterImportResultDto>> ImportMastersAsync(
            Guid batchId,
            Guid companyId,
            TallyMastersSummaryDto masters,
            TallyMappingConfigDto? mappingConfig = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import ledgers (creates COA, Customers, Vendors, Bank Accounts)
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportLedgersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyLedgerDto> ledgers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import stock groups
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportStockGroupsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyStockGroupDto> stockGroups,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import stock items
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportStockItemsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyStockItemDto> stockItems,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import godowns (warehouses)
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportGodownsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyGodownDto> godowns,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import units of measure
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportUnitsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyUnitDto> units,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import cost centers as tags
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportCostCentersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyCostCenterDto> costCenters,
            List<TallyCostCategoryDto> costCategories,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service for mapping Tally vouchers to our transactions
    /// </summary>
    public interface ITallyVoucherMappingService
    {
        /// <summary>
        /// Import all vouchers from parsed Tally data
        /// </summary>
        Task<Result<TallyVoucherImportResultDto>> ImportVouchersAsync(
            Guid batchId,
            Guid companyId,
            TallyVouchersSummaryDto vouchers,
            TallyImportRequestDto request,
            IProgress<TallyImportProgressDto>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import sales vouchers as invoices
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportSalesVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import purchase vouchers as vendor invoices
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportPurchaseVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import receipt vouchers as payments
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportReceiptVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import payment vouchers as vendor payments
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportPaymentVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import journal/contra vouchers as journal entries
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportJournalVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import stock journals as stock transfers/movements
        /// </summary>
        Task<Result<TallyImportCountsDto>> ImportStockVouchersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyVoucherDto> vouchers,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of master import
    /// </summary>
    public class TallyMasterImportResultDto
    {
        public TallyImportCountsDto Ledgers { get; set; } = new();
        public TallyImportCountsDto StockGroups { get; set; } = new();
        public TallyImportCountsDto StockItems { get; set; } = new();
        public TallyImportCountsDto Godowns { get; set; } = new();
        public TallyImportCountsDto Units { get; set; } = new();
        public TallyImportCountsDto CostCenters { get; set; } = new();
        public int TotalImported { get; set; }
        public int TotalFailed { get; set; }
        public int TotalSuspense { get; set; }
        public List<TallyImportErrorDto> Errors { get; set; } = new();
    }

    /// <summary>
    /// Result of voucher import
    /// </summary>
    public class TallyVoucherImportResultDto
    {
        public TallyImportCountsDto Sales { get; set; } = new();
        public TallyImportCountsDto Purchases { get; set; } = new();
        public TallyImportCountsDto Receipts { get; set; } = new();
        public TallyImportCountsDto Payments { get; set; } = new();
        public TallyImportCountsDto Journals { get; set; } = new();
        public TallyImportCountsDto StockJournals { get; set; } = new();
        public TallyImportCountsDto CreditNotes { get; set; } = new();
        public TallyImportCountsDto DebitNotes { get; set; } = new();
        public Dictionary<string, int> ByVoucherType { get; set; } = new();
        public int TotalImported { get; set; }
        public int TotalFailed { get; set; }
        public int TotalSkipped { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public List<TallyImportErrorDto> Errors { get; set; } = new();
    }
}
