using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Invoices
{
    /// <summary>
    /// Data transfer object for creating Invoices
    /// </summary>
    public class CreateInvoicesDto
    {
        /// <summary>
        /// CompanyId
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// PartyId
        /// </summary>
        public Guid? PartyId { get; set; }

        /// <summary>
        /// InvoiceNumber
        /// </summary>
        [Required(ErrorMessage = "InvoiceNumber is required")]
        [StringLength(255, ErrorMessage = "InvoiceNumber cannot exceed 255 characters")]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// InvoiceDate
        /// </summary>
        public DateOnly InvoiceDate { get; set; }

        /// <summary>
        /// DueDate
        /// </summary>
        public DateOnly DueDate { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [StringLength(255, ErrorMessage = "Status cannot exceed 255 characters")]
        public string? Status { get; set; }

        /// <summary>
        /// Subtotal
        /// </summary>
        [Required(ErrorMessage = "Subtotal is required")]
        public decimal Subtotal { get; set; }

        /// <summary>
        /// TaxAmount
        /// </summary>
        public decimal? TaxAmount { get; set; }

        /// <summary>
        /// DiscountAmount
        /// </summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// TotalAmount
        /// </summary>
        [Required(ErrorMessage = "TotalAmount is required")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// PaidAmount
        /// </summary>
        public decimal? PaidAmount { get; set; }

        /// <summary>
        /// Currency
        /// </summary>
        [StringLength(255, ErrorMessage = "Currency cannot exceed 255 characters")]
        public string? Currency { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        [StringLength(255, ErrorMessage = "Notes cannot exceed 255 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Terms
        /// </summary>
        [StringLength(255, ErrorMessage = "Terms cannot exceed 255 characters")]
        public string? Terms { get; set; }

        /// <summary>
        /// PaymentInstructions
        /// </summary>
        public string? PaymentInstructions { get; set; }

        /// <summary>
        /// PoNumber
        /// </summary>
        [StringLength(255, ErrorMessage = "PoNumber cannot exceed 255 characters")]
        public string? PoNumber { get; set; }

        /// <summary>
        /// ProjectName
        /// </summary>
        [StringLength(255, ErrorMessage = "ProjectName cannot exceed 255 characters")]
        public string? ProjectName { get; set; }

        /// <summary>
        /// SentAt
        /// </summary>
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// ViewedAt
        /// </summary>
        public DateTime? ViewedAt { get; set; }

        /// <summary>
        /// PaidAt
        /// </summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// CreatedAt
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// UpdatedAt
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // GST Classification

        /// <summary>
        /// Invoice type: export, domestic_b2b, domestic_b2c, sez, deemed_export
        /// </summary>
        [StringLength(50, ErrorMessage = "InvoiceType cannot exceed 50 characters")]
        public string? InvoiceType { get; set; } = "export";

        /// <summary>
        /// Supply type: intra_state, inter_state, export
        /// </summary>
        [StringLength(20, ErrorMessage = "SupplyType cannot exceed 20 characters")]
        public string? SupplyType { get; set; }

        /// <summary>
        /// Place of supply (state code or 'export')
        /// </summary>
        [StringLength(50, ErrorMessage = "PlaceOfSupply cannot exceed 50 characters")]
        public string? PlaceOfSupply { get; set; }

        /// <summary>
        /// Whether reverse charge mechanism applies
        /// </summary>
        public bool ReverseCharge { get; set; }

        /// <summary>
        /// Exchange rate to INR on invoice date (RBI reference rate)
        /// Only required for non-INR currency invoices
        /// </summary>
        public decimal? ExchangeRate { get; set; }

        // GST Totals

        /// <summary>
        /// Total CGST amount for the invoice
        /// </summary>
        public decimal TotalCgst { get; set; }

        /// <summary>
        /// Total SGST amount for the invoice
        /// </summary>
        public decimal TotalSgst { get; set; }

        /// <summary>
        /// Total IGST amount for the invoice
        /// </summary>
        public decimal TotalIgst { get; set; }

        /// <summary>
        /// Total Cess amount for the invoice
        /// </summary>
        public decimal TotalCess { get; set; }

        // E-invoicing fields

        /// <summary>
        /// Whether e-invoicing is applicable
        /// </summary>
        public bool EInvoiceApplicable { get; set; }

        /// <summary>
        /// Invoice Reference Number from e-invoice portal
        /// </summary>
        [StringLength(100, ErrorMessage = "EInvoiceIrn cannot exceed 100 characters")]
        public string? EInvoiceIrn { get; set; }

        /// <summary>
        /// Acknowledgement number from e-invoice portal
        /// </summary>
        [StringLength(100, ErrorMessage = "EInvoiceAckNumber cannot exceed 100 characters")]
        public string? EInvoiceAckNumber { get; set; }

        /// <summary>
        /// Acknowledgement date from e-invoice portal
        /// </summary>
        public DateTime? EInvoiceAckDate { get; set; }

        /// <summary>
        /// QR code data for e-invoice
        /// </summary>
        public string? EInvoiceQrCode { get; set; }

        // Shipping details

        /// <summary>
        /// Shipping/delivery address
        /// </summary>
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// Name of transporter
        /// </summary>
        [StringLength(255, ErrorMessage = "TransporterName cannot exceed 255 characters")]
        public string? TransporterName { get; set; }

        /// <summary>
        /// Vehicle number
        /// </summary>
        [StringLength(50, ErrorMessage = "VehicleNumber cannot exceed 50 characters")]
        public string? VehicleNumber { get; set; }

        /// <summary>
        /// E-way bill number
        /// </summary>
        [StringLength(50, ErrorMessage = "EwayBillNumber cannot exceed 50 characters")]
        public string? EwayBillNumber { get; set; }
    }
}
