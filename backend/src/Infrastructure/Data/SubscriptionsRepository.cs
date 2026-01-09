using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data;

public class SubscriptionsRepository : ISubscriptionsRepository
{
    private readonly string _connectionString;

    public SubscriptionsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Subscriptions?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Subscriptions>("SELECT * FROM subscriptions WHERE id=@id", new { id });
    }

    public async Task<IEnumerable<Subscriptions>> GetAllAsync(Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        if (companyId.HasValue)
        {
            return await connection.QueryAsync<Subscriptions>("SELECT * FROM subscriptions WHERE company_id=@companyId", new { companyId = companyId.Value });
        }
        return await connection.QueryAsync<Subscriptions>("SELECT * FROM subscriptions");
    }

    public async Task<(IEnumerable<Subscriptions> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortDescending = false, Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var allowedColumns = new[] { "id", "company_id", "name", "vendor", "plan_name", "category", "status", "start_date", "renewal_date", "renewal_period", "seats_total", "seats_used", "cost_per_period", "currency", "auto_renew", "created_at", "updated_at" };

        var builder = SqlQueryBuilder
            .From("subscriptions", allowedColumns)
            .SearchAcross(new[] { "name", "vendor", "plan_name", "category", "license_key" }, searchTerm)
            .ApplyFilters(filters)
            .Paginate(pageNumber, pageSize);

        var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
        var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
        builder.OrderBy(orderBy, sortDescending);

        var (dataSql, parameters) = builder.BuildSelect();
        var (countSql, _) = builder.BuildCount();

        using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
        var items = await multi.ReadAsync<Subscriptions>();
        var total = await multi.ReadSingleAsync<int>();
        return (items, total);
    }

    public async Task<Subscriptions> AddAsync(Subscriptions entity)
    {
        // Auto-calculate cost_per_period from cost_per_seat if provided
        if (entity.CostPerSeat.HasValue && entity.SeatsTotal.HasValue && entity.SeatsTotal.Value > 0 && !entity.CostPerPeriod.HasValue)
        {
            entity.CostPerPeriod = entity.CostPerSeat.Value * entity.SeatsTotal.Value;
        }
        // Auto-calculate cost_per_seat from cost_per_period if provided
        else if (entity.CostPerPeriod.HasValue && entity.SeatsTotal.HasValue && entity.SeatsTotal.Value > 0 && !entity.CostPerSeat.HasValue)
        {
            entity.CostPerSeat = entity.CostPerPeriod.Value / entity.SeatsTotal.Value;
        }

        const string sql = @"INSERT INTO subscriptions
        (company_id, name, vendor, plan_name, category, status, start_date, renewal_date, renewal_period, seats_total, seats_used, license_key, cost_per_period, cost_per_seat, currency, billing_cycle_start, billing_cycle_end, auto_renew, url, notes, paused_on, resumed_on, cancelled_on, created_at, updated_at)
        VALUES (@CompanyId, @Name, @Vendor, @PlanName, @Category, @Status, @StartDate, @RenewalDate, @RenewalPeriod, @SeatsTotal, @SeatsUsed, @LicenseKey, @CostPerPeriod, @CostPerSeat, @Currency, @BillingCycleStart, @BillingCycleEnd, @AutoRenew, @Url, @Notes, @PausedOn, @ResumedOn, @CancelledOn, NOW(), NOW())
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<Subscriptions>(sql, entity);
    }

    public async Task UpdateAsync(Subscriptions entity)
    {
        // Auto-calculate cost_per_period from cost_per_seat if provided
        if (entity.CostPerSeat.HasValue && entity.SeatsTotal.HasValue && entity.SeatsTotal.Value > 0 && !entity.CostPerPeriod.HasValue)
        {
            entity.CostPerPeriod = entity.CostPerSeat.Value * entity.SeatsTotal.Value;
        }
        // Auto-calculate cost_per_seat from cost_per_period if provided
        else if (entity.CostPerPeriod.HasValue && entity.SeatsTotal.HasValue && entity.SeatsTotal.Value > 0 && !entity.CostPerSeat.HasValue)
        {
            entity.CostPerSeat = entity.CostPerPeriod.Value / entity.SeatsTotal.Value;
        }

        const string sql = @"UPDATE subscriptions SET
            name=@Name,
            vendor=@Vendor,
            plan_name=@PlanName,
            category=@Category,
            status=@Status,
            start_date=@StartDate,
            renewal_date=@RenewalDate,
            renewal_period=@RenewalPeriod,
            seats_total=@SeatsTotal,
            seats_used=@SeatsUsed,
            license_key=@LicenseKey,
            cost_per_period=@CostPerPeriod,
            cost_per_seat=@CostPerSeat,
            currency=@Currency,
            billing_cycle_start=@BillingCycleStart,
            billing_cycle_end=@BillingCycleEnd,
            auto_renew=@AutoRenew,
            url=@Url,
            notes=@Notes,
            paused_on=@PausedOn,
            resumed_on=@ResumedOn,
            cancelled_on=@CancelledOn,
            updated_at=NOW()
        WHERE id=@Id";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM subscriptions WHERE id=@id", new { id });
    }

    public async Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId, string? reconciledBy)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            @"UPDATE subscriptions SET
                bank_transaction_id = @bankTransactionId,
                reconciled_at = NOW(),
                reconciled_by = @reconciledBy,
                updated_at = NOW()
            WHERE id = @id",
            new { id, bankTransactionId, reconciledBy });
    }

    public async Task ClearReconciliationAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            @"UPDATE subscriptions SET
                bank_transaction_id = NULL,
                reconciled_at = NULL,
                reconciled_by = NULL,
                updated_at = NOW()
            WHERE id = @id",
            new { id });
    }

    public async Task<IEnumerable<SubscriptionAssignments>> GetAssignmentsAsync(Guid subscriptionId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<SubscriptionAssignments>("SELECT * FROM subscription_assignments WHERE subscription_id=@subscriptionId ORDER BY assigned_on DESC", new { subscriptionId });
    }

    public async Task<IEnumerable<SubscriptionAssignments>> GetAssignmentsByEmployeeAsync(Guid employeeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<SubscriptionAssignments>(
            "SELECT * FROM subscription_assignments WHERE employee_id = @employeeId ORDER BY assigned_on DESC",
            new { employeeId });
    }

    public async Task<SubscriptionAssignments> AddAssignmentAsync(SubscriptionAssignments assignment)
    {
        const string sql = @"INSERT INTO subscription_assignments
        (subscription_id, target_type, employee_id, company_id, seat_identifier, role, assigned_on, revoked_on, notes, created_at, updated_at)
        VALUES (@SubscriptionId, @TargetType, @EmployeeId, @CompanyId, @SeatIdentifier, @Role, @AssignedOn, @RevokedOn, @Notes, NOW(), NOW())
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<SubscriptionAssignments>(sql, assignment);
    }

    public async Task RevokeAssignmentAsync(Guid assignmentId, DateTime? revokedOn)
    {
        const string sql = @"UPDATE subscription_assignments SET revoked_on=@revokedOn, updated_at=NOW() WHERE id=@assignmentId";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { assignmentId, revokedOn });
    }

    public async Task PauseSubscriptionAsync(Guid subscriptionId, DateTime? pausedOn)
    {
        const string sql = @"UPDATE subscriptions SET status='on_hold', paused_on=@pausedOn, updated_at=NOW() WHERE id=@subscriptionId";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { subscriptionId, pausedOn = pausedOn ?? DateTime.UtcNow.Date });
    }

    public async Task ResumeSubscriptionAsync(Guid subscriptionId, DateTime? resumedOn)
    {
        const string sql = @"UPDATE subscriptions SET status='active', resumed_on=@resumedOn, updated_at=NOW() WHERE id=@subscriptionId";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { subscriptionId, resumedOn = resumedOn ?? DateTime.UtcNow.Date });
    }

    public async Task CancelSubscriptionAsync(Guid subscriptionId, DateTime? cancelledOn)
    {
        const string sql = @"UPDATE subscriptions SET status='cancelled', cancelled_on=@cancelledOn, updated_at=NOW() WHERE id=@subscriptionId";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { subscriptionId, cancelledOn = cancelledOn ?? DateTime.UtcNow.Date });
    }
}



