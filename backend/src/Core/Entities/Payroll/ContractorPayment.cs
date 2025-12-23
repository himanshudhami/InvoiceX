namespace Core.Entities.Payroll
{
    /// <summary>
    /// Simplified payment record for contractors (consulting/contract work)
    /// </summary>
    public class ContractorPayment
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Period ====================

        public int PaymentMonth { get; set; }
        public int PaymentYear { get; set; }

        // ==================== Reference ====================

        /// <summary>
        /// Contractor's invoice number
        /// </summary>
        public string? InvoiceNumber { get; set; }

        /// <summary>
        /// Contract/PO reference
        /// </summary>
        public string? ContractReference { get; set; }

        // ==================== Payment Details ====================

        /// <summary>
        /// Gross amount (before TDS)
        /// </summary>
        public decimal GrossAmount { get; set; }

        /// <summary>
        /// TDS section: '194C' (contractors 2%) or '194J' (professionals 10%)
        /// </summary>
        public string TdsSection { get; set; } = "194J";

        /// <summary>
        /// TDS rate (based on section and PAN availability)
        /// 194C: 2% (individuals), 1% (transporters)
        /// 194J: 10% (professionals)
        /// No PAN: 20% (higher rate if PAN not provided)
        /// </summary>
        public decimal TdsRate { get; set; } = 10.00m;

        /// <summary>
        /// TDS amount deducted
        /// </summary>
        public decimal TdsAmount { get; set; }

        /// <summary>
        /// Contractor's PAN number (required for Form 26Q)
        /// </summary>
        public string? ContractorPan { get; set; }

        /// <summary>
        /// Whether PAN has been verified against income tax records
        /// </summary>
        public bool PanVerified { get; set; }

        /// <summary>
        /// Other deductions (if any)
        /// </summary>
        public decimal OtherDeductions { get; set; }

        /// <summary>
        /// Net amount payable
        /// </summary>
        public decimal NetPayable { get; set; }

        // ==================== GST (if applicable) ====================

        public bool GstApplicable { get; set; } = false;
        public decimal GstRate { get; set; } = 18.00m;
        public decimal GstAmount { get; set; }

        /// <summary>
        /// Total invoice amount (Gross + GST)
        /// </summary>
        public decimal? TotalInvoiceAmount { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Status: pending, approved, paid, cancelled
        /// </summary>
        public string Status { get; set; } = "pending";

        // ==================== Payment Info ====================

        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }

        /// <summary>
        /// Bank account used for contractor payment
        /// </summary>
        public Guid? BankAccountId { get; set; }

        // ==================== Journal Entry Linkage ====================

        /// <summary>
        /// Journal entry created on approval (expense recognition)
        /// Dr. Professional Fees, Cr. TDS Payable + Contractor Payable
        /// </summary>
        public Guid? AccrualJournalEntryId { get; set; }

        /// <summary>
        /// Journal entry created on payment (liability settlement)
        /// Dr. Contractor Payable, Cr. Bank
        /// </summary>
        public Guid? DisbursementJournalEntryId { get; set; }

        // ==================== Notes ====================

        /// <summary>
        /// Description of work/services
        /// </summary>
        public string? Description { get; set; }

        public string? Remarks { get; set; }

        // ==================== Metadata ====================

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public Employees? Employee { get; set; }
        public Companies? Company { get; set; }
    }
}
