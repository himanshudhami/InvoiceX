using Application.DTOs.PaymentAllocations;
using Core.Entities;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for payment allocation operations
    /// </summary>
    public interface IPaymentAllocationService
    {
        // ==================== Basic CRUD ====================

        /// <summary>
        /// Get allocation by ID
        /// </summary>
        Task<Result<PaymentAllocation>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all allocations
        /// </summary>
        Task<Result<IEnumerable<PaymentAllocation>>> GetAllAsync();

        /// <summary>
        /// Get paginated allocations with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<PaymentAllocation> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Create a new allocation
        /// </summary>
        Task<Result<PaymentAllocation>> CreateAsync(CreatePaymentAllocationDto dto);

        /// <summary>
        /// Update an existing allocation
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdatePaymentAllocationDto dto);

        /// <summary>
        /// Delete an allocation
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        // ==================== Query Operations ====================

        /// <summary>
        /// Get all allocations for a payment
        /// </summary>
        Task<Result<IEnumerable<PaymentAllocation>>> GetByPaymentIdAsync(Guid paymentId);

        /// <summary>
        /// Get all allocations for an invoice
        /// </summary>
        Task<Result<IEnumerable<PaymentAllocation>>> GetByInvoiceIdAsync(Guid invoiceId);

        /// <summary>
        /// Get all allocations for a company
        /// </summary>
        Task<Result<IEnumerable<PaymentAllocation>>> GetByCompanyIdAsync(Guid companyId);

        // ==================== Allocation Operations ====================

        /// <summary>
        /// Allocate a payment to one or more invoices
        /// </summary>
        Task<Result<IEnumerable<PaymentAllocation>>> AllocatePaymentAsync(BulkAllocationDto dto);

        /// <summary>
        /// Get unallocated amount for a payment
        /// </summary>
        Task<Result<decimal>> GetUnallocatedAmountAsync(Guid paymentId);

        /// <summary>
        /// Get payment allocation summary (with all allocations)
        /// </summary>
        Task<Result<PaymentAllocationSummaryDto>> GetPaymentAllocationSummaryAsync(Guid paymentId);

        // ==================== Invoice Status ====================

        /// <summary>
        /// Get payment status for an invoice
        /// </summary>
        Task<Result<InvoicePaymentStatusDto>> GetInvoicePaymentStatusAsync(Guid invoiceId);

        /// <summary>
        /// Get payment status for all invoices of a company
        /// </summary>
        Task<Result<IEnumerable<InvoicePaymentStatusDto>>> GetCompanyInvoicePaymentStatusAsync(
            Guid companyId,
            string? financialYear = null);

        /// <summary>
        /// Get unpaid or partially paid invoices for a customer (for allocation suggestions)
        /// </summary>
        Task<Result<IEnumerable<InvoicePaymentStatusDto>>> GetUnpaidInvoicesForCustomerAsync(Guid customerId);

        // ==================== Bulk Operations ====================

        /// <summary>
        /// Remove all allocations for a payment
        /// </summary>
        Task<Result> RemoveAllAllocationsAsync(Guid paymentId);
    }
}
