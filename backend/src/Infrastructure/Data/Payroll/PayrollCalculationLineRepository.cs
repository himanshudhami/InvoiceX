using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Payroll;

/// <summary>
/// Repository implementation for payroll calculation lines using Dapper.
/// </summary>
public class PayrollCalculationLineRepository : IPayrollCalculationLineRepository
{
    private readonly string _connectionString;

    public PayrollCalculationLineRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<PayrollCalculationLine?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<PayrollCalculationLine>(
            "SELECT * FROM payroll_calculation_lines WHERE id = @id",
            new { id });
    }

    public async Task<IEnumerable<PayrollCalculationLine>> GetByTransactionIdAsync(Guid transactionId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<PayrollCalculationLine>(
            @"SELECT * FROM payroll_calculation_lines
              WHERE transaction_id = @transactionId
              ORDER BY
                CASE line_type
                    WHEN 'earning' THEN 1
                    WHEN 'deduction' THEN 2
                    WHEN 'statutory' THEN 3
                    WHEN 'employer_contribution' THEN 4
                    ELSE 5
                END,
                line_sequence,
                created_at",
            new { transactionId });
    }

    public async Task<IEnumerable<PayrollCalculationLine>> GetByTransactionAndTypeAsync(Guid transactionId, string lineType)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<PayrollCalculationLine>(
            @"SELECT * FROM payroll_calculation_lines
              WHERE transaction_id = @transactionId AND line_type = @lineType
              ORDER BY line_sequence, created_at",
            new { transactionId, lineType });
    }

    public async Task<PayrollCalculationLine?> GetByRuleCodeAsync(Guid transactionId, string ruleCode)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<PayrollCalculationLine>(
            @"SELECT * FROM payroll_calculation_lines
              WHERE transaction_id = @transactionId AND rule_code = @ruleCode
              ORDER BY line_sequence
              LIMIT 1",
            new { transactionId, ruleCode });
    }

    public async Task<IDictionary<string, decimal>> GetSummaryByTypeAsync(Guid transactionId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<(string LineType, decimal Total)>(
            @"SELECT line_type, SUM(computed_amount) as Total
              FROM payroll_calculation_lines
              WHERE transaction_id = @transactionId
              GROUP BY line_type",
            new { transactionId });

        return results.ToDictionary(r => r.LineType, r => r.Total);
    }

    public async Task<PayrollCalculationLine> AddAsync(PayrollCalculationLine entity)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = @"INSERT INTO payroll_calculation_lines
            (transaction_id, line_type, line_sequence, rule_code, description,
             base_amount, rate, computed_amount, config_version, config_snapshot,
             notes, created_at)
            VALUES
            (@TransactionId, @LineType, @LineSequence, @RuleCode, @Description,
             @BaseAmount, @Rate, @ComputedAmount, @ConfigVersion, @ConfigSnapshot::jsonb,
             @Notes, NOW())
            RETURNING *";
        return await connection.QuerySingleAsync<PayrollCalculationLine>(sql, entity);
    }

    public async Task AddRangeAsync(IEnumerable<PayrollCalculationLine> entities)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var sql = @"INSERT INTO payroll_calculation_lines
                (transaction_id, line_type, line_sequence, rule_code, description,
                 base_amount, rate, computed_amount, config_version, config_snapshot,
                 notes, created_at)
                VALUES
                (@TransactionId, @LineType, @LineSequence, @RuleCode, @Description,
                 @BaseAmount, @Rate, @ComputedAmount, @ConfigVersion, @ConfigSnapshot::jsonb,
                 @Notes, NOW())";

            foreach (var entity in entities)
            {
                await connection.ExecuteAsync(sql, entity, transaction);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteByTransactionIdAsync(Guid transactionId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "DELETE FROM payroll_calculation_lines WHERE transaction_id = @transactionId",
            new { transactionId });
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "DELETE FROM payroll_calculation_lines WHERE id = @id",
            new { id });
    }

    public async Task<bool> ExistsForTransactionAsync(Guid transactionId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM payroll_calculation_lines WHERE transaction_id = @transactionId",
            new { transactionId });
        return count > 0;
    }
}
