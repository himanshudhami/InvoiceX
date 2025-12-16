using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll;

/// <summary>
/// Repository interface for tax parameters.
/// Provides methods to retrieve parameterized tax calculation values.
/// </summary>
public interface ITaxParameterRepository
{
    /// <summary>
    /// Get a tax parameter by ID
    /// </summary>
    Task<TaxParameter?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all active tax parameters
    /// </summary>
    Task<IEnumerable<TaxParameter>> GetAllAsync();

    /// <summary>
    /// Get all parameters for a specific financial year
    /// </summary>
    Task<IEnumerable<TaxParameter>> GetByFinancialYearAsync(string financialYear);

    /// <summary>
    /// Get parameters for a specific regime and financial year
    /// </summary>
    Task<IEnumerable<TaxParameter>> GetByRegimeAndYearAsync(string regime, string financialYear);

    /// <summary>
    /// Get a specific parameter by code, regime, and financial year
    /// </summary>
    Task<TaxParameter?> GetParameterAsync(string parameterCode, string regime, string financialYear);

    /// <summary>
    /// Get a parameter value with a default fallback
    /// </summary>
    Task<decimal> GetParameterValueAsync(string parameterCode, string regime, string financialYear, decimal defaultValue = 0);

    /// <summary>
    /// Get all parameters for a regime as a dictionary (code -> value)
    /// </summary>
    Task<Dictionary<string, decimal>> GetAllParametersForRegimeAsync(string regime, string financialYear);

    /// <summary>
    /// Get paged list of tax parameters with filtering
    /// </summary>
    Task<(IEnumerable<TaxParameter> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);

    /// <summary>
    /// Add a new tax parameter
    /// </summary>
    Task<TaxParameter> AddAsync(TaxParameter entity);

    /// <summary>
    /// Update an existing tax parameter
    /// </summary>
    Task UpdateAsync(TaxParameter entity);

    /// <summary>
    /// Delete a tax parameter
    /// </summary>
    Task DeleteAsync(Guid id);
}
