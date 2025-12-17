using Application.DTOs.Auth;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// Service interface for authentication operations
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        Task<Result<TokenResponseDto>> LoginAsync(LoginDto dto, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Register a new user (admin operation)
        /// </summary>
        Task<Result<UserInfoDto>> RegisterAsync(RegisterUserDto dto, Guid createdBy);

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        Task<Result<TokenResponseDto>> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Revoke a refresh token (logout)
        /// </summary>
        Task<Result> RevokeTokenAsync(string refreshToken, string? reason = null);

        /// <summary>
        /// Revoke all refresh tokens for a user (logout from all devices)
        /// </summary>
        Task<Result> RevokeAllTokensAsync(Guid userId, string? reason = null);

        /// <summary>
        /// Change password for the current user
        /// </summary>
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

        /// <summary>
        /// Reset password for a user (admin operation)
        /// </summary>
        Task<Result> ResetPasswordAsync(ResetPasswordDto dto, Guid resetBy);

        /// <summary>
        /// Get user by ID
        /// </summary>
        Task<Result<UserInfoDto>> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Get all users for a company (admin operation)
        /// </summary>
        Task<Result<(IEnumerable<UserInfoDto> Items, int TotalCount)>> GetUsersAsync(
            Guid companyId, int pageNumber, int pageSize, string? searchTerm = null, string? role = null);

        /// <summary>
        /// Update user details (admin operation)
        /// </summary>
        Task<Result<UserInfoDto>> UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid updatedBy);

        /// <summary>
        /// Deactivate user account
        /// </summary>
        Task<Result> DeactivateUserAsync(Guid userId);

        /// <summary>
        /// Activate user account
        /// </summary>
        Task<Result> ActivateUserAsync(Guid userId);

        /// <summary>
        /// Validate user credentials (for internal use)
        /// </summary>
        Task<Result<User>> ValidateCredentialsAsync(string email, string password);
    }
}
