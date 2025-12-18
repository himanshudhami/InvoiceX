using Core.Entities.Leave;
using Core.Interfaces.Leave;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Leave
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly string _connectionString;

        public HolidayRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Holiday?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Holiday>(
                "SELECT * FROM holidays WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<Holiday>> GetByYearAsync(int year)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Holiday>(
                "SELECT * FROM holidays WHERE year = @year ORDER BY date",
                new { year });
        }

        public async Task<IEnumerable<Holiday>> GetByCompanyAndYearAsync(Guid companyId, int year)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Holiday>(
                "SELECT * FROM holidays WHERE company_id = @companyId AND year = @year ORDER BY date",
                new { companyId, year });
        }

        public async Task<IEnumerable<Holiday>> GetByCompanyAndDateRangeAsync(Guid companyId, DateTime fromDate, DateTime toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Holiday>(
                @"SELECT * FROM holidays
                  WHERE company_id = @companyId
                    AND date >= @fromDate
                    AND date <= @toDate
                  ORDER BY date",
                new { companyId, fromDate, toDate });
        }

        public async Task<Holiday?> GetByCompanyAndDateAsync(Guid companyId, DateTime date)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Holiday>(
                "SELECT * FROM holidays WHERE company_id = @companyId AND date = @date",
                new { companyId, date });
        }

        public async Task<Holiday> AddAsync(Holiday entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                INSERT INTO holidays (
                    company_id, name, date, year, is_optional, description,
                    created_at, updated_at
                ) VALUES (
                    @CompanyId, @Name, @Date, @Year, @IsOptional, @Description,
                    NOW(), NOW()
                ) RETURNING *";

            return await connection.QuerySingleAsync<Holiday>(sql, entity);
        }

        public async Task UpdateAsync(Holiday entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                UPDATE holidays SET
                    name = @Name,
                    date = @Date,
                    year = @Year,
                    is_optional = @IsOptional,
                    description = @Description,
                    updated_at = NOW()
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM holidays WHERE id = @id", new { id });
        }

        public async Task<bool> ExistsAsync(Guid companyId, DateTime date, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM holidays WHERE company_id = @companyId AND date = @date AND id != @excludeId"
                : "SELECT COUNT(*) FROM holidays WHERE company_id = @companyId AND date = @date";
            var count = await connection.QuerySingleAsync<int>(sql, new { companyId, date, excludeId });
            return count > 0;
        }

        public async Task<int> GetHolidayCountAsync(Guid companyId, DateTime fromDate, DateTime toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QuerySingleAsync<int>(
                @"SELECT COUNT(*)
                  FROM holidays
                  WHERE company_id = @companyId
                    AND date >= @fromDate
                    AND date <= @toDate",
                new { companyId, fromDate, toDate });
        }

        public async Task<bool> IsHolidayAsync(Guid companyId, DateTime date)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM holidays WHERE company_id = @companyId AND date = @date",
                new { companyId, date });
            return count > 0;
        }
    }
}
