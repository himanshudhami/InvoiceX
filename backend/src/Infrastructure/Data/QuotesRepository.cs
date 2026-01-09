using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class QuotesRepository : IQuotesRepository
    {
        private readonly string _connectionString;

        // Whitelist of allowed columns for sorting and filtering
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "company_id", "party_id", "quote_number", "quote_date",
            "valid_until", "status", "subtotal", "tax_amount", "discount_amount",
            "total_amount", "currency", "notes", "terms",
            "converted_to_invoice_id", "converted_at",
            "created_at", "updated_at"
        };

        // Columns searchable via ILIKE
        private static readonly string[] SearchableColumns = new[]
        {
            "quote_number", "status", "notes", "terms"
        };

        private const string SelectColumns = @"
            id,
            company_id,
            party_id,
            quote_number,
            quote_date,
            valid_until,
            status,
            subtotal,
            tax_amount,
            discount_amount,
            total_amount,
            currency,
            notes,
            terms,
            converted_to_invoice_id,
            converted_at,
            created_at,
            updated_at";

        public QuotesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Quotes?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Quotes>(
                $"SELECT {SelectColumns} FROM quotes WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<Quotes>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Quotes>(
                $"SELECT {SelectColumns} FROM quotes ORDER BY created_at DESC");
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

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                conditions.Add($"({string.Join(" OR ", SearchableColumns.Select(c => $"{c} ILIKE @searchTerm"))})");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            if (filters != null)
            {
                if (filters.TryGetValue("companyId", out var companyId) && companyId != null)
                {
                    conditions.Add("company_id = @companyId");
                    parameters.Add("companyId", companyId);
                }

                if (filters.TryGetValue("partyId", out var partyId) && partyId != null)
                {
                    conditions.Add("party_id = @partyId");
                    parameters.Add("partyId", partyId);
                }

                if (filters.TryGetValue("status", out var status) && !string.IsNullOrWhiteSpace(status?.ToString()))
                {
                    conditions.Add("status = @status");
                    parameters.Add("status", status);
                }

                if (filters.TryGetValue("valid_until_from", out var validFrom) && validFrom != null)
                {
                    conditions.Add("valid_until >= @validUntilFrom");
                    parameters.Add("validUntilFrom", validFrom);
                }

                if (filters.TryGetValue("valid_until_to", out var validTo) && validTo != null)
                {
                    conditions.Add("valid_until <= @validUntilTo");
                    parameters.Add("validUntilTo", validTo);
                }

                if (filters.TryGetValue("total_amount_from", out var totalFrom) && totalFrom != null)
                {
                    conditions.Add("total_amount >= @totalAmountFrom");
                    parameters.Add("totalAmountFrom", totalFrom);
                }

                if (filters.TryGetValue("total_amount_to", out var totalTo) && totalTo != null)
                {
                    conditions.Add("total_amount <= @totalAmountTo");
                    parameters.Add("totalAmountTo", totalTo);
                }
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            var sortColumn = !string.IsNullOrWhiteSpace(sortBy) && AllowedColumns.Contains(sortBy.ToLower())
                ? sortBy.ToLower()
                : "created_at";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("limit", pageSize);
            parameters.Add("offset", offset);

            var dataSql = $@"
                SELECT {SelectColumns}
                FROM quotes
                {whereClause}
                ORDER BY {sortColumn} {sortDirection}
                LIMIT @limit OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*)
                FROM quotes
                {whereClause}";

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Quotes>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Quotes> AddAsync(Quotes entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                INSERT INTO quotes (
                    id, company_id, party_id, quote_number, quote_date, valid_until,
                    status, subtotal, tax_amount, discount_amount,
                    total_amount, currency, notes, terms,
                    created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @PartyId, @QuoteNumber, @QuoteDate, @ValidUntil,
                    @Status, @Subtotal, @TaxAmount, @DiscountAmount,
                    @TotalAmount, @Currency, @Notes, @Terms,
                    @CreatedAt, @UpdatedAt
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
                    party_id = @PartyId,
                    quote_number = @QuoteNumber,
                    quote_date = @QuoteDate,
                    valid_until = @ValidUntil,
                    status = @Status,
                    subtotal = @Subtotal,
                    discount_amount = @DiscountAmount,
                    tax_amount = @TaxAmount,
                    total_amount = @TotalAmount,
                    currency = @Currency,
                    notes = @Notes,
                    terms = @Terms,
                    converted_to_invoice_id = @ConvertedToInvoiceId,
                    converted_at = @ConvertedAt,
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
