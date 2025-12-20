using Core.Entities.EInvoice;

namespace Core.Interfaces.EInvoice
{
    public interface IEInvoiceAuditLogRepository
    {
        Task<EInvoiceAuditLog?> GetByIdAsync(Guid id);
        Task<IEnumerable<EInvoiceAuditLog>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<IEnumerable<EInvoiceAuditLog>> GetByCompanyIdAsync(Guid companyId, int limit = 100);
        Task<EInvoiceAuditLog?> GetByIrnAsync(string irn);
        Task<(IEnumerable<EInvoiceAuditLog> Items, int TotalCount)> GetPagedAsync(
            Guid companyId,
            int pageNumber = 1,
            int pageSize = 20,
            string? actionType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
        Task<EInvoiceAuditLog> AddAsync(EInvoiceAuditLog auditLog);
        Task<IEnumerable<EInvoiceAuditLog>> GetErrorsAsync(Guid companyId, int limit = 50);
    }
}
