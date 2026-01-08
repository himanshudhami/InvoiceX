using Application.DTOs.Migration;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Creates bank transaction records from Tally Payment vouchers for bank reconciliation.
    /// Single Responsibility: Map Tally voucher to BankTransaction entity.
    /// Separation of Concerns: Bank transaction creation is separate from business entity creation.
    /// </summary>
    public interface ITallyBankTransactionMapper
    {
        /// <summary>
        /// Creates a bank transaction record for a payment voucher.
        /// Should be called AFTER the business entity (vendor_payment, etc.) is created.
        /// </summary>
        /// <param name="batchId">Migration batch ID for rollback support</param>
        /// <param name="companyId">Company ID</param>
        /// <param name="voucher">The Tally payment voucher</param>
        /// <param name="matchedEntityType">Type of business entity: vendor_payments, contractor_payments, statutory_payments, journal_entries</param>
        /// <param name="matchedEntityId">ID of the created business entity (null if creation failed)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with created BankTransaction or error</returns>
        Task<Result<BankTransaction>> CreateBankTransactionAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            string matchedEntityType,
            Guid? matchedEntityId,
            CancellationToken cancellationToken = default);
    }
}
