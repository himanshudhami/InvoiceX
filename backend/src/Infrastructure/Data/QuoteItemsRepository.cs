using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class QuoteItemsRepository : IQuoteItemsRepository
    {
        private readonly string _connectionString;

        public QuoteItemsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<QuoteItems?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<QuoteItems>(
                $"SELECT * FROM quote_items WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<QuoteItems>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<QuoteItems>(
                $"SELECT * FROM quote_items ORDER BY sort_order");
        }

        public async Task<(IEnumerable<QuoteItems> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "quote_id", "product_id", "description", "quantity", "unit_price", "tax_rate", "discount_rate", "line_total", "sort_order", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("quote_items", allowedColumns)
                .SearchAcross(new string[] { "description" }, searchTerm)
                .ApplyFilters(filters)
                .OrderBy(sortBy ?? "sort_order", sortDescending)
                .Paginate(pageNumber, pageSize);

            var (countQuery, countParams) = builder.BuildCount();
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, countParams);

            var (selectQuery, selectParams) = builder.BuildSelect();
            var items = await connection.QueryAsync<QuoteItems>(selectQuery, selectParams);

            return (items, totalCount);
        }

        public async Task<QuoteItems> AddAsync(QuoteItems entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                INSERT INTO quote_items (
                    id, quote_id, product_id, description, quantity, unit_price,
                    tax_rate, discount_rate, line_total, sort_order, created_at, updated_at
                ) VALUES (
                    @Id, @QuoteId, @ProductId, @Description, @Quantity, @UnitPrice,
                    @TaxRate, @DiscountRate, @LineTotal, @SortOrder, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(query, entity);
            return entity;
        }

        public async Task UpdateAsync(QuoteItems entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                UPDATE quote_items SET
                    quote_id = @QuoteId,
                    product_id = @ProductId,
                    description = @Description,
                    quantity = @Quantity,
                    unit_price = @UnitPrice,
                    tax_rate = @TaxRate,
                    discount_rate = @DiscountRate,
                    line_total = @LineTotal,
                    sort_order = @SortOrder,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(query, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM quote_items WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<QuoteItems>> GetByQuoteIdAsync(Guid quoteId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<QuoteItems>(
                $"SELECT * FROM quote_items WHERE quote_id = @quoteId ORDER BY sort_order",
                new { quoteId });
        }
    }
}
