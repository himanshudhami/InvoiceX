using Application.DTOs.Announcements;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces;

/// <summary>
/// Service interface for Announcements operations
/// </summary>
public interface IAnnouncementsService
{
    /// <summary>
    /// Get announcement by ID
    /// </summary>
    Task<Result<AnnouncementDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get paginated announcements with filtering
    /// </summary>
    Task<Result<(IEnumerable<AnnouncementSummaryDto> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);

    /// <summary>
    /// Get announcements by company
    /// </summary>
    Task<Result<IEnumerable<AnnouncementSummaryDto>>> GetByCompanyAsync(Guid companyId, bool activeOnly = true);

    /// <summary>
    /// Get active announcements for an employee (portal use)
    /// </summary>
    Task<Result<IEnumerable<AnnouncementSummaryDto>>> GetActiveForEmployeeAsync(Guid companyId, Guid employeeId);

    /// <summary>
    /// Get unread count for an employee
    /// </summary>
    Task<Result<int>> GetUnreadCountAsync(Guid companyId, Guid employeeId);

    /// <summary>
    /// Create a new announcement
    /// </summary>
    Task<Result<Announcement>> CreateAsync(CreateAnnouncementDto dto);

    /// <summary>
    /// Update an existing announcement
    /// </summary>
    Task<Result> UpdateAsync(Guid id, UpdateAnnouncementDto dto);

    /// <summary>
    /// Delete an announcement
    /// </summary>
    Task<Result> DeleteAsync(Guid id);

    /// <summary>
    /// Mark announcement as read by employee
    /// </summary>
    Task<Result> MarkAsReadAsync(Guid announcementId, Guid employeeId);
}
