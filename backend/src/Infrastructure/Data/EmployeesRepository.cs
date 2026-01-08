using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;
using System.Text;

namespace Infrastructure.Data
{
    public class EmployeesRepository : IEmployeesRepository
    {
        private readonly string _connectionString;

        public EmployeesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Employees?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT e.*, m.employee_name AS manager_name
                FROM employees e
                LEFT JOIN employees m ON e.manager_id = m.id
                WHERE e.id = @id";
            return await connection.QueryFirstOrDefaultAsync<Employees>(sql, new { id });
        }

        public async Task<Employees?> GetByEmployeeIdAsync(string employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT e.*, m.employee_name AS manager_name
                FROM employees e
                LEFT JOIN employees m ON e.manager_id = m.id
                WHERE e.employee_id = @employeeId";
            return await connection.QueryFirstOrDefaultAsync<Employees>(sql, new { employeeId });
        }

        public async Task<IEnumerable<Employees>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT e.*, m.employee_name AS manager_name
                FROM employees e
                LEFT JOIN employees m ON e.manager_id = m.id
                ORDER BY e.employee_name";
            return await connection.QueryAsync<Employees>(sql);
        }

        public async Task<(IEnumerable<Employees> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { 
                "id", "employee_name", "email", "phone", "employee_id", "department", 
                "designation", "hire_date", "status", "bank_account_number", "bank_name", 
                "ifsc_code", "pan_number", "address_line1", "address_line2", "city", 
                "state", "zip_code", "country", "contract_type", "company", "company_id",
                "created_at", "updated_at" 
            };
            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var normalizedSort = GetSafeSortColumn(sortBy, allowedSet);

            var builder = SqlQueryBuilder
                .From("employees", allowedColumns)
                .SearchAcross(new[] { "employee_name", "email", "phone", "employee_id", "department", "designation", "company" }, searchTerm)
                .ApplyFilters(filters)
                .OrderBy(normalizedSort, sortDescending)
                .Paginate(pageNumber, pageSize);

            var (sql, parameters) = builder.BuildSelect();
            var items = await connection.QueryAsync<Employees>(sql, parameters);

            var (countSql, countParameters) = builder.BuildCount();
            var totalCount = await connection.QuerySingleAsync<int>(countSql, countParameters);

            return (items, totalCount);
        }

        public async Task<Employees> AddAsync(Employees entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO employees (
                    id, employee_name, email, phone, employee_id, department, designation,
                    hire_date, status, bank_account_number, bank_name, ifsc_code, pan_number,
                    address_line1, address_line2, city, state, zip_code, country,
                    contract_type, company, company_id,
                    employment_type, tally_ledger_guid,
                    created_at, updated_at
                ) VALUES (
                    @Id, @EmployeeName, @Email, @Phone, @EmployeeId, @Department, @Designation,
                    @HireDate, @Status, @BankAccountNumber, @BankName, @IfscCode, @PanNumber,
                    @AddressLine1, @AddressLine2, @City, @State, @ZipCode, @Country,
                    @ContractType, @Company, @CompanyId,
                    @EmploymentType, @TallyLedgerGuid,
                    @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(sql, entity);
            return entity;
        }

        public async Task UpdateAsync(Employees entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE employees SET 
                    employee_name = @EmployeeName, email = @Email, phone = @Phone, 
                    employee_id = @EmployeeId, department = @Department, designation = @Designation,
                    hire_date = @HireDate, status = @Status, bank_account_number = @BankAccountNumber,
                    bank_name = @BankName, ifsc_code = @IfscCode, pan_number = @PanNumber,
                    address_line1 = @AddressLine1, address_line2 = @AddressLine2, city = @City,
                    state = @State, zip_code = @ZipCode, country = @Country,
                    contract_type = @ContractType, company = @Company, company_id = @CompanyId,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM employees WHERE id = @id", new { id });
        }

        public async Task<bool> EmployeeIdExistsAsync(string employeeId, Guid? excludeId = null, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT COUNT(*) FROM employees WHERE employee_id = @employeeId";
            var parameters = new DynamicParameters();
            parameters.Add("employeeId", employeeId);

            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
                parameters.Add("excludeId", excludeId.Value);
            }

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
                parameters.Add("companyId", companyId.Value);
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, Guid? companyId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT COUNT(*) FROM employees WHERE email = @email";
            var parameters = new DynamicParameters();
            parameters.Add("email", email);

            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
                parameters.Add("excludeId", excludeId.Value);
            }

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
                parameters.Add("companyId", companyId.Value);
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        public async Task<IEnumerable<Employees>> GetByManagerIdAsync(Guid managerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT * FROM employees
                WHERE manager_id = @managerId
                ORDER BY employee_name";
            return await connection.QueryAsync<Employees>(sql, new { managerId });
        }

        public async Task<Employees?> GetByNameAsync(Guid companyId, string employeeName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Employees>(
                @"SELECT * FROM employees
                  WHERE company_id = @companyId AND LOWER(employee_name) = LOWER(@employeeName)",
                new { companyId, employeeName });
        }

        public async Task<Employees?> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Employees>(
                @"SELECT * FROM employees
                  WHERE company_id = @companyId AND tally_ledger_guid = @tallyLedgerGuid",
                new { companyId, tallyLedgerGuid });
        }

        private static string GetSafeSortColumn(string? sortBy, HashSet<string> allowedColumns)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return "employee_name";
            }

            if (allowedColumns.Contains(sortBy))
            {
                return sortBy;
            }

            var snakeCase = ToSnakeCase(sortBy);
            return allowedColumns.Contains(snakeCase) ? snakeCase : "employee_name";
        }

        private static string ToSnakeCase(string value)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsUpper(c) && i > 0)
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
