using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Payroll
{
    /// <summary>
    /// Repository for ESI eligibility period tracking.
    /// Implements the 6-month ESI contribution rule.
    /// </summary>
    public class EsiEligibilityRepository : IEsiEligibilityRepository
    {
        private readonly string _connectionString;

        public EsiEligibilityRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<EsiEligibilityPeriod?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EsiEligibilityPeriod>(
                "SELECT * FROM esi_eligibility_periods WHERE id = @id",
                new { id });
        }

        public async Task<EsiEligibilityPeriod?> GetActiveForEmployeeAsync(Guid employeeId, DateTime asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EsiEligibilityPeriod>(
                @"SELECT * FROM esi_eligibility_periods
                  WHERE employee_id = @employeeId
                    AND period_start <= @asOfDate
                    AND period_end >= @asOfDate
                    AND is_active = true
                  ORDER BY period_start DESC
                  LIMIT 1",
                new { employeeId, asOfDate });
        }

        public async Task<IEnumerable<EsiEligibilityPeriod>> GetByEmployeeAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EsiEligibilityPeriod>(
                "SELECT * FROM esi_eligibility_periods WHERE employee_id = @employeeId ORDER BY period_start DESC",
                new { employeeId });
        }

        public async Task<EsiEligibilityPeriod?> GetByEmployeeAndPeriodAsync(Guid employeeId, DateTime periodStart)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EsiEligibilityPeriod>(
                @"SELECT * FROM esi_eligibility_periods
                  WHERE employee_id = @employeeId AND period_start = @periodStart",
                new { employeeId, periodStart });
        }

        public async Task<EsiEligibilityPeriod> AddAsync(EsiEligibilityPeriod period)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO esi_eligibility_periods
                (employee_id, company_id, period_start, period_end, contribution_period,
                 financial_year, initial_gross_salary, was_eligible_at_start, is_active,
                 crossed_ceiling_date, crossed_ceiling_gross, created_at, updated_at)
                VALUES
                (@EmployeeId, @CompanyId, @PeriodStart, @PeriodEnd, @ContributionPeriod,
                 @FinancialYear, @InitialGrossSalary, @WasEligibleAtStart, @IsActive,
                 @CrossedCeilingDate, @CrossedCeilingGross, NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<EsiEligibilityPeriod>(sql, period);
        }

        public async Task UpdateAsync(EsiEligibilityPeriod period)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE esi_eligibility_periods SET
                is_active = @IsActive,
                crossed_ceiling_date = @CrossedCeilingDate,
                crossed_ceiling_gross = @CrossedCeilingGross,
                updated_at = NOW()
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, period);
        }

        /// <summary>
        /// Determine the ESI contribution period for a given payroll month/year.
        /// Period 1: April-September (returns April 1 as start)
        /// Period 2: October-March (returns October 1 as start)
        /// </summary>
        private (DateTime PeriodStart, DateTime PeriodEnd, string ContributionPeriod, string FinancialYear)
            GetContributionPeriod(int payrollMonth, int payrollYear)
        {
            // Determine if this month is in Apr-Sep or Oct-Mar period
            bool isAprSepPeriod = payrollMonth >= 4 && payrollMonth <= 9;

            DateTime periodStart, periodEnd;
            string contributionPeriod;
            string financialYear;

            if (isAprSepPeriod)
            {
                // April-September period
                periodStart = new DateTime(payrollYear, 4, 1);
                periodEnd = new DateTime(payrollYear, 9, 30);
                contributionPeriod = "apr_sep";
                // Financial year for Apr-Sep is current year - next year
                financialYear = $"{payrollYear}-{(payrollYear + 1) % 100:D2}";
            }
            else if (payrollMonth >= 10)
            {
                // October-December (same calendar year)
                periodStart = new DateTime(payrollYear, 10, 1);
                periodEnd = new DateTime(payrollYear + 1, 3, 31);
                contributionPeriod = "oct_mar";
                financialYear = $"{payrollYear}-{(payrollYear + 1) % 100:D2}";
            }
            else
            {
                // January-March (previous calendar year's Oct-Mar period)
                periodStart = new DateTime(payrollYear - 1, 10, 1);
                periodEnd = new DateTime(payrollYear, 3, 31);
                contributionPeriod = "oct_mar";
                financialYear = $"{payrollYear - 1}-{payrollYear % 100:D2}";
            }

            return (periodStart, periodEnd, contributionPeriod, financialYear);
        }

        public async Task<bool> IsEligibleForPeriodAsync(
            Guid employeeId,
            Guid companyId,
            decimal currentGross,
            int payrollMonth,
            int payrollYear,
            decimal esiCeiling)
        {
            var (periodStart, periodEnd, _, _) = GetContributionPeriod(payrollMonth, payrollYear);

            // Check if there's an existing eligibility record for this period
            var existingPeriod = await GetByEmployeeAndPeriodAsync(employeeId, periodStart);

            if (existingPeriod != null)
            {
                // Employee was tracked at the start of this period
                // If they were eligible at start, they must continue contributing
                return existingPeriod.WasEligibleAtStart;
            }

            // No record exists - this is potentially the first month of a new period
            // or employee is new. Check current gross against ceiling.
            return currentGross <= esiCeiling;
        }

        public async Task<EsiEligibilityPeriod> EnsureEligibilityPeriodAsync(
            Guid employeeId,
            Guid companyId,
            decimal currentGross,
            int payrollMonth,
            int payrollYear,
            decimal esiCeiling)
        {
            var (periodStart, periodEnd, contributionPeriod, financialYear) =
                GetContributionPeriod(payrollMonth, payrollYear);

            // Check if period record already exists
            var existingPeriod = await GetByEmployeeAndPeriodAsync(employeeId, periodStart);

            if (existingPeriod != null)
            {
                // Record exists - check if we need to update ceiling crossing
                if (existingPeriod.WasEligibleAtStart &&
                    existingPeriod.CrossedCeilingDate == null &&
                    currentGross > esiCeiling)
                {
                    // Employee's salary crossed ceiling this month
                    existingPeriod.CrossedCeilingDate = new DateTime(payrollYear, payrollMonth, 1);
                    existingPeriod.CrossedCeilingGross = currentGross;
                    await UpdateAsync(existingPeriod);
                }
                return existingPeriod;
            }

            // Create new eligibility record for this period
            var newPeriod = new EsiEligibilityPeriod
            {
                EmployeeId = employeeId,
                CompanyId = companyId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ContributionPeriod = contributionPeriod,
                FinancialYear = financialYear,
                InitialGrossSalary = currentGross,
                WasEligibleAtStart = currentGross <= esiCeiling,
                IsActive = true
            };

            return await AddAsync(newPeriod);
        }
    }
}
