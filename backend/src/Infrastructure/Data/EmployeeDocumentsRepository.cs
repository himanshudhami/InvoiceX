using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data;

public class EmployeeDocumentsRepository : IEmployeeDocumentsRepository
{
    private readonly string _connectionString;

    public EmployeeDocumentsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<EmployeeDocument?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT d.*, e.employee_name
            FROM employee_documents d
            LEFT JOIN employees e ON d.employee_id = e.id
            WHERE d.id = @id";

        return await connection.QueryFirstOrDefaultAsync<EmployeeDocument>(sql, new { id });
    }

    public async Task<IEnumerable<EmployeeDocument>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT d.*, e.employee_name
            FROM employee_documents d
            LEFT JOIN employees e ON d.employee_id = e.id
            ORDER BY d.created_at DESC";

        return await connection.QueryAsync<EmployeeDocument>(sql);
    }

    public async Task<(IEnumerable<EmployeeDocument> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var allowedColumns = new[] { "id", "title", "document_type", "file_name", "financial_year", "created_at", "company_id", "employee_id", "is_company_wide" };

        var builder = SqlQueryBuilder
            .From("employee_documents d LEFT JOIN employees e ON d.employee_id = e.id", allowedColumns)
            .Select("d.*, e.employee_name")
            .SearchAcross(new[] { "d.title", "d.file_name", "d.description" }, searchTerm)
            .ApplyFilters(filters)
            .OrderBy(sortBy ?? "created_at", sortDescending)
            .Paginate(pageNumber, pageSize);

        var (sql, parameters) = builder.BuildSelect();
        var items = await connection.QueryAsync<EmployeeDocument>(sql, parameters);

        var (countSql, countParameters) = builder.BuildCount();
        var totalCount = await connection.QuerySingleAsync<int>(countSql, countParameters);

        return (items, totalCount);
    }

    public async Task<IEnumerable<EmployeeDocument>> GetByEmployeeAsync(Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<EmployeeDocument>(
            "SELECT * FROM employee_documents WHERE employee_id = @employeeId ORDER BY created_at DESC",
            new { employeeId });
    }

    public async Task<IEnumerable<EmployeeDocument>> GetByCompanyAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT d.*, e.employee_name
            FROM employee_documents d
            LEFT JOIN employees e ON d.employee_id = e.id
            WHERE d.company_id = @companyId
            ORDER BY d.created_at DESC";

        return await connection.QueryAsync<EmployeeDocument>(sql, new { companyId });
    }

    public async Task<IEnumerable<EmployeeDocument>> GetCompanyWideDocumentsAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<EmployeeDocument>(
            "SELECT * FROM employee_documents WHERE company_id = @companyId AND is_company_wide = TRUE ORDER BY title",
            new { companyId });
    }

    public async Task<IEnumerable<EmployeeDocument>> GetByTypeAsync(Guid employeeId, string documentType)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<EmployeeDocument>(
            "SELECT * FROM employee_documents WHERE employee_id = @employeeId AND document_type = @documentType ORDER BY created_at DESC",
            new { employeeId, documentType });
    }

    public async Task<EmployeeDocument> AddAsync(EmployeeDocument entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO employee_documents (
                id, employee_id, company_id, document_type, title, description,
                file_url, file_name, file_size, mime_type, financial_year,
                is_company_wide, uploaded_by, created_at, updated_at
            ) VALUES (
                @Id, @EmployeeId, @CompanyId, @DocumentType, @Title, @Description,
                @FileUrl, @FileName, @FileSize, @MimeType, @FinancialYear,
                @IsCompanyWide, @UploadedBy, @CreatedAt, @UpdatedAt
            )";

        await connection.ExecuteAsync(sql, entity);
        return entity;
    }

    public async Task UpdateAsync(EmployeeDocument entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        entity.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE employee_documents SET
                title = @Title, description = @Description, document_type = @DocumentType,
                file_url = @FileUrl, file_name = @FileName, file_size = @FileSize,
                mime_type = @MimeType, financial_year = @FinancialYear,
                is_company_wide = @IsCompanyWide, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM employee_documents WHERE id = @id", new { id });
    }

    // Document Requests
    public async Task<DocumentRequest?> GetRequestByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT r.*, e.employee_name
            FROM document_requests r
            LEFT JOIN employees e ON r.employee_id = e.id
            WHERE r.id = @id";

        return await connection.QueryFirstOrDefaultAsync<DocumentRequest>(sql, new { id });
    }

    public async Task<IEnumerable<DocumentRequest>> GetRequestsByEmployeeAsync(Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<DocumentRequest>(
            "SELECT * FROM document_requests WHERE employee_id = @employeeId ORDER BY created_at DESC",
            new { employeeId });
    }

    public async Task<IEnumerable<DocumentRequest>> GetPendingRequestsAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT r.*, e.employee_name
            FROM document_requests r
            LEFT JOIN employees e ON r.employee_id = e.id
            WHERE e.company_id = @companyId AND r.status = 'pending'
            ORDER BY r.created_at ASC";

        return await connection.QueryAsync<DocumentRequest>(sql, new { companyId });
    }

    public async Task<(IEnumerable<DocumentRequest> Items, int TotalCount)> GetRequestsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var allowedColumns = new[] { "id", "document_type", "status", "purpose", "created_at" };

        var builder = SqlQueryBuilder
            .From("document_requests r LEFT JOIN employees e ON r.employee_id = e.id", allowedColumns)
            .Select("r.*, e.employee_name")
            .SearchAcross(new[] { "r.document_type", "r.purpose" }, searchTerm)
            .ApplyFilters(filters)
            .OrderBy(sortBy ?? "created_at", sortDescending)
            .Paginate(pageNumber, pageSize);

        var (sql, parameters) = builder.BuildSelect();
        var items = await connection.QueryAsync<DocumentRequest>(sql, parameters);

        var (countSql, countParameters) = builder.BuildCount();
        var totalCount = await connection.QuerySingleAsync<int>(countSql, countParameters);

        return (items, totalCount);
    }

    public async Task<DocumentRequest> AddRequestAsync(DocumentRequest request)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        request.Id = Guid.NewGuid();
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO document_requests (
                id, employee_id, document_type, purpose, status, created_at, updated_at
            ) VALUES (
                @Id, @EmployeeId, @DocumentType, @Purpose, @Status, @CreatedAt, @UpdatedAt
            )";

        await connection.ExecuteAsync(sql, request);
        return request;
    }

    public async Task UpdateRequestAsync(DocumentRequest request)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        request.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE document_requests SET
                status = @Status, processed_by = @ProcessedBy, processed_at = @ProcessedAt,
                rejection_reason = @RejectionReason, document_id = @DocumentId,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, request);
    }
}
