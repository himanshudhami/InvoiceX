using Application.Interfaces;
using Application.DTOs.SupportTickets;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services;

/// <summary>
/// Service implementation for Support Tickets operations
/// </summary>
public class SupportTicketsService : ISupportTicketsService
{
    private readonly ISupportTicketsRepository _repository;
    private readonly IMapper _mapper;

    public SupportTicketsService(ISupportTicketsRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc />
    public async Task<Result<SupportTicketDetailDto>> GetByIdAsync(Guid id)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        var entity = await _repository.GetByIdWithMessagesAsync(id);
        if (entity == null)
            return Error.NotFound($"Support ticket with ID {id} not found");

        return Result<SupportTicketDetailDto>.Success(_mapper.Map<SupportTicketDetailDto>(entity));
    }

    /// <inheritdoc />
    public async Task<Result<(IEnumerable<SupportTicketSummaryDto> Items, int TotalCount)>> GetPagedAsync(
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
            var dtos = _mapper.Map<IEnumerable<SupportTicketSummaryDto>>(result.Items);

            return Result<(IEnumerable<SupportTicketSummaryDto>, int)>.Success((dtos, result.TotalCount));
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve support tickets: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<SupportTicketSummaryDto>>> GetByCompanyAsync(Guid companyId, string? status = null)
    {
        if (companyId == default)
            return Error.Validation("Company ID cannot be empty");

        try
        {
            IEnumerable<SupportTicket> entities;
            if (!string.IsNullOrEmpty(status))
            {
                entities = await _repository.GetByStatusAsync(companyId, status);
            }
            else
            {
                entities = await _repository.GetByCompanyAsync(companyId);
            }
            var dtos = _mapper.Map<IEnumerable<SupportTicketSummaryDto>>(entities);
            return Result<IEnumerable<SupportTicketSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve support tickets: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<SupportTicketSummaryDto>>> GetByEmployeeAsync(Guid employeeId)
    {
        if (employeeId == default)
            return Error.Validation("Employee ID cannot be empty");

        try
        {
            var entities = await _repository.GetByEmployeeAsync(employeeId);
            var dtos = _mapper.Map<IEnumerable<SupportTicketSummaryDto>>(entities);
            return Result<IEnumerable<SupportTicketSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve support tickets: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SupportTicket>> CreateAsync(CreateSupportTicketDto dto)
    {
        try
        {
            var entity = _mapper.Map<SupportTicket>(dto);
            entity.Id = Guid.NewGuid();
            entity.TicketNumber = await GenerateTicketNumberAsync();
            entity.Status = "open";
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddAsync(entity);
            return Result<SupportTicket>.Success(created);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to create support ticket: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, UpdateSupportTicketDto dto)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Support ticket with ID {id} not found");

            var oldStatus = existing.Status;
            _mapper.Map(dto, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            // If status changed to resolved, set resolved timestamp
            if (oldStatus != "resolved" && dto.Status == "resolved")
            {
                existing.ResolvedAt = DateTime.UtcNow;
            }

            await _repository.UpdateAsync(existing);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to update support ticket: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SupportTicketMessage>> AddMessageAsync(Guid ticketId, Guid senderId, string senderType, CreateTicketMessageDto dto)
    {
        if (ticketId == default)
            return Error.Validation("Ticket ID cannot be empty");
        if (senderId == default)
            return Error.Validation("Sender ID cannot be empty");

        try
        {
            var ticket = await _repository.GetByIdAsync(ticketId);
            if (ticket == null)
                return Error.NotFound($"Support ticket with ID {ticketId} not found");

            var message = new SupportTicketMessage
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                SenderId = senderId,
                SenderType = senderType,
                Message = dto.Message,
                AttachmentUrl = dto.AttachmentUrl,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddMessageAsync(message);
            return Result<SupportTicketMessage>.Success(created);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to add message: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<FaqItemDto>>> GetFaqItemsAsync(Guid? companyId = null, string? category = null)
    {
        try
        {
            var entities = await _repository.GetFaqItemsAsync(companyId);
            // Filter by category in memory if specified
            if (!string.IsNullOrEmpty(category))
            {
                entities = entities.Where(f => f.Category == category);
            }
            var dtos = _mapper.Map<IEnumerable<FaqItemDto>>(entities);
            return Result<IEnumerable<FaqItemDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve FAQ items: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<FaqItem>> GetFaqByIdAsync(Guid id)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        var entity = await _repository.GetFaqByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"FAQ item with ID {id} not found");

        return Result<FaqItem>.Success(entity);
    }

    /// <inheritdoc />
    public async Task<Result<FaqItem>> CreateFaqAsync(CreateFaqDto dto)
    {
        try
        {
            var entity = _mapper.Map<FaqItem>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddFaqAsync(entity);
            return Result<FaqItem>.Success(created);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to create FAQ item: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateFaqAsync(Guid id, UpdateFaqDto dto)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        try
        {
            var existing = await _repository.GetFaqByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"FAQ item with ID {id} not found");

            _mapper.Map(dto, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateFaqAsync(existing);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to update FAQ item: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteFaqAsync(Guid id)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        try
        {
            var existing = await _repository.GetFaqByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"FAQ item with ID {id} not found");

            await _repository.DeleteFaqAsync(id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to delete FAQ item: {ex.Message}");
        }
    }

    private async Task<string> GenerateTicketNumberAsync()
    {
        // Generate ticket number in format: TKT-YYYYMMDD-XXXX
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"TKT-{today}-{random}";
    }
}
