using Application.DTOs.SupportTickets;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces;

/// <summary>
/// Service interface for Support Tickets operations
/// </summary>
public interface ISupportTicketsService
{
    // Ticket operations
    /// <summary>
    /// Get ticket by ID with messages
    /// </summary>
    Task<Result<SupportTicketDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get paginated tickets with filtering (admin)
    /// </summary>
    Task<Result<(IEnumerable<SupportTicketSummaryDto> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);

    /// <summary>
    /// Get tickets by company (admin)
    /// </summary>
    Task<Result<IEnumerable<SupportTicketSummaryDto>>> GetByCompanyAsync(Guid companyId, string? status = null);

    /// <summary>
    /// Get tickets by employee (portal)
    /// </summary>
    Task<Result<IEnumerable<SupportTicketSummaryDto>>> GetByEmployeeAsync(Guid employeeId);

    /// <summary>
    /// Create a new support ticket
    /// </summary>
    Task<Result<SupportTicket>> CreateAsync(CreateSupportTicketDto dto);

    /// <summary>
    /// Update a support ticket (admin)
    /// </summary>
    Task<Result> UpdateAsync(Guid id, UpdateSupportTicketDto dto);

    /// <summary>
    /// Add a message to a ticket
    /// </summary>
    Task<Result<SupportTicketMessage>> AddMessageAsync(Guid ticketId, Guid senderId, string senderType, CreateTicketMessageDto dto);

    // FAQ operations
    /// <summary>
    /// Get FAQ items by category
    /// </summary>
    Task<Result<IEnumerable<FaqItemDto>>> GetFaqItemsAsync(Guid? companyId = null, string? category = null);

    /// <summary>
    /// Get FAQ item by ID
    /// </summary>
    Task<Result<FaqItem>> GetFaqByIdAsync(Guid id);

    /// <summary>
    /// Create a new FAQ item
    /// </summary>
    Task<Result<FaqItem>> CreateFaqAsync(CreateFaqDto dto);

    /// <summary>
    /// Update a FAQ item
    /// </summary>
    Task<Result> UpdateFaqAsync(Guid id, UpdateFaqDto dto);

    /// <summary>
    /// Delete a FAQ item
    /// </summary>
    Task<Result> DeleteFaqAsync(Guid id);
}
