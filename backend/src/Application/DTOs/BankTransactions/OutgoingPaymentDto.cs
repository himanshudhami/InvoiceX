namespace Application.DTOs.BankTransactions;

/// <summary>
/// DTO for unified outgoing payment view across all expense types
/// </summary>
public class OutgoingPaymentDto
{
    /// <summary>
    /// Unique identifier of the payment record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type: salary, contractor, expense_claim, subscription, loan_payment, asset_maintenance
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable type display name
    /// </summary>
    public string TypeDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Date of payment
    /// </summary>
    public DateOnly PaymentDate { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Name of the payee
    /// </summary>
    public string? PayeeName { get; set; }

    /// <summary>
    /// Description or purpose
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display name combining type, payee, and description
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Payment reference number
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Whether this payment is reconciled with a bank transaction
    /// </summary>
    public bool IsReconciled { get; set; }

    /// <summary>
    /// ID of the linked bank transaction (if reconciled)
    /// </summary>
    public Guid? BankTransactionId { get; set; }

    /// <summary>
    /// Date when reconciliation was done
    /// </summary>
    public DateTime? ReconciledAt { get; set; }

    /// <summary>
    /// TDS amount (for contractor payments)
    /// </summary>
    public decimal? TdsAmount { get; set; }

    /// <summary>
    /// TDS section (194C, 194J, etc.)
    /// </summary>
    public string? TdsSection { get; set; }

    /// <summary>
    /// Category (for expense claims)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Status of the original payment record
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>
/// Summary of outgoing payments for dashboard
/// </summary>
public class OutgoingPaymentsSummaryDto
{
    /// <summary>
    /// Total number of outgoing payments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of reconciled payments
    /// </summary>
    public int ReconciledCount { get; set; }

    /// <summary>
    /// Number of unreconciled payments
    /// </summary>
    public int UnreconciledCount { get; set; }

    /// <summary>
    /// Total amount of all payments
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Total amount of reconciled payments
    /// </summary>
    public decimal ReconciledAmount { get; set; }

    /// <summary>
    /// Total amount of unreconciled payments
    /// </summary>
    public decimal UnreconciledAmount { get; set; }

    /// <summary>
    /// Breakdown by payment type
    /// </summary>
    public Dictionary<string, OutgoingPaymentTypeBreakdown> ByType { get; set; } = new();
}

/// <summary>
/// Breakdown of payments by type
/// </summary>
public class OutgoingPaymentTypeBreakdown
{
    /// <summary>
    /// Display name of the type
    /// </summary>
    public string TypeDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Count of payments
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Count of reconciled payments
    /// </summary>
    public int ReconciledCount { get; set; }

    /// <summary>
    /// Count of unreconciled payments
    /// </summary>
    public int UnreconciledCount { get; set; }
}
