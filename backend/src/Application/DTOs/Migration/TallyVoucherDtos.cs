namespace Application.DTOs.Migration
{
    /// <summary>
    /// Base class for all Tally vouchers
    /// </summary>
    public class TallyVoucherDto
    {
        public string Guid { get; set; } = string.Empty;
        public string VoucherNumber { get; set; } = string.Empty;
        public string VoucherType { get; set; } = string.Empty;
        public string? VoucherTypeName { get; set; }
        public DateOnly Date { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ReferenceDate { get; set; }
        public string? Narration { get; set; }

        // Party Info
        public string? PartyLedgerName { get; set; }
        public string? PartyLedgerGuid { get; set; }

        // Amounts
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public decimal? ExchangeRate { get; set; }

        // GST Info
        public string? PlaceOfSupply { get; set; }
        public bool IsReverseCharge { get; set; }
        public string? GstinOfParty { get; set; }

        // E-Invoice/E-Way
        public string? EInvoiceIrn { get; set; }
        public string? EWayBillNumber { get; set; }

        // Ledger Entries (for accounting)
        public List<TallyLedgerEntryDto> LedgerEntries { get; set; } = new();

        // Inventory Entries (for stock impact)
        public List<TallyInventoryEntryDto> InventoryEntries { get; set; } = new();

        // Bill Allocations (for bill-wise tracking)
        public List<TallyBillAllocationDto> BillAllocations { get; set; } = new();

        // Cost Center Allocations
        public List<TallyCostAllocationDto> CostAllocations { get; set; } = new();

        // Batch Allocations
        public List<TallyBatchAllocationDto> BatchAllocations { get; set; } = new();

        // Flags
        public bool IsCancelled { get; set; }
        public bool IsOptional { get; set; }
        public bool IsPostDated { get; set; }

        // Mapping Result
        public string? TargetEntity { get; set; }
        public Guid? TargetId { get; set; }
        public Guid? JournalEntryId { get; set; }
    }

    /// <summary>
    /// Ledger entry line within a voucher
    /// </summary>
    public class TallyLedgerEntryDto
    {
        public string LedgerName { get; set; } = string.Empty;
        public string? LedgerGuid { get; set; }
        public decimal Amount { get; set; }
        // Tally convention: Negative amount = Debit, Positive amount = Credit
        public bool IsDebit => Amount < 0;

        // GST breakdown
        public decimal? CgstAmount { get; set; }
        public decimal? SgstAmount { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CessAmount { get; set; }

        // TDS
        public decimal? TdsAmount { get; set; }
        public string? TdsSection { get; set; }
        public decimal? TdsRate { get; set; }

        // Cost allocations for this line
        public List<TallyCostAllocationDto> CostAllocations { get; set; } = new();

        // Bill allocations for this line
        public List<TallyBillAllocationDto> BillAllocations { get; set; } = new();
    }

    /// <summary>
    /// Inventory entry line within a voucher
    /// </summary>
    public class TallyInventoryEntryDto
    {
        public string StockItemName { get; set; } = string.Empty;
        public string? StockItemGuid { get; set; }

        // Quantity
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? ActualQuantity { get; set; }
        public decimal? BilledQuantity { get; set; }

        // Rates and Amount
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal? Discount { get; set; }
        public string? DiscountType { get; set; } // percentage or amount

        // Location
        public string? GodownName { get; set; }
        public string? GodownGuid { get; set; }

        // For stock transfers
        public string? DestinationGodownName { get; set; }
        public string? DestinationGodownGuid { get; set; }

        // GST
        public string? HsnCode { get; set; }
        public decimal? GstRate { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstAmount { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CessAmount { get; set; }

        // Batch allocations
        public List<TallyBatchAllocationDto> BatchAllocations { get; set; } = new();

        // Order reference
        public string? OrderNumber { get; set; }
        public DateOnly? OrderDate { get; set; }
    }

    /// <summary>
    /// Bill allocation for bill-wise tracking
    /// </summary>
    public class TallyBillAllocationDto
    {
        public string Name { get; set; } = string.Empty;
        public string BillType { get; set; } = string.Empty; // "New Ref", "Agst Ref", "Advance", "On Account"
        public decimal Amount { get; set; }
        public DateOnly? BillDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public string? BillCreditPeriod { get; set; }
    }

    /// <summary>
    /// Cost center allocation
    /// </summary>
    public class TallyCostAllocationDto
    {
        public string CostCenterName { get; set; } = string.Empty;
        public string? CostCenterGuid { get; set; }
        public string? CostCategoryName { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Batch allocation for batch-tracked items
    /// </summary>
    public class TallyBatchAllocationDto
    {
        public string BatchName { get; set; } = string.Empty;
        public string? BatchGuid { get; set; }
        public string? GodownName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public DateOnly? ManufacturingDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Summary of all parsed vouchers
    /// </summary>
    public class TallyVouchersSummaryDto
    {
        public List<TallyVoucherDto> Vouchers { get; set; } = new();

        // Counts by type
        public int SalesCount { get; set; }
        public int PurchaseCount { get; set; }
        public int ReceiptCount { get; set; }
        public int PaymentCount { get; set; }
        public int JournalCount { get; set; }
        public int ContraCount { get; set; }
        public int CreditNoteCount { get; set; }
        public int DebitNoteCount { get; set; }
        public int StockJournalCount { get; set; }
        public int PhysicalStockCount { get; set; }
        public int DeliveryNoteCount { get; set; }
        public int ReceiptNoteCount { get; set; }
        public int OtherCount { get; set; }

        // Totals
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalPurchaseAmount { get; set; }
        public decimal TotalReceiptAmount { get; set; }
        public decimal TotalPaymentAmount { get; set; }

        // Date range
        public DateOnly? MinDate { get; set; }
        public DateOnly? MaxDate { get; set; }

        // Count breakdown by type
        public Dictionary<string, int> CountsByVoucherType { get; set; } = new();
        public Dictionary<string, decimal> AmountsByVoucherType { get; set; } = new();
    }

    /// <summary>
    /// Complete parsed Tally data
    /// </summary>
    public class TallyParsedDataDto
    {
        public TallyMastersSummaryDto Masters { get; set; } = new();
        public TallyVouchersSummaryDto Vouchers { get; set; } = new();

        // File metadata
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string Format { get; set; } = "xml"; // xml or json
        public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
        public int ParseDurationMs { get; set; }

        // Validation results
        public List<TallyValidationIssueDto> ValidationIssues { get; set; } = new();
        public bool HasErrors => ValidationIssues.Any(v => v.Severity == "error");
        public bool HasWarnings => ValidationIssues.Any(v => v.Severity == "warning");
    }

    /// <summary>
    /// Validation issue found during parsing
    /// </summary>
    public class TallyValidationIssueDto
    {
        public string Severity { get; set; } = "warning"; // error, warning, info
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? RecordType { get; set; }
        public string? RecordName { get; set; }
        public string? RecordGuid { get; set; }
        public string? Field { get; set; }
        public string? ExpectedValue { get; set; }
        public string? ActualValue { get; set; }
    }
}
