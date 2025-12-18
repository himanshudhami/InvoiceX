namespace Core.Abstractions
{
    /// <summary>
    /// Interface that entities must implement to participate in the approval workflow.
    /// This allows the approval engine to work with any activity type generically.
    /// </summary>
    public interface IApprovableActivity
    {
        /// <summary>
        /// The type identifier for this activity (e.g., "leave", "asset_request", "expense")
        /// Used to find the appropriate workflow template
        /// </summary>
        string ActivityType { get; }

        /// <summary>
        /// The unique identifier for this specific activity instance
        /// </summary>
        Guid ActivityId { get; }

        /// <summary>
        /// The employee who is requesting approval
        /// </summary>
        Guid RequestorId { get; }

        /// <summary>
        /// The company this activity belongs to
        /// </summary>
        Guid CompanyId { get; }

        /// <summary>
        /// A display-friendly title for this activity (shown in approval lists)
        /// </summary>
        string GetDisplayTitle();

        /// <summary>
        /// Called by the workflow engine when the activity is fully approved
        /// Implementation should update the entity's status and perform any post-approval logic
        /// </summary>
        Task OnApprovedAsync();

        /// <summary>
        /// Called by the workflow engine when the activity is rejected
        /// Implementation should update the entity's status and store the rejection reason
        /// </summary>
        Task OnRejectedAsync(string reason, Guid rejectedBy);

        /// <summary>
        /// Called when the activity is cancelled by the requestor
        /// Implementation should update the entity's status
        /// </summary>
        Task OnCancelledAsync();

        /// <summary>
        /// Returns context data that can be used to evaluate step conditions
        /// For example: {"total_days": 5, "leave_type": "sick", "amount": 10000}
        /// </summary>
        Dictionary<string, object> GetConditionContext();
    }

    /// <summary>
    /// Common activity types as constants
    /// </summary>
    public static class ActivityTypes
    {
        public const string Leave = "leave";
        public const string AssetRequest = "asset_request";
        public const string Expense = "expense";
        public const string Travel = "travel";
        public const string Timesheet = "timesheet";
        public const string Reimbursement = "reimbursement";
        public const string PurchaseOrder = "purchase_order";

        public static readonly string[] All = new[]
        {
            Leave,
            AssetRequest,
            Expense,
            Travel,
            Timesheet,
            Reimbursement,
            PurchaseOrder
        };
    }
}
