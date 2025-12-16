using Application.DTOs.Payments;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Payments operations
    /// Enhanced for Indian tax compliance with TDS tracking and direct payments
    /// </summary>
    public interface IPaymentsService
    {
        /// <summary>
        /// Get Payments by ID
        /// </summary>
        Task<Result<Payments>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all Payments entities
        /// </summary>
        Task<Result<IEnumerable<Payments>>> GetAllAsync();

        /// <summary>
        /// Get paginated Payments entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Payments> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new Payments (invoice-linked or direct)
        /// </summary>
        Task<Result<Payments>> CreateAsync(CreatePaymentsDto dto);

        /// <summary>
        /// Update an existing Payments
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdatePaymentsDto dto);

        /// <summary>
        /// Delete a Payments by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// Check if Payments exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);

        // ==================== New Methods ====================

        /// <summary>
        /// Get payments by invoice ID
        /// </summary>
        Task<Result<IEnumerable<Payments>>> GetByInvoiceIdAsync(Guid invoiceId);

        /// <summary>
        /// Get payments by company ID
        /// </summary>
        Task<Result<IEnumerable<Payments>>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get payments by customer ID
        /// </summary>
        Task<Result<IEnumerable<Payments>>> GetByCustomerIdAsync(Guid customerId);

        /// <summary>
        /// Get payments for a specific financial year
        /// </summary>
        Task<Result<IEnumerable<Payments>>> GetByFinancialYearAsync(string financialYear, Guid? companyId = null);

        /// <summary>
        /// Get income summary for financial reports
        /// </summary>
        Task<Result<IncomeSummaryDto>> GetIncomeSummaryAsync(
            Guid? companyId = null,
            string? financialYear = null,
            int? year = null,
            int? month = null);

        /// <summary>
        /// Get TDS summary for compliance reporting
        /// </summary>
        Task<Result<IEnumerable<TdsSummaryDto>>> GetTdsSummaryAsync(Guid? companyId, string financialYear);
    }

    /// <summary>
    /// Income summary for financial reports
    /// </summary>
    public class IncomeSummaryDto
    {
        public decimal TotalGross { get; set; }
        public decimal TotalTds { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalInr { get; set; }
    }

    /// <summary>
    /// TDS summary for compliance reporting
    /// </summary>
    public class TdsSummaryDto
    {
        public string? CustomerName { get; set; }
        public string? CustomerPan { get; set; }
        public string? TdsSection { get; set; }
        public int PaymentCount { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalTds { get; set; }
        public decimal TotalNet { get; set; }
    }
}
