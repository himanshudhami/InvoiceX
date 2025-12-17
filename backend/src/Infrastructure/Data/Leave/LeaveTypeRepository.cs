using Core.Entities.Leave;
using Core.Interfaces.Leave;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Leave
{
    public class LeaveTypeRepository : ILeaveTypeRepository
    {
        private readonly string _connectionString;

        public LeaveTypeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<LeaveType?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LeaveType>(
                "SELECT * FROM leave_types WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<LeaveType>> GetAllByCompanyAsync(Guid companyId, bool activeOnly = true)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = activeOnly
                ? "SELECT * FROM leave_types WHERE company_id = @companyId AND is_active = true ORDER BY sort_order, name"
                : "SELECT * FROM leave_types WHERE company_id = @companyId ORDER BY sort_order, name";
            return await connection.QueryAsync<LeaveType>(sql, new { companyId });
        }

        public async Task<LeaveType?> GetByCodeAsync(Guid companyId, string code)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LeaveType>(
                "SELECT * FROM leave_types WHERE company_id = @companyId AND code = @code",
                new { companyId, code });
        }

        public async Task<LeaveType> AddAsync(LeaveType entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                INSERT INTO leave_types (
                    company_id, name, code, description, days_per_year,
                    carry_forward_allowed, max_carry_forward_days, encashment_allowed, max_encashment_days,
                    requires_approval, min_days_notice, max_consecutive_days, is_active, color_code, sort_order,
                    created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @CompanyId, @Name, @Code, @Description, @DaysPerYear,
                    @CarryForwardAllowed, @MaxCarryForwardDays, @EncashmentAllowed, @MaxEncashmentDays,
                    @RequiresApproval, @MinDaysNotice, @MaxConsecutiveDays, @IsActive, @ColorCode, @SortOrder,
                    NOW(), NOW(), @CreatedBy, @UpdatedBy
                ) RETURNING *";

            return await connection.QuerySingleAsync<LeaveType>(sql, entity);
        }

        public async Task UpdateAsync(LeaveType entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                UPDATE leave_types SET
                    name = @Name,
                    code = @Code,
                    description = @Description,
                    days_per_year = @DaysPerYear,
                    carry_forward_allowed = @CarryForwardAllowed,
                    max_carry_forward_days = @MaxCarryForwardDays,
                    encashment_allowed = @EncashmentAllowed,
                    max_encashment_days = @MaxEncashmentDays,
                    requires_approval = @RequiresApproval,
                    min_days_notice = @MinDaysNotice,
                    max_consecutive_days = @MaxConsecutiveDays,
                    is_active = @IsActive,
                    color_code = @ColorCode,
                    sort_order = @SortOrder,
                    updated_at = NOW(),
                    updated_by = @UpdatedBy
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM leave_types WHERE id = @id", new { id });
        }

        public async Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM leave_types WHERE company_id = @companyId AND code = @code AND id != @excludeId"
                : "SELECT COUNT(*) FROM leave_types WHERE company_id = @companyId AND code = @code";
            var count = await connection.QuerySingleAsync<int>(sql, new { companyId, code, excludeId });
            return count > 0;
        }
    }
}
