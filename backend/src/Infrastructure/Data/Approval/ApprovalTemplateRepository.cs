using Core.Entities.Approval;
using Core.Interfaces.Approval;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Approval
{
    public class ApprovalTemplateRepository : IApprovalTemplateRepository
    {
        private readonly string _connectionString;

        public ApprovalTemplateRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        #region Template Operations

        public async Task<ApprovalWorkflowTemplate?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApprovalWorkflowTemplate>(
                "SELECT * FROM approval_workflow_templates WHERE id = @id",
                new { id });
        }

        public async Task<ApprovalWorkflowTemplate?> GetByIdWithStepsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var template = await connection.QueryFirstOrDefaultAsync<ApprovalWorkflowTemplate>(
                "SELECT * FROM approval_workflow_templates WHERE id = @id",
                new { id });

            if (template != null)
            {
                var steps = await connection.QueryAsync<ApprovalWorkflowStep>(
                    "SELECT * FROM approval_workflow_steps WHERE template_id = @id ORDER BY step_order",
                    new { id });
                template.Steps = steps.ToList();
            }

            return template;
        }

        public async Task<IEnumerable<ApprovalWorkflowTemplate>> GetByCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ApprovalWorkflowTemplate>(
                @"SELECT * FROM approval_workflow_templates
                WHERE company_id = @companyId
                ORDER BY activity_type, name",
                new { companyId });
        }

        public async Task<IEnumerable<ApprovalWorkflowTemplate>> GetByCompanyAndActivityTypeAsync(Guid companyId, string activityType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ApprovalWorkflowTemplate>(
                @"SELECT * FROM approval_workflow_templates
                WHERE company_id = @companyId AND activity_type = @activityType
                ORDER BY is_default DESC, name",
                new { companyId, activityType });
        }

        public async Task<ApprovalWorkflowTemplate?> GetDefaultTemplateAsync(Guid companyId, string activityType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApprovalWorkflowTemplate>(
                @"SELECT * FROM approval_workflow_templates
                WHERE company_id = @companyId
                  AND activity_type = @activityType
                  AND is_default = TRUE
                  AND is_active = TRUE",
                new { companyId, activityType });
        }

        public async Task<ApprovalWorkflowTemplate> AddAsync(ApprovalWorkflowTemplate template)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            template.Id = Guid.NewGuid();
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO approval_workflow_templates (
                    id, company_id, activity_type, name, description,
                    is_active, is_default, created_at, updated_at
                ) VALUES (
                    @Id, @CompanyId, @ActivityType, @Name, @Description,
                    @IsActive, @IsDefault, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(sql, template);
            return template;
        }

        public async Task UpdateAsync(ApprovalWorkflowTemplate template)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            template.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE approval_workflow_templates SET
                    name = @Name,
                    description = @Description,
                    is_active = @IsActive,
                    is_default = @IsDefault,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, template);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM approval_workflow_templates WHERE id = @id",
                new { id });
        }

        public async Task SetAsDefaultAsync(Guid templateId, Guid companyId, string activityType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Remove default from other templates of same type
                await connection.ExecuteAsync(
                    @"UPDATE approval_workflow_templates
                    SET is_default = FALSE, updated_at = NOW()
                    WHERE company_id = @companyId
                      AND activity_type = @activityType
                      AND is_default = TRUE",
                    new { companyId, activityType },
                    transaction);

                // Set new default
                await connection.ExecuteAsync(
                    @"UPDATE approval_workflow_templates
                    SET is_default = TRUE, updated_at = NOW()
                    WHERE id = @templateId",
                    new { templateId },
                    transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Step Operations

        public async Task<ApprovalWorkflowStep?> GetStepByIdAsync(Guid stepId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApprovalWorkflowStep>(
                "SELECT * FROM approval_workflow_steps WHERE id = @stepId",
                new { stepId });
        }

        public async Task<IEnumerable<ApprovalWorkflowStep>> GetStepsByTemplateAsync(Guid templateId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ApprovalWorkflowStep>(
                "SELECT * FROM approval_workflow_steps WHERE template_id = @templateId ORDER BY step_order",
                new { templateId });
        }

        public async Task<ApprovalWorkflowStep> AddStepAsync(ApprovalWorkflowStep step)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            step.Id = Guid.NewGuid();
            step.CreatedAt = DateTime.UtcNow;
            step.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO approval_workflow_steps (
                    id, template_id, step_order, name, approver_type,
                    approver_role, approver_user_id, is_required, can_skip,
                    auto_approve_after_days, conditions_json, created_at, updated_at
                ) VALUES (
                    @Id, @TemplateId, @StepOrder, @Name, @ApproverType,
                    @ApproverRole, @ApproverUserId, @IsRequired, @CanSkip,
                    @AutoApproveAfterDays, @ConditionsJson::jsonb, @CreatedAt, @UpdatedAt
                )";

            await connection.ExecuteAsync(sql, step);
            return step;
        }

        public async Task UpdateStepAsync(ApprovalWorkflowStep step)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            step.UpdatedAt = DateTime.UtcNow;

            const string sql = @"
                UPDATE approval_workflow_steps SET
                    name = @Name,
                    approver_type = @ApproverType,
                    approver_role = @ApproverRole,
                    approver_user_id = @ApproverUserId,
                    is_required = @IsRequired,
                    can_skip = @CanSkip,
                    auto_approve_after_days = @AutoApproveAfterDays,
                    conditions_json = @ConditionsJson::jsonb,
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, step);
        }

        public async Task DeleteStepAsync(Guid stepId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM approval_workflow_steps WHERE id = @stepId",
                new { stepId });
        }

        public async Task ReorderStepsAsync(Guid templateId, IEnumerable<Guid> orderedStepIds)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var order = 1;
                foreach (var stepId in orderedStepIds)
                {
                    await connection.ExecuteAsync(
                        @"UPDATE approval_workflow_steps
                        SET step_order = @order, updated_at = NOW()
                        WHERE id = @stepId AND template_id = @templateId",
                        new { stepId, templateId, order },
                        transaction);
                    order++;
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> GetMaxStepOrderAsync(Guid templateId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT MAX(step_order) FROM approval_workflow_steps WHERE template_id = @templateId",
                new { templateId });
            return result ?? 0;
        }

        #endregion
    }
}
