using Application.DTOs.EmployeeDocuments;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces;

/// <summary>
/// Service interface for Employee Documents operations
/// </summary>
public interface IEmployeeDocumentsService
{
    // Document operations
    /// <summary>
    /// Get document by ID
    /// </summary>
    Task<Result<EmployeeDocumentDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get paginated documents with filtering (admin)
    /// </summary>
    Task<Result<(IEnumerable<EmployeeDocumentSummaryDto> Items, int TotalCount)>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null);
    Task<Result<IEnumerable<EmployeeDocumentSummaryDto>>> GetByCompanyAsync(Guid companyId);

    /// <summary>
    /// Get documents for an employee (including company-wide)
    /// </summary>
    Task<Result<IEnumerable<EmployeeDocumentSummaryDto>>> GetByEmployeeAsync(Guid employeeId, Guid companyId, string? documentType = null);

    /// <summary>
    /// Get company-wide documents
    /// </summary>
    Task<Result<IEnumerable<EmployeeDocumentSummaryDto>>> GetCompanyWideAsync(Guid companyId);

    /// <summary>
    /// Create a new document
    /// </summary>
    Task<Result<EmployeeDocument>> CreateAsync(CreateEmployeeDocumentDto dto);

    /// <summary>
    /// Update a document
    /// </summary>
    Task<Result> UpdateAsync(Guid id, UpdateEmployeeDocumentDto dto);

    /// <summary>
    /// Delete a document
    /// </summary>
    Task<Result> DeleteAsync(Guid id);

    // Document Request operations
    /// <summary>
    /// Get document request by ID
    /// </summary>
    Task<Result<DocumentRequestSummaryDto>> GetRequestByIdAsync(Guid id);

    /// <summary>
    /// Get document requests by employee
    /// </summary>
    Task<Result<IEnumerable<DocumentRequestSummaryDto>>> GetRequestsByEmployeeAsync(Guid employeeId);

    /// <summary>
    /// Get pending document requests (admin)
    /// </summary>
    Task<Result<IEnumerable<DocumentRequestSummaryDto>>> GetPendingRequestsAsync(Guid companyId);

    /// <summary>
    /// Create a document request
    /// </summary>
    Task<Result<DocumentRequest>> CreateRequestAsync(Guid employeeId, CreateDocumentRequestDto dto);

    /// <summary>
    /// Update a document request (admin - approve/reject)
    /// </summary>
    Task<Result> UpdateRequestAsync(Guid id, Guid processedBy, UpdateDocumentRequestDto dto);
}
