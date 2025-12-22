using Application.DTOs.BankTransactions;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// Service for importing bank statements
    /// </summary>
    public interface IBankStatementImportService
    {
        /// <summary>
        /// Import bank transactions from a batch
        /// </summary>
        Task<Result<ImportBankTransactionsResult>> ImportTransactionsAsync(ImportBankTransactionsRequest request);

        /// <summary>
        /// Get transactions by import batch ID
        /// </summary>
        Task<Result<IEnumerable<BankTransaction>>> GetByImportBatchIdAsync(Guid batchId);

        /// <summary>
        /// Delete all transactions from an import batch
        /// </summary>
        Task<Result> DeleteImportBatchAsync(Guid batchId);
    }
}
