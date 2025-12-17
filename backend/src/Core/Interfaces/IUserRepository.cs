using Core.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Repository interface for User entities
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Get user by ID
        /// </summary>
        Task<User?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get user by email (case-insensitive)
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Get user by employee ID
        /// </summary>
        Task<User?> GetByEmployeeIdAsync(Guid employeeId);

        /// <summary>
        /// Check if email already exists
        /// </summary>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// Get all users for a company
        /// </summary>
        Task<IEnumerable<User>> GetByCompanyIdAsync(Guid companyId);

        /// <summary>
        /// Get paged users for a company
        /// </summary>
        Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? role = null);

        /// <summary>
        /// Create a new user
        /// </summary>
        Task<User> CreateAsync(User user);

        /// <summary>
        /// Update an existing user
        /// </summary>
        Task UpdateAsync(User user);

        /// <summary>
        /// Update last login timestamp
        /// </summary>
        Task UpdateLastLoginAsync(Guid userId);

        /// <summary>
        /// Increment failed login attempts
        /// </summary>
        Task IncrementFailedLoginAttemptsAsync(Guid userId);

        /// <summary>
        /// Reset failed login attempts
        /// </summary>
        Task ResetFailedLoginAttemptsAsync(Guid userId);

        /// <summary>
        /// Lock user account until specified time
        /// </summary>
        Task LockUserAsync(Guid userId, DateTime lockoutEnd);

        /// <summary>
        /// Unlock user account
        /// </summary>
        Task UnlockUserAsync(Guid userId);

        /// <summary>
        /// Deactivate user account
        /// </summary>
        Task DeactivateAsync(Guid userId);

        /// <summary>
        /// Activate user account
        /// </summary>
        Task ActivateAsync(Guid userId);

        /// <summary>
        /// Change user password
        /// </summary>
        Task ChangePasswordAsync(Guid userId, string newPasswordHash);
    }

    /// <summary>
    /// Repository interface for RefreshToken entities
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Get refresh token by ID
        /// </summary>
        Task<RefreshToken?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get refresh token by token string
        /// </summary>
        Task<RefreshToken?> GetByTokenAsync(string token);

        /// <summary>
        /// Get all active tokens for a user
        /// </summary>
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId);

        /// <summary>
        /// Create a new refresh token
        /// </summary>
        Task<RefreshToken> CreateAsync(RefreshToken token);

        /// <summary>
        /// Revoke a token
        /// </summary>
        Task RevokeAsync(Guid tokenId, string? reason = null, Guid? replacedByTokenId = null);

        /// <summary>
        /// Revoke all tokens for a user
        /// </summary>
        Task RevokeAllByUserIdAsync(Guid userId, string? reason = null);

        /// <summary>
        /// Delete expired tokens (cleanup job)
        /// </summary>
        Task DeleteExpiredTokensAsync();
    }
}
