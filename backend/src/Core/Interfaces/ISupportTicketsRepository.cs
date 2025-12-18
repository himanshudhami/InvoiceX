using Core.Entities;

namespace Core.Interfaces;

public interface ISupportTicketsRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id);
    Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id);
    Task<IEnumerable<SupportTicket>> GetAllAsync();
    Task<(IEnumerable<SupportTicket> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<IEnumerable<SupportTicket>> GetByEmployeeAsync(Guid employeeId);
    Task<IEnumerable<SupportTicket>> GetByCompanyAsync(Guid companyId);
    Task<IEnumerable<SupportTicket>> GetByStatusAsync(Guid companyId, string status);
    Task<IEnumerable<SupportTicket>> GetByAssigneeAsync(Guid assigneeId);
    Task<string> GenerateTicketNumberAsync(Guid companyId);
    Task<SupportTicket> AddAsync(SupportTicket entity);
    Task UpdateAsync(SupportTicket entity);
    Task DeleteAsync(Guid id);

    // Messages
    Task<IEnumerable<SupportTicketMessage>> GetMessagesAsync(Guid ticketId);
    Task<SupportTicketMessage> AddMessageAsync(SupportTicketMessage message);

    // FAQ
    Task<IEnumerable<FaqItem>> GetFaqItemsAsync(Guid? companyId = null);
    Task<FaqItem?> GetFaqByIdAsync(Guid id);
    Task<FaqItem> AddFaqAsync(FaqItem item);
    Task UpdateFaqAsync(FaqItem item);
    Task DeleteFaqAsync(Guid id);
}
