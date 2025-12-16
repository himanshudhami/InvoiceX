using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class CompaniesRepository : ICompaniesRepository
    {
        private readonly string _connectionString;

        public CompaniesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Companies?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Companies>(
                $"SELECT * FROM companies WHERE id = @id", 
                new { id });
        }

        public async Task<IEnumerable<Companies>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Companies>(
                $"SELECT * FROM companies");
        }

        public async Task<(IEnumerable<Companies> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "name", "logo_url", "address_line1", "address_line2", "city", "state", "zip_code", "country", "email", "phone", "website", "tax_number", "payment_instructions", "signature_type", "signature_data", "signature_name", "signature_font", "signature_color", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("companies", allowedColumns)
                .SearchAcross(new string[] { "name", "logo_url", "address_line1", "address_line2", "city", "state", "zip_code", "country", "email", "phone", "website", "tax_number",  }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, System.StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "id";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<Companies>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<Companies> AddAsync(Companies entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO companies 
                (name, logo_url, address_line1, address_line2, city, state, zip_code, country, email, phone, website, tax_number, payment_instructions, signature_type, signature_data, signature_name, signature_font, signature_color, created_at, updated_at)
                VALUES
                (@Name, @LogoUrl, @AddressLine1, @AddressLine2, @City, @State, @ZipCode, @Country, @Email, @Phone, @Website, @TaxNumber, @PaymentInstructions, @SignatureType, @SignatureData, @SignatureName, @SignatureFont, @SignatureColor, NOW(), NOW())
                RETURNING *";
            
            var createdEntity = await connection.QuerySingleAsync<Companies>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(Companies entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE companies SET
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                name = @Name,
                logo_url = @LogoUrl,
                address_line1 = @AddressLine1,
                address_line2 = @AddressLine2,
                city = @City,
                state = @State,
                zip_code = @ZipCode,
                country = @Country,
                email = @Email,
                phone = @Phone,
                website = @Website,
                tax_number = @TaxNumber,
                payment_instructions = @PaymentInstructions,
                signature_type = @SignatureType,
                signature_data = @SignatureData,
                signature_name = @SignatureName,
                signature_font = @SignatureFont,
                signature_color = @SignatureColor,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM companies WHERE id = @id", 
                new { id });
        }
    }
}