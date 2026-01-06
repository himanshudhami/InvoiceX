using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Common;

namespace Application.Interfaces.Inventory
{
    /// <summary>
    /// Service interface for Stock Group operations
    /// </summary>
    public interface IStockGroupService
    {
        Task<Result<StockGroup>> GetByIdAsync(Guid id);
        Task<Result<IEnumerable<StockGroup>>> GetByCompanyIdAsync(Guid companyId);
        Task<Result<(IEnumerable<StockGroup> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Result<IEnumerable<StockGroup>>> GetHierarchyAsync(Guid companyId);
        Task<Result<IEnumerable<StockGroup>>> GetActiveAsync(Guid companyId);
        Task<Result<string>> GetFullPathAsync(Guid stockGroupId);
        Task<Result<StockGroup>> CreateAsync(CreateStockGroupDto dto);
        Task<Result> UpdateAsync(Guid id, UpdateStockGroupDto dto);
        Task<Result> DeleteAsync(Guid id);
    }
}
