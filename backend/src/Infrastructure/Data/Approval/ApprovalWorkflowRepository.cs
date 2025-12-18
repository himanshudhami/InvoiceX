using Core.Entities.Approval;
using Core.Interfaces.Approval;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Approval
{
    public class ApprovalWorkflowRepository : IApprovalWorkflowRepository
    {
        private readonly string _connectionString;

        public ApprovalWorkflowRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        #region Request Operations

        public async Task<ApprovalRequest?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApprovalRequest>(
                "SELECT * FROM approval_requests WHERE id = @id",
                new { id });
        }

        public async Task<ApprovalRequest?> GetByIdWithStepsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var request = await connection.QueryFirstOrDefaultAsync<ApprovalRequest>(
                "SELECT * FROM approval_requests WHERE id = @id",
                new { id });

            if (request != null)
            {
                var steps = await connection.QueryAsync<ApprovalRequestStep>(
                    "SELECT * FROM approval_request_steps WHERE request_id = @id ORDER BY step_order",
                    new { id });
                request.Steps = steps.ToList();
            }

            return request;
        }

        public async Task<ApprovalRequest?> GetByActivityAsync(string activityType, Guid activityId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApprovalRequest>(
                @"SELECT * FROM approval_requests
                WHERE activity_type = @activityType AND activity_id = @activityId
                ORDER BY created_at DESC
                LIMIT 1",
                new { activityType, activityId });
        }

        public async Task<IEnumerable<ApprovalRequest>> GetByRequestorAsync(Guid requestorId, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM approval_requests WHERE requestor_id = @requestorId";

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND status = @status";
            }

            sql += " ORDER BY created_at DESC";

            return await connection.QueryAsync<ApprovalRequest>(sql, new { requestorId, status });
        }

        public async Task<IEnumerable<ApprovalRequest>> GetByCompanyAsync(Guid companyId, string? status = null, string? activityType = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM approval_requests WHERE company_id = @companyId";

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND status = @status";
            }

            if (!string.IsNullOrEmpty(activityType))
            {
                sql += " AND activity_type = @activityType";
            }

            sql += " ORDER BY created_at DESC";

            return await connection.QueryAsync<ApprovalRequest>(sql, new { companyId, status, activityType });
        }

        public async Task<ApprovalRequest> AddAsync(ApprovalRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            request.Id = Guid.NewGuid();
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO approval_requests (
                    id, company_id, template_id, activity_type, activity_id,
                    requestor_id, current_step, status, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @TemplateId, @ActivityType, @ActivityId,
                    @RequestorId, @CurrentStep, @Status, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(sql, request);
            return request;
        }

        public async Task UpdateAsync(ApprovalRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            request.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE approval_requests SET
                    current_step = @CurrentStep,
                    status = @Status,
                    completed_at = @CompletedAt,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, request);
        }

        public async Task UpdateStatusAsync(Guid requestId, string status, DateTime? completedAt = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE approval_requests SET
                    status = @status,
                    completed_at = @completedAt,
                    updated_at = NOW()
                WHERE id = @requestId",
                new { requestId, status, completedAt });
        }

        #endregion

        #region Request Step Operations

        public async Task<ApprovalRequestStep?> GetStepByIdAsync(Guid stepId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApprovalRequestStep>(
                "SELECT * FROM approval_request_steps WHERE id = @stepId",
                new { stepId });
        }

        public async Task<IEnumerable<ApprovalRequestStep>> GetStepsByRequestAsync(Guid requestId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ApprovalRequestStep>(
                "SELECT * FROM approval_request_steps WHERE request_id = @requestId ORDER BY step_order",
                new { requestId });
        }

        public async Task<ApprovalRequestStep?> GetCurrentStepAsync(Guid requestId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApprovalRequestStep>(
                @"SELECT s.* FROM approval_request_steps s
                INNER JOIN approval_requests r ON r.id = s.request_id
                WHERE s.request_id = @requestId
                  AND s.step_order = r.current_step",
                new { requestId });
        }

        public async Task<ApprovalRequestStep> AddStepAsync(ApprovalRequestStep step)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            step.Id = Guid.NewGuid();
            step.CreatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO approval_request_steps (
                    id, request_id, step_order, step_name, approver_type,
                    assigned_to_id, status, created_at
                ) VALUES (
                    @Id, @RequestId, @StepOrder, @StepName, @ApproverType,
                    @AssignedToId, @Status, @CreatedAt
                )";

            await connection.ExecuteAsync(sql, step);
            return step;
        }

        public async Task UpdateStepAsync(ApprovalRequestStep step)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"
                UPDATE approval_request_steps SET
                    assigned_to_id = @AssignedToId,
                    status = @Status,
                    action_by_id = @ActionById,
                    action_at = @ActionAt,
                    comments = @Comments
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, step);
        }

        public async Task UpdateStepStatusAsync(Guid stepId, string status, Guid? actionById, string? comments)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE approval_request_steps SET
                    status = @status,
                    action_by_id = @actionById,
                    action_at = NOW(),
                    comments = @comments
                WHERE id = @stepId",
                new { stepId, status, actionById, comments });
        }

        #endregion

        #region Pending Approvals

        public async Task<IEnumerable<ApprovalRequestStep>> GetPendingApprovalsForUserAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ApprovalRequestStep>(
                @"SELECT s.* FROM approval_request_steps s
                INNER JOIN approval_requests r ON r.id = s.request_id
                WHERE s.assigned_to_id = @employeeId
                  AND s.status = 'pending'
                  AND r.status = 'in_progress'
                  AND s.step_order = r.current_step
                ORDER BY s.created_at",
                new { employeeId });
        }

        public async Task<int> GetPendingApprovalsCountForUserAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QuerySingleAsync<int>(
                @"SELECT COUNT(*) FROM approval_request_steps s
                INNER JOIN approval_requests r ON r.id = s.request_id
                WHERE s.assigned_to_id = @employeeId
                  AND s.status = 'pending'
                  AND r.status = 'in_progress'
                  AND s.step_order = r.current_step",
                new { employeeId });
        }

        #endregion

        #region Bulk Operations

        public async Task<ApprovalRequest> CreateRequestWithStepsAsync(ApprovalRequest request, IEnumerable<ApprovalRequestStep> steps)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Create request
                request.Id = Guid.NewGuid();
                request.CreatedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                const string requestSql = @"
                    INSERT INTO approval_requests (
                        id, company_id, template_id, activity_type, activity_id,
                        requestor_id, current_step, status, created_at, updated_at
                    ) VALUES (
                        @Id, @CompanyId, @TemplateId, @ActivityType, @ActivityId,
                        @RequestorId, @CurrentStep, @Status, @CreatedAt, @UpdatedAt
                    )";

                await connection.ExecuteAsync(requestSql, request, transaction);

                // Create all steps
                const string stepSql = @"
                    INSERT INTO approval_request_steps (
                        id, request_id, step_order, step_name, approver_type,
                        assigned_to_id, status, created_at
                    ) VALUES (
                        @Id, @RequestId, @StepOrder, @StepName, @ApproverType,
                        @AssignedToId, @Status, @CreatedAt
                    )";

                foreach (var step in steps)
                {
                    step.Id = Guid.NewGuid();
                    step.RequestId = request.Id;
                    step.CreatedAt = DateTime.UtcNow;
                    await connection.ExecuteAsync(stepSql, step, transaction);
                }

                await transaction.CommitAsync();
                request.Steps = steps.ToList();
                return request;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion
    }
}
