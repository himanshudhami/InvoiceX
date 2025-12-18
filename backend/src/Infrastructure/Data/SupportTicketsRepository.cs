using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data;

public class SupportTicketsRepository : ISupportTicketsRepository
{
    private readonly string _connectionString;

    public SupportTicketsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT t.*,
                   e.employee_name,
                   u.email as assigned_to_name
            FROM support_tickets t
            LEFT JOIN employees e ON t.employee_id = e.id
            LEFT JOIN users u ON t.assigned_to = u.id
            WHERE t.id = @id";

        return await connection.QueryFirstOrDefaultAsync<SupportTicket>(sql, new { id });
    }

    public async Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var ticket = await GetByIdAsync(id);
        if (ticket != null)
        {
            var messages = await GetMessagesAsync(id);
            // Attach messages via a property or return tuple
        }
        return ticket;
    }

    public async Task<IEnumerable<SupportTicket>> GetAllAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT t.*,
                   e.employee_name
            FROM support_tickets t
            LEFT JOIN employees e ON t.employee_id = e.id
            ORDER BY t.created_at DESC";

        return await connection.QueryAsync<SupportTicket>(sql);
    }

    public async Task<(IEnumerable<SupportTicket> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var allowedColumns = new[] { "id", "ticket_number", "subject", "category", "priority", "status", "created_at" };

        var builder = SqlQueryBuilder
            .From("support_tickets t LEFT JOIN employees e ON t.employee_id = e.id", allowedColumns)
            .Select("t.*, e.employee_name")
            .SearchAcross(new[] { "t.ticket_number", "t.subject", "t.description" }, searchTerm)
            .ApplyFilters(filters)
            .OrderBy(sortBy ?? "created_at", sortDescending)
            .Paginate(pageNumber, pageSize);

        var (sql, parameters) = builder.BuildSelect();
        var items = await connection.QueryAsync<SupportTicket>(sql, parameters);

        var (countSql, countParameters) = builder.BuildCount();
        var totalCount = await connection.QuerySingleAsync<int>(countSql, countParameters);

        return (items, totalCount);
    }

    public async Task<IEnumerable<SupportTicket>> GetByEmployeeAsync(Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<SupportTicket>(
            "SELECT * FROM support_tickets WHERE employee_id = @employeeId ORDER BY created_at DESC",
            new { employeeId });
    }

    public async Task<IEnumerable<SupportTicket>> GetByCompanyAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT t.*, e.employee_name
            FROM support_tickets t
            LEFT JOIN employees e ON t.employee_id = e.id
            WHERE t.company_id = @companyId
            ORDER BY t.created_at DESC";

        return await connection.QueryAsync<SupportTicket>(sql, new { companyId });
    }

    public async Task<IEnumerable<SupportTicket>> GetByStatusAsync(Guid companyId, string status)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT t.*, e.employee_name
            FROM support_tickets t
            LEFT JOIN employees e ON t.employee_id = e.id
            WHERE t.company_id = @companyId AND t.status = @status
            ORDER BY t.created_at DESC";

        return await connection.QueryAsync<SupportTicket>(sql, new { companyId, status });
    }

    public async Task<IEnumerable<SupportTicket>> GetByAssigneeAsync(Guid assigneeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<SupportTicket>(
            "SELECT * FROM support_tickets WHERE assigned_to = @assigneeId ORDER BY created_at DESC",
            new { assigneeId });
    }

    public async Task<string> GenerateTicketNumberAsync(Guid companyId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var year = DateTime.UtcNow.Year;
        var count = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) + 1 FROM support_tickets WHERE company_id = @companyId AND EXTRACT(YEAR FROM created_at) = @year",
            new { companyId, year });
        return $"TKT-{year}-{count:D4}";
    }

    public async Task<SupportTicket> AddAsync(SupportTicket entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(entity.TicketNumber))
        {
            entity.TicketNumber = await GenerateTicketNumberAsync(entity.CompanyId);
        }

        const string sql = @"
            INSERT INTO support_tickets (
                id, company_id, employee_id, ticket_number, subject, description,
                category, priority, status, assigned_to, created_at, updated_at
            ) VALUES (
                @Id, @CompanyId, @EmployeeId, @TicketNumber, @Subject, @Description,
                @Category, @Priority, @Status, @AssignedTo, @CreatedAt, @UpdatedAt
            )";

        await connection.ExecuteAsync(sql, entity);
        return entity;
    }

    public async Task UpdateAsync(SupportTicket entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        entity.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE support_tickets SET
                subject = @Subject, description = @Description, category = @Category,
                priority = @Priority, status = @Status, assigned_to = @AssignedTo,
                resolved_at = @ResolvedAt, resolution_notes = @ResolutionNotes,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM support_tickets WHERE id = @id", new { id });
    }

    public async Task<IEnumerable<SupportTicketMessage>> GetMessagesAsync(Guid ticketId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT m.*,
                   CASE
                       WHEN m.sender_type = 'employee' THEN e.employee_name
                       ELSE u.email
                   END as sender_name
            FROM support_ticket_messages m
            LEFT JOIN employees e ON m.sender_type = 'employee' AND m.sender_id = e.id
            LEFT JOIN users u ON m.sender_type = 'admin' AND m.sender_id = u.id
            WHERE m.ticket_id = @ticketId
            ORDER BY m.created_at ASC";

        return await connection.QueryAsync<SupportTicketMessage>(sql, new { ticketId });
    }

    public async Task<SupportTicketMessage> AddMessageAsync(SupportTicketMessage message)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        message.Id = Guid.NewGuid();
        message.CreatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO support_ticket_messages (
                id, ticket_id, sender_id, sender_type, message, attachment_url, created_at
            ) VALUES (
                @Id, @TicketId, @SenderId, @SenderType, @Message, @AttachmentUrl, @CreatedAt
            )";

        await connection.ExecuteAsync(sql, message);
        return message;
    }

    public async Task<IEnumerable<FaqItem>> GetFaqItemsAsync(Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT * FROM faq_items
            WHERE is_active = TRUE AND (company_id IS NULL OR company_id = @companyId)
            ORDER BY category, sort_order";

        return await connection.QueryAsync<FaqItem>(sql, new { companyId });
    }

    public async Task<FaqItem?> GetFaqByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<FaqItem>(
            "SELECT * FROM faq_items WHERE id = @id",
            new { id });
    }

    public async Task<FaqItem> AddFaqAsync(FaqItem item)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO faq_items (
                id, company_id, category, question, answer, sort_order, is_active, created_at, updated_at
            ) VALUES (
                @Id, @CompanyId, @Category, @Question, @Answer, @SortOrder, @IsActive, @CreatedAt, @UpdatedAt
            )";

        await connection.ExecuteAsync(sql, item);
        return item;
    }

    public async Task UpdateFaqAsync(FaqItem item)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        item.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE faq_items SET
                category = @Category, question = @Question, answer = @Answer,
                sort_order = @SortOrder, is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, item);
    }

    public async Task DeleteFaqAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM faq_items WHERE id = @id", new { id });
    }
}
