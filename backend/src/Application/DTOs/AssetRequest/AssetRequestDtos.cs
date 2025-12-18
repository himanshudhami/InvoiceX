namespace Application.DTOs.AssetRequest
{
    /// <summary>
    /// Asset request summary for list views
    /// </summary>
    public class AssetRequestSummaryDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? FulfilledAt { get; set; }
    }

    /// <summary>
    /// Asset request detail
    /// </summary>
    public class AssetRequestDetailDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Justification { get; set; }
        public string? Specifications { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public DateTime? RequestedByDate { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Approval info
        public Guid? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        // Fulfillment info
        public Guid? AssignedAssetId { get; set; }
        public string? AssignedAssetName { get; set; }
        public Guid? FulfilledBy { get; set; }
        public string? FulfilledByName { get; set; }
        public DateTime? FulfilledAt { get; set; }
        public string? FulfillmentNotes { get; set; }

        // Computed
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public bool CanFulfill { get; set; }

        // Approval workflow info
        public Guid? ApprovalRequestId { get; set; }
        public bool HasApprovalWorkflow { get; set; }
        public int? CurrentApprovalStep { get; set; }
        public int? TotalApprovalSteps { get; set; }
    }

    /// <summary>
    /// Create asset request
    /// </summary>
    public class CreateAssetRequestDto
    {
        public string AssetType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Justification { get; set; }
        public string? Specifications { get; set; }
        public string Priority { get; set; } = "normal";
        public int Quantity { get; set; } = 1;
        public decimal? EstimatedBudget { get; set; }
        public DateTime? RequestedByDate { get; set; }
    }

    /// <summary>
    /// Update asset request
    /// </summary>
    public class UpdateAssetRequestDto
    {
        public string? AssetType { get; set; }
        public string? Category { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Justification { get; set; }
        public string? Specifications { get; set; }
        public string? Priority { get; set; }
        public int? Quantity { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public DateTime? RequestedByDate { get; set; }
    }

    /// <summary>
    /// Approve asset request
    /// </summary>
    public class ApproveAssetRequestDto
    {
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Reject asset request
    /// </summary>
    public class RejectAssetRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cancel asset request
    /// </summary>
    public class CancelAssetRequestDto
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Fulfill asset request
    /// </summary>
    public class FulfillAssetRequestDto
    {
        public Guid? AssignedAssetId { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Asset request statistics
    /// </summary>
    public class AssetRequestStatsDto
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int FulfilledRequests { get; set; }
        public int UnfulfilledApproved { get; set; }
    }
}
