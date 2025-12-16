namespace WebApi.DTOs
{
    public class BankTransactionFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true; // Default to newest first

        // Bank account filter
        public Guid? BankAccountId { get; set; }

        // Transaction details filters
        public string? TransactionType { get; set; } // credit, debit
        public string? Category { get; set; }

        // Reconciliation filters
        public bool? IsReconciled { get; set; }
        public string? ReconciledType { get; set; }

        // Import filters
        public string? ImportSource { get; set; }
        public Guid? ImportBatchId { get; set; }

        // Date filters
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public Dictionary<string, object> GetFilters()
        {
            var filters = new Dictionary<string, object>();

            if (BankAccountId.HasValue)
                filters.Add("bank_account_id", BankAccountId.Value);

            if (!string.IsNullOrWhiteSpace(TransactionType))
                filters.Add("transaction_type", TransactionType);

            if (!string.IsNullOrWhiteSpace(Category))
                filters.Add("category", Category);

            if (IsReconciled.HasValue)
                filters.Add("is_reconciled", IsReconciled.Value);

            if (!string.IsNullOrWhiteSpace(ReconciledType))
                filters.Add("reconciled_type", ReconciledType);

            if (!string.IsNullOrWhiteSpace(ImportSource))
                filters.Add("import_source", ImportSource);

            if (ImportBatchId.HasValue)
                filters.Add("import_batch_id", ImportBatchId.Value);

            return filters;
        }
    }
}
