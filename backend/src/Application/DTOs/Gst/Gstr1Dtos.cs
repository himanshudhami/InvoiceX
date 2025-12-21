namespace Application.DTOs.Gst
{
    /// <summary>
    /// Complete GSTR-1 data for a return period
    /// </summary>
    public class Gstr1DataDto
    {
        public Guid CompanyId { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string ReturnPeriod { get; set; } = string.Empty;  // MMYYYY format
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string LegalName { get; set; } = string.Empty;

        // Tables
        public List<Gstr1B2bDto> B2b { get; set; } = new();       // Table 4
        public List<Gstr1B2clDto> B2cl { get; set; } = new();     // Table 5
        public List<Gstr1ExportDto> Exp { get; set; } = new();    // Table 6
        public List<Gstr1HsnSummaryDto> Hsn { get; set; } = new(); // Table 12
        public List<Gstr1DocIssuedDto> DocIssued { get; set; } = new(); // Table 13

        // Summaries
        public Gstr1TaxSummaryDto TaxSummary { get; set; } = new();
    }

    /// <summary>
    /// Table 4 - B2B Supplies (to registered persons)
    /// </summary>
    public class Gstr1B2bDto
    {
        public string ReceiverGstin { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public decimal InvoiceValue { get; set; }
        public string PlaceOfSupply { get; set; } = string.Empty;
        public bool ReverseCharge { get; set; }
        public string InvoiceType { get; set; } = "R";  // R=Regular, DE=Deemed Export, SEZ=SEZ
        public string? EcommerceGstin { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public string? Irn { get; set; }
        public string? IrnDate { get; set; }
    }

    /// <summary>
    /// Table 5 - B2C Large (to unregistered persons, interstate > 2.5L)
    /// </summary>
    public class Gstr1B2clDto
    {
        public string PlaceOfSupply { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public decimal InvoiceValue { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public string? EcommerceGstin { get; set; }
    }

    /// <summary>
    /// Table 6 - Exports
    /// </summary>
    public class Gstr1ExportDto
    {
        public string ExportType { get; set; } = string.Empty;  // WPAY = With Payment, WOPAY = Without Payment
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public decimal InvoiceValue { get; set; }
        public string? PortCode { get; set; }
        public string? ShippingBillNumber { get; set; }
        public DateOnly? ShippingBillDate { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }

        // Additional fields for tracking
        public Guid InvoiceId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerCountry { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal ForeignCurrencyValue { get; set; }
        public string? LutNumber { get; set; }
    }

    /// <summary>
    /// Table 12 - HSN-wise Summary
    /// </summary>
    public class Gstr1HsnSummaryDto
    {
        public string HsnCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Uqc { get; set; } = string.Empty;  // Unit Quantity Code
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal CessAmount { get; set; }
    }

    /// <summary>
    /// Table 13 - Document Issued Summary
    /// </summary>
    public class Gstr1DocIssuedDto
    {
        public string DocumentType { get; set; } = string.Empty;  // Invoices, Credit Notes, Debit Notes
        public string SrNoFrom { get; set; } = string.Empty;
        public string SrNoTo { get; set; } = string.Empty;
        public int TotalNumber { get; set; }
        public int Cancelled { get; set; }
        public int NetIssued => TotalNumber - Cancelled;
    }

    /// <summary>
    /// GSTR-1 filing record
    /// </summary>
    public class Gstr1FilingDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string ReturnPeriod { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";  // draft, generated, filed, amended
        public DateTime GeneratedAt { get; set; }
        public DateTime? FiledAt { get; set; }
        public string? Arn { get; set; }

        // Summary
        public int TotalB2bInvoices { get; set; }
        public int TotalExportInvoices { get; set; }
        public decimal TotalTaxableValue { get; set; }
        public decimal TotalTaxAmount { get; set; }
    }

    /// <summary>
    /// Tax summary for GSTR-1
    /// </summary>
    public class Gstr1TaxSummaryDto
    {
        // Outward taxable supplies (other than zero rated, nil rated, exempted)
        public decimal OutwardTaxableB2bValue { get; set; }
        public decimal OutwardTaxableB2cValue { get; set; }
        public decimal OutwardTaxableTotalValue { get; set; }

        // Zero rated supplies
        public decimal ZeroRatedExportsWithTaxValue { get; set; }
        public decimal ZeroRatedExportsWithTaxIgst { get; set; }
        public decimal ZeroRatedExportsWithoutTaxValue { get; set; }

        // Tax amounts
        public decimal TotalIgst { get; set; }
        public decimal TotalCgst { get; set; }
        public decimal TotalSgst { get; set; }
        public decimal TotalCess { get; set; }
        public decimal TotalTax => TotalIgst + TotalCgst + TotalSgst + TotalCess;
    }

    /// <summary>
    /// GSTR-1 validation result
    /// </summary>
    public class Gstr1ValidationResultDto
    {
        public bool IsValid { get; set; }
        public int TotalInvoices { get; set; }
        public int ValidInvoices { get; set; }
        public int InvalidInvoices { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<Gstr1InvoiceValidationDto> InvoiceValidations { get; set; } = new();
    }

    /// <summary>
    /// Individual invoice validation
    /// </summary>
    public class Gstr1InvoiceValidationDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Missing invoice in GSTR-1
    /// </summary>
    public class MissingInvoiceDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
