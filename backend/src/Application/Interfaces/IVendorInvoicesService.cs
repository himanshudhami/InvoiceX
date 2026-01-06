using Application.DTOs.VendorInvoices;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Vendor Invoices operations
    /// </summary>
    public interface IVendorInvoicesService
    {
        /// <summary>
        /// Get Vendor Invoice by ID
        /// </summary>
        Task<Result<VendorInvoice>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get Vendor Invoice by ID with items
        /// </summary>
        Task<Result<VendorInvoice>> GetByIdWithItemsAsync(Guid id);

        /// <summary>
        /// Get all Vendor Invoices
        /// </summary>
        Task<Result<IEnumerable<VendorInvoice>>> GetAllAsync();

        /// <summary>
        /// Get paginated Vendor Invoices with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<VendorInvoice> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Get pending approval invoices for a company
        /// </summary>
        Task<Result<IEnumerable<VendorInvoice>>> GetPendingApprovalAsync(Guid companyId);

        /// <summary>
        /// Get unpaid invoices for a company
        /// </summary>
        Task<Result<IEnumerable<VendorInvoice>>> GetUnpaidAsync(Guid companyId);

        /// <summary>
        /// Get overdue invoices for a company
        /// </summary>
        Task<Result<IEnumerable<VendorInvoice>>> GetOverdueAsync(Guid companyId);

        /// <summary>
        /// Get ITC eligible invoices for a company
        /// </summary>
        Task<Result<IEnumerable<VendorInvoice>>> GetItcEligibleAsync(Guid companyId);

        /// <summary>
        /// Get invoices not yet matched with GSTR-2B
        /// </summary>
        Task<Result<IEnumerable<VendorInvoice>>> GetUnmatchedWithGstr2BAsync(Guid companyId);

        /// <summary>
        /// Create a new Vendor Invoice
        /// </summary>
        Task<Result<VendorInvoice>> CreateAsync(CreateVendorInvoiceDto dto);

        /// <summary>
        /// Update an existing Vendor Invoice
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateVendorInvoiceDto dto);

        /// <summary>
        /// Delete a Vendor Invoice
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// Update invoice status
        /// </summary>
        Task<Result> UpdateStatusAsync(Guid id, string status);

        /// <summary>
        /// Approve an invoice
        /// </summary>
        Task<Result> ApproveAsync(Guid id, Guid approvedBy, string? notes = null);

        /// <summary>
        /// Mark invoice as posted to ledger
        /// </summary>
        Task<Result> MarkAsPostedAsync(Guid id, Guid journalId);

        /// <summary>
        /// Check if Vendor Invoice exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}
