using Application.DTOs.Approval;
using Core.Abstractions;
using Core.Common;

namespace Application.Interfaces.Approval
{
    /// <summary>
    /// Service interface for approval workflow operations
    /// </summary>
    public interface IApprovalWorkflowService
    {
        /// <summary>
        /// Starts a new approval workflow for an approvable activity
        /// </summary>
        Task<Result<ApprovalRequestDetailDto>> StartWorkflowAsync(IApprovableActivity activity);

        /// <summary>
        /// Gets pending approvals for a user
        /// </summary>
        Task<Result<IEnumerable<PendingApprovalDto>>> GetPendingApprovalsForUserAsync(Guid employeeId);

        /// <summary>
        /// Gets the count of pending approvals for a user
        /// </summary>
        Task<Result<int>> GetPendingApprovalsCountAsync(Guid employeeId);

        /// <summary>
        /// Approves the current step of an approval request
        /// </summary>
        Task<Result<ApprovalRequestDetailDto>> ApproveAsync(Guid requestId, Guid approverId, ApproveRequestDto dto);

        /// <summary>
        /// Rejects an approval request (terminates the workflow)
        /// </summary>
        Task<Result<ApprovalRequestDetailDto>> RejectAsync(Guid requestId, Guid approverId, RejectRequestDto dto);

        /// <summary>
        /// Cancels an approval request (by the requestor)
        /// </summary>
        Task<Result> CancelAsync(Guid requestId, Guid requestorId);

        /// <summary>
        /// Gets the approval request status with full step details
        /// </summary>
        Task<Result<ApprovalRequestDetailDto>> GetRequestStatusAsync(Guid requestId);

        /// <summary>
        /// Gets the approval status for a specific activity
        /// </summary>
        Task<Result<ApprovalRequestDetailDto?>> GetActivityApprovalStatusAsync(string activityType, Guid activityId);

        /// <summary>
        /// Gets all approval requests by a requestor
        /// </summary>
        Task<Result<IEnumerable<ApprovalRequestDto>>> GetRequestsByRequestorAsync(Guid requestorId, string? status = null);
    }
}
