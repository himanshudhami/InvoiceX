using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data
{
    /// <summary>
    /// Repository implementation for UserCompanyAssignment entities.
    /// Manages the many-to-many relationship between users and companies for Company Admins.
    /// </summary>
    public class UserCompanyAssignmentRepository : IUserCompanyAssignmentRepository
    {
        private readonly string _connectionString;

        public UserCompanyAssignmentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserCompanyAssignment?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserCompanyAssignment>(
                "SELECT * FROM user_company_assignments WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<UserCompanyAssignment>> GetByUserIdAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<UserCompanyAssignment>(
                @"SELECT * FROM user_company_assignments
                  WHERE user_id = @userId
                  ORDER BY is_primary DESC, created_at",
                new { userId });
        }

        public async Task<IEnumerable<UserCompanyAssignment>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<UserCompanyAssignment>(
                @"SELECT * FROM user_company_assignments
                  WHERE company_id = @companyId
                  ORDER BY created_at",
                new { companyId });
        }

        public async Task<UserCompanyAssignment?> GetByUserAndCompanyAsync(Guid userId, Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserCompanyAssignment>(
                @"SELECT * FROM user_company_assignments
                  WHERE user_id = @userId AND company_id = @companyId",
                new { userId, companyId });
        }

        public async Task<UserCompanyAssignment?> GetPrimaryByUserIdAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserCompanyAssignment>(
                @"SELECT * FROM user_company_assignments
                  WHERE user_id = @userId AND is_primary = TRUE
                  LIMIT 1",
                new { userId });
        }

        public async Task<bool> HasAccessToCompanyAsync(Guid userId, Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.QuerySingleAsync<int>(
                @"SELECT COUNT(*) FROM user_company_assignments
                  WHERE user_id = @userId AND company_id = @companyId",
                new { userId, companyId });
            return count > 0;
        }

        public async Task<UserCompanyAssignment> CreateAsync(UserCompanyAssignment assignment)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            assignment.Id = Guid.NewGuid();
            assignment.CreatedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO user_company_assignments (
                    id, user_id, company_id, role, is_primary,
                    created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @Id, @UserId, @CompanyId, @Role, @IsPrimary,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy
                )";

            await connection.ExecuteAsync(sql, assignment);
            return assignment;
        }

        public async Task UpdateAsync(UserCompanyAssignment assignment)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            assignment.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE user_company_assignments SET
                    role = @Role,
                    is_primary = @IsPrimary,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, assignment);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM user_company_assignments WHERE id = @id",
                new { id });
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM user_company_assignments WHERE user_id = @userId",
                new { userId });
        }

        public async Task SetPrimaryAsync(Guid userId, Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // First, unset all primaries for this user
            await connection.ExecuteAsync(
                @"UPDATE user_company_assignments
                  SET is_primary = FALSE, updated_at = @now
                  WHERE user_id = @userId AND is_primary = TRUE",
                new { userId, now = DateTime.UtcNow });

            // Then, set the specified company as primary
            await connection.ExecuteAsync(
                @"UPDATE user_company_assignments
                  SET is_primary = TRUE, updated_at = @now
                  WHERE user_id = @userId AND company_id = @companyId",
                new { userId, companyId, now = DateTime.UtcNow });
        }
    }
}
