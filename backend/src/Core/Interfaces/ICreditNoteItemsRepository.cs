using Core.Entities;

namespace Core.Interfaces
{
    public interface ICreditNoteItemsRepository
    {
        Task<CreditNoteItems?> GetByIdAsync(Guid id);
        Task<IEnumerable<CreditNoteItems>> GetByCreditNoteIdAsync(Guid creditNoteId);
        Task<CreditNoteItems> AddAsync(CreditNoteItems entity);
        Task UpdateAsync(CreditNoteItems entity);
        Task DeleteAsync(Guid id);
        Task DeleteByCreditNoteIdAsync(Guid creditNoteId);
    }
}
