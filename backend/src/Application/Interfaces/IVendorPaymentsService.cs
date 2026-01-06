using Application.DTOs.VendorPayments;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Vendor Payments operations
    /// </summary>
    public interface IVendorPaymentsService
    {
        /// <summary>
        /// Get Vendor Payment by ID
        /// </summary>
        Task<Result<VendorPayment>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get Vendor Payment by ID with allocations
        /// </summary>
        Task<Result<VendorPayment>> GetByIdWithAllocationsAsync(Guid id);

        /// <summary>
        /// Get all Vendor Payments
        /// </summary>
        Task<Result<IEnumerable<VendorPayment>>> GetAllAsync();

        /// <summary>
        /// Get paginated Vendor Payments with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<VendorPayment> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Get pending approval payments
        /// </summary>
        Task<Result<IEnumerable<VendorPayment>>> GetPendingApprovalAsync(Guid companyId);

        /// <summary>
        /// Get unreconciled payments
        /// </summary>
        Task<Result<IEnumerable<VendorPayment>>> GetUnreconciledAsync(Guid companyId);

        /// <summary>
        /// Get TDS payments for a financial year
        /// </summary>
        Task<Result<IEnumerable<VendorPayment>>> GetTdsPaymentsAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get payments with pending TDS deposit
        /// </summary>
        Task<Result<IEnumerable<VendorPayment>>> GetPendingTdsDepositAsync(Guid companyId);

        /// <summary>
        /// Get total TDS deducted for a financial year
        /// </summary>
        Task<Result<decimal>> GetTotalTdsDeductedAsync(Guid companyId, string financialYear);

        /// <summary>
        /// Get total paid to a vendor
        /// </summary>
        Task<Result<decimal>> GetTotalPaidToVendorAsync(Guid vendorId, DateOnly? fromDate = null, DateOnly? toDate = null);

        /// <summary>
        /// Create a new Vendor Payment
        /// </summary>
        Task<Result<VendorPayment>> CreateAsync(CreateVendorPaymentDto dto);

        /// <summary>
        /// Update an existing Vendor Payment
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateVendorPaymentDto dto);

        /// <summary>
        /// Delete a Vendor Payment
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// Update payment status
        /// </summary>
        Task<Result> UpdateStatusAsync(Guid id, string status);

        /// <summary>
        /// Mark payment as posted to ledger
        /// </summary>
        Task<Result> MarkAsPostedAsync(Guid id, Guid journalId);

        /// <summary>
        /// Mark payment as reconciled with bank
        /// </summary>
        Task<Result> MarkAsReconciledAsync(Guid id, Guid bankTransactionId);

        /// <summary>
        /// Mark TDS as deposited
        /// </summary>
        Task<Result> MarkTdsDepositedAsync(Guid id, string challanNumber, DateOnly depositDate);

        /// <summary>
        /// Add allocation to payment
        /// </summary>
        Task<Result<VendorPaymentAllocation>> AddAllocationAsync(Guid paymentId, CreateVendorPaymentAllocationDto dto);

        /// <summary>
        /// Check if Vendor Payment exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}
