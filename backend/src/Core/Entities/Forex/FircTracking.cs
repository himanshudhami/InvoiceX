namespace Core.Entities.Forex
{
    /// <summary>
    /// Foreign Inward Remittance Certificate tracking for FEMA/RBI compliance
    /// </summary>
    public class FircTracking
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // FIRC details
        public string? FircNumber { get; set; }
        public DateOnly? FircDate { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string? BankBranch { get; set; }
        public string? BankSwiftCode { get; set; }

        // Remittance details
        public string PurposeCode { get; set; } = string.Empty;  // P0802 for software
        public string ForeignCurrency { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public decimal InrAmount { get; set; }
        public decimal ExchangeRate { get; set; }

        // Remitter details
        public string? RemitterName { get; set; }
        public string? RemitterCountry { get; set; }
        public string? RemitterBank { get; set; }

        // Beneficiary
        public string BeneficiaryName { get; set; } = string.Empty;
        public string? BeneficiaryAccount { get; set; }

        // Linked payment
        public Guid? PaymentId { get; set; }

        // EDPMS compliance
        public bool EdpmsReported { get; set; }
        public DateOnly? EdpmsReportDate { get; set; }
        public string? EdpmsReference { get; set; }

        // Status
        public string Status { get; set; } = "received";  // received, linked, reconciled

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Companies? Company { get; set; }
        public Payments? Payment { get; set; }
        public ICollection<FircInvoiceLink>? InvoiceLinks { get; set; }
    }

    /// <summary>
    /// Links FIRC to invoices (one FIRC can cover multiple invoices)
    /// </summary>
    public class FircInvoiceLink
    {
        public Guid Id { get; set; }
        public Guid FircId { get; set; }
        public Guid InvoiceId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal? AllocatedAmountInr { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public FircTracking? Firc { get; set; }
        public Invoices? Invoice { get; set; }
    }
}
