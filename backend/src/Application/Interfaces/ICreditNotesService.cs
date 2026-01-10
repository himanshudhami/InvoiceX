using Application.DTOs.CreditNotes;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces
{
    public interface ICreditNotesService
    {
        Task<Result<CreditNotes>> GetByIdAsync(Guid id);
        Task<Result<IEnumerable<CreditNotes>>> GetAllAsync();
        Task<Result<(IEnumerable<CreditNotes> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);

        Task<Result<CreditNotes>> CreateAsync(CreateCreditNotesDto dto, List<CreditNoteItemDto>? items = null);
        Task<Result> UpdateAsync(Guid id, UpdateCreditNotesDto dto);
        Task<Result> DeleteAsync(Guid id);
        Task<Result<bool>> ExistsAsync(Guid id);

        // Credit note from invoice
        Task<Result<CreditNotes>> CreateFromInvoiceAsync(CreateCreditNoteFromInvoiceDto dto);

        // Get credit notes for an invoice
        Task<Result<IEnumerable<CreditNotes>>> GetByInvoiceIdAsync(Guid invoiceId);

        // Generate next credit note number
        Task<Result<string>> GenerateNextNumberAsync(Guid companyId);

        // Issue credit note (change status from draft to issued)
        Task<Result<CreditNotes>> IssueAsync(Guid id);

        // Cancel credit note
        Task<Result<CreditNotes>> CancelAsync(Guid id, string? reason = null);

        // Get credit note items
        Task<Result<IEnumerable<CreditNoteItems>>> GetItemsAsync(Guid creditNoteId);
    }
}
