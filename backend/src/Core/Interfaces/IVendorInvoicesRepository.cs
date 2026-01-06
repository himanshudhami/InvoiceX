using Core.Entities;

namespace Core.Interfaces
{
    public interface IVendorInvoicesRepository
    {
        Task<VendorInvoice?> GetByIdAsync(Guid id);
        Task<VendorInvoice?> GetByIdWithItemsAsync(Guid id);
        Task<IEnumerable<VendorInvoice>> GetAllAsync();
        Task<IEnumerable<VendorInvoice>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<VendorInvoice>> GetByVendorIdAsync(Guid vendorId);
        Task<(IEnumerable<VendorInvoice> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        // Status-based queries
        Task<IEnumerable<VendorInvoice>> GetPendingApprovalAsync(Guid companyId);
        Task<IEnumerable<VendorInvoice>> GetUnpaidAsync(Guid companyId);
        Task<IEnumerable<VendorInvoice>> GetOverdueAsync(Guid companyId);

        // ITC/GST queries
        Task<IEnumerable<VendorInvoice>> GetItcEligibleAsync(Guid companyId);
        Task<IEnumerable<VendorInvoice>> GetUnmatchedWithGstr2BAsync(Guid companyId);

        // Tally migration
        Task<VendorInvoice?> GetByTallyGuidAsync(string tallyVoucherGuid);

        // CRUD
        Task<VendorInvoice> AddAsync(VendorInvoice entity);
        Task UpdateAsync(VendorInvoice entity);
        Task DeleteAsync(Guid id);

        // Status updates
        Task UpdateStatusAsync(Guid id, string status);
        Task MarkAsPostedAsync(Guid id, Guid journalId);

        // Items
        Task<IEnumerable<VendorInvoiceItem>> GetItemsAsync(Guid vendorInvoiceId);
        Task<VendorInvoiceItem> AddItemAsync(VendorInvoiceItem item);
        Task UpdateItemAsync(VendorInvoiceItem item);
        Task DeleteItemAsync(Guid itemId);
        Task DeleteItemsByInvoiceIdAsync(Guid vendorInvoiceId);

        // Bulk operations
        Task<IEnumerable<VendorInvoice>> BulkAddAsync(IEnumerable<VendorInvoice> entities);
    }
}
