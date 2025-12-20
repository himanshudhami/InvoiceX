using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for payment allocation operations
    /// </summary>
    public interface IPaymentAllocationRepository
    {
        // ==================== Basic CRUD ====================

        Task<PaymentAllocation?> GetByIdAsync(Guid id);
        Task<IEnumerable<PaymentAllocation>> GetAllAsync();
        Task<(IEnumerable<PaymentAllocation> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<PaymentAllocation> AddAsync(PaymentAllocation entity);
        Task UpdateAsync(PaymentAllocation entity);
        Task DeleteAsync(Guid id);

        // ==================== Query by Related Entities ====================

        /// <summary>
        /// Get all allocations for a specific payment
        /// </summary>
        Task<IEnumerable<PaymentAllocation>> GetByPaymentIdAsync(Guid paymentId);

        /// <summary>
        /// Get all allocations for a specific invoice
        /// </summary>
        Task<IEnumerable<PaymentAllocation>> GetByInvoiceIdAsync(Guid invoiceId);

        /// <summary>
        /// Get all allocations for a company
        /// </summary>
        Task<IEnumerable<PaymentAllocation>> GetByCompanyIdAsync(Guid companyId);

        // ==================== Allocation Summary ====================

        /// <summary>
        /// Get total allocated amount for a payment
        /// </summary>
        Task<decimal> GetTotalAllocatedForPaymentAsync(Guid paymentId);

        /// <summary>
        /// Get total allocated amount for an invoice
        /// </summary>
        Task<decimal> GetTotalAllocatedForInvoiceAsync(Guid invoiceId);

        /// <summary>
        /// Get unallocated amount for a payment
        /// </summary>
        Task<decimal> GetUnallocatedAmountAsync(Guid paymentId);

        // ==================== Invoice Payment Status ====================

        /// <summary>
        /// Get payment status for an invoice (total paid, balance due, status)
        /// </summary>
        Task<(decimal TotalPaid, decimal BalanceDue, string Status)> GetInvoicePaymentStatusAsync(Guid invoiceId);

        /// <summary>
        /// Get all invoices with payment status for a company
        /// </summary>
        Task<IEnumerable<dynamic>> GetInvoicePaymentSummaryAsync(Guid companyId, string? financialYear = null);

        // ==================== Bulk Operations ====================

        /// <summary>
        /// Add multiple allocations in a transaction
        /// </summary>
        Task<IEnumerable<PaymentAllocation>> AddBulkAsync(IEnumerable<PaymentAllocation> allocations);

        /// <summary>
        /// Delete all allocations for a payment
        /// </summary>
        Task DeleteByPaymentIdAsync(Guid paymentId);
    }
}
