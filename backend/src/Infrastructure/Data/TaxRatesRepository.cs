using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class TaxRatesRepository : ITaxRatesRepository
    {
        private readonly string _connectionString;

        public TaxRatesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<TaxRates?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TaxRates>(
                $"SELECT * FROM tax_rates WHERE id = @id", 
                new { id });
        }

        public async Task<IEnumerable<TaxRates>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<TaxRates>(
                $"SELECT * FROM tax_rates");
        }

        public async Task<(IEnumerable<TaxRates> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "company_id", "name", "rate", "is_default", "is_active", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("tax_rates", allowedColumns)
                .SearchAcross(new string[] { "name",  }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, System.StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "id";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<TaxRates>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<TaxRates> AddAsync(TaxRates entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO tax_rates 
                (company_id, name, rate, is_default, is_active, created_at, updated_at)
                VALUES
                (@CompanyId, @Name, @Rate, @IsDefault, @IsActive, NOW(), NOW())
                RETURNING *";
            
            var createdEntity = await connection.QuerySingleAsync<TaxRates>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(TaxRates entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE tax_rates SET
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                company_id = @CompanyId,
                name = @Name,
                rate = @Rate,
                is_default = @IsDefault,
                is_active = @IsActive,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM tax_rates WHERE id = @id", 
                new { id });
        }
    }
}