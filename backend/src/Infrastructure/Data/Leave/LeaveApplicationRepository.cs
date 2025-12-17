using Core.Entities.Leave;
using Core.Interfaces.Leave;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Leave
{
    public class LeaveApplicationRepository : ILeaveApplicationRepository
    {
        private readonly string _connectionString;

        public LeaveApplicationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<LeaveApplication?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<LeaveApplication>(
                "SELECT * FROM leave_applications WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<LeaveApplication>> GetByEmployeeAsync(Guid employeeId, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = status != null
                ? "SELECT * FROM leave_applications WHERE employee_id = @employeeId AND status = @status ORDER BY from_date DESC"
                : "SELECT * FROM leave_applications WHERE employee_id = @employeeId ORDER BY from_date DESC";
            return await connection.QueryAsync<LeaveApplication>(sql, new { employeeId, status });
        }

        public async Task<IEnumerable<LeaveApplication>> GetByCompanyAsync(Guid companyId, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = status != null
                ? "SELECT * FROM leave_applications WHERE company_id = @companyId AND status = @status ORDER BY applied_at DESC"
                : "SELECT * FROM leave_applications WHERE company_id = @companyId ORDER BY applied_at DESC";
            return await connection.QueryAsync<LeaveApplication>(sql, new { companyId, status });
        }

        public async Task<(IEnumerable<LeaveApplication> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "employee_id", "leave_type_id", "company_id", "from_date", "to_date",
                "total_days", "is_half_day", "half_day_type", "reason", "status",
                "applied_at", "approved_by", "approved_at", "rejection_reason",
                "cancelled_at", "cancellation_reason", "emergency_contact", "handover_notes",
                "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("leave_applications", allowedColumns)
                .SearchAcross(new[] { "reason", "emergency_contact" }, searchTerm)
                .ApplyFilters(filters)
                .OrderBy(sortBy ?? "applied_at", sortDescending)
                .Paginate(pageNumber, pageSize);

            var (sql, parameters) = builder.BuildSelect();
            var items = await connection.QueryAsync<LeaveApplication>(sql, parameters);

            var (countSql, countParameters) = builder.BuildCount();
            var totalCount = await connection.QuerySingleAsync<int>(countSql, countParameters);

            return (items, totalCount);
        }

        public async Task<IEnumerable<LeaveApplication>> GetPendingApprovalAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LeaveApplication>(
                @"SELECT la.*, e.employee_name, lt.name as LeaveTypeName, lt.code as LeaveTypeCode
                  FROM leave_applications la
                  JOIN employees e ON la.employee_id = e.id
                  JOIN leave_types lt ON la.leave_type_id = lt.id
                  WHERE la.company_id = @companyId AND la.status = 'pending'
                  ORDER BY la.applied_at",
                new { companyId });
        }

        public async Task<IEnumerable<LeaveApplication>> GetOverlappingAsync(
            Guid employeeId,
            DateTime fromDate,
            DateTime toDate,
            Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? @"SELECT * FROM leave_applications
                    WHERE employee_id = @employeeId
                      AND status NOT IN ('cancelled', 'rejected', 'withdrawn')
                      AND from_date <= @toDate
                      AND to_date >= @fromDate
                      AND id != @excludeId"
                : @"SELECT * FROM leave_applications
                    WHERE employee_id = @employeeId
                      AND status NOT IN ('cancelled', 'rejected', 'withdrawn')
                      AND from_date <= @toDate
                      AND to_date >= @fromDate";
            return await connection.QueryAsync<LeaveApplication>(sql, new { employeeId, fromDate, toDate, excludeId });
        }

        public async Task<IEnumerable<LeaveApplication>> GetApprovedForDateRangeAsync(
            Guid companyId,
            DateTime fromDate,
            DateTime toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<LeaveApplication>(
                @"SELECT la.*, e.employee_name, lt.name as LeaveTypeName, lt.code as LeaveTypeCode, lt.color_code as LeaveTypeColor
                  FROM leave_applications la
                  JOIN employees e ON la.employee_id = e.id
                  JOIN leave_types lt ON la.leave_type_id = lt.id
                  WHERE la.company_id = @companyId
                    AND la.status = 'approved'
                    AND la.from_date <= @toDate
                    AND la.to_date >= @fromDate
                  ORDER BY la.from_date",
                new { companyId, fromDate, toDate });
        }

        public async Task<LeaveApplication> AddAsync(LeaveApplication entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                INSERT INTO leave_applications (
                    employee_id, leave_type_id, company_id, from_date, to_date,
                    total_days, is_half_day, half_day_type, reason, status, applied_at,
                    approved_by, approved_at, rejection_reason, cancelled_at, cancellation_reason,
                    emergency_contact, handover_notes, attachment_url, created_at, updated_at
                ) VALUES (
                    @EmployeeId, @LeaveTypeId, @CompanyId, @FromDate, @ToDate,
                    @TotalDays, @IsHalfDay, @HalfDayType, @Reason, @Status, NOW(),
                    @ApprovedBy, @ApprovedAt, @RejectionReason, @CancelledAt, @CancellationReason,
                    @EmergencyContact, @HandoverNotes, @AttachmentUrl, NOW(), NOW()
                ) RETURNING *";

            return await connection.QuerySingleAsync<LeaveApplication>(sql, entity);
        }

        public async Task UpdateAsync(LeaveApplication entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                UPDATE leave_applications SET
                    from_date = @FromDate,
                    to_date = @ToDate,
                    total_days = @TotalDays,
                    is_half_day = @IsHalfDay,
                    half_day_type = @HalfDayType,
                    reason = @Reason,
                    emergency_contact = @EmergencyContact,
                    handover_notes = @HandoverNotes,
                    attachment_url = @AttachmentUrl,
                    updated_at = NOW()
                WHERE id = @Id AND status = 'pending'";

            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM leave_applications WHERE id = @id", new { id });
        }

        public async Task UpdateStatusAsync(Guid id, string status, Guid? approvedBy = null, string? reason = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = status switch
            {
                "approved" => @"UPDATE leave_applications SET
                    status = @status, approved_by = @approvedBy, approved_at = NOW(), updated_at = NOW()
                    WHERE id = @id",
                "rejected" => @"UPDATE leave_applications SET
                    status = @status, rejection_reason = @reason, approved_by = @approvedBy, approved_at = NOW(), updated_at = NOW()
                    WHERE id = @id",
                "cancelled" => @"UPDATE leave_applications SET
                    status = @status, cancelled_at = NOW(), cancellation_reason = @reason, updated_at = NOW()
                    WHERE id = @id",
                "withdrawn" => @"UPDATE leave_applications SET
                    status = @status, cancelled_at = NOW(), cancellation_reason = @reason, updated_at = NOW()
                    WHERE id = @id",
                _ => @"UPDATE leave_applications SET status = @status, updated_at = NOW() WHERE id = @id"
            };

            await connection.ExecuteAsync(sql, new { id, status, approvedBy, reason });
        }
    }
}
