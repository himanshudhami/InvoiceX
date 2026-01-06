using Core.Entities;

namespace Core.Interfaces
{
    public interface IVendorPaymentsRepository
    {
        Task<VendorPayment?> GetByIdAsync(Guid id);
        Task<VendorPayment?> GetByIdWithAllocationsAsync(Guid id);
        Task<IEnumerable<VendorPayment>> GetAllAsync();
        Task<IEnumerable<VendorPayment>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<VendorPayment>> GetByVendorIdAsync(Guid vendorId);
        Task<(IEnumerable<VendorPayment> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        // Status-based queries
        Task<IEnumerable<VendorPayment>> GetPendingApprovalAsync(Guid companyId);
        Task<IEnumerable<VendorPayment>> GetUnreconciledAsync(Guid companyId);

        // TDS queries
        Task<IEnumerable<VendorPayment>> GetTdsPaymentsAsync(Guid companyId, string financialYear);
        Task<IEnumerable<VendorPayment>> GetPendingTdsDepositAsync(Guid companyId);

        // Tally migration
        Task<VendorPayment?> GetByTallyGuidAsync(string tallyVoucherGuid);

        // CRUD
        Task<VendorPayment> AddAsync(VendorPayment entity);
        Task UpdateAsync(VendorPayment entity);
        Task DeleteAsync(Guid id);

        // Status updates
        Task UpdateStatusAsync(Guid id, string status);
        Task MarkAsPostedAsync(Guid id, Guid journalId);
        Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId);
        Task MarkTdsDepositedAsync(Guid id, string challanNumber, DateOnly depositDate);

        // Allocations
        Task<IEnumerable<VendorPaymentAllocation>> GetAllocationsAsync(Guid vendorPaymentId);
        Task<IEnumerable<VendorPaymentAllocation>> GetAllocationsByInvoiceAsync(Guid vendorInvoiceId);
        Task<VendorPaymentAllocation> AddAllocationAsync(VendorPaymentAllocation allocation);
        Task UpdateAllocationAsync(VendorPaymentAllocation allocation);
        Task DeleteAllocationAsync(Guid allocationId);
        Task DeleteAllocationsByPaymentIdAsync(Guid vendorPaymentId);

        // Reports
        Task<decimal> GetTotalPaidToVendorAsync(Guid vendorId, DateOnly? fromDate = null, DateOnly? toDate = null);
        Task<decimal> GetTotalTdsDeductedAsync(Guid companyId, string financialYear);

        // Bulk operations
        Task<IEnumerable<VendorPayment>> BulkAddAsync(IEnumerable<VendorPayment> entities);
    }
}
