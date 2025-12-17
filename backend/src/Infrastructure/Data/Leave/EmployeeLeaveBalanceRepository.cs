using Core.Entities.Leave;
using Core.Interfaces.Leave;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Leave
{
    public class EmployeeLeaveBalanceRepository : IEmployeeLeaveBalanceRepository
    {
        private readonly string _connectionString;

        public EmployeeLeaveBalanceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<EmployeeLeaveBalance?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeeLeaveBalance>(
                "SELECT * FROM employee_leave_balances WHERE id = @id",
                new { id });
        }

        public async Task<EmployeeLeaveBalance?> GetByEmployeeTypeYearAsync(Guid employeeId, Guid leaveTypeId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeeLeaveBalance>(
                @"SELECT * FROM employee_leave_balances
                  WHERE employee_id = @employeeId
                    AND leave_type_id = @leaveTypeId
                    AND financial_year = @financialYear",
                new { employeeId, leaveTypeId, financialYear });
        }

        public async Task<IEnumerable<EmployeeLeaveBalance>> GetByEmployeeAsync(Guid employeeId, string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = financialYear != null
                ? @"SELECT elb.*, lt.name as LeaveTypeName, lt.code as LeaveTypeCode
                    FROM employee_leave_balances elb
                    JOIN leave_types lt ON elb.leave_type_id = lt.id
                    WHERE elb.employee_id = @employeeId AND elb.financial_year = @financialYear
                    ORDER BY lt.sort_order"
                : @"SELECT elb.*, lt.name as LeaveTypeName, lt.code as LeaveTypeCode
                    FROM employee_leave_balances elb
                    JOIN leave_types lt ON elb.leave_type_id = lt.id
                    WHERE elb.employee_id = @employeeId
                    ORDER BY elb.financial_year DESC, lt.sort_order";
            return await connection.QueryAsync<EmployeeLeaveBalance>(sql, new { employeeId, financialYear });
        }

        public async Task<IEnumerable<EmployeeLeaveBalance>> GetByCompanyAndYearAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeLeaveBalance>(
                @"SELECT elb.*
                  FROM employee_leave_balances elb
                  JOIN employees e ON elb.employee_id = e.id
                  WHERE e.company_id = @companyId AND elb.financial_year = @financialYear",
                new { companyId, financialYear });
        }

        public async Task<EmployeeLeaveBalance> AddAsync(EmployeeLeaveBalance entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                INSERT INTO employee_leave_balances (
                    employee_id, leave_type_id, financial_year,
                    opening_balance, accrued, taken, carry_forwarded, adjusted, encashed,
                    created_at, updated_at
                ) VALUES (
                    @EmployeeId, @LeaveTypeId, @FinancialYear,
                    @OpeningBalance, @Accrued, @Taken, @CarryForwarded, @Adjusted, @Encashed,
                    NOW(), NOW()
                ) RETURNING *";

            return await connection.QuerySingleAsync<EmployeeLeaveBalance>(sql, entity);
        }

        public async Task UpdateAsync(EmployeeLeaveBalance entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                UPDATE employee_leave_balances SET
                    opening_balance = @OpeningBalance,
                    accrued = @Accrued,
                    taken = @Taken,
                    carry_forwarded = @CarryForwarded,
                    adjusted = @Adjusted,
                    encashed = @Encashed,
                    updated_at = NOW()
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM employee_leave_balances WHERE id = @id", new { id });
        }

        public async Task IncrementTakenAsync(Guid employeeId, Guid leaveTypeId, string financialYear, decimal days)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE employee_leave_balances
                  SET taken = taken + @days, updated_at = NOW()
                  WHERE employee_id = @employeeId
                    AND leave_type_id = @leaveTypeId
                    AND financial_year = @financialYear",
                new { employeeId, leaveTypeId, financialYear, days });
        }

        public async Task DecrementTakenAsync(Guid employeeId, Guid leaveTypeId, string financialYear, decimal days)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE employee_leave_balances
                  SET taken = taken - @days, updated_at = NOW()
                  WHERE employee_id = @employeeId
                    AND leave_type_id = @leaveTypeId
                    AND financial_year = @financialYear",
                new { employeeId, leaveTypeId, financialYear, days });
        }

        public async Task InitializeBalancesAsync(Guid employeeId, Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Get all active leave types for the company
            var leaveTypes = await connection.QueryAsync<LeaveType>(
                "SELECT * FROM leave_types WHERE company_id = @companyId AND is_active = true",
                new { companyId });

            foreach (var leaveType in leaveTypes)
            {
                // Check if balance already exists
                var exists = await connection.QuerySingleAsync<int>(
                    @"SELECT COUNT(*) FROM employee_leave_balances
                      WHERE employee_id = @employeeId
                        AND leave_type_id = @leaveTypeId
                        AND financial_year = @financialYear",
                    new { employeeId, leaveTypeId = leaveType.Id, financialYear });

                if (exists == 0)
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO employee_leave_balances (
                            employee_id, leave_type_id, financial_year,
                            opening_balance, accrued, taken, carry_forwarded, adjusted, encashed,
                            created_at, updated_at
                        ) VALUES (
                            @employeeId, @leaveTypeId, @financialYear,
                            @daysPerYear, 0, 0, 0, 0, 0,
                            NOW(), NOW()
                        )",
                        new { employeeId, leaveTypeId = leaveType.Id, financialYear, daysPerYear = leaveType.DaysPerYear });
                }
            }
        }

        public async Task CarryForwardBalancesAsync(Guid employeeId, string fromYear, string toYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Get balances from previous year with carry forward allowed
            var previousBalances = await connection.QueryAsync<dynamic>(
                @"SELECT elb.*, lt.carry_forward_allowed, lt.max_carry_forward_days, lt.days_per_year
                  FROM employee_leave_balances elb
                  JOIN leave_types lt ON elb.leave_type_id = lt.id
                  WHERE elb.employee_id = @employeeId
                    AND elb.financial_year = @fromYear
                    AND lt.carry_forward_allowed = true",
                new { employeeId, fromYear });

            foreach (var balance in previousBalances)
            {
                var availableBalance = (decimal)balance.opening_balance + (decimal)balance.accrued +
                                      (decimal)balance.carry_forwarded + (decimal)balance.adjusted -
                                      (decimal)balance.taken - (decimal)balance.encashed;

                var carryForward = Math.Min(availableBalance, (decimal)balance.max_carry_forward_days);
                if (carryForward <= 0) continue;

                // Check if new year balance exists
                var exists = await connection.QuerySingleAsync<int>(
                    @"SELECT COUNT(*) FROM employee_leave_balances
                      WHERE employee_id = @employeeId
                        AND leave_type_id = @leaveTypeId
                        AND financial_year = @toYear",
                    new { employeeId, leaveTypeId = (Guid)balance.leave_type_id, toYear });

                if (exists == 0)
                {
                    // Create new balance with carry forward
                    await connection.ExecuteAsync(
                        @"INSERT INTO employee_leave_balances (
                            employee_id, leave_type_id, financial_year,
                            opening_balance, accrued, taken, carry_forwarded, adjusted, encashed,
                            created_at, updated_at
                        ) VALUES (
                            @employeeId, @leaveTypeId, @toYear,
                            @daysPerYear, 0, 0, @carryForward, 0, 0,
                            NOW(), NOW()
                        )",
                        new {
                            employeeId,
                            leaveTypeId = (Guid)balance.leave_type_id,
                            toYear,
                            daysPerYear = (decimal)balance.days_per_year,
                            carryForward
                        });
                }
                else
                {
                    // Update existing balance with carry forward
                    await connection.ExecuteAsync(
                        @"UPDATE employee_leave_balances
                          SET carry_forwarded = @carryForward, updated_at = NOW()
                          WHERE employee_id = @employeeId
                            AND leave_type_id = @leaveTypeId
                            AND financial_year = @toYear",
                        new { employeeId, leaveTypeId = (Guid)balance.leave_type_id, toYear, carryForward });
                }
            }
        }
    }
}
