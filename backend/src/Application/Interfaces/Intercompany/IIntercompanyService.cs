using Core.Common;
using Core.Entities.Intercompany;

namespace Application.Interfaces.Intercompany
{
    /// <summary>
    /// Service for managing intercompany transactions and reconciliation
    /// </summary>
    public interface IIntercompanyService
    {
        /// <summary>
        /// Check if an invoice is an intercompany transaction
        /// </summary>
        Task<bool> IsIntercompanyTransactionAsync(Guid invoicingCompanyId, Guid customerId);

        /// <summary>
        /// Record an intercompany invoice transaction
        /// Creates entries in both companies' books
        /// </summary>
        Task<Result<IntercompanyTransaction>> RecordInvoiceAsync(
            Guid invoiceId,
            Guid invoicingCompanyId,
            Guid customerCompanyId,
            decimal amount,
            string currency,
            DateOnly invoiceDate,
            string invoiceNumber);

        /// <summary>
        /// Record an intercompany payment
        /// </summary>
        Task<Result<IntercompanyTransaction>> RecordPaymentAsync(
            Guid paymentId,
            Guid payingCompanyId,
            Guid receivingCompanyId,
            decimal amount,
            string currency,
            DateOnly paymentDate,
            string? referenceNumber);

        /// <summary>
        /// Get intercompany balance between two companies
        /// </summary>
        Task<Result<IntercompanyBalance>> GetBalanceAsync(Guid fromCompanyId, Guid toCompanyId);

        /// <summary>
        /// Get all intercompany balances for a company
        /// </summary>
        Task<Result<IEnumerable<IntercompanyBalance>>> GetAllBalancesForCompanyAsync(Guid companyId);

        /// <summary>
        /// Reconcile matching transactions between companies
        /// </summary>
        Task<Result> ReconcileTransactionsAsync(Guid transactionId, Guid counterpartyTransactionId, Guid reconciledBy);

        /// <summary>
        /// Auto-match unreconciled transactions
        /// </summary>
        Task<Result<int>> AutoReconcileAsync(Guid? companyId = null);

        /// <summary>
        /// Get unreconciled transactions for a company
        /// </summary>
        Task<Result<IEnumerable<IntercompanyTransaction>>> GetUnreconciledTransactionsAsync(Guid? companyId = null);

        /// <summary>
        /// Generate intercompany reconciliation report
        /// </summary>
        Task<Result<IntercompanyReconciliationReport>> GetReconciliationReportAsync(
            Guid companyId,
            DateOnly asOfDate);

        /// <summary>
        /// Get company group structure (parent/subsidiary hierarchy)
        /// </summary>
        Task<Result<IEnumerable<CompanyRelationship>>> GetGroupStructureAsync(Guid companyId);

        /// <summary>
        /// Add a company relationship
        /// </summary>
        Task<Result<CompanyRelationship>> AddRelationshipAsync(
            Guid parentCompanyId,
            Guid childCompanyId,
            string relationshipType,
            decimal ownershipPercentage,
            string consolidationMethod);
    }

    /// <summary>
    /// Intercompany reconciliation report
    /// </summary>
    public class IntercompanyReconciliationReport
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateOnly AsOfDate { get; set; }
        public IEnumerable<IntercompanyBalanceSummary> Balances { get; set; } = new List<IntercompanyBalanceSummary>();
        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }
        public decimal NetPosition { get; set; }
        public int UnreconciledCount { get; set; }
        public decimal UnreconciledAmount { get; set; }
    }

    public class IntercompanyBalanceSummary
    {
        public Guid CounterpartyId { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;
        public decimal OurBalance { get; set; }
        public decimal TheirBalance { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; } = string.Empty; // "Matched", "Unmatched"
        public int TransactionCount { get; set; }
        public DateOnly? LastTransactionDate { get; set; }
    }
}
