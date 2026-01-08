using Application.DTOs.Migration;
using Core.Common;
using Core.Entities.Payroll;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Maps Tally Payment vouchers to ContractorPayment entities
    /// </summary>
    public interface ITallyContractorPaymentMapper
    {
        /// <summary>
        /// Maps a Tally payment voucher to a ContractorPayment and saves it
        /// </summary>
        /// <param name="batchId">Migration batch ID for rollback support</param>
        /// <param name="companyId">Company ID</param>
        /// <param name="voucher">The Tally payment voucher</param>
        /// <param name="classification">Classification result with target info</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with created ContractorPayment or error</returns>
        Task<Result<ContractorPayment>> MapAndSaveAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            TallyPaymentClassificationResult classification,
            CancellationToken cancellationToken = default);
    }
}
