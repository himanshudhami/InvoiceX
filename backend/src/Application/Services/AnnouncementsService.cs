using Application.Interfaces;
using Application.DTOs.Announcements;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services;

/// <summary>
/// Service implementation for Announcements operations
/// </summary>
public class AnnouncementsService : IAnnouncementsService
{
    private readonly IAnnouncementsRepository _repository;
    private readonly IMapper _mapper;

    public AnnouncementsService(IAnnouncementsRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc />
    public async Task<Result<AnnouncementDetailDto>> GetByIdAsync(Guid id)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Announcement with ID {id} not found");

        return Result<AnnouncementDetailDto>.Success(_mapper.Map<AnnouncementDetailDto>(entity));
    }

    /// <inheritdoc />
    public async Task<Result<(IEnumerable<AnnouncementSummaryDto> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        try
        {
            if (pageNumber < 1)
                return Error.Validation("Page number must be greater than 0");
            if (pageSize < 1 || pageSize > 100)
                return Error.Validation("Page size must be between 1 and 100");

            var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
            var dtos = _mapper.Map<IEnumerable<AnnouncementSummaryDto>>(result.Items);

            return Result<(IEnumerable<AnnouncementSummaryDto>, int)>.Success((dtos, result.TotalCount));
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve announcements: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<AnnouncementSummaryDto>>> GetByCompanyAsync(Guid companyId, bool activeOnly = true)
    {
        if (companyId == default)
            return Error.Validation("Company ID cannot be empty");

        try
        {
            var entities = await _repository.GetByCompanyAsync(companyId, activeOnly);
            var dtos = _mapper.Map<IEnumerable<AnnouncementSummaryDto>>(entities);
            return Result<IEnumerable<AnnouncementSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve announcements: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<AnnouncementSummaryDto>>> GetActiveForEmployeeAsync(Guid companyId, Guid employeeId)
    {
        if (companyId == default)
            return Error.Validation("Company ID cannot be empty");
        if (employeeId == default)
            return Error.Validation("Employee ID cannot be empty");

        try
        {
            var entities = await _repository.GetActiveForEmployeeAsync(companyId, employeeId);
            var dtos = _mapper.Map<IEnumerable<AnnouncementSummaryDto>>(entities);
            return Result<IEnumerable<AnnouncementSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve announcements: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> GetUnreadCountAsync(Guid companyId, Guid employeeId)
    {
        if (companyId == default)
            return Error.Validation("Company ID cannot be empty");
        if (employeeId == default)
            return Error.Validation("Employee ID cannot be empty");

        try
        {
            var count = await _repository.GetUnreadCountAsync(companyId, employeeId);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to get unread count: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Announcement>> CreateAsync(CreateAnnouncementDto dto)
    {
        try
        {
            var entity = _mapper.Map<Announcement>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddAsync(entity);
            return Result<Announcement>.Success(created);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to create announcement: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, UpdateAnnouncementDto dto)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Announcement with ID {id} not found");

            _mapper.Map(dto, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to update announcement: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Announcement with ID {id} not found");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to delete announcement: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> MarkAsReadAsync(Guid announcementId, Guid employeeId)
    {
        if (announcementId == default)
            return Error.Validation("Announcement ID cannot be empty");
        if (employeeId == default)
            return Error.Validation("Employee ID cannot be empty");

        try
        {
            await _repository.MarkAsReadAsync(announcementId, employeeId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to mark announcement as read: {ex.Message}");
        }
    }
}
