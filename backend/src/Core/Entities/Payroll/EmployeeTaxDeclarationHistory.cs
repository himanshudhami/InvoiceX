namespace Core.Entities.Payroll
{
    /// <summary>
    /// Audit trail entry for employee tax declaration changes
    /// </summary>
    public class EmployeeTaxDeclarationHistory
    {
        public Guid Id { get; set; }
        public Guid DeclarationId { get; set; }

        /// <summary>
        /// Type of action: created, updated, submitted, verified, rejected, locked, unlocked, revised
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// User who made the change
        /// </summary>
        public string ChangedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the change
        /// </summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// JSON snapshot of field values before the change
        /// </summary>
        public string? PreviousValues { get; set; }

        /// <summary>
        /// JSON snapshot of field values after the change
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// Reason for rejection (when action=rejected)
        /// </summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Additional comments from reviewer (when action=rejected)
        /// </summary>
        public string? RejectionComments { get; set; }

        /// <summary>
        /// IP address of the user making the change
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public EmployeeTaxDeclaration? Declaration { get; set; }
    }
}
