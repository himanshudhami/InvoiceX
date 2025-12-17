using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    /// <summary>
    /// Login request DTO
    /// </summary>
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Register a new user (admin creates employee accounts)
    /// </summary>
    public class RegisterUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Employee";

        [Required]
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Link to existing employee (optional for admin users)
        /// </summary>
        public Guid? EmployeeId { get; set; }
    }

    /// <summary>
    /// Token response after successful authentication
    /// </summary>
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = null!;
    }

    /// <summary>
    /// User information returned after authentication
    /// </summary>
    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public Guid? EmployeeId { get; set; }
    }

    /// <summary>
    /// Refresh token request
    /// </summary>
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Change password request
    /// </summary>
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Admin reset password for a user
    /// </summary>
    public class ResetPasswordDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    public class UpdateUserDto
    {
        public string? DisplayName { get; set; }
        public string? Role { get; set; }
        public Guid? EmployeeId { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// JWT configuration settings
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Access token expiration in minutes (default: 15)
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 15;

        /// <summary>
        /// Refresh token expiration in days (default: 7)
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }

    /// <summary>
    /// User roles
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string HR = "HR";
        public const string Accountant = "Accountant";
        public const string Manager = "Manager";
        public const string Employee = "Employee";

        public static readonly string[] All = { Admin, HR, Accountant, Manager, Employee };
        public static readonly string[] AdminRoles = { Admin, HR, Accountant };
    }
}
