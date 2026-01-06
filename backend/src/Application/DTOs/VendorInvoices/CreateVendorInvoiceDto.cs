using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.VendorInvoices
{
    /// <summary>
    /// Data transfer object for creating Vendor Invoices
    /// </summary>
    public class CreateVendorInvoiceDto
    {
        public Guid? CompanyId { get; set; }

        [Required(ErrorMessage = "Vendor ID is required")]
        public Guid VendorId { get; set; }

        [Required(ErrorMessage = "Invoice number is required")]
        [StringLength(100, ErrorMessage = "Invoice number cannot exceed 100 characters")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Internal reference cannot exceed 100 characters")]
        public string? InternalReference { get; set; }

        [Required(ErrorMessage = "Invoice date is required")]
        public DateOnly InvoiceDate { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        public DateOnly DueDate { get; set; }

        public DateOnly? ReceivedDate { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string? Status { get; set; } = "draft";

        // Amounts
        public decimal Subtotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }

        [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters")]
        public string? Currency { get; set; } = "INR";

        public string? Notes { get; set; }

        [StringLength(50, ErrorMessage = "PO number cannot exceed 50 characters")]
        public string? PoNumber { get; set; }

        // GST Classification
        [StringLength(30, ErrorMessage = "Invoice type cannot exceed 30 characters")]
        public string? InvoiceType { get; set; } = "purchase_b2b";

        [StringLength(30, ErrorMessage = "Supply type cannot exceed 30 characters")]
        public string? SupplyType { get; set; }

        [StringLength(5, ErrorMessage = "Place of supply cannot exceed 5 characters")]
        public string? PlaceOfSupply { get; set; }

        public bool ReverseCharge { get; set; }
        public bool RcmApplicable { get; set; }

        // GST Totals
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalCess { get; set; }

        // ITC
        public bool ItcEligible { get; set; } = true;
        public decimal ItcClaimedAmount { get; set; }

        [StringLength(50, ErrorMessage = "ITC ineligible reason cannot exceed 50 characters")]
        public string? ItcIneligibleReason { get; set; }

        public bool MatchedWithGstr2B { get; set; }

        [StringLength(20, ErrorMessage = "GSTR-2B period cannot exceed 20 characters")]
        public string? Gstr2BPeriod { get; set; }

        // TDS
        public bool TdsApplicable { get; set; }

        [StringLength(10, ErrorMessage = "TDS section cannot exceed 10 characters")]
        public string? TdsSection { get; set; }

        public decimal? TdsRate { get; set; }
        public decimal? TdsAmount { get; set; }

        // Import fields
        [StringLength(50, ErrorMessage = "Bill of entry number cannot exceed 50 characters")]
        public string? BillOfEntryNumber { get; set; }

        public DateOnly? BillOfEntryDate { get; set; }

        [StringLength(10, ErrorMessage = "Port code cannot exceed 10 characters")]
        public string? PortCode { get; set; }

        public decimal? ForeignCurrencyAmount { get; set; }

        [StringLength(10, ErrorMessage = "Foreign currency cannot exceed 10 characters")]
        public string? ForeignCurrency { get; set; }

        public decimal? ExchangeRate { get; set; }

        // Default expense account
        public Guid? ExpenseAccountId { get; set; }

        // Items (optional - can be added separately)
        public List<CreateVendorInvoiceItemDto>? Items { get; set; }
    }

    /// <summary>
    /// Data transfer object for creating Vendor Invoice Items
    /// </summary>
    public class CreateVendorInvoiceItemDto
    {
        public Guid? ProductId { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? DiscountRate { get; set; }
        public decimal LineTotal { get; set; }
        public int SortOrder { get; set; }

        // HSN/SAC
        [StringLength(20, ErrorMessage = "HSN/SAC code cannot exceed 20 characters")]
        public string? HsnSacCode { get; set; }

        public bool IsService { get; set; }

        // GST breakdown
        public decimal? CgstRate { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstRate { get; set; }
        public decimal? SgstAmount { get; set; }
        public decimal? IgstRate { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CessRate { get; set; }
        public decimal? CessAmount { get; set; }

        // ITC
        public bool ItcEligible { get; set; } = true;

        [StringLength(30, ErrorMessage = "ITC category cannot exceed 30 characters")]
        public string? ItcCategory { get; set; }

        [StringLength(50, ErrorMessage = "ITC ineligible reason cannot exceed 50 characters")]
        public string? ItcIneligibleReason { get; set; }

        // Account mapping
        public Guid? ExpenseAccountId { get; set; }
        public Guid? CostCenterId { get; set; }
    }
}
