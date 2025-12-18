using Core.Entities;
using Core.Interfaces;
using Dapper;
using Infrastructure.Data.Common;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class QuotesRepository : IQuotesRepository
    {
        private readonly string _connectionString;

        // Whitelist of allowed columns for sorting and filtering
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "company_id", "customer_id", "quote_number", "quote_date",
            "expiry_date", "status", "total_amount", "created_at", "notes", "project_name"
        };

        // Columns searchable via ILIKE
        private static readonly string[] SearchableColumns = new[]
        {
            "quote_number", "status", "notes", "project_name"
        };

        public QuotesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Quotes?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Quotes>(
                "SELECT * FROM quotes WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<Quotes>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Quotes>(
                "SELECT * FROM quotes ORDER BY created_at DESC");
        }

        public async Task<(IEnumerable<Quotes> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Use SqlQueryBuilder for secure parameterized queries
            var builder = SqlQueryBuilder.From("quotes", AllowedColumns);

            // Apply search across multiple columns
            builder.SearchAcross(SearchableColumns, searchTerm);

            // Apply filters using safe parameterized queries
            if (filters != null)
            {
                // Map filter keys to database column names
                var dbFilters = new Dictionary<string, object?>();
                if (filters.TryGetValue("companyId", out var companyId) && companyId != null)
                    dbFilters["company_id"] = companyId;
                if (filters.TryGetValue("customerId", out var customerId) && customerId != null)
                    dbFilters["customer_id"] = customerId;
                if (filters.TryGetValue("status", out var status) && !string.IsNullOrEmpty(status?.ToString()))
                    dbFilters["status"] = status;

                builder.ApplyFilters(dbFilters);
            }

            // Apply sorting with column whitelist validation
            var sortColumn = !string.IsNullOrEmpty(sortBy) && AllowedColumns.Contains(sortBy.ToLower())
                ? sortBy.ToLower()
                : "created_at";
            builder.OrderBy(sortColumn, sortDescending || (sortColumn == "created_at" && string.IsNullOrEmpty(sortBy)));

            // Apply pagination
            builder.Paginate(pageNumber, pageSize);

            // Get total count using the builder
            var (countSql, countParams) = builder.BuildCount();
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, countParams);

            // Get items using the builder
            var (itemsSql, itemsParams) = builder.BuildSelect();
            var items = await connection.QueryAsync<Quotes>(itemsSql, itemsParams);

            return (items, totalCount);
        }

        public async Task<Quotes> AddAsync(Quotes entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                INSERT INTO quotes (
                    id, company_id, customer_id, quote_number, quote_date, expiry_date,
                    status, subtotal, discount_type, discount_value, discount_amount,
                    tax_amount, total_amount, currency, notes, terms, payment_instructions,
                    po_number, project_name, sent_at, viewed_at, accepted_at, rejected_at,
                    rejected_reason, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @CustomerId, @QuoteNumber, @QuoteDate, @ExpiryDate,
                    @Status, @Subtotal, @DiscountType, @DiscountValue, @DiscountAmount,
                    @TaxAmount, @TotalAmount, @Currency, @Notes, @Terms, @PaymentInstructions,
                    @PoNumber, @ProjectName, @SentAt, @ViewedAt, @AcceptedAt, @RejectedAt,
                    @RejectedReason, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(query, entity);
            return entity;
        }

        public async Task UpdateAsync(Quotes entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                UPDATE quotes SET
                    company_id = @CompanyId,
                    customer_id = @CustomerId,
                    quote_number = @QuoteNumber,
                    quote_date = @QuoteDate,
                    expiry_date = @ExpiryDate,
                    status = @Status,
                    subtotal = @Subtotal,
                    discount_type = @DiscountType,
                    discount_value = @DiscountValue,
                    discount_amount = @DiscountAmount,
                    tax_amount = @TaxAmount,
                    total_amount = @TotalAmount,
                    currency = @Currency,
                    notes = @Notes,
                    terms = @Terms,
                    payment_instructions = @PaymentInstructions,
                    po_number = @PoNumber,
                    project_name = @ProjectName,
                    sent_at = @SentAt,
                    viewed_at = @ViewedAt,
                    accepted_at = @AcceptedAt,
                    rejected_at = @RejectedAt,
                    rejected_reason = @RejectedReason,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(query, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM quotes WHERE id = @id",
                new { id });
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM quotes WHERE id = @id",
                new { id });
            return count > 0;
        }
    }
}
