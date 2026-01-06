using Application.DTOs.Inventory;
using Core.Entities.Inventory;
using Core.Common;

namespace Application.Interfaces.Inventory
{
    /// <summary>
    /// Service interface for Stock Transfer operations
    /// </summary>
    public interface IStockTransferService
    {
        Task<Result<StockTransfer>> GetByIdAsync(Guid id);
        Task<Result<StockTransfer>> GetByIdWithItemsAsync(Guid id);
        Task<Result<(IEnumerable<StockTransfer> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Result<IEnumerable<StockTransfer>>> GetPendingTransfersAsync(Guid companyId);
        Task<Result<IEnumerable<StockTransfer>>> GetByStatusAsync(Guid companyId, string status);
        Task<Result<StockTransfer>> CreateAsync(CreateStockTransferDto dto);
        Task<Result> UpdateAsync(Guid id, UpdateStockTransferDto dto);
        Task<Result> DeleteAsync(Guid id);
        Task<Result> DispatchAsync(Guid id, Guid userId);
        Task<Result> CompleteAsync(Guid id, Guid userId);
        Task<Result> CancelAsync(Guid id, Guid userId);
    }
}
