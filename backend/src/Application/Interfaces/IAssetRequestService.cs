using Application.DTOs.AssetRequest;
using Core.Common;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for asset request operations
    /// </summary>
    public interface IAssetRequestService
    {
        /// <summary>
        /// Gets an asset request by ID
        /// </summary>
        Task<Result<AssetRequestDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all asset requests for a company
        /// </summary>
        Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetByCompanyAsync(Guid companyId, string? status = null);

        /// <summary>
        /// Gets all asset requests by an employee
        /// </summary>
        Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetByEmployeeAsync(Guid employeeId, string? status = null);

        /// <summary>
        /// Gets pending asset requests for a company
        /// </summary>
        Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetPendingForCompanyAsync(Guid companyId);

        /// <summary>
        /// Gets approved but unfulfilled requests
        /// </summary>
        Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetApprovedUnfulfilledAsync(Guid companyId);

        /// <summary>
        /// Creates a new asset request
        /// </summary>
        Task<Result<AssetRequestDetailDto>> CreateAsync(Guid employeeId, Guid companyId, CreateAssetRequestDto dto);

        /// <summary>
        /// Updates an asset request
        /// </summary>
        Task<Result<AssetRequestDetailDto>> UpdateAsync(Guid employeeId, Guid requestId, UpdateAssetRequestDto dto);

        /// <summary>
        /// Approves an asset request (legacy - direct approval)
        /// </summary>
        Task<Result<AssetRequestDetailDto>> ApproveAsync(Guid requestId, Guid approvedBy, ApproveAssetRequestDto dto);

        /// <summary>
        /// Rejects an asset request
        /// </summary>
        Task<Result<AssetRequestDetailDto>> RejectAsync(Guid requestId, Guid rejectedBy, RejectAssetRequestDto dto);

        /// <summary>
        /// Cancels an asset request
        /// </summary>
        Task<Result<AssetRequestDetailDto>> CancelAsync(Guid requestId, Guid cancelledBy, CancelAssetRequestDto dto);

        /// <summary>
        /// Fulfills an asset request
        /// </summary>
        Task<Result<AssetRequestDetailDto>> FulfillAsync(Guid requestId, Guid fulfilledBy, FulfillAssetRequestDto dto);

        /// <summary>
        /// Withdraws an asset request (by employee)
        /// </summary>
        Task<Result> WithdrawAsync(Guid employeeId, Guid requestId, string? reason = null);

        /// <summary>
        /// Gets request statistics for a company
        /// </summary>
        Task<Result<AssetRequestStatsDto>> GetStatsAsync(Guid companyId);

        /// <summary>
        /// Deletes an asset request
        /// </summary>
        Task<Result> DeleteAsync(Guid requestId);

        /// <summary>
        /// Updates the status of an asset request (called by workflow approval system)
        /// </summary>
        /// <param name="requestId">The asset request ID</param>
        /// <param name="status">New status (approved/rejected)</param>
        /// <param name="reason">Optional rejection reason</param>
        Task<Result> UpdateStatusAsync(Guid requestId, string status, string? reason = null);
    }
}
