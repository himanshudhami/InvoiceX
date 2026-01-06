using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Common;

namespace Application.Interfaces.Inventory
{
    /// <summary>
    /// Service interface for Stock Movement operations
    /// </summary>
    public interface IStockMovementService
    {
        Task<Result<StockMovement>> GetByIdAsync(Guid id);
        Task<Result<(IEnumerable<StockMovement> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Result<IEnumerable<StockLedgerDto>>> GetStockLedgerAsync(
            Guid stockItemId,
            Guid? warehouseId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null);
        Task<Result<StockPositionDto>> GetStockPositionAsync(
            Guid stockItemId,
            Guid? warehouseId = null,
            DateOnly? asOfDate = null);
        Task<Result<StockMovement>> RecordMovementAsync(CreateStockMovementDto dto);
        Task<Result> UpdateAsync(Guid id, UpdateStockMovementDto dto);
        Task<Result> DeleteAsync(Guid id);
        Task<Result> RecalculateRunningTotalsAsync(Guid stockItemId, Guid? warehouseId = null);
    }
}
