using Core.Abstractions;

namespace Core.Entities
{
    /// <summary>
    /// Asset request submitted by an employee for IT equipment, furniture, etc.
    /// Implements IApprovableActivity to participate in the generic approval workflow.
    /// </summary>
    public class AssetRequest : IApprovableActivity
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid EmployeeId { get; set; }

        /// <summary>
        /// Type of asset being requested (laptop, monitor, keyboard, desk, chair, etc.)
        /// </summary>
        public string AssetType { get; set; } = string.Empty;

        /// <summary>
        /// Category of asset (IT Equipment, Office Furniture, Peripherals, etc.)
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Brief title for the request
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of what is needed
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Business justification for the request
        /// </summary>
        public string? Justification { get; set; }

        /// <summary>
        /// Specific specifications or model preferences
        /// </summary>
        public string? Specifications { get; set; }

        /// <summary>
        /// Priority: low, normal, high, urgent
        /// </summary>
        public string Priority { get; set; } = "normal";

        /// <summary>
        /// Request status: pending, in_progress, approved, rejected, fulfilled, cancelled
        /// </summary>
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Quantity requested (default 1)
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Estimated budget (if known)
        /// </summary>
        public decimal? EstimatedBudget { get; set; }

        /// <summary>
        /// Requested delivery date
        /// </summary>
        public DateTime? RequestedByDate { get; set; }

        public DateTime RequestedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Approval tracking
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        // Fulfillment tracking (after approval)
        public Guid? AssignedAssetId { get; set; }
        public Guid? FulfilledBy { get; set; }
        public DateTime? FulfilledAt { get; set; }
        public string? FulfillmentNotes { get; set; }

        // Navigation properties
        public Employees? Employee { get; set; }
        public Employees? Approver { get; set; }
        public Employees? Fulfiller { get; set; }
        public Assets? AssignedAsset { get; set; }

        // Computed properties
        public bool CanEdit => Status == "pending";
        public bool CanCancel => Status == "pending" || Status == "approved";
        public bool CanFulfill => Status == "approved" && !FulfilledAt.HasValue;

        // ==================== IApprovableActivity Implementation ====================

        /// <summary>
        /// Activity type identifier for the approval workflow
        /// </summary>
        public string ActivityType => ActivityTypes.AssetRequest;

        /// <summary>
        /// The unique identifier for this asset request
        /// </summary>
        public Guid ActivityId => Id;

        /// <summary>
        /// The employee requesting the asset
        /// </summary>
        public Guid RequestorId => EmployeeId;

        /// <summary>
        /// Gets a display-friendly title for the asset request
        /// </summary>
        public string GetDisplayTitle()
        {
            var qty = Quantity > 1 ? $" x{Quantity}" : "";
            return $"{Title}{qty} ({Category})";
        }

        /// <summary>
        /// Called when the asset request is fully approved
        /// </summary>
        public Task OnApprovedAsync()
        {
            Status = AssetRequestStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the asset request is rejected
        /// </summary>
        public Task OnRejectedAsync(string reason, Guid rejectedBy)
        {
            Status = AssetRequestStatus.Rejected;
            RejectionReason = reason;
            ApprovedBy = rejectedBy;
            UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the asset request is cancelled
        /// </summary>
        public Task OnCancelledAsync()
        {
            Status = AssetRequestStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns context data for evaluating workflow step conditions
        /// </summary>
        public Dictionary<string, object> GetConditionContext()
        {
            return new Dictionary<string, object>
            {
                { "category", Category },
                { "asset_type", AssetType },
                { "priority", Priority },
                { "quantity", Quantity },
                { "estimated_budget", EstimatedBudget ?? 0 }
            };
        }
    }

    /// <summary>
    /// Asset request status constants
    /// </summary>
    public static class AssetRequestStatus
    {
        public const string Pending = "pending";
        public const string InProgress = "in_progress";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Fulfilled = "fulfilled";
        public const string Cancelled = "cancelled";
    }

    /// <summary>
    /// Asset request priority constants
    /// </summary>
    public static class AssetRequestPriority
    {
        public const string Low = "low";
        public const string Normal = "normal";
        public const string High = "high";
        public const string Urgent = "urgent";
    }

    /// <summary>
    /// Asset category constants
    /// </summary>
    public static class AssetCategory
    {
        public const string ITEquipment = "IT Equipment";
        public const string OfficeFurniture = "Office Furniture";
        public const string Peripherals = "Peripherals";
        public const string Software = "Software";
        public const string MobileDevices = "Mobile Devices";
        public const string Other = "Other";

        public static readonly string[] All = new[]
        {
            ITEquipment, OfficeFurniture, Peripherals, Software, MobileDevices, Other
        };
    }
}
