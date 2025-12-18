using Application.Interfaces;
using Application.DTOs.EmployeeDocuments;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services;

/// <summary>
/// Service implementation for Employee Documents operations
/// </summary>
public class EmployeeDocumentsService : IEmployeeDocumentsService
{
    private readonly IEmployeeDocumentsRepository _repository;
    private readonly IMapper _mapper;

    public EmployeeDocumentsService(IEmployeeDocumentsRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc />
    public async Task<Result<EmployeeDocumentDetailDto>> GetByIdAsync(Guid id)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Document with ID {id} not found");

        return Result<EmployeeDocumentDetailDto>.Success(_mapper.Map<EmployeeDocumentDetailDto>(entity));
    }

    /// <inheritdoc />
    public async Task<Result<(IEnumerable<EmployeeDocumentSummaryDto> Items, int TotalCount)>> GetPagedAsync(
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
            var dtos = _mapper.Map<IEnumerable<EmployeeDocumentSummaryDto>>(result.Items);

            return Result<(IEnumerable<EmployeeDocumentSummaryDto>, int)>.Success((dtos, result.TotalCount));
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve documents: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<EmployeeDocumentSummaryDto>>> GetByEmployeeAsync(Guid employeeId, Guid companyId, string? documentType = null)
    {
        if (employeeId == default)
            return Error.Validation("Employee ID cannot be empty");
        if (companyId == default)
            return Error.Validation("Company ID cannot be empty");

        try
        {
            IEnumerable<EmployeeDocument> entities;
            if (!string.IsNullOrEmpty(documentType))
            {
                entities = await _repository.GetByTypeAsync(employeeId, documentType);
            }
            else
            {
                var employeeDocs = await _repository.GetByEmployeeAsync(employeeId);
                var companyWideDocs = await _repository.GetCompanyWideDocumentsAsync(companyId);
                entities = employeeDocs.Concat(companyWideDocs);
            }
            var dtos = _mapper.Map<IEnumerable<EmployeeDocumentSummaryDto>>(entities);
            return Result<IEnumerable<EmployeeDocumentSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve documents: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<EmployeeDocumentSummaryDto>>> GetCompanyWideAsync(Guid companyId)
    {
        if (companyId == default)
            return Error.Validation("Company ID cannot be empty");

        try
        {
            var entities = await _repository.GetCompanyWideDocumentsAsync(companyId);
            var dtos = _mapper.Map<IEnumerable<EmployeeDocumentSummaryDto>>(entities);
            return Result<IEnumerable<EmployeeDocumentSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve documents: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<EmployeeDocument>> CreateAsync(CreateEmployeeDocumentDto dto)
    {
        try
        {
            var entity = _mapper.Map<EmployeeDocument>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddAsync(entity);
            return Result<EmployeeDocument>.Success(created);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to create document: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, UpdateEmployeeDocumentDto dto)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Document with ID {id} not found");

            _mapper.Map(dto, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to update document: {ex.Message}");
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
                return Error.NotFound($"Document with ID {id} not found");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to delete document: {ex.Message}");
        }
    }

    // Document Request operations
    /// <inheritdoc />
    public async Task<Result<DocumentRequestSummaryDto>> GetRequestByIdAsync(Guid id)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        var entity = await _repository.GetRequestByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Document request with ID {id} not found");

        return Result<DocumentRequestSummaryDto>.Success(_mapper.Map<DocumentRequestSummaryDto>(entity));
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<DocumentRequestSummaryDto>>> GetRequestsByEmployeeAsync(Guid employeeId)
    {
        if (employeeId == default)
            return Error.Validation("Employee ID cannot be empty");

        try
        {
            var entities = await _repository.GetRequestsByEmployeeAsync(employeeId);
            var dtos = _mapper.Map<IEnumerable<DocumentRequestSummaryDto>>(entities);
            return Result<IEnumerable<DocumentRequestSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve document requests: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<DocumentRequestSummaryDto>>> GetPendingRequestsAsync(Guid companyId)
    {
        if (companyId == default)
            return Error.Validation("Company ID cannot be empty");

        try
        {
            var entities = await _repository.GetPendingRequestsAsync(companyId);
            var dtos = _mapper.Map<IEnumerable<DocumentRequestSummaryDto>>(entities);
            return Result<IEnumerable<DocumentRequestSummaryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to retrieve pending requests: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentRequest>> CreateRequestAsync(Guid employeeId, CreateDocumentRequestDto dto)
    {
        if (employeeId == default)
            return Error.Validation("Employee ID cannot be empty");

        try
        {
            var entity = _mapper.Map<DocumentRequest>(dto);
            entity.Id = Guid.NewGuid();
            entity.EmployeeId = employeeId;
            entity.Status = "pending";
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddRequestAsync(entity);
            return Result<DocumentRequest>.Success(created);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to create document request: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateRequestAsync(Guid id, Guid processedBy, UpdateDocumentRequestDto dto)
    {
        if (id == default)
            return Error.Validation("ID cannot be empty");

        try
        {
            var existing = await _repository.GetRequestByIdAsync(id);
            if (existing == null)
                return Error.NotFound($"Document request with ID {id} not found");

            existing.Status = dto.Status;
            existing.RejectionReason = dto.RejectionReason;
            existing.DocumentId = dto.DocumentId;
            existing.ProcessedBy = processedBy;
            existing.ProcessedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateRequestAsync(existing);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to update document request: {ex.Message}");
        }
    }
}
