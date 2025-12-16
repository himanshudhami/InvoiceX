using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class CustomersRepository : ICustomersRepository
    {
        private readonly string _connectionString;

        public CustomersRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Customers?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Customers>(
                $"SELECT * FROM customers WHERE id = @id", 
                new { id });
        }

        public async Task<IEnumerable<Customers>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Customers>(
                $"SELECT * FROM customers");
        }

        public async Task<(IEnumerable<Customers> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "company_id", "name", "company_name", "email", "phone", "address_line1", "address_line2", "city", "state", "zip_code", "country", "tax_number", "notes", "credit_limit", "payment_terms", "is_active", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("customers", allowedColumns)
                .SearchAcross(new string[] { "name", "company_name", "email", "phone", "address_line1", "address_line2", "city", "state", "zip_code", "country", "tax_number", "notes",  }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, System.StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "id";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Customers>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Customers> AddAsync(Customers entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO customers 
                (company_id, name, company_name, email, phone, address_line1, address_line2, city, state, zip_code, country, tax_number, notes, credit_limit, payment_terms, is_active, created_at, updated_at)
                VALUES
                (@CompanyId, @Name, @CompanyName, @Email, @Phone, @AddressLine1, @AddressLine2, @City, @State, @ZipCode, @Country, @TaxNumber, @Notes, @CreditLimit, @PaymentTerms, @IsActive, NOW(), NOW())
                RETURNING *";
            
            var createdEntity = await connection.QuerySingleAsync<Customers>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(Customers entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE customers SET
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                company_id = @CompanyId,
                name = @Name,
                company_name = @CompanyName,
                email = @Email,
                phone = @Phone,
                address_line1 = @AddressLine1,
                address_line2 = @AddressLine2,
                city = @City,
                state = @State,
                zip_code = @ZipCode,
                country = @Country,
                tax_number = @TaxNumber,
                notes = @Notes,
                credit_limit = @CreditLimit,
                payment_terms = @PaymentTerms,
                is_active = @IsActive,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM customers WHERE id = @id", 
                new { id });
        }
    }
}