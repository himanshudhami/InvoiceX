using Core.Entities;

namespace Core.Interfaces
{
    public interface ICreditNotesRepository
    {
        // CRUD operations
        Task<CreditNotes?> GetByIdAsync(Guid id);
        Task<IEnumerable<CreditNotes>> GetAllAsync();
        Task<(IEnumerable<CreditNotes> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<CreditNotes> AddAsync(CreditNotes entity);
        Task UpdateAsync(CreditNotes entity);
        Task DeleteAsync(Guid id);

        // Domain-specific queries
        Task<IEnumerable<CreditNotes>> GetByCompanyIdAsync(Guid companyId);
        Task<IEnumerable<CreditNotes>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<CreditNotes?> GetByNumberAsync(Guid companyId, string creditNoteNumber);
        Task<string> GenerateNextNumberAsync(Guid companyId);
        Task<decimal> GetTotalCreditedAmountForInvoiceAsync(Guid invoiceId);
    }
}
