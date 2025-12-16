using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll;

/// <summary>
/// Repository interface for payroll calculation lines.
/// Provides methods for storing and retrieving calculation audit trails.
/// </summary>
public interface IPayrollCalculationLineRepository
{
    /// <summary>
    /// Get a calculation line by ID
    /// </summary>
    Task<PayrollCalculationLine?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all calculation lines for a transaction
    /// </summary>
    Task<IEnumerable<PayrollCalculationLine>> GetByTransactionIdAsync(Guid transactionId);

    /// <summary>
    /// Get calculation lines by type (earning, deduction, employer_contribution)
    /// </summary>
    Task<IEnumerable<PayrollCalculationLine>> GetByTransactionAndTypeAsync(Guid transactionId, string lineType);

    /// <summary>
    /// Get a specific calculation line by rule code for a transaction
    /// </summary>
    Task<PayrollCalculationLine?> GetByRuleCodeAsync(Guid transactionId, string ruleCode);

    /// <summary>
    /// Get summary of calculation lines grouped by type
    /// </summary>
    Task<IDictionary<string, decimal>> GetSummaryByTypeAsync(Guid transactionId);

    /// <summary>
    /// Add a single calculation line
    /// </summary>
    Task<PayrollCalculationLine> AddAsync(PayrollCalculationLine entity);

    /// <summary>
    /// Add multiple calculation lines (bulk insert)
    /// </summary>
    Task AddRangeAsync(IEnumerable<PayrollCalculationLine> entities);

    /// <summary>
    /// Delete all calculation lines for a transaction
    /// </summary>
    Task DeleteByTransactionIdAsync(Guid transactionId);

    /// <summary>
    /// Delete a specific calculation line
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Check if calculation lines exist for a transaction
    /// </summary>
    Task<bool> ExistsForTransactionAsync(Guid transactionId);
}
