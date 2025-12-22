using Application.DTOs.BankTransactions;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service for managing outgoing payments view
    /// </summary>
    public interface IOutgoingPaymentsService
    {
        /// <summary>
        /// Get paginated list of outgoing payments
        /// </summary>
        Task<Result<(IEnumerable<OutgoingPaymentDto> Items, int TotalCount)>> GetOutgoingPaymentsAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 20,
            bool? reconciled = null,
            List<string>? types = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);

        /// <summary>
        /// Get summary of outgoing payments
        /// </summary>
        Task<Result<OutgoingPaymentsSummaryDto>> GetOutgoingPaymentsSummaryAsync(
            Guid companyId,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);
    }
}
