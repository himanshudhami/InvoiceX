using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for vendor payment allocation operations
    /// </summary>
    public interface IVendorPaymentAllocationRepository
    {
        Task<VendorPaymentAllocation?> GetByIdAsync(Guid id);
        Task<VendorPaymentAllocation> AddAsync(VendorPaymentAllocation entity);
        Task<IEnumerable<VendorPaymentAllocation>> AddBulkAsync(IEnumerable<VendorPaymentAllocation> allocations);
        Task<IEnumerable<VendorPaymentAllocation>> GetByPaymentIdAsync(Guid vendorPaymentId);
        Task<IEnumerable<VendorPaymentAllocation>> GetByInvoiceIdAsync(Guid vendorInvoiceId);
        Task<decimal> GetTotalAllocatedForInvoiceAsync(Guid vendorInvoiceId);
        Task DeleteByPaymentIdAsync(Guid vendorPaymentId);
    }
}
