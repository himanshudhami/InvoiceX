using Core.Entities.EInvoice;

namespace Core.Interfaces.EInvoice
{
    public interface IEInvoiceCredentialsRepository
    {
        Task<EInvoiceCredentials?> GetByIdAsync(Guid id);
        Task<EInvoiceCredentials?> GetByCompanyIdAsync(Guid companyId, string environment = "production");
        Task<IEnumerable<EInvoiceCredentials>> GetAllByCompanyIdAsync(Guid companyId);
        Task<EInvoiceCredentials> AddAsync(EInvoiceCredentials credentials);
        Task UpdateAsync(EInvoiceCredentials credentials);
        Task DeleteAsync(Guid id);
        Task UpdateTokenAsync(Guid id, string authToken, DateTime tokenExpiry, string? sek = null);
    }
}
