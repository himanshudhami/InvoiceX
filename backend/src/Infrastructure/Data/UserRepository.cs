using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;
using System.Text;

namespace Infrastructure.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE id = @id",
                new { id });
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE LOWER(email) = LOWER(@email)",
                new { email });
        }

        public async Task<User?> GetByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE employee_id = @employeeId",
                new { employeeId });
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM users WHERE LOWER(email) = LOWER(@email)",
                new { email });
            return count > 0;
        }

        public async Task<IEnumerable<User>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<User>(
                "SELECT * FROM users WHERE company_id = @companyId ORDER BY display_name",
                new { companyId });
        }

        public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? role = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClauses = new List<string> { "company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);
            parameters.Add("offset", (pageNumber - 1) * pageSize);
            parameters.Add("limit", pageSize);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("(LOWER(email) LIKE @search OR LOWER(display_name) LIKE @search)");
                parameters.Add("search", $"%{searchTerm.ToLower()}%");
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                whereClauses.Add("role = @role");
                parameters.Add("role", role);
            }

            var whereClause = string.Join(" AND ", whereClauses);

            var sql = $@"
                SELECT * FROM users
                WHERE {whereClause}
                ORDER BY display_name
                OFFSET @offset LIMIT @limit";

            var countSql = $"SELECT COUNT(*) FROM users WHERE {whereClause}";

            var items = await connection.QueryAsync<User>(sql, parameters);
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<User> CreateAsync(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO users (
                    id, email, password_hash, display_name, role, company_id, employee_id,
                    is_active, last_login_at, failed_login_attempts, lockout_end_at,
                    created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @Id, @Email, @PasswordHash, @DisplayName, @Role, @CompanyId, @EmployeeId,
                    @IsActive, @LastLoginAt, @FailedLoginAttempts, @LockoutEndAt,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy
                )";

            await connection.ExecuteAsync(sql, user);
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            user.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE users SET
                    email = @Email,
                    display_name = @DisplayName,
                    role = @Role,
                    employee_id = @EmployeeId,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, user);
        }

        public async Task UpdateLastLoginAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET last_login_at = @now, failed_login_attempts = 0, updated_at = @now WHERE id = @userId",
                new { userId, now = DateTime.UtcNow });
        }

        public async Task IncrementFailedLoginAttemptsAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET failed_login_attempts = failed_login_attempts + 1, updated_at = @now WHERE id = @userId",
                new { userId, now = DateTime.UtcNow });
        }

        public async Task ResetFailedLoginAttemptsAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET failed_login_attempts = 0, updated_at = @now WHERE id = @userId",
                new { userId, now = DateTime.UtcNow });
        }

        public async Task LockUserAsync(Guid userId, DateTime lockoutEnd)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET lockout_end_at = @lockoutEnd, updated_at = @now WHERE id = @userId",
                new { userId, lockoutEnd, now = DateTime.UtcNow });
        }

        public async Task UnlockUserAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET lockout_end_at = NULL, failed_login_attempts = 0, updated_at = @now WHERE id = @userId",
                new { userId, now = DateTime.UtcNow });
        }

        public async Task DeactivateAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET is_active = FALSE, updated_at = @now WHERE id = @userId",
                new { userId, now = DateTime.UtcNow });
        }

        public async Task ActivateAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET is_active = TRUE, updated_at = @now WHERE id = @userId",
                new { userId, now = DateTime.UtcNow });
        }

        public async Task ChangePasswordAsync(Guid userId, string newPasswordHash)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET password_hash = @newPasswordHash, updated_at = @now WHERE id = @userId",
                new { userId, newPasswordHash, now = DateTime.UtcNow });
        }
    }

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly string _connectionString;

        public RefreshTokenRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<RefreshToken?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                "SELECT * FROM refresh_tokens WHERE id = @id",
                new { id });
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                "SELECT * FROM refresh_tokens WHERE token = @token",
                new { token });
        }

        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RefreshToken>(
                @"SELECT * FROM refresh_tokens
                  WHERE user_id = @userId
                    AND is_revoked = FALSE
                    AND expires_at > @now
                  ORDER BY created_at DESC",
                new { userId, now = DateTime.UtcNow });
        }

        public async Task<RefreshToken> CreateAsync(RefreshToken token)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            token.Id = Guid.NewGuid();
            token.CreatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO refresh_tokens (
                    id, user_id, token, expires_at, is_revoked, revoked_at, revoked_reason,
                    created_by_ip, created_by_user_agent, replaced_by_token_id, created_at
                ) VALUES (
                    @Id, @UserId, @Token, @ExpiresAt, @IsRevoked, @RevokedAt, @RevokedReason,
                    @CreatedByIp, @CreatedByUserAgent, @ReplacedByTokenId, @CreatedAt
                )";

            await connection.ExecuteAsync(sql, token);
            return token;
        }

        public async Task RevokeAsync(Guid tokenId, string? reason = null, Guid? replacedByTokenId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE refresh_tokens SET
                    is_revoked = TRUE,
                    revoked_at = @now,
                    revoked_reason = @reason,
                    replaced_by_token_id = @replacedByTokenId
                  WHERE id = @tokenId",
                new { tokenId, reason, replacedByTokenId, now = DateTime.UtcNow });
        }

        public async Task RevokeAllByUserIdAsync(Guid userId, string? reason = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE refresh_tokens SET
                    is_revoked = TRUE,
                    revoked_at = @now,
                    revoked_reason = @reason
                  WHERE user_id = @userId AND is_revoked = FALSE",
                new { userId, reason, now = DateTime.UtcNow });
        }

        public async Task DeleteExpiredTokensAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Delete tokens that expired more than 7 days ago
            await connection.ExecuteAsync(
                "DELETE FROM refresh_tokens WHERE expires_at < @cutoff",
                new { cutoff = DateTime.UtcNow.AddDays(-7) });
        }
    }
}
