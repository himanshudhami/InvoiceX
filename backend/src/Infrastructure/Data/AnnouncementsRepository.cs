using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data;

public class AnnouncementsRepository : IAnnouncementsRepository
{
    private readonly string _connectionString;

    public AnnouncementsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Announcement?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Announcement>(
            "SELECT * FROM announcements WHERE id = @id",
            new { id });
    }

    public async Task<IEnumerable<Announcement>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Announcement>(
            "SELECT * FROM announcements ORDER BY is_pinned DESC, published_at DESC");
    }

    public async Task<(IEnumerable<Announcement> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var allowedColumns = new[] { "id", "title", "category", "priority", "is_pinned", "published_at", "created_at" };

        var builder = SqlQueryBuilder
            .From("announcements", allowedColumns)
            .SearchAcross(new[] { "title", "content" }, searchTerm)
            .ApplyFilters(filters)
            .OrderBy(sortBy ?? "published_at", sortDescending)
            .Paginate(pageNumber, pageSize);

        var (sql, parameters) = builder.BuildSelect();
        var items = await connection.QueryAsync<Announcement>(sql, parameters);

        var (countSql, countParameters) = builder.BuildCount();
        var totalCount = await connection.QuerySingleAsync<int>(countSql, countParameters);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Announcement>> GetByCompanyAsync(Guid companyId, bool activeOnly = true)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM announcements
            WHERE company_id = @companyId";

        if (activeOnly)
        {
            sql += @" AND (published_at IS NULL OR published_at <= NOW())
                      AND (expires_at IS NULL OR expires_at > NOW())";
        }

        sql += " ORDER BY is_pinned DESC, published_at DESC";

        return await connection.QueryAsync<Announcement>(sql, new { companyId });
    }

    public async Task<IEnumerable<Announcement>> GetActiveForEmployeeAsync(Guid companyId, Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT a.*,
                   CASE WHEN ar.id IS NOT NULL THEN TRUE ELSE FALSE END as is_read
            FROM announcements a
            LEFT JOIN announcement_reads ar ON a.id = ar.announcement_id AND ar.employee_id = @employeeId
            WHERE a.company_id = @companyId
              AND (a.published_at IS NULL OR a.published_at <= NOW())
              AND (a.expires_at IS NULL OR a.expires_at > NOW())
            ORDER BY a.is_pinned DESC, a.published_at DESC";

        return await connection.QueryAsync<Announcement>(sql, new { companyId, employeeId });
    }

    public async Task<int> GetUnreadCountAsync(Guid companyId, Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT COUNT(*)
            FROM announcements a
            LEFT JOIN announcement_reads ar ON a.id = ar.announcement_id AND ar.employee_id = @employeeId
            WHERE a.company_id = @companyId
              AND ar.id IS NULL
              AND (a.published_at IS NULL OR a.published_at <= NOW())
              AND (a.expires_at IS NULL OR a.expires_at > NOW())";

        return await connection.QuerySingleAsync<int>(sql, new { companyId, employeeId });
    }

    public async Task<Announcement> AddAsync(Announcement entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO announcements (
                id, company_id, title, content, category, priority,
                is_pinned, published_at, expires_at, created_by,
                created_at, updated_at
            ) VALUES (
                @Id, @CompanyId, @Title, @Content, @Category, @Priority,
                @IsPinned, @PublishedAt, @ExpiresAt, @CreatedBy,
                @CreatedAt, @UpdatedAt
            )";

        await connection.ExecuteAsync(sql, entity);
        return entity;
    }

    public async Task UpdateAsync(Announcement entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        entity.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE announcements SET
                title = @Title, content = @Content, category = @Category,
                priority = @Priority, is_pinned = @IsPinned,
                published_at = @PublishedAt, expires_at = @ExpiresAt,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM announcements WHERE id = @id", new { id });
    }

    public async Task MarkAsReadAsync(Guid announcementId, Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO announcement_reads (id, announcement_id, employee_id, read_at)
            VALUES (@Id, @AnnouncementId, @EmployeeId, @ReadAt)
            ON CONFLICT (announcement_id, employee_id) DO NOTHING";

        await connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            AnnouncementId = announcementId,
            EmployeeId = employeeId,
            ReadAt = DateTime.UtcNow
        });
    }

    public async Task<bool> IsReadByEmployeeAsync(Guid announcementId, Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM announcement_reads WHERE announcement_id = @announcementId AND employee_id = @employeeId)",
            new { announcementId, employeeId });
    }
}
