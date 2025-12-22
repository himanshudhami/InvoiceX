namespace Application.DTOs.BankTransactions;

/// <summary>
/// DTO for debit (outgoing payment) reconciliation suggestions
/// </summary>
public class DebitReconciliationSuggestionDto
{
    /// <summary>
    /// The unique identifier of the source record
    /// </summary>
    public Guid RecordId { get; set; }

    /// <summary>
    /// Type of record: salary, contractor, expense_claim, subscription, loan_payment, asset_maintenance
    /// </summary>
    public string RecordType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the record type
    /// </summary>
    public string RecordTypeDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Date when the payment was made
    /// </summary>
    public DateOnly PaymentDate { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Name of the payee (employee, contractor, vendor, etc.)
    /// </summary>
    public string? PayeeName { get; set; }

    /// <summary>
    /// Description or purpose of the payment
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reference number or transaction ID
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Match confidence score (0-100)
    /// </summary>
    public int MatchScore { get; set; }

    /// <summary>
    /// Absolute difference between bank transaction amount and this payment
    /// </summary>
    public decimal AmountDifference { get; set; }

    /// <summary>
    /// TDS amount if applicable (Indian tax context)
    /// </summary>
    public decimal? TdsAmount { get; set; }

    /// <summary>
    /// TDS section (194C, 194J, etc.) if applicable
    /// </summary>
    public string? TdsSection { get; set; }

    /// <summary>
    /// Whether this payment is already reconciled to another bank transaction
    /// </summary>
    public bool IsReconciled { get; set; }

    /// <summary>
    /// Category of the expense (for expense claims)
    /// </summary>
    public string? Category { get; set; }
}
