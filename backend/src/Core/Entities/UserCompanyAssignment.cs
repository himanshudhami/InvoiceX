namespace Core.Entities
{
    /// <summary>
    /// Junction table for assigning specific companies to Admin/HR users (Company Admins).
    /// This enables granular multi-tenancy where Company Admins can manage specific companies only.
    /// </summary>
    public class UserCompanyAssignment
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The user who has access to this company
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The company the user has access to
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// The user's role within this specific company (Admin, HR, etc.)
        /// </summary>
        public string Role { get; set; } = "Admin";

        /// <summary>
        /// Marks the primary company for UI default selection
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
