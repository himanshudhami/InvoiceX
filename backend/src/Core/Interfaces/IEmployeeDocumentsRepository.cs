using Core.Entities;

namespace Core.Interfaces;

public interface IEmployeeDocumentsRepository
{
    Task<EmployeeDocument?> GetByIdAsync(Guid id);
    Task<IEnumerable<EmployeeDocument>> GetAllAsync();
    Task<(IEnumerable<EmployeeDocument> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<IEnumerable<EmployeeDocument>> GetByEmployeeAsync(Guid employeeId);
    Task<IEnumerable<EmployeeDocument>> GetByCompanyAsync(Guid companyId);
    Task<IEnumerable<EmployeeDocument>> GetCompanyWideDocumentsAsync(Guid companyId);
    Task<IEnumerable<EmployeeDocument>> GetByTypeAsync(Guid employeeId, string documentType);
    Task<EmployeeDocument> AddAsync(EmployeeDocument entity);
    Task UpdateAsync(EmployeeDocument entity);
    Task DeleteAsync(Guid id);

    // Document Requests
    Task<DocumentRequest?> GetRequestByIdAsync(Guid id);
    Task<IEnumerable<DocumentRequest>> GetRequestsByEmployeeAsync(Guid employeeId);
    Task<IEnumerable<DocumentRequest>> GetPendingRequestsAsync(Guid companyId);
    Task<(IEnumerable<DocumentRequest> Items, int TotalCount)> GetRequestsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<DocumentRequest> AddRequestAsync(DocumentRequest request);
    Task UpdateRequestAsync(DocumentRequest request);
}
