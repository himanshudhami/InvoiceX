using Core.Entities;
using Core.Interfaces;
using Dapper;
using Infrastructure.Data.Common;
using Npgsql;

namespace Infrastructure.Data
{
    /// <summary>
    /// Repository implementation for asset requests
    /// </summary>
    public class AssetRequestRepository : IAssetRequestRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllowedColumns = new[]
        {
            "id", "company_id", "employee_id", "asset_type", "category", "title",
            "description", "justification", "specifications", "priority", "status",
            "quantity", "estimated_budget", "requested_by_date", "requested_at",
            "created_at", "updated_at", "approved_by", "approved_at", "rejection_reason",
            "cancelled_at", "cancellation_reason", "assigned_asset_id", "fulfilled_by",
            "fulfilled_at", "fulfillment_notes"
        };

        public AssetRequestRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<AssetRequest?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AssetRequest>(
                @"SELECT id, company_id AS CompanyId, employee_id AS EmployeeId,
                         asset_type AS AssetType, category, title, description, justification,
                         specifications, priority, status, quantity, estimated_budget AS EstimatedBudget,
                         requested_by_date AS RequestedByDate, requested_at AS RequestedAt,
                         created_at AS CreatedAt, updated_at AS UpdatedAt,
                         approved_by AS ApprovedBy, approved_at AS ApprovedAt,
                         rejection_reason AS RejectionReason, cancelled_at AS CancelledAt,
                         cancellation_reason AS CancellationReason, assigned_asset_id AS AssignedAssetId,
                         fulfilled_by AS FulfilledBy, fulfilled_at AS FulfilledAt,
                         fulfillment_notes AS FulfillmentNotes
                  FROM asset_requests
                  WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<AssetRequest>> GetByCompanyAsync(Guid companyId, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT id, company_id AS CompanyId, employee_id AS EmployeeId,
                               asset_type AS AssetType, category, title, description, justification,
                               specifications, priority, status, quantity, estimated_budget AS EstimatedBudget,
                               requested_by_date AS RequestedByDate, requested_at AS RequestedAt,
                               created_at AS CreatedAt, updated_at AS UpdatedAt,
                               approved_by AS ApprovedBy, approved_at AS ApprovedAt,
                               rejection_reason AS RejectionReason, cancelled_at AS CancelledAt,
                               cancellation_reason AS CancellationReason, assigned_asset_id AS AssignedAssetId,
                               fulfilled_by AS FulfilledBy, fulfilled_at AS FulfilledAt,
                               fulfillment_notes AS FulfillmentNotes
                        FROM asset_requests
                        WHERE company_id = @companyId";

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND status = @status";
            }

            sql += " ORDER BY requested_at DESC";

            return await connection.QueryAsync<AssetRequest>(sql, new { companyId, status });
        }

        public async Task<IEnumerable<AssetRequest>> GetByEmployeeAsync(Guid employeeId, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT id, company_id AS CompanyId, employee_id AS EmployeeId,
                               asset_type AS AssetType, category, title, description, justification,
                               specifications, priority, status, quantity, estimated_budget AS EstimatedBudget,
                               requested_by_date AS RequestedByDate, requested_at AS RequestedAt,
                               created_at AS CreatedAt, updated_at AS UpdatedAt,
                               approved_by AS ApprovedBy, approved_at AS ApprovedAt,
                               rejection_reason AS RejectionReason, cancelled_at AS CancelledAt,
                               cancellation_reason AS CancellationReason, assigned_asset_id AS AssignedAssetId,
                               fulfilled_by AS FulfilledBy, fulfilled_at AS FulfilledAt,
                               fulfillment_notes AS FulfillmentNotes
                        FROM asset_requests
                        WHERE employee_id = @employeeId";

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND status = @status";
            }

            sql += " ORDER BY requested_at DESC";

