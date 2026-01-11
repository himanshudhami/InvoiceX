namespace Core.Entities.Migration
{
    /// <summary>
    /// Maps Tally ledger names to modern control accounts + party references.
    /// Enables Tally import/export without per-party COA entries.
    /// Part of COA Modernization (Task 16).
    /// </summary>
    public class TallyLedgerMapping
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // ==================== Tally Identifiers ====================

        /// <summary>
        /// Tally ledger name (e.g., "RK WORLDINFOCOM", "Customer ABC")
        /// </summary>
        public string TallyLedgerName { get; set; } = string.Empty;

        /// <summary>
        /// Tally GUID for exact matching during sync
        /// </summary>
        public string? TallyLedgerGuid { get; set; }

        /// <summary>
        /// Tally parent group (e.g., "Sundry Creditors", "Sundry Debtors")
        /// </summary>
        public string? TallyParentGroup { get; set; }

        // ==================== Modern System Mapping ====================

        /// <summary>
        /// Control account ID (e.g., Trade Payables 2100, Trade Receivables 1120)
        /// </summary>
        public Guid? ControlAccountId { get; set; }

        /// <summary>
        /// Party type: "vendor" or "customer"
        /// </summary>
        public string? PartyType { get; set; }

        /// <summary>
        /// Party ID in the parties table
        /// </summary>
        public Guid? PartyId { get; set; }

        /// <summary>
        /// Legacy COA ID for migration reference (if migrated from per-party COA)
        /// </summary>
        public Guid? LegacyCOAId { get; set; }

        // ==================== Opening Balance ====================

        /// <summary>
        /// Opening balance from Tally
        /// </summary>
        public decimal? OpeningBalance { get; set; }

        /// <summary>
        /// Opening balance date
        /// </summary>
        public DateOnly? OpeningBalanceDate { get; set; }

        // ==================== Status ====================

        /// <summary>
        /// Whether this mapping is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last sync timestamp
        /// </summary>
        public DateTime? LastSyncAt { get; set; }

        // ==================== Audit ====================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ==================== Navigation ====================

        public Companies? Company { get; set; }
        public Ledger.ChartOfAccount? ControlAccount { get; set; }
        public Party? Party { get; set; }
    }
}
