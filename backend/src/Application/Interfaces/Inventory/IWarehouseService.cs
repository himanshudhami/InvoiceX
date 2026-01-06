using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Common;

namespace Application.Interfaces.Inventory
{
    /// <summary>
    /// Service interface for Warehouse operations
    /// </summary>
    public interface IWarehouseService
    {
        Task<Result<Warehouse>> GetByIdAsync(Guid id);
        Task<Result<IEnumerable<Warehouse>>> GetByCompanyIdAsync(Guid companyId);
        Task<Result<(IEnumerable<Warehouse> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Result<Warehouse?>> GetDefaultAsync(Guid companyId);
        Task<Result<IEnumerable<Warehouse>>> GetActiveAsync(Guid companyId);
        Task<Result<Warehouse>> CreateAsync(CreateWarehouseDto dto);
        Task<Result> UpdateAsync(Guid id, UpdateWarehouseDto dto);
        Task<Result> DeleteAsync(Guid id);
        Task<Result> SetDefaultAsync(Guid companyId, Guid warehouseId);
    }
}
