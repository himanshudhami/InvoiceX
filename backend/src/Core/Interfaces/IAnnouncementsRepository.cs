using Core.Entities;

namespace Core.Interfaces;

public interface IAnnouncementsRepository
{
    Task<Announcement?> GetByIdAsync(Guid id);
    Task<IEnumerable<Announcement>> GetAllAsync();
    Task<(IEnumerable<Announcement> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<IEnumerable<Announcement>> GetByCompanyAsync(Guid companyId, bool activeOnly = true);
    Task<IEnumerable<Announcement>> GetActiveForEmployeeAsync(Guid companyId, Guid employeeId);
    Task<int> GetUnreadCountAsync(Guid companyId, Guid employeeId);
    Task<Announcement> AddAsync(Announcement entity);
    Task UpdateAsync(Announcement entity);
    Task DeleteAsync(Guid id);

    // Read tracking
    Task MarkAsReadAsync(Guid announcementId, Guid employeeId);
    Task<bool> IsReadByEmployeeAsync(Guid announcementId, Guid employeeId);
}