            return await connection.QueryAsync<AssetRequest>(sql, new { employeeId, status });
        }

        public async Task<IEnumerable<AssetRequest>> GetPendingForCompanyAsync(Guid companyId)
        {
            return await GetByCompanyAsync(companyId, AssetRequestStatus.Pending);
        }

        public async Task<IEnumerable<AssetRequest>> GetApprovedUnfulfilledAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<AssetRequest>(
                @"SELECT id, company_id AS CompanyId, employee_id AS EmployeeId,
                         asset_type AS AssetType, category, title, description, justification,
                         specifications, priority, status, quantity, estimated_budget AS EstimatedBudget,
                         requested_by_date AS RequestedByDate, requested_at AS RequestedAt,
                         created_at AS CreatedAt, updated_at AS UpdatedAt,
                         approved_by AS ApprovedBy, approved_at AS ApprovedAt,
                         rejection_reason AS RejectionReason, cancelled_at AS CancelledAt,
                         cancellation_reason AS CancellationReason, assigned_asset_id AS AssignedAssetId,
                         fulfilled_by AS FulfilledBy, fulfilled_at AS FulfilledAt,
                         fulfillment_notes AS FulfillmentNotes
                  FROM asset_requests
                  WHERE company_id = @companyId
                    AND status = 'approved'
                    AND fulfilled_at IS NULL
                  ORDER BY approved_at ASC",
                new { companyId });
        }

        public async Task<(IEnumerable<AssetRequest> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? companyId = null,
            Guid? employeeId = null,
            string? status = null,
            string? category = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false)
        {
            var filters = new Dictionary<string, object>();
            if (companyId.HasValue) filters["company_id"] = companyId.Value;
            if (employeeId.HasValue) filters["employee_id"] = employeeId.Value;
            if (!string.IsNullOrEmpty(status)) filters["status"] = status;
            if (!string.IsNullOrEmpty(category)) filters["category"] = category;

            var builder = SqlQueryBuilder
                .From("asset_requests", AllowedColumns)
                .SearchAcross(new[] { "title", "description", "asset_type", "category" }, searchTerm)
                .ApplyFilters(filters)
                .OrderBy(sortBy ?? "requested_at", sortDescending || sortBy == null)
                .Paginate(pageNumber, pageSize);

            var (baseSql, selectParams) = builder.BuildSelect();
            // Replace SELECT * with custom column list for proper mapping
            var selectSql = baseSql.Replace("SELECT *", @"SELECT id, company_id AS CompanyId, employee_id AS EmployeeId,
                  asset_type AS AssetType, category, title, description, justification,
                  specifications, priority, status, quantity, estimated_budget AS EstimatedBudget,
                  requested_by_date AS RequestedByDate, requested_at AS RequestedAt,
                  created_at AS CreatedAt, updated_at AS UpdatedAt,
                  approved_by AS ApprovedBy, approved_at AS ApprovedAt,
                  rejection_reason AS RejectionReason, cancelled_at AS CancelledAt,
                  cancellation_reason AS CancellationReason, assigned_asset_id AS AssignedAssetId,
                  fulfilled_by AS FulfilledBy, fulfilled_at AS FulfilledAt,
                  fulfillment_notes AS FulfillmentNotes");

            var (countSql, countParams) = builder.BuildCount();

            using var connection = new NpgsqlConnection(_connectionString);
            var items = await connection.QueryAsync<AssetRequest>(selectSql, selectParams);
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, countParams);

            return (items, totalCount);
        }

        public async Task<AssetRequest> AddAsync(AssetRequest request)
        {
            request.Id = Guid.NewGuid();
            request.RequestedAt = DateTime.UtcNow;
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO asset_requests (
                    id, company_id, employee_id, asset_type, category, title, description,
                    justification, specifications, priority, status, quantity, estimated_budget,
                    requested_by_date, requested_at, created_at, updated_at
                  ) VALUES (
                    @Id, @CompanyId, @EmployeeId, @AssetType, @Category, @Title, @Description,
                    @Justification, @Specifications, @Priority, @Status, @Quantity, @EstimatedBudget,
                    @RequestedByDate, @RequestedAt, @CreatedAt, @UpdatedAt
                  )",
                request);

            return request;
        }

        public async Task UpdateAsync(AssetRequest request)
        {
            request.UpdatedAt = DateTime.UtcNow;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE asset_requests SET
                    asset_type = @AssetType,
                    category = @Category,
                    title = @Title,
                    description = @Description,
                    justification = @Justification,
                    specifications = @Specifications,
                    priority = @Priority,
                    status = @Status,
                    quantity = @Quantity,
                    estimated_budget = @EstimatedBudget,
                    requested_by_date = @RequestedByDate,
                    updated_at = @UpdatedAt,
                    approved_by = @ApprovedBy,
                    approved_at = @ApprovedAt,
                    rejection_reason = @RejectionReason,
                    cancelled_at = @CancelledAt,
                    cancellation_reason = @CancellationReason,
                    assigned_asset_id = @AssignedAssetId,
                    fulfilled_by = @FulfilledBy,
                    fulfilled_at = @FulfilledAt,
                    fulfillment_notes = @FulfillmentNotes
                  WHERE id = @Id",
                request);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM asset_requests WHERE id = @id",
                new { id });
        }

        public async Task UpdateStatusAsync(Guid id, string status, Guid? actionBy = null, string? reason = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"UPDATE asset_requests SET
                        status = @status,
                        updated_at = @updatedAt";

            if (status == AssetRequestStatus.Approved)
            {
                sql += ", approved_by = @actionBy, approved_at = @updatedAt";
            }
            else if (status == AssetRequestStatus.Rejected)
            {
                sql += ", approved_by = @actionBy, rejection_reason = @reason";
            }
            else if (status == AssetRequestStatus.Cancelled)
            {
                sql += ", cancelled_at = @updatedAt, cancellation_reason = @reason";
            }

            sql += " WHERE id = @id";

            await connection.ExecuteAsync(sql, new { id, status, actionBy, reason, updatedAt = DateTime.UtcNow });
        }

        public async Task FulfillAsync(Guid id, Guid fulfilledBy, Guid? assignedAssetId, string? notes)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE asset_requests SET
                    status = @status,
                    fulfilled_by = @fulfilledBy,
                    fulfilled_at = @fulfilledAt,
                    assigned_asset_id = @assignedAssetId,
                    fulfillment_notes = @notes,
                    updated_at = @fulfilledAt
                  WHERE id = @id",
                new
                {
                    id,
                    status = AssetRequestStatus.Fulfilled,
                    fulfilledBy,
                    fulfilledAt = DateTime.UtcNow,
                    assignedAssetId,
                    notes
                });
        }

        public async Task<AssetRequestStats> GetStatsAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AssetRequestStats>(
                @"SELECT
                    COUNT(*) AS TotalRequests,
                    COUNT(*) FILTER (WHERE status = 'pending') AS PendingRequests,
                    COUNT(*) FILTER (WHERE status = 'approved') AS ApprovedRequests,
                    COUNT(*) FILTER (WHERE status = 'rejected') AS RejectedRequests,
                    COUNT(*) FILTER (WHERE status = 'fulfilled') AS FulfilledRequests,
                    COUNT(*) FILTER (WHERE status = 'approved' AND fulfilled_at IS NULL) AS UnfulfilledApproved
                  FROM asset_requests
                  WHERE company_id = @companyId",
                new { companyId }) ?? new AssetRequestStats();
        }
    }
}
