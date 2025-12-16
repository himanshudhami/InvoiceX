using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Payroll
{
    /// <summary>
    /// Repository implementation for tax declaration audit history
    /// </summary>
    public class EmployeeTaxDeclarationHistoryRepository : IEmployeeTaxDeclarationHistoryRepository
    {
        private readonly string _connectionString;

        public EmployeeTaxDeclarationHistoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByDeclarationIdAsync(Guid declarationId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT id, declaration_id, action, changed_by, changed_at,
                       previous_values, new_values, rejection_reason, rejection_comments,
                       ip_address, user_agent, created_at
                FROM employee_tax_declaration_history
                WHERE declaration_id = @declarationId
                ORDER BY changed_at DESC";

            return await connection.QueryAsync<EmployeeTaxDeclarationHistory>(sql, new { declarationId });
        }

        public async Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByActionAsync(string action, string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT h.id, h.declaration_id, h.action, h.changed_by, h.changed_at,
                       h.previous_values, h.new_values, h.rejection_reason, h.rejection_comments,
                       h.ip_address, h.user_agent, h.created_at
                FROM employee_tax_declaration_history h
                INNER JOIN employee_tax_declarations d ON h.declaration_id = d.id
                WHERE h.action = @action";

            if (!string.IsNullOrEmpty(financialYear))
            {
                sql += " AND d.financial_year = @financialYear";
            }

            sql += " ORDER BY h.changed_at DESC";

            return await connection.QueryAsync<EmployeeTaxDeclarationHistory>(sql, new { action, financialYear });
        }

        public async Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByEmployeeIdAsync(Guid employeeId, int limit = 50)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT h.id, h.declaration_id, h.action, h.changed_by, h.changed_at,
                       h.previous_values, h.new_values, h.rejection_reason, h.rejection_comments,
                       h.ip_address, h.user_agent, h.created_at
                FROM employee_tax_declaration_history h
                INNER JOIN employee_tax_declarations d ON h.declaration_id = d.id
                WHERE d.employee_id = @employeeId
                ORDER BY h.changed_at DESC
                LIMIT @limit";

            return await connection.QueryAsync<EmployeeTaxDeclarationHistory>(sql, new { employeeId, limit });
        }

        public async Task<EmployeeTaxDeclarationHistory> AddAsync(EmployeeTaxDeclarationHistory history)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                INSERT INTO employee_tax_declaration_history
                    (id, declaration_id, action, changed_by, changed_at,
                     previous_values, new_values, rejection_reason, rejection_comments,
                     ip_address, user_agent, created_at)
                VALUES
                    (@Id, @DeclarationId, @Action, @ChangedBy, @ChangedAt,
                     @PreviousValues::jsonb, @NewValues::jsonb, @RejectionReason, @RejectionComments,
                     @IpAddress, @UserAgent, @CreatedAt)
                RETURNING *";

            history.Id = Guid.NewGuid();
            history.ChangedAt = DateTime.UtcNow;
            history.CreatedAt = DateTime.UtcNow;

            return await connection.QuerySingleAsync<EmployeeTaxDeclarationHistory>(sql, history);
        }

        public async Task<int> GetRevisionCountAsync(Guid declarationId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                SELECT COUNT(*)
                FROM employee_tax_declaration_history
                WHERE declaration_id = @declarationId
                AND action = 'revised'";

            return await connection.ExecuteScalarAsync<int>(sql, new { declarationId });
        }

        public async Task<IEnumerable<EmployeeTaxDeclarationHistory>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? action = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT id, declaration_id, action, changed_by, changed_at,
                       previous_values, new_values, rejection_reason, rejection_comments,
                       ip_address, user_agent, created_at
                FROM employee_tax_declaration_history
                WHERE changed_at >= @startDate AND changed_at <= @endDate";

            if (!string.IsNullOrEmpty(action))
            {
                sql += " AND action = @action";
            }

            sql += " ORDER BY changed_at DESC";

            return await connection.QueryAsync<EmployeeTaxDeclarationHistory>(sql, new { startDate, endDate, action });
        }
    }
}
