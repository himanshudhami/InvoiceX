using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class InvoicesRepository : IInvoicesRepository
    {
        private readonly string _connectionString;

        public InvoicesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Invoices?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Invoices>(
                $"SELECT * FROM invoices WHERE id = @id", 
                new { id });
        }

        public async Task<IEnumerable<Invoices>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Invoices>(
                $"SELECT * FROM invoices");
        }

        public async Task<(IEnumerable<Invoices> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "company_id", "customer_id", "invoice_number", "invoice_date", "due_date", "status", "subtotal", "tax_amount", "discount_amount", "total_amount", "paid_amount", "currency", "notes", "terms", "payment_instructions", "po_number", "project_name", "sent_at", "viewed_at", "paid_at", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("invoices", allowedColumns)
                .SearchAcross(new string[] { "invoice_number", "status", "currency", "notes", "terms", "payment_instructions", "po_number", "project_name",  }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, System.StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "id";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Invoices>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Invoices> AddAsync(Invoices entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO invoices 
                (company_id, customer_id, invoice_number, invoice_date, due_date, status, subtotal, tax_amount, discount_amount, total_amount, paid_amount, currency, notes, terms, payment_instructions, po_number, project_name, sent_at, viewed_at, paid_at, created_at, updated_at)
                VALUES
                (@CompanyId, @CustomerId, @InvoiceNumber, @InvoiceDate, @DueDate, @Status, @Subtotal, @TaxAmount, @DiscountAmount, @TotalAmount, @PaidAmount, @Currency, @Notes, @Terms, @PaymentInstructions, @PoNumber, @ProjectName, @SentAt, @ViewedAt, @PaidAt, NOW(), NOW())
                RETURNING *";
            
            var createdEntity = await connection.QuerySingleAsync<Invoices>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(Invoices entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE invoices SET
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                company_id = @CompanyId,
                customer_id = @CustomerId,
                invoice_number = @InvoiceNumber,
                invoice_date = @InvoiceDate,
                due_date = @DueDate,
                status = @Status,
                subtotal = @Subtotal,
                tax_amount = @TaxAmount,
                discount_amount = @DiscountAmount,
                total_amount = @TotalAmount,
                paid_amount = @PaidAmount,
                currency = @Currency,
                notes = @Notes,
                terms = @Terms,
                payment_instructions = @PaymentInstructions,
                po_number = @PoNumber,
                project_name = @ProjectName,
                sent_at = @SentAt,
                viewed_at = @ViewedAt,
                paid_at = @PaidAt,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM invoices WHERE id = @id", 
                new { id });
        }
    }
}