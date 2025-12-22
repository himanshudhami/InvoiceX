using Application.DTOs.BankTransactions;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service for Bank Reconciliation Statement generation
    /// </summary>
    public interface IBrsService
    {
        /// <summary>
        /// Generate a Bank Reconciliation Statement (BRS)
        /// </summary>
        Task<Result<BankReconciliationStatementDto>> GenerateBrsAsync(Guid bankAccountId, DateOnly asOfDate);

        /// <summary>
        /// Generate an enhanced BRS with journal entry perspective for CA compliance.
        /// Includes:
        /// - Ledger balance calculated from journal entries
        /// - TDS summary by section
        /// - Audit metrics (unlinked JE count)
        /// - Difference type analysis
        /// </summary>
        Task<Result<EnhancedBrsReportDto>> GenerateEnhancedBrsAsync(
            Guid bankAccountId,
            DateOnly asOfDate,
            DateOnly? periodStart = null);
    }
}
