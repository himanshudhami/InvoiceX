using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for asset requests
    /// </summary>
    public interface IAssetRequestRepository
    {
        /// <summary>
        /// Gets an asset request by ID
        /// </summary>
        Task<AssetRequest?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all asset requests for a company
        /// </summary>
        Task<IEnumerable<AssetRequest>> GetByCompanyAsync(Guid companyId, string? status = null);

        /// <summary>
        /// Gets all asset requests by an employee
        /// </summary>
        Task<IEnumerable<AssetRequest>> GetByEmployeeAsync(Guid employeeId, string? status = null);

        /// <summary>
        /// Gets pending asset requests for a company (for admin/HR processing)
        /// </summary>
        Task<IEnumerable<AssetRequest>> GetPendingForCompanyAsync(Guid companyId);

        /// <summary>
        /// Gets approved but unfulfilled requests for a company
        /// </summary>
        Task<IEnumerable<AssetRequest>> GetApprovedUnfulfilledAsync(Guid companyId);

        /// <summary>
        /// Gets paged asset requests with filtering and sorting
        /// </summary>
        Task<(IEnumerable<AssetRequest> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            Guid? employeeId = null,
            string? status = null,
            string? category = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false);

        /// <summary>
        /// Adds a new asset request
        /// </summary>
        Task<AssetRequest> AddAsync(AssetRequest request);

        /// <summary>
        /// Updates an existing asset request
        /// </summary>
        Task UpdateAsync(AssetRequest request);

        /// <summary>
        /// Deletes an asset request
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Updates the status of an asset request
        /// </summary>
        Task UpdateStatusAsync(Guid id, string status, Guid? actionBy = null, string? reason = null);

        /// <summary>
        /// Marks an asset request as fulfilled
        /// </summary>
        Task FulfillAsync(Guid id, Guid fulfilledBy, Guid? assignedAssetId, string? notes);

        /// <summary>
        /// Gets request statistics for a company
        /// </summary>
        Task<AssetRequestStats> GetStatsAsync(Guid companyId);
    }

    /// <summary>
    /// Asset request statistics
    /// </summary>
    public class AssetRequestStats
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int FulfilledRequests { get; set; }
        public int UnfulfilledApproved { get; set; }
    }
}
