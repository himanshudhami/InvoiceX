using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class EmployeeSalaryTransactionsRepository : IEmployeeSalaryTransactionsRepository
    {
        private readonly string _connectionString;

        private sealed class MonthlySummaryRow
        {
            public decimal? TotalGrossSalary { get; set; }
            public decimal? TotalNetSalary { get; set; }
            public decimal? TotalPfEmployee { get; set; }
            public decimal? TotalPfEmployer { get; set; }
            public decimal? TotalPt { get; set; }
            public decimal? TotalIncomeTax { get; set; }
            public long TotalEmployees { get; set; }
            public long FulltimeEmployees { get; set; }
            public long ConsultingEmployees { get; set; }
        }

        private sealed class YearlySummaryRow
        {
            public decimal? TotalGrossSalary { get; set; }
            public decimal? TotalNetSalary { get; set; }
            public decimal? TotalPfEmployee { get; set; }
            public decimal? TotalPfEmployer { get; set; }
            public decimal? TotalPt { get; set; }
            public decimal? TotalIncomeTax { get; set; }
            public long TotalEmployees { get; set; }
            public long FulltimeEmployees { get; set; }
            public long ConsultingEmployees { get; set; }
        }

        public EmployeeSalaryTransactionsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<EmployeeSalaryTransactions?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT st.*, e.employee_name, e.email, e.department, e.designation, e.contract_type
                FROM employee_salary_transactions st
                INNER JOIN employees e ON st.employee_id = e.id
                WHERE st.id = @id";

            var result = await connection.QueryAsync<EmployeeSalaryTransactions, Employees, EmployeeSalaryTransactions>(
                sql,
                (transaction, employee) => 
                {
                    transaction.Employee = employee;
                    return transaction;
                },
                new { id },
                splitOn: "employee_name");

            return result.FirstOrDefault();
        }

        public async Task<EmployeeSalaryTransactions?> GetByEmployeeAndMonthAsync(Guid employeeId, int salaryMonth, int salaryYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeeSalaryTransactions>(
                "SELECT * FROM employee_salary_transactions WHERE employee_id = @employeeId AND salary_month = @salaryMonth AND salary_year = @salaryYear",
                new { employeeId, salaryMonth, salaryYear });
        }

        public async Task<IEnumerable<EmployeeSalaryTransactions>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT st.*, e.employee_name, e.email, e.department, e.designation, e.company_id
                FROM employee_salary_transactions st
                INNER JOIN employees e ON st.employee_id = e.id
                ORDER BY st.salary_year DESC, st.salary_month DESC, e.employee_name";

            var result = await connection.QueryAsync<EmployeeSalaryTransactions, Employees, EmployeeSalaryTransactions>(
                sql,
                (transaction, employee) => 
                {
                    transaction.Employee = employee;
                    return transaction;
                },
                splitOn: "employee_name");

            return result;
        }

        public async Task<IEnumerable<EmployeeSalaryTransactions>> GetByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeSalaryTransactions>(
                "SELECT * FROM employee_salary_transactions WHERE employee_id = @employeeId ORDER BY salary_year DESC, salary_month DESC",
                new { employeeId });
        }

        public async Task<IEnumerable<EmployeeSalaryTransactions>> GetByMonthYearAsync(int salaryMonth, int salaryYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT st.*, e.employee_name, e.email, e.department, e.designation, e.company_id
                FROM employee_salary_transactions st
                INNER JOIN employees e ON st.employee_id = e.id
                WHERE st.salary_month = @salaryMonth AND st.salary_year = @salaryYear
                ORDER BY e.employee_name";

            var result = await connection.QueryAsync<EmployeeSalaryTransactions, Employees, EmployeeSalaryTransactions>(
                sql,
                (transaction, employee) => 
                {
                    transaction.Employee = employee;
                    return transaction;
                },
                new { salaryMonth, salaryYear },
                splitOn: "employee_name");

            return result;
        }

        public async Task<(IEnumerable<EmployeeSalaryTransactions> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            // We need to join with employees table for search and display purposes
            var allowedColumns = new[] { 
                "st.id", "st.employee_id", "st.company_id", "st.salary_month", "st.salary_year", "st.basic_salary",
                "st.hra", "st.conveyance", "st.medical_allowance", "st.special_allowance",
                "st.lta", "st.other_allowances", "st.gross_salary", "st.pf_employee",
                "st.pf_employer", "st.pt", "st.income_tax", "st.other_deductions",
                "st.net_salary", "st.payment_date", "st.payment_method", "st.payment_reference",
                "st.status", "st.remarks", "st.currency", "st.transaction_type", "st.created_at", "st.updated_at",
                "e.employee_name", "e.email", "e.department", "e.contract_type"
            };

            // Custom query builder for joined tables
            var baseQuery = @"
                FROM employee_salary_transactions st
                INNER JOIN employees e ON st.employee_id = e.id";

            var whereConditions = new List<string>();
            var parameters = new DynamicParameters();

            // Add search conditions
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereConditions.Add("(e.employee_name ILIKE @searchTerm OR e.email ILIKE @searchTerm OR e.department ILIKE @searchTerm)");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            // Add filter conditions
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    if (filter.Value != null)
                    {
                        var column = filter.Key.ToLowerInvariant();
                        if (allowedColumns.Any(c => c.EndsWith($".{column}") || c == column))
                        {
                            if (column == "salary_year" || column == "salary_month")
                            {
                                whereConditions.Add($"st.{column} = @{filter.Key}");
                            }
                            else if (column == "status")
                            {
                                whereConditions.Add($"st.status = @{filter.Key}");
                            }
                            else if (column == "department")
                            {
                                whereConditions.Add($"e.department = @{filter.Key}");
                            }
                            else if (column == "company")
                            {
                                whereConditions.Add($"e.company = @{filter.Key}");
                            }
                            else if (column == "company_id")
                            {
                                whereConditions.Add($"st.company_id = @{filter.Key}");
                            }
                            else if (column == "transaction_type")
                            {
                                whereConditions.Add($"st.transaction_type = @{filter.Key}");
                            }
                            else if (column == "contract_type")
                            {
                                // Support special value "NOT_CONTRACT" to filter for fulltime employees
                                if (filter.Value?.ToString() == "NOT_CONTRACT")
                                {
                                    whereConditions.Add($"(LOWER(e.contract_type) != 'contract' OR e.contract_type IS NULL)");
                                }
                                else
                                {
                                    whereConditions.Add($"e.contract_type = @{filter.Key}");
                                }
                            }
                            parameters.Add(filter.Key, filter.Value);
                        }
                    }
                }
            }

            var whereClause = whereConditions.Count > 0 ? " WHERE " + string.Join(" AND ", whereConditions) : "";

            // Order by clause
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) 
                ? $" ORDER BY {sortBy} {(sortDescending ? "DESC" : "ASC")}"
                : " ORDER BY st.salary_year DESC, st.salary_month DESC, e.employee_name";

            // Build queries
            var selectSql = $@"
                SELECT st.*, e.employee_name, e.email, e.department, e.designation, e.company_id, e.contract_type
                {baseQuery}
                {whereClause}
                {orderBy}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*)
                {baseQuery}
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", (pageNumber - 1) * pageSize);

            var items = await connection.QueryAsync<EmployeeSalaryTransactions, Employees, EmployeeSalaryTransactions>(
                selectSql,
                (transaction, employee) => 
                {
                    transaction.Employee = employee;
                    return transaction;
                },
                parameters,
                splitOn: "employee_name");

            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            return (items, totalCount);
        }

        public async Task<EmployeeSalaryTransactions> AddAsync(EmployeeSalaryTransactions entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO employee_salary_transactions (
                    id, employee_id, company_id, salary_month, salary_year, basic_salary, hra, conveyance,
                    medical_allowance, special_allowance, lta, other_allowances, gross_salary,
                    pf_employee, pf_employer, pt, income_tax, other_deductions, net_salary,
                    payment_date, payment_method, payment_reference, status, remarks, currency,
                    transaction_type, created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @Id, @EmployeeId, @CompanyId, @SalaryMonth, @SalaryYear, @BasicSalary, @Hra, @Conveyance,
                    @MedicalAllowance, @SpecialAllowance, @Lta, @OtherAllowances, @GrossSalary,
                    @PfEmployee, @PfEmployer, @Pt, @IncomeTax, @OtherDeductions, @NetSalary,
                    @PaymentDate, @PaymentMethod, @PaymentReference, @Status, @Remarks, @Currency,
                    @TransactionType, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy
                )";

            await connection.ExecuteAsync(sql, entity);
            return entity;
        }

        public async Task UpdateAsync(EmployeeSalaryTransactions entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            entity.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE employee_salary_transactions SET 
                    basic_salary = @BasicSalary, hra = @Hra, conveyance = @Conveyance,
                    medical_allowance = @MedicalAllowance, special_allowance = @SpecialAllowance,
                    lta = @Lta, other_allowances = @OtherAllowances, gross_salary = @GrossSalary,
                    pf_employee = @PfEmployee, pf_employer = @PfEmployer, pt = @Pt,
                    income_tax = @IncomeTax, other_deductions = @OtherDeductions, 
                    net_salary = @NetSalary, payment_date = @PaymentDate, payment_method = @PaymentMethod,
                    payment_reference = @PaymentReference, status = @Status, remarks = @Remarks,
                    currency = @Currency, transaction_type = @TransactionType, updated_at = @UpdatedAt, updated_by = @UpdatedBy
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM employee_salary_transactions WHERE id = @id", new { id });
        }

        public async Task<bool> SalaryRecordExistsAsync(Guid employeeId, int salaryMonth, int salaryYear, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT COUNT(*) FROM employee_salary_transactions WHERE employee_id = @employeeId AND salary_month = @salaryMonth AND salary_year = @salaryYear";
            object parameters = new { employeeId, salaryMonth, salaryYear };

            if (excludeId.HasValue)
            {
                sql += " AND id != @excludeId";
                parameters = new { employeeId, salaryMonth, salaryYear, excludeId = excludeId.Value };
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        public async Task<IEnumerable<EmployeeSalaryTransactions>> BulkAddAsync(IEnumerable<EmployeeSalaryTransactions> entities)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            foreach (var entity in entities)
            {
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                
                // Ensure TransactionType is set and valid (safeguard)
                if (string.IsNullOrWhiteSpace(entity.TransactionType))
                {
                    entity.TransactionType = "salary";
                }
                else
                {
                    // Normalize to lowercase to match database constraint
                    var normalized = entity.TransactionType.ToLowerInvariant();
                    var allowedTypes = new[] { "salary", "consulting", "bonus", "reimbursement", "gift" };
                    if (allowedTypes.Contains(normalized))
                    {
                        entity.TransactionType = normalized;
                    }
                    else
                    {
                        entity.TransactionType = "salary"; // Default if invalid
                    }
                }
            }

            const string sql = @"
                INSERT INTO employee_salary_transactions (
                    id, employee_id, company_id, salary_month, salary_year, basic_salary, hra, conveyance,
                    medical_allowance, special_allowance, lta, other_allowances, gross_salary,
                    pf_employee, pf_employer, pt, income_tax, other_deductions, net_salary,
                    payment_date, payment_method, payment_reference, status, remarks, currency,
                    transaction_type, created_at, updated_at, created_by, updated_by
                ) VALUES (
                    @Id, @EmployeeId, @CompanyId, @SalaryMonth, @SalaryYear, @BasicSalary, @Hra, @Conveyance,
                    @MedicalAllowance, @SpecialAllowance, @Lta, @OtherAllowances, @GrossSalary,
                    @PfEmployee, @PfEmployer, @Pt, @IncomeTax, @OtherDeductions, @NetSalary,
                    @PaymentDate, @PaymentMethod, @PaymentReference, @Status, @Remarks, @Currency,
                    @TransactionType, @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy
                )";

            await connection.ExecuteAsync(sql, entities);
            return entities;
        }

        public async Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(int salaryMonth, int salaryYear, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT 
                    SUM(st.gross_salary) as TotalGrossSalary,
                    SUM(st.net_salary) as TotalNetSalary,
                    SUM(st.pf_employee) as TotalPfEmployee,
                    SUM(st.pf_employer) as TotalPfEmployer,
                    SUM(st.pt) as TotalPt,
                    SUM(st.income_tax) as TotalIncomeTax,
                    COUNT(DISTINCT st.employee_id) as TotalEmployees,
                    COUNT(DISTINCT CASE WHEN LOWER(e.contract_type) = 'contract' THEN st.employee_id END) as ConsultingEmployees,
                    COUNT(DISTINCT CASE WHEN LOWER(e.contract_type) != 'contract' OR e.contract_type IS NULL THEN st.employee_id END) as FulltimeEmployees
                FROM employee_salary_transactions st
                INNER JOIN employees e ON st.employee_id = e.id
                WHERE st.salary_month = @salaryMonth AND st.salary_year = @salaryYear";

            var parameters = new DynamicParameters();
            parameters.Add("salaryMonth", salaryMonth);
            parameters.Add("salaryYear", salaryYear);
            
            if (companyId.HasValue)
            {
                sql += " AND st.company_id = @companyId";
                parameters.Add("companyId", companyId.Value);
            }

            var result = await connection.QuerySingleOrDefaultAsync<MonthlySummaryRow>(sql, parameters);
            
            // If no results found, return zeros
            if (result == null)
            {
                return new Dictionary<string, decimal>
                {
                    ["TotalGrossSalary"] = 0,
                    ["TotalNetSalary"] = 0,
                    ["TotalPfEmployee"] = 0,
                    ["TotalPfEmployer"] = 0,
                    ["TotalPt"] = 0,
                    ["TotalIncomeTax"] = 0,
                    ["TotalEmployees"] = 0
                };
            }
            
            return new Dictionary<string, decimal>
            {
                ["TotalGrossSalary"] = result.TotalGrossSalary ?? 0,
                ["TotalNetSalary"] = result.TotalNetSalary ?? 0,
                ["TotalPfEmployee"] = result.TotalPfEmployee ?? 0,
                ["TotalPfEmployer"] = result.TotalPfEmployer ?? 0,
                ["TotalPt"] = result.TotalPt ?? 0,
                ["TotalIncomeTax"] = result.TotalIncomeTax ?? 0,
                ["TotalEmployees"] = Convert.ToDecimal(result.TotalEmployees)
            };
        }

        public async Task<Dictionary<string, decimal>> GetYearlySummaryAsync(int salaryYear, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT 
                    SUM(st.gross_salary) as TotalGrossSalary,
                    SUM(st.net_salary) as TotalNetSalary,
                    SUM(st.pf_employee) as TotalPfEmployee,
                    SUM(st.pf_employer) as TotalPfEmployer,
                    SUM(st.pt) as TotalPt,
                    SUM(st.income_tax) as TotalIncomeTax,
                    COUNT(DISTINCT st.employee_id) as TotalEmployees,
                    COUNT(DISTINCT CASE WHEN LOWER(e.contract_type) = 'contract' THEN st.employee_id END) as ConsultingEmployees,
                    COUNT(DISTINCT CASE WHEN LOWER(e.contract_type) != 'contract' OR e.contract_type IS NULL THEN st.employee_id END) as FulltimeEmployees
                FROM employee_salary_transactions st
                INNER JOIN employees e ON st.employee_id = e.id
                WHERE st.salary_year = @salaryYear";

            var parameters = new DynamicParameters();
            parameters.Add("salaryYear", salaryYear);
            
            if (companyId.HasValue)
            {
                sql += " AND st.company_id = @companyId";
                parameters.Add("companyId", companyId.Value);
            }

            var result = await connection.QuerySingleOrDefaultAsync<YearlySummaryRow>(sql, parameters);
            
            // If no results found, return zeros
            if (result == null)
            {
                return new Dictionary<string, decimal>
                {
                    ["TotalGrossSalary"] = 0,
                    ["TotalNetSalary"] = 0,
                    ["TotalPfEmployee"] = 0,
                    ["TotalPfEmployer"] = 0,
                    ["TotalPt"] = 0,
                    ["TotalIncomeTax"] = 0,
                    ["TotalEmployees"] = 0,
                    ["FulltimeEmployees"] = 0,
                    ["ConsultingEmployees"] = 0
                };
            }
            
            return new Dictionary<string, decimal>
            {
                ["TotalGrossSalary"] = result.TotalGrossSalary ?? 0,
                ["TotalNetSalary"] = result.TotalNetSalary ?? 0,
                ["TotalPfEmployee"] = result.TotalPfEmployee ?? 0,
                ["TotalPfEmployer"] = result.TotalPfEmployer ?? 0,
                ["TotalPt"] = result.TotalPt ?? 0,
                ["TotalIncomeTax"] = result.TotalIncomeTax ?? 0,
                ["TotalEmployees"] = Convert.ToDecimal(result.TotalEmployees),
                ["FulltimeEmployees"] = Convert.ToDecimal(result.FulltimeEmployees),
                ["ConsultingEmployees"] = Convert.ToDecimal(result.ConsultingEmployees)
            };
        }
    }
}