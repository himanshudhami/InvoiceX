using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Data.Payroll
{
    public class EmployeeTaxDeclarationRepository : IEmployeeTaxDeclarationRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "employee_id", "financial_year", "tax_regime", "sec_80c_ppf", "sec_80c_elss",
            "sec_80c_life_insurance", "sec_80c_home_loan_principal", "sec_80c_children_tuition",
            "sec_80c_nsc", "sec_80c_sukanya_samriddhi", "sec_80c_fixed_deposit", "sec_80c_others",
            "sec_80ccd_nps", "sec_80d_self_family", "sec_80d_parents", "sec_80d_preventive_checkup",
            "sec_80d_self_senior_citizen", "sec_80d_parents_senior_citizen", "sec_80e_education_loan",
            "sec_24_home_loan_interest", "sec_80g_donations", "sec_80tta_savings_interest",
            "hra_rent_paid_annual", "hra_metro_city", "hra_landlord_pan", "hra_landlord_name",
            "other_income_annual", "prev_employer_income", "prev_employer_tds", "prev_employer_pf",
            "prev_employer_pt", "status", "submitted_at", "verified_by", "verified_at", "locked_at",
            "proof_documents", "created_at", "updated_at"
        };

        public EmployeeTaxDeclarationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<EmployeeTaxDeclaration?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeeTaxDeclaration>(
                "SELECT * FROM employee_tax_declarations WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<EmployeeTaxDeclaration>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeTaxDeclaration>(
                "SELECT * FROM employee_tax_declarations ORDER BY financial_year DESC, created_at DESC");
        }

        public async Task<EmployeeTaxDeclaration?> GetByEmployeeAndYearAsync(Guid employeeId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EmployeeTaxDeclaration>(
                "SELECT * FROM employee_tax_declarations WHERE employee_id = @employeeId AND financial_year = @financialYear",
                new { employeeId, financialYear });
        }

        public async Task<IEnumerable<EmployeeTaxDeclaration>> GetByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeTaxDeclaration>(
                "SELECT * FROM employee_tax_declarations WHERE employee_id = @employeeId ORDER BY financial_year DESC",
                new { employeeId });
        }

        public async Task<IEnumerable<EmployeeTaxDeclaration>> GetByFinancialYearAsync(string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EmployeeTaxDeclaration>(
                "SELECT * FROM employee_tax_declarations WHERE financial_year = @financialYear ORDER BY created_at DESC",
                new { financialYear });
        }

        public async Task<IEnumerable<EmployeeTaxDeclaration>> GetPendingVerificationAsync(string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM employee_tax_declarations WHERE status = 'submitted'";

            if (!string.IsNullOrEmpty(financialYear))
            {
                sql += " AND financial_year = @financialYear";
            }

            sql += " ORDER BY submitted_at ASC";

            return await connection.QueryAsync<EmployeeTaxDeclaration>(sql, new { financialYear });
        }

        public async Task<(IEnumerable<EmployeeTaxDeclaration> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Build WHERE clause manually to support company filtering via JOIN
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            var paramIndex = 0;

            // Handle company_id filter by joining with employees table
            Guid? companyId = null;
            if (filters != null && filters.TryGetValue("company_id", out var companyIdObj))
            {
                companyId = companyIdObj as Guid?;
            }

            // Apply other filters
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    if (filter.Key == "company_id") continue; // Handle separately via JOIN
                    if (filter.Value != null && AllowedColumns.Contains(filter.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        var paramName = $"p{paramIndex++}";
                        whereClauses.Add($"etd.{filter.Key} = @{paramName}");
                        parameters.Add(paramName, filter.Value);
                    }
                }
            }

            // Apply search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchParam = $"p{paramIndex++}";
                var searchValue = $"%{searchTerm}%";
                whereClauses.Add($"(etd.financial_year::text ILIKE @{searchParam} OR etd.tax_regime::text ILIKE @{searchParam} OR etd.status::text ILIKE @{searchParam} OR etd.hra_landlord_name::text ILIKE @{searchParam} OR e.employee_name ILIKE @{searchParam})");
                parameters.Add(searchParam, searchValue);
            }

            // Add company filter via JOIN
            if (companyId.HasValue)
            {
                var companyParam = $"p{paramIndex++}";
                whereClauses.Add($"e.company_id = @{companyParam}");
                parameters.Add(companyParam, companyId.Value);
            }

            var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            // Build ORDER BY
            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderByColumn = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            var orderBy = $"ORDER BY etd.{orderByColumn} {(sortDescending ? "DESC" : "ASC")}";

            // Build pagination
            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", pageSize);

            // Build SQL with JOIN to employees table for company filtering
            var dataSql = $@"
                SELECT etd.*
                FROM employee_tax_declarations etd
                LEFT JOIN employees e ON etd.employee_id = e.id
                {whereClause}
                {orderBy}
                LIMIT @limit OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*)
                FROM employee_tax_declarations etd
                LEFT JOIN employees e ON etd.employee_id = e.id
                {whereClause}";

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<EmployeeTaxDeclaration>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<EmployeeTaxDeclaration> AddAsync(EmployeeTaxDeclaration entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO employee_tax_declarations
                (employee_id, financial_year, tax_regime, sec_80c_ppf, sec_80c_elss, sec_80c_life_insurance,
                 sec_80c_home_loan_principal, sec_80c_children_tuition, sec_80c_nsc, sec_80c_sukanya_samriddhi,
                 sec_80c_fixed_deposit, sec_80c_others, sec_80ccd_nps, sec_80d_self_family, sec_80d_parents,
                 sec_80d_preventive_checkup, sec_80d_self_senior_citizen, sec_80d_parents_senior_citizen,
                 sec_80e_education_loan, sec_24_home_loan_interest, sec_80g_donations, sec_80tta_savings_interest,
                 hra_rent_paid_annual, hra_metro_city, hra_landlord_pan, hra_landlord_name,
                 other_income_annual, prev_employer_income, prev_employer_tds, prev_employer_pf,
                 prev_employer_pt, status, submitted_at, verified_by, verified_at, locked_at,
                 proof_documents, created_at, updated_at)
                VALUES
                (@EmployeeId, @FinancialYear, @TaxRegime, @Sec80cPpf, @Sec80cElss, @Sec80cLifeInsurance,
                 @Sec80cHomeLoanPrincipal, @Sec80cChildrenTuition, @Sec80cNsc, @Sec80cSukanyaSamriddhi,
                 @Sec80cFixedDeposit, @Sec80cOthers, @Sec80ccdNps, @Sec80dSelfFamily, @Sec80dParents,
                 @Sec80dPreventiveCheckup, @Sec80dSelfSeniorCitizen, @Sec80dParentsSeniorCitizen,
                 @Sec80eEducationLoan, @Sec24HomeLoanInterest, @Sec80gDonations, @Sec80ttaSavingsInterest,
                 @HraRentPaidAnnual, @HraMetroCity, @HraLandlordPan, @HraLandlordName,
                 @OtherIncomeAnnual, @PrevEmployerIncome, @PrevEmployerTds, @PrevEmployerPf,
                 @PrevEmployerPt, @Status, @SubmittedAt, @VerifiedBy, @VerifiedAt, @LockedAt,
                 CASE WHEN @ProofDocuments IS NULL OR @ProofDocuments = '' THEN NULL::jsonb ELSE CAST(@ProofDocuments AS jsonb) END, NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<EmployeeTaxDeclaration>(sql, entity);
        }

        public async Task UpdateAsync(EmployeeTaxDeclaration entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE employee_tax_declarations SET
                employee_id = @EmployeeId,
                financial_year = @FinancialYear,
                tax_regime = @TaxRegime,
                sec_80c_ppf = @Sec80cPpf,
                sec_80c_elss = @Sec80cElss,
                sec_80c_life_insurance = @Sec80cLifeInsurance,
                sec_80c_home_loan_principal = @Sec80cHomeLoanPrincipal,
                sec_80c_children_tuition = @Sec80cChildrenTuition,
                sec_80c_nsc = @Sec80cNsc,
                sec_80c_sukanya_samriddhi = @Sec80cSukanyaSamriddhi,
                sec_80c_fixed_deposit = @Sec80cFixedDeposit,
                sec_80c_others = @Sec80cOthers,
                sec_80ccd_nps = @Sec80ccdNps,
                sec_80d_self_family = @Sec80dSelfFamily,
                sec_80d_parents = @Sec80dParents,
                sec_80d_preventive_checkup = @Sec80dPreventiveCheckup,
                sec_80d_self_senior_citizen = @Sec80dSelfSeniorCitizen,
                sec_80d_parents_senior_citizen = @Sec80dParentsSeniorCitizen,
                sec_80e_education_loan = @Sec80eEducationLoan,
                sec_24_home_loan_interest = @Sec24HomeLoanInterest,
                sec_80g_donations = @Sec80gDonations,
                sec_80tta_savings_interest = @Sec80ttaSavingsInterest,
                hra_rent_paid_annual = @HraRentPaidAnnual,
                hra_metro_city = @HraMetroCity,
                hra_landlord_pan = @HraLandlordPan,
                hra_landlord_name = @HraLandlordName,
                other_income_annual = @OtherIncomeAnnual,
                prev_employer_income = @PrevEmployerIncome,
                prev_employer_tds = @PrevEmployerTds,
                prev_employer_pf = @PrevEmployerPf,
                prev_employer_pt = @PrevEmployerPt,
                status = @Status,
                submitted_at = @SubmittedAt,
                verified_by = @VerifiedBy,
                verified_at = @VerifiedAt,
                locked_at = @LockedAt,
                proof_documents = CASE WHEN @ProofDocuments IS NULL OR @ProofDocuments = '' THEN NULL::jsonb ELSE CAST(@ProofDocuments AS jsonb) END,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM employee_tax_declarations WHERE id = @id", new { id });
        }

        public async Task<bool> ExistsForEmployeeAndYearAsync(Guid employeeId, string financialYear, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM employee_tax_declarations WHERE employee_id = @employeeId AND financial_year = @financialYear AND id != @excludeId"
                : "SELECT COUNT(*) FROM employee_tax_declarations WHERE employee_id = @employeeId AND financial_year = @financialYear";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { employeeId, financialYear, excludeId });
            return count > 0;
        }

        public async Task UpdateStatusAsync(Guid id, string status, string? verifiedBy = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE employee_tax_declarations SET
                status = @status,
                verified_by = CASE WHEN @status = 'verified' THEN @verifiedBy ELSE verified_by END,
                verified_at = CASE WHEN @status = 'verified' THEN NOW() ELSE verified_at END,
                locked_at = CASE WHEN @status = 'locked' THEN NOW() ELSE locked_at END,
                submitted_at = CASE WHEN @status = 'submitted' THEN NOW() ELSE submitted_at END,
                updated_at = NOW()
                WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, status, verifiedBy });
        }

        public async Task LockDeclarationsAsync(string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE employee_tax_declarations SET
                status = 'locked',
                locked_at = NOW(),
                updated_at = NOW()
                WHERE financial_year = @financialYear AND status = 'verified'";
            await connection.ExecuteAsync(sql, new { financialYear });
        }
    }
}
