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
            return await connection.QueryFirstOrDefaultAsync<Employees>(
                "SELECT * FROM employees WHERE id = @id", 
                new { id });
        }

        public async Task<Employees?> GetByEmployeeIdAsync(string employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Employees>(
                "SELECT * FROM employees WHERE employee_id = @employeeId", 
                new { employeeId });
        }

        public async Task<IEnumerable<Employees>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Employees>(
                "SELECT * FROM employees ORDER BY employee_name");
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
                    created_at, updated_at
                ) VALUES (
                    @Id, @EmployeeName, @Email, @Phone, @EmployeeId, @Department, @Designation,
                    @HireDate, @Status, @BankAccountNumber, @BankName, @IfscCode, @PanNumber,
                    @AddressLine1, @AddressLine2, @City, @State, @ZipCode, @Country,
                    @ContractType, @Company, @CompanyId,
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

        public async Task<bool> EmployeeIdExistsAsync(string employeeId, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT COUNT(*) FROM employees WHERE employee_id = @employeeId";
            object parameters = new { employeeId };

            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
                parameters = new { employeeId, excludeId = excludeId.Value };
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT COUNT(*) FROM employees WHERE email = @email";
            object parameters = new { email };

            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
                parameters = new { email, excludeId = excludeId.Value };
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
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
