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
    }
}
