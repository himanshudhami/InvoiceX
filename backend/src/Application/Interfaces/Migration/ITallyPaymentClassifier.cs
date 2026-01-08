using Application.DTOs.Migration;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Classifies Tally Payment vouchers into specific payment types
    /// for routing to appropriate import handlers.
    /// </summary>
    public interface ITallyPaymentClassifier
    {
        /// <summary>
        /// Analyzes a payment voucher and determines its type based on
        /// party ledger group, narration patterns, and ledger entries.
        /// </summary>
        /// <param name="companyId">Company ID for party lookup</param>
        /// <param name="voucher">The Tally payment voucher to classify</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Classification result with type, target ledger info, and amount</returns>
        Task<TallyPaymentClassificationResult> ClassifyAsync(
            Guid companyId,
            TallyVoucherDto voucher,
            CancellationToken cancellationToken = default);
    }
}
