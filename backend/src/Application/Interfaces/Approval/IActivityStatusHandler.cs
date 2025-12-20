using Core.Common;

namespace Application.Interfaces.Approval
{
    /// <summary>
    /// Interface for handling activity status updates when approval workflow completes.
    /// Each activity type (leave, asset_request, expense, etc.) implements its own handler.
    /// This allows the workflow engine to remain generic and extensible.
    /// </summary>
    public interface IActivityStatusHandler
    {
        /// <summary>
        /// The activity type this handler is responsible for (e.g., "leave", "asset_request")
        /// </summary>
        string ActivityType { get; }

        /// <summary>
        /// Called when the approval workflow is fully approved
        /// </summary>
        /// <param name="activityId">The ID of the activity (e.g., leave application ID, asset request ID)</param>
        /// <param name="approvedBy">The ID of the final approver</param>
        Task<Result> OnApprovedAsync(Guid activityId, Guid approvedBy);

        /// <summary>
        /// Called when the approval workflow is rejected
        /// </summary>
        /// <param name="activityId">The ID of the activity</param>
        /// <param name="rejectedBy">The ID of the person who rejected</param>
        /// <param name="reason">The rejection reason</param>
        Task<Result> OnRejectedAsync(Guid activityId, Guid rejectedBy, string reason);

        /// <summary>
        /// Called when the activity is cancelled by the requestor
        /// </summary>
        /// <param name="activityId">The ID of the activity</param>
        /// <param name="cancelledBy">The ID of the person who cancelled</param>
        /// <param name="reason">Optional cancellation reason</param>
        Task<Result> OnCancelledAsync(Guid activityId, Guid cancelledBy, string? reason = null);
    }
}
