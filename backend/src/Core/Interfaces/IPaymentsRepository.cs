using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IPaymentsRepository
    {
        Task<Payments?> GetByIdAsync(Guid id);
        Task<IEnumerable<Payments>> GetAllAsync();
        Task<(IEnumerable<Payments> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Payments> AddAsync(Payments entity);
        Task UpdateAsync(Payments entity);
        Task DeleteAsync(Guid id);

        // New methods for enhanced payment tracking
        Task<IEnumerable<Payments>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<IEnumerable<Payments>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<Payments>> GetByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<Payments>> GetByFinancialYearAsync(string financialYear, Guid? companyId = null);

        // Income summary for financial reports
        Task<(decimal TotalGross, decimal TotalTds, decimal TotalNet, decimal TotalInr)> GetIncomeSummaryAsync(
            Guid? companyId = null,
            string? financialYear = null,
            int? year = null,
            int? month = null);

        // TDS summary for compliance reporting
        Task<IEnumerable<dynamic>> GetTdsSummaryAsync(Guid? companyId, string financialYear);

        Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId, string? reconciledBy);
        Task ClearReconciliationAsync(Guid id);

        // ==================== Tally Migration ====================

        /// <summary>
        /// Get payment by Tally voucher GUID
        /// </summary>
        Task<Payments?> GetByTallyGuidAsync(Guid companyId, string tallyVoucherGuid);
    }
}
