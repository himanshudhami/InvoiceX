namespace Application.DTOs.BankTransactions;

/// <summary>
/// Request DTO for searching reconciliation candidates with filters
/// </summary>
public class ReconciliationSearchRequest
{
    /// <summary>
    /// Company ID to search within
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Transaction type filter: "credit" or "debit"
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Free-text search across payee name, description, reference
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Minimum amount filter
    /// </summary>
    public decimal? AmountMin { get; set; }

    /// <summary>
    /// Maximum amount filter
    /// </summary>
    public decimal? AmountMax { get; set; }

    /// <summary>
    /// Start date filter
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// End date filter
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Filter by specific record types: salary, contractor, expense_claim, subscription, loan_payment, asset_maintenance
    /// </summary>
    public List<string>? RecordTypes { get; set; }

    /// <summary>
    /// Category filter (for expense claims)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether to include already reconciled records
    /// </summary>
    public bool IncludeReconciled { get; set; } = false;

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 20;
}
