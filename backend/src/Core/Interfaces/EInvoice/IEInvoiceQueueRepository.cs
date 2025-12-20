using Core.Entities.EInvoice;

namespace Core.Interfaces.EInvoice
{
    public interface IEInvoiceQueueRepository
    {
        Task<EInvoiceQueue?> GetByIdAsync(Guid id);
        Task<EInvoiceQueue?> GetByInvoiceIdAsync(Guid invoiceId, string? status = null);
        Task<IEnumerable<EInvoiceQueue>> GetPendingAsync(int limit = 50);
        Task<IEnumerable<EInvoiceQueue>> GetRetryableAsync(DateTime currentTime, int limit = 50);
        Task<IEnumerable<EInvoiceQueue>> GetByCompanyIdAsync(Guid companyId, string? status = null);
        Task<EInvoiceQueue> AddAsync(EInvoiceQueue queueItem);
        Task UpdateAsync(EInvoiceQueue queueItem);
        Task UpdateStatusAsync(Guid id, string status, string? errorCode = null, string? errorMessage = null);
        Task MarkAsProcessingAsync(Guid id, string processorId);
        Task MarkAsCompletedAsync(Guid id);
        Task MarkAsFailedAsync(Guid id, string errorCode, string errorMessage, DateTime? nextRetryAt = null);
        Task DeleteAsync(Guid id);
        Task CancelByInvoiceIdAsync(Guid invoiceId);
    }
}
