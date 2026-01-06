using Application.DTOs.Vendors;
using Core.Entities;
using Core.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for Vendors operations
    /// </summary>
    public interface IVendorsService
    {
        /// <summary>
        /// Get Vendors by ID
        /// </summary>
        Task<Result<Vendors>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all Vendors entities
        /// </summary>
        Task<Result<IEnumerable<Vendors>>> GetAllAsync();

        /// <summary>
        /// Get paginated Vendors entities with filtering and sorting
        /// </summary>
        Task<Result<(IEnumerable<Vendors> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        /// <summary>
        /// Get vendor by GSTIN
        /// </summary>
        Task<Result<Vendors>> GetByGstinAsync(Guid companyId, string gstin);

        /// <summary>
        /// Get vendor by PAN
        /// </summary>
        Task<Result<Vendors>> GetByPanAsync(Guid companyId, string panNumber);

        /// <summary>
        /// Get all MSME vendors for a company
        /// </summary>
        Task<Result<IEnumerable<Vendors>>> GetMsmeVendorsAsync(Guid companyId);

        /// <summary>
        /// Get all TDS-applicable vendors for a company
        /// </summary>
        Task<Result<IEnumerable<Vendors>>> GetTdsApplicableVendorsAsync(Guid companyId);

        /// <summary>
        /// Get vendor's outstanding balance
        /// </summary>
        Task<Result<decimal>> GetOutstandingBalanceAsync(Guid vendorId);

        /// <summary>
        /// Create a new Vendors
        /// </summary>
        Task<Result<Vendors>> CreateAsync(CreateVendorsDto dto);

        /// <summary>
        /// Update an existing Vendors
        /// </summary>
        Task<Result> UpdateAsync(Guid id, UpdateVendorsDto dto);

        /// <summary>
        /// Delete a Vendors by ID
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// Check if Vendors exists
        /// </summary>
        Task<Result<bool>> ExistsAsync(Guid id);
    }
}
