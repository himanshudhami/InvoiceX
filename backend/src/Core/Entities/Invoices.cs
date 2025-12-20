namespace Core.Entities
{
    public class Invoices
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? CustomerId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public DateOnly DueDate { get; set; }
        public string? Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public string? Currency { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public string? PaymentInstructions { get; set; }
        public string? PoNumber { get; set; }
        public string? ProjectName { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ViewedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // GST Classification
        public string? InvoiceType { get; set; } = "export"; // export, domestic_b2b, domestic_b2c, sez, deemed_export
        public string? SupplyType { get; set; } // intra_state, inter_state, export
        public string? PlaceOfSupply { get; set; } // State code or 'export'
        public bool ReverseCharge { get; set; }

        // GST Totals
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalIgst { get; set; }
        public decimal TotalCess { get; set; }

        // E-invoicing fields
        public bool EInvoiceApplicable { get; set; }
        public string? EInvoiceIrn { get; set; }
        public string? EInvoiceAckNumber { get; set; }
        public DateTime? EInvoiceAckDate { get; set; }
        public string? EInvoiceQrCode { get; set; }
        public string? EInvoiceSignedJson { get; set; }
        public string? EInvoiceStatus { get; set; } = "not_applicable"; // not_applicable, pending, generated, cancelled, failed
        public DateTime? EInvoiceCancelDate { get; set; }
        public string? EInvoiceCancelReason { get; set; }

        // E-way bill
        public string? EwayBillNumber { get; set; }
        public DateTime? EwayBillDate { get; set; }
        public DateTime? EwayBillValidUntil { get; set; }

        // Export-specific fields
        public string? ExportType { get; set; } // EXPWP, EXPWOP
        public string? PortCode { get; set; }
        public string? ShippingBillNumber { get; set; }
        public DateOnly? ShippingBillDate { get; set; }
        public decimal ExportDuty { get; set; }
        public string? ForeignCurrency { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? ForeignCurrencyAmount { get; set; }

        // SEZ-specific fields
        public string? SezCategory { get; set; } // SEZWP, SEZWOP

        // B2C specific
        public bool B2cLarge { get; set; } // TRUE if B2C > 2.5L

        // Shipping details
        public string? ShippingAddress { get; set; }
        public string? TransporterName { get; set; }
        public string? VehicleNumber { get; set; }
    }
}