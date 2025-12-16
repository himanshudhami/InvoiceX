using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class InvoiceTemplatesRepository : IInvoiceTemplatesRepository
    {
        private readonly string _connectionString;

        public InvoiceTemplatesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<InvoiceTemplates?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<InvoiceTemplates>(
                $"SELECT * FROM invoice_templates WHERE id = @id", 
                new { id });
        }

        public async Task<IEnumerable<InvoiceTemplates>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<InvoiceTemplates>(
                $"SELECT * FROM invoice_templates");
        }

        public async Task<(IEnumerable<InvoiceTemplates> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] { "id", "company_id", "name", "template_data", "is_default", "created_at", "updated_at" };

            var builder = SqlQueryBuilder
                .From("invoice_templates", allowedColumns)
                .SearchAcross(new string[] { "name", "template_data",  }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, System.StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "id";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<InvoiceTemplates>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<InvoiceTemplates> AddAsync(InvoiceTemplates entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO invoice_templates 
                (company_id, name, template_data, is_default, created_at, updated_at)
                VALUES
                (@CompanyId, @Name, @TemplateData, @IsDefault, NOW(), NOW())
                RETURNING *";
            
            var createdEntity = await connection.QuerySingleAsync<InvoiceTemplates>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(InvoiceTemplates entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE invoice_templates SET
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                
                company_id = @CompanyId,
                name = @Name,
                template_data = @TemplateData,
                is_default = @IsDefault,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                $"DELETE FROM invoice_templates WHERE id = @id", 
                new { id });
        }
    }
}