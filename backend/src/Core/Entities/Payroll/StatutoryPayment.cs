namespace Core.Entities.Payroll
{
    /// <summary>
    /// Represents a statutory payment (TDS, PF, ESI, PT challan)
    /// Used for tracking government remittances and reconciliation
    /// </summary>
    public class StatutoryPayment
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // Payment identification
        public string PaymentType { get; set; } = string.Empty; // TDS_192, PF, ESI, PT_KA, etc.
        public string? ReferenceNumber { get; set; } // Challan number, CRN, acknowledgment number

        // Period information
        public string FinancialYear { get; set; } = string.Empty; // Format: 2024-25
        public int PeriodMonth { get; set; } // 1-12 (April = 1 for Indian FY)
        public int PeriodYear { get; set; } // Calendar year of the period
        public string? Quarter { get; set; } // Q1, Q2, Q3, Q4 (for quarterly filings)

        // Amount details
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalAmount { get; set; }

        // Payment details
        public DateOnly? PaymentDate { get; set; }
        public string? PaymentMode { get; set; } // neft, rtgs, online, cheque, upi
        public string? BankName { get; set; }
        public Guid? BankAccountId { get; set; }
        public string? BankReference { get; set; } // UTR number, cheque number

        // TDS specific fields
        public string? BsrCode { get; set; } // Bank branch code for TDS
        public string? ReceiptNumber { get; set; } // CIN (Challan Identification Number)

        // PF specific fields
        public string? Trrn { get; set; } // TRRN for ECR filing

        // ESI specific fields
        public string? ChallanNumber { get; set; }

        // Status tracking
        public string Status { get; set; } = "pending"; // pending, paid, verified, filed, cancelled
        public DateOnly DueDate { get; set; }

        // Journal linkage
        public Guid? JournalEntryId { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public Guid? PaidBy { get; set; }
        public DateTime? PaidAt { get; set; }
        public Guid? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public Guid? FiledBy { get; set; }
        public DateTime? FiledAt { get; set; }
        public string? Notes { get; set; }

        // Calculated properties
        public bool IsOverdue => PaymentDate == null && DateOnly.FromDateTime(DateTime.Today) > DueDate;

        public int DaysOverdue => IsOverdue
            ? DateOnly.FromDateTime(DateTime.Today).DayNumber - DueDate.DayNumber
            : 0;

        public bool IsPaid => Status == "paid" || Status == "verified" || Status == "filed";
    }

    /// <summary>
    /// View model for pending statutory payments dashboard
    /// </summary>
    public class PendingStatutoryPaymentView
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string PaymentTypeName { get; set; } = string.Empty;
        public string PaymentCategory { get; set; } = string.Empty;
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public DateOnly DueDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty; // paid, overdue, due_soon, upcoming
        public int DaysOverdue { get; set; }
        public Guid? StatutoryPaymentId { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string? ChallanStatus { get; set; }
    }

    /// <summary>
    /// Allocation of statutory payment to payroll source
    /// </summary>
    public class StatutoryPaymentAllocation
    {
        public Guid Id { get; set; }
        public Guid StatutoryPaymentId { get; set; }
        public Guid? PayrollRunId { get; set; }
        public Guid? PayrollTransactionId { get; set; }
        public Guid? ContractorPaymentId { get; set; }
        public decimal AmountAllocated { get; set; }
        public string AllocationType { get; set; } = "both"; // employee, employer, both
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Lookup table for statutory payment types
    /// </summary>
    public class StatutoryPaymentType
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // tax, pf, esi, pt, lwf
        public int DueDay { get; set; }
        public int GracePeriodDays { get; set; }
        public string PenaltyType { get; set; } = string.Empty;
        public decimal PenaltyRate { get; set; }
        public string? FilingForm { get; set; }
        public string PaymentFrequency { get; set; } = "monthly";
        public string? PayableAccountCode { get; set; }
        public string? Remarks { get; set; }
    }
}
