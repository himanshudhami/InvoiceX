using Core.Entities.Expense;
using Core.Interfaces.Expense;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Expense
{
    /// <summary>
    /// Dapper implementation of expense claim repository.
    /// </summary>
    public class ExpenseClaimRepository : IExpenseClaimRepository
    {
        private readonly string _connectionString;

        private const string BaseSelectSql = @"
            SELECT ec.id, ec.company_id, ec.employee_id, ec.claim_number, ec.title,
                   ec.description, ec.category_id, ec.expense_date, ec.amount, ec.currency,
                   ec.status, ec.approval_request_id, ec.submitted_at,
                   ec.approved_at, ec.approved_by, ec.rejected_at, ec.rejected_by,
                   ec.rejection_reason, ec.reimbursed_at, ec.reimbursement_reference,
                   ec.reimbursement_notes, ec.bank_transaction_id, ec.reconciled_at,
                   ec.reconciled_by, ec.created_at, ec.updated_at,
                   e.employee_name,
                   cat.name as category_name,
                   approver.employee_name as approved_by_name,
                   rejecter.employee_name as rejected_by_name
            FROM expense_claims ec
            LEFT JOIN employees e ON ec.employee_id = e.id
            LEFT JOIN expense_categories cat ON ec.category_id = cat.id
            LEFT JOIN employees approver ON ec.approved_by = approver.id
            LEFT JOIN employees rejecter ON ec.rejected_by = rejecter.id";

        public ExpenseClaimRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<ExpenseClaim?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ExpenseClaim>(
                $"{BaseSelectSql} WHERE ec.id = @id",
                new { id });
        }

        public async Task<ExpenseClaim?> GetByClaimNumberAsync(Guid companyId, string claimNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ExpenseClaim>(
                $"{BaseSelectSql} WHERE ec.company_id = @companyId AND ec.claim_number = @claimNumber",
                new { companyId, claimNumber });
        }

        public async Task<IEnumerable<ExpenseClaim>> GetByEmployeeAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ExpenseClaim>(
                $"{BaseSelectSql} WHERE ec.employee_id = @employeeId ORDER BY ec.created_at DESC",
                new { employeeId });
        }

        public async Task<IEnumerable<ExpenseClaim>> GetByStatusAsync(Guid companyId, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ExpenseClaim>(
                $"{BaseSelectSql} WHERE ec.company_id = @companyId AND ec.status = @status ORDER BY ec.created_at DESC",
                new { companyId, status });
        }

        public async Task<IEnumerable<ExpenseClaim>> GetPendingForManagerAsync(Guid managerId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ExpenseClaim>(
                $@"{BaseSelectSql}
                WHERE ec.employee_id IN (
                    SELECT id FROM employees WHERE manager_id = @managerId
                )
                AND ec.status IN ('submitted', 'pending_approval')
                ORDER BY ec.submitted_at ASC",
                new { managerId });
        }

        public async Task<(IEnumerable<ExpenseClaim> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? status = null,
            Guid? employeeId = null,
            Guid? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var conditions = new List<string> { "ec.company_id = @companyId" };
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                conditions.Add("(LOWER(ec.title) LIKE LOWER(@searchTerm) OR ec.claim_number LIKE @searchTerm OR LOWER(e.employee_name) LIKE LOWER(@searchTerm))");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                conditions.Add("ec.status = @status");
                parameters.Add("status", status);
            }

            if (employeeId.HasValue)
            {
                conditions.Add("ec.employee_id = @employeeId");
                parameters.Add("employeeId", employeeId.Value);
            }

            if (categoryId.HasValue)
            {
                conditions.Add("ec.category_id = @categoryId");
                parameters.Add("categoryId", categoryId.Value);
            }

            if (fromDate.HasValue)
            {
                conditions.Add("ec.expense_date >= @fromDate");
                parameters.Add("fromDate", fromDate.Value);
            }

            if (toDate.HasValue)
            {
                conditions.Add("ec.expense_date <= @toDate");
                parameters.Add("toDate", toDate.Value);
            }

            var whereClause = "WHERE " + string.Join(" AND ", conditions);

            var countSql = $@"SELECT COUNT(*) FROM expense_claims ec
                              LEFT JOIN employees e ON ec.employee_id = e.id
                              {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            var dataSql = $@"{BaseSelectSql}
                             {whereClause}
                             ORDER BY ec.created_at DESC
                             LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<ExpenseClaim>(dataSql, parameters);

            return (items, totalCount);
        }

        public async Task<(IEnumerable<ExpenseClaim> Items, int TotalCount)> GetPagedByEmployeeAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClause = "WHERE ec.employee_id = @employeeId";
            var parameters = new DynamicParameters();
            parameters.Add("employeeId", employeeId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                // Support comma-separated status values (e.g., "draft,submitted,pending_approval")
                var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .ToArray();
                if (statuses.Length == 1)
                {
                    whereClause += " AND ec.status = @status";
                    parameters.Add("status", statuses[0]);
                }
                else if (statuses.Length > 1)
                {
                    whereClause += " AND ec.status = ANY(@statuses)";
                    parameters.Add("statuses", statuses);
                }
            }

            var countSql = $"SELECT COUNT(*) FROM expense_claims ec {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            var dataSql = $@"{BaseSelectSql}
                             {whereClause}
                             ORDER BY ec.created_at DESC
                             LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<ExpenseClaim>(dataSql, parameters);

            return (items, totalCount);
        }

        public async Task<ExpenseClaim> AddAsync(ExpenseClaim claim)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"INSERT INTO expense_claims
                        (id, company_id, employee_id, claim_number, title, description,
                         category_id, expense_date, amount, currency, status,
                         approval_request_id, submitted_at, created_at, updated_at)
                        VALUES
                        (@Id, @CompanyId, @EmployeeId, @ClaimNumber, @Title, @Description,
                         @CategoryId, @ExpenseDate, @Amount, @Currency, @Status,
                         @ApprovalRequestId, @SubmittedAt, @CreatedAt, @UpdatedAt)
                        RETURNING *";

            claim.Id = claim.Id == Guid.Empty ? Guid.NewGuid() : claim.Id;
            claim.CreatedAt = DateTime.UtcNow;
            claim.UpdatedAt = DateTime.UtcNow;

            var result = await connection.QuerySingleAsync<ExpenseClaim>(sql, claim);

            // Re-fetch with joins to get navigation properties
            return await GetByIdAsync(result.Id) ?? result;
        }

        public async Task UpdateAsync(ExpenseClaim claim)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            claim.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"UPDATE expense_claims
                  SET title = @Title,
                      description = @Description,
                      category_id = @CategoryId,
                      expense_date = @ExpenseDate,
                      amount = @Amount,
                      currency = @Currency,
                      status = @Status,
                      approval_request_id = @ApprovalRequestId,
                      submitted_at = @SubmittedAt,
                      approved_at = @ApprovedAt,
                      approved_by = @ApprovedBy,
                      rejected_at = @RejectedAt,
                      rejected_by = @RejectedBy,
                      rejection_reason = @RejectionReason,
                      reimbursed_at = @ReimbursedAt,
                      reimbursement_reference = @ReimbursementReference,
                      reimbursement_notes = @ReimbursementNotes,
                      updated_at = @UpdatedAt
                  WHERE id = @Id",
                claim);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM expense_claims WHERE id = @id AND status = 'draft'",
                new { id });
        }

        public async Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId, string? reconciledBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE expense_claims SET
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
                @"UPDATE expense_claims SET
                    bank_transaction_id = NULL,
                    reconciled_at = NULL,
                    reconciled_by = NULL,
                    updated_at = NOW()
                WHERE id = @id",
                new { id });
        }

        public async Task<string> GenerateClaimNumberAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QuerySingleAsync<string>(
                "SELECT generate_expense_claim_number(@companyId)",
                new { companyId });
        }

        public async Task<ExpenseSummary> GetSummaryAsync(
            Guid companyId,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var dateFilter = "";
            var parameters = new DynamicParameters();
            parameters.Add("companyId", companyId);

            if (fromDate.HasValue)
            {
                dateFilter += " AND expense_date >= @fromDate";
                parameters.Add("fromDate", fromDate.Value);
            }
            if (toDate.HasValue)
            {
                dateFilter += " AND expense_date <= @toDate";
                parameters.Add("toDate", toDate.Value);
            }

            var summarySql = $@"
                SELECT
                    COUNT(*) as total_claims,
                    COUNT(*) FILTER (WHERE status = 'draft') as draft_claims,
                    COUNT(*) FILTER (WHERE status IN ('submitted', 'pending_approval')) as pending_claims,
                    COUNT(*) FILTER (WHERE status = 'approved') as approved_claims,
                    COUNT(*) FILTER (WHERE status = 'rejected') as rejected_claims,
                    COUNT(*) FILTER (WHERE status = 'reimbursed') as reimbursed_claims,
                    COALESCE(SUM(amount), 0) as total_amount,
                    COALESCE(SUM(amount) FILTER (WHERE status IN ('submitted', 'pending_approval')), 0) as pending_amount,
                    COALESCE(SUM(amount) FILTER (WHERE status = 'approved'), 0) as approved_amount,
                    COALESCE(SUM(amount) FILTER (WHERE status = 'reimbursed'), 0) as reimbursed_amount
                FROM expense_claims
                WHERE company_id = @companyId {dateFilter}";

            var summary = await connection.QuerySingleAsync<ExpenseSummary>(summarySql, parameters);

            // Get amount by category
            var categorySql = $@"
                SELECT cat.name, COALESCE(SUM(ec.amount), 0) as amount
                FROM expense_claims ec
                JOIN expense_categories cat ON ec.category_id = cat.id
                WHERE ec.company_id = @companyId {dateFilter}
                GROUP BY cat.name
                ORDER BY amount DESC";

            var categoryAmounts = await connection.QueryAsync<(string Name, decimal Amount)>(categorySql, parameters);
            summary.AmountByCategory = categoryAmounts.ToDictionary(x => x.Name, x => x.Amount);

            return summary;
        }
    }
}
