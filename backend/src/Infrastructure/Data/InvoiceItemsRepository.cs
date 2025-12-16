using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class InvoiceItemsRepository : IInvoiceItemsRepository
    {
        private readonly string _connectionString;

        public InvoiceItemsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<InvoiceItems?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<InvoiceItems>(
                $"SELECT * FROM invoice_items WHERE id = @id", 
                new { id });
        }

        public async Task<IEnumerable<InvoiceItems>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<InvoiceItems>(
                $"SELECT * FROM invoice_items");
        }

        public async Task<(IEnumerable<InvoiceItems> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "invoice_id", "product_id", "description", "quantity", "unit_price", "tax_rate", "discount_rate", "line_total", "sort_order", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("invoice_items", allowedColumns)
                .SearchAcross(new string[] { "description",  }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, System.StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "id";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<InvoiceItems>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<InvoiceItems> AddAsync(InvoiceItems entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO invoice_items 
                (invoice_id, product_id, description, quantity, unit_price, tax_rate, discount_rate, line_total, sort_order, created_at, updated_at)
                VALUES
                (@InvoiceId, @ProductId, @Description, @Quantity, @UnitPrice, @TaxRate, @DiscountRate, @LineTotal, @SortOrder, NOW(), NOW())
                RETURNING *";
            
            var createdEntity = await connection.QuerySingleAsync<InvoiceItems>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(InvoiceItems entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE invoice_items SET
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                invoice_id = @InvoiceId,
                product_id = @ProductId,
                description = @Description,
                quantity = @Quantity,
                unit_price = @UnitPrice,
                tax_rate = @TaxRate,
                discount_rate = @DiscountRate,
                line_total = @LineTotal,
                sort_order = @SortOrder,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM invoice_items WHERE id = @id", 
                new { id });
        }
    }
}