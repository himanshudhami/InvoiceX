using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Common;

namespace Application.Interfaces.Inventory
{
    /// <summary>
    /// Service interface for Stock Item operations
    /// </summary>
    public interface IStockItemService
    {
        Task<Result<StockItem>> GetByIdAsync(Guid id);
        Task<Result<StockItem>> GetByIdWithDetailsAsync(Guid id);
        Task<Result<IEnumerable<StockItem>>> GetByCompanyIdAsync(Guid companyId);
        Task<Result<(IEnumerable<StockItem> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Result<IEnumerable<StockItem>>> GetActiveAsync(Guid companyId);
        Task<Result<IEnumerable<StockItem>>> GetLowStockItemsAsync(Guid companyId);
        Task<Result<IEnumerable<StockItem>>> GetByStockGroupIdAsync(Guid stockGroupId);
        Task<Result<StockPositionDto>> GetStockPositionAsync(Guid stockItemId, Guid? warehouseId = null);
        Task<Result<StockItem>> CreateAsync(CreateStockItemDto dto);
        Task<Result> UpdateAsync(Guid id, UpdateStockItemDto dto);
        Task<Result> DeleteAsync(Guid id);
        Task<Result> RecalculateStockAsync(Guid stockItemId);
    }
}
