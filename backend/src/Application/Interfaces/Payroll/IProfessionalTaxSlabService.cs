using Application.DTOs.Payroll;
using Core.Common;

namespace Application.Interfaces.Payroll;

/// <summary>
/// Service interface for managing Professional Tax slabs
/// </summary>
public interface IProfessionalTaxSlabService
{
    /// <summary>
    /// Get all PT slabs, optionally filtered by state
    /// </summary>
    Task<Result<IEnumerable<ProfessionalTaxSlabDto>>> GetAllAsync(string? state = null);

    /// <summary>
    /// Get a PT slab by ID
    /// </summary>
    Task<Result<ProfessionalTaxSlabDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get PT slabs for a specific state
    /// </summary>
    Task<Result<IEnumerable<ProfessionalTaxSlabDto>>> GetByStateAsync(string state);

    /// <summary>
    /// Get the applicable PT slab for a given income and state
    /// </summary>
    Task<Result<ProfessionalTaxSlabDto?>> GetSlabForIncomeAsync(decimal monthlyIncome, string state);

    /// <summary>
    /// Get all distinct states that have PT slabs configured
    /// </summary>
    Task<Result<IEnumerable<string>>> GetDistinctStatesAsync();

    /// <summary>
    /// Create a new PT slab
    /// </summary>
    Task<Result<ProfessionalTaxSlabDto>> CreateAsync(CreateProfessionalTaxSlabDto dto);

    /// <summary>
    /// Update an existing PT slab
    /// </summary>
    Task<Result<bool>> UpdateAsync(Guid id, UpdateProfessionalTaxSlabDto dto);

    /// <summary>
    /// Delete a PT slab
    /// </summary>
    Task<Result<bool>> DeleteAsync(Guid id);

    /// <summary>
    /// Bulk create PT slabs (for imports)
    /// </summary>
    Task<Result<IEnumerable<ProfessionalTaxSlabDto>>> BulkCreateAsync(IEnumerable<CreateProfessionalTaxSlabDto> dtos);
}
