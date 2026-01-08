namespace Application.DTOs.Migration
{
    /// <summary>
    /// Parsed Tally Ledger (Account) data
    /// </summary>
    public class TallyLedgerDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
        public string? Alias { get; set; }

        // Classification
        public string? LedgerGroup { get; set; }
        public bool IsBillWiseOn { get; set; }
        public bool IsRevenue { get; set; }
        public bool IsDeemed { get; set; }

        // Balance
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        // Contact Info
        public string? Address { get; set; }
        public string? StateName { get; set; }
        public string? CountryName { get; set; }
        public string? Pincode { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? MobileNumber { get; set; }

        // GST Info
        public string? Gstin { get; set; }
        public string? GstRegistrationType { get; set; }
        public string? StateCode { get; set; }
        public string? PartyGstin { get; set; }

        // Bank Info (for Bank Accounts)
        public string? BankAccountNumber { get; set; }
        public string? IfscCode { get; set; }
        public string? BankBranchName { get; set; }

        // PAN
        public string? PanNumber { get; set; }

        // Credit Info
        public decimal? CreditLimit { get; set; }
        public int? CreditDays { get; set; }

        // Mapping Result (set during import)
        public string? TargetEntity { get; set; }
        public Guid? TargetId { get; set; }
    }

    /// <summary>
    /// Parsed Tally Stock Group data
    /// </summary>
    public class TallyStockGroupDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
        public string? Alias { get; set; }
        public bool IsAddable { get; set; } = true;
        public string? BaseUnits { get; set; }
        public string? AdditionalUnits { get; set; }

        // Mapping Result
        public Guid? TargetId { get; set; }
    }

    /// <summary>
    /// Parsed Tally Stock Item data
    /// </summary>
    public class TallyStockItemDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
        public string? Alias { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }

        // Classification
        public string? StockGroup { get; set; }
        public string? Category { get; set; }

        // Units
        public string? BaseUnits { get; set; }
        public string? AdditionalUnits { get; set; }
        public decimal? Conversion { get; set; }

        // Opening Stock
        public decimal OpeningQuantity { get; set; }
        public decimal OpeningRate { get; set; }
        public decimal OpeningValue { get; set; }

        // Current Stock
        public decimal ClosingQuantity { get; set; }
        public decimal ClosingRate { get; set; }
        public decimal ClosingValue { get; set; }

        // GST Info
        public string? GstApplicable { get; set; }
        public string? HsnCode { get; set; }
        public string? SacCode { get; set; }
        public decimal? GstRate { get; set; }
        public decimal? IgstRate { get; set; }
        public decimal? CgstRate { get; set; }
        public decimal? SgstRate { get; set; }
        public decimal? CessRate { get; set; }

        // Batch/Tracking
        public bool IsBatchEnabled { get; set; }
        public bool IsPerishable { get; set; }
        public bool HasExpiryDate { get; set; }

        // Valuation
        public string? CostingMethod { get; set; } // FIFO, LIFO, Weighted Average

        // Rates
        public decimal? StandardCost { get; set; }
        public decimal? StandardPrice { get; set; }
        public decimal? ReorderLevel { get; set; }
        public decimal? MinimumOrderQty { get; set; }

        // Mapping Result
        public Guid? TargetId { get; set; }
    }

    /// <summary>
    /// Parsed Tally Godown (Warehouse) data
    /// </summary>
    public class TallyGodownDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
        public string? Address { get; set; }
        public bool IsInternal { get; set; } = true;
        public bool HasNoStock { get; set; }

        // Mapping Result
        public Guid? TargetId { get; set; }
    }

    /// <summary>
    /// Parsed Tally Unit of Measure data
    /// </summary>
    public class TallyUnitDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsSimpleUnit { get; set; } = true;
        public string? BaseUnits { get; set; }
        public string? AdditionalUnits { get; set; }
        public decimal? Conversion { get; set; }
        public int? DecimalPlaces { get; set; }

        // Mapping Result
        public Guid? TargetId { get; set; }
    }

    /// <summary>
    /// Parsed Tally Cost Center data
    /// </summary>
    public class TallyCostCenterDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
        public string? Category { get; set; }
        public string? RevenueItem { get; set; }
        public string? EmailId { get; set; }

        // Mapping Result
        public Guid? TargetId { get; set; }
        public string? TargetTagGroup { get; set; }
    }

    /// <summary>
    /// Parsed Tally Cost Category data
    /// </summary>
    public class TallyCostCategoryDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool AllocateRevenue { get; set; }
        public bool AllocateNonRevenue { get; set; }

        // Maps to tag_group
        public string? TargetTagGroup { get; set; }
    }

    /// <summary>
    /// Parsed Tally Currency data
    /// </summary>
    public class TallyCurrencyDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string? FormalName { get; set; }
        public string IsoCode { get; set; } = string.Empty;
        public int DecimalPlaces { get; set; } = 2;
        public decimal? ExchangeRate { get; set; }
    }

    /// <summary>
    /// Parsed Tally Voucher Type customization data
    /// </summary>
    public class TallyVoucherTypeDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Parent { get; set; }
        public string? NumberingMethod { get; set; }
        public bool UseForPoS { get; set; }
        public bool UseForJobwork { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Summary of all parsed masters
    /// </summary>
    public class TallyMastersSummaryDto
    {
        public List<TallyLedgerDto> Ledgers { get; set; } = new();
        public List<TallyStockGroupDto> StockGroups { get; set; } = new();
        public List<TallyStockItemDto> StockItems { get; set; } = new();
        public List<TallyGodownDto> Godowns { get; set; } = new();
        public List<TallyUnitDto> Units { get; set; } = new();
        public List<TallyCostCenterDto> CostCenters { get; set; } = new();
        public List<TallyCostCategoryDto> CostCategories { get; set; } = new();
        public List<TallyCurrencyDto> Currencies { get; set; } = new();
        public List<TallyVoucherTypeDto> VoucherTypes { get; set; } = new();

        // Summary counts by ledger group
        public Dictionary<string, int> LedgerCountsByGroup { get; set; } = new();

        // Company info extracted from file
        public string? TallyCompanyName { get; set; }
        public string? TallyCompanyGuid { get; set; }
        public string? FinancialYearFrom { get; set; }
        public string? FinancialYearTo { get; set; }
    }
}
