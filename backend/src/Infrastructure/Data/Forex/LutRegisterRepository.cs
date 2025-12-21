using Core.Entities.Forex;
using Core.Interfaces.Forex;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Forex
{
    public class LutRegisterRepository : ILutRegisterRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "lut_number", "financial_year", "gstin",
            "valid_from", "valid_to", "filing_date", "arn", "status",
            "created_at", "updated_at", "created_by", "notes"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "lut_number", "financial_year", "gstin", "arn"
        };

        public LutRegisterRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<LutRegister?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LutRegister>(
                "SELECT * FROM lut_register WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<LutRegister>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LutRegister>(
                "SELECT * FROM lut_register ORDER BY valid_from DESC");
        }

        public async Task<(IEnumerable<LutRegister> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null,
            string? sortBy = null, bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("lut_register", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "valid_from";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<LutRegister>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<LutRegister> AddAsync(LutRegister entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO lut_register (
                    company_id, lut_number, financial_year, gstin,
                    valid_from, valid_to, filing_date, arn, status,
                    created_at, updated_at, created_by, notes
                ) VALUES (
                    @CompanyId, @LutNumber, @FinancialYear, @Gstin,
                    @ValidFrom, @ValidTo, @FilingDate, @Arn, @Status,
                    NOW(), NOW(), @CreatedBy, @Notes
                ) RETURNING *";
            return await connection.QuerySingleAsync<LutRegister>(sql, entity);
        }

        public async Task UpdateAsync(LutRegister entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE lut_register SET
                    company_id = @CompanyId, lut_number = @LutNumber,
                    financial_year = @FinancialYear, gstin = @Gstin,
                    valid_from = @ValidFrom, valid_to = @ValidTo,
                    filing_date = @FilingDate, arn = @Arn, status = @Status,
                    updated_at = NOW(), notes = @Notes
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM lut_register WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<LutRegister>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LutRegister>(
                "SELECT * FROM lut_register WHERE company_id = @companyId ORDER BY valid_from DESC",
                new { companyId });
        }

        public async Task<LutRegister?> GetActiveForCompanyAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LutRegister>(
                @"SELECT * FROM lut_register
                  WHERE company_id = @companyId
                    AND financial_year = @financialYear
                    AND status = 'active'
                  ORDER BY valid_from DESC
                  LIMIT 1",
                new { companyId, financialYear });
        }

        public async Task<LutRegister?> GetValidForDateAsync(Guid companyId, DateOnly date)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LutRegister>(
                @"SELECT * FROM lut_register
                  WHERE company_id = @companyId
                    AND status = 'active'
                    AND valid_from <= @date AND valid_to >= @date
                  ORDER BY valid_from DESC
                  LIMIT 1",
                new { companyId, date });
        }

        public async Task<bool> IsLutValidAsync(Guid companyId, DateOnly invoiceDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM lut_register
                  WHERE company_id = @companyId
                    AND status = 'active'
                    AND valid_from <= @invoiceDate AND valid_to >= @invoiceDate",
                new { companyId, invoiceDate });
            return count > 0;
        }

        public async Task ExpireOldLutsAsync(DateOnly asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE lut_register SET
                    status = 'expired', updated_at = NOW()
                  WHERE status = 'active' AND valid_to < @asOfDate",
                new { asOfDate });
        }

        public async Task SupersedeAsync(Guid oldLutId, Guid newLutId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE lut_register SET
                    status = 'superseded', updated_at = NOW(),
                    notes = COALESCE(notes, '') || ' Superseded by ' || @newLutId::text
                  WHERE id = @oldLutId AND status = 'active'",
                new { oldLutId, newLutId });
        }
    }
}
