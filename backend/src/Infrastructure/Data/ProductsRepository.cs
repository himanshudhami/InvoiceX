using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly string _connectionString;

        public ProductsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Products?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Products>(
                $"SELECT * FROM products WHERE id = @id", 
                new { id });
        }

        public async Task<IEnumerable<Products>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Products>(
                $"SELECT * FROM products");
        }

        public async Task<(IEnumerable<Products> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "company_id", "name", "description", "sku", "category", "type", "unit_price", "unit", "tax_rate", "is_active", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("products", allowedColumns)
                .SearchAcross(new string[] { "name", "description", "sku", "category", "type", "unit",  }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, System.StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "id";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Products>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Products> AddAsync(Products entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO products 
                (company_id, name, description, sku, category, type, unit_price, unit, tax_rate, is_active, created_at, updated_at)
                VALUES
                (@CompanyId, @Name, @Description, @Sku, @Category, @Type, @UnitPrice, @Unit, @TaxRate, @IsActive, NOW(), NOW())
                RETURNING *";
            
            var createdEntity = await connection.QuerySingleAsync<Products>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(Products entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE products SET
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                company_id = @CompanyId,
                name = @Name,
                description = @Description,
                sku = @Sku,
                category = @Category,
                type = @Type,
                unit_price = @UnitPrice,
                unit = @Unit,
                tax_rate = @TaxRate,
                is_active = @IsActive,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM products WHERE id = @id", 
                new { id });
        }
    }
}