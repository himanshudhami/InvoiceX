using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll;

/// <summary>
/// Repository interface for salary components.
/// Provides methods to retrieve component configurations for wage base calculations.
/// </summary>
public interface ISalaryComponentRepository
{
    /// <summary>
    /// Get a salary component by ID
    /// </summary>
    Task<SalaryComponent?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all active salary components (global and company-specific)
    /// </summary>
    Task<IEnumerable<SalaryComponent>> GetAllAsync();

    /// <summary>
    /// Get components for a specific company (includes global defaults and company overrides)
    /// </summary>
    Task<IEnumerable<SalaryComponent>> GetByCompanyIdAsync(Guid? companyId);

    /// <summary>
    /// Get a component by code for a company (checks company-specific first, then global)
    /// </summary>
    Task<SalaryComponent?> GetByCodeAsync(string componentCode, Guid? companyId = null);

    /// <summary>
    /// Get all components that contribute to PF wage base
    /// </summary>
    Task<IEnumerable<SalaryComponent>> GetPfWageComponentsAsync(Guid? companyId = null);

    /// <summary>
    /// Get all components that contribute to ESI wage base
    /// </summary>
    Task<IEnumerable<SalaryComponent>> GetEsiWageComponentsAsync(Guid? companyId = null);

    /// <summary>
    /// Get all components that are taxable
    /// </summary>
    Task<IEnumerable<SalaryComponent>> GetTaxableComponentsAsync(Guid? companyId = null);

    /// <summary>
    /// Get components by type (earning, deduction, employer_contribution)
    /// </summary>
    Task<IEnumerable<SalaryComponent>> GetByTypeAsync(string componentType, Guid? companyId = null);

    /// <summary>
    /// Get paged list with filtering
    /// </summary>
    Task<(IEnumerable<SalaryComponent> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);

    /// <summary>
    /// Add a new salary component
    /// </summary>
    Task<SalaryComponent> AddAsync(SalaryComponent entity);

    /// <summary>
    /// Update an existing salary component
    /// </summary>
    Task UpdateAsync(SalaryComponent entity);

    /// <summary>
    /// Delete a salary component
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Check if a component code exists for a company
    /// </summary>
    Task<bool> ComponentCodeExistsAsync(string componentCode, Guid? companyId, Guid? excludeId = null);
}
