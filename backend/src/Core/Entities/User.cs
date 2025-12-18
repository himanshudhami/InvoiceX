namespace Core.Entities
{
    /// <summary>
    /// Represents a user account for authentication
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Email used for login (unique)
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt hashed password
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User role: Admin, HR, Accountant, Manager, Employee
        /// </summary>
        public string Role { get; set; } = "Employee";

        /// <summary>
        /// Company the user belongs to (required for tenant isolation).
        /// For regular employees, this is their assigned company.
        /// For Company Admins, this may be their "home" company but they access assigned companies via UserCompanyAssignments.
        /// </summary>
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Link to employee record if user is an employee (nullable for admin-only users)
        /// </summary>
        public Guid? EmployeeId { get; set; }

        /// <summary>
        /// Super Admin has access to ALL companies. Takes precedence over company assignments.
        /// </summary>
        public bool IsSuperAdmin { get; set; } = false;

        /// <summary>
        /// Whether the account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Number of failed login attempts (for lockout)
        /// </summary>
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>
        /// Account locked until this time (if locked)
        /// </summary>
        public DateTime? LockoutEndAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
