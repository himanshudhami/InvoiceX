using Application.DTOs.AssetRequest;
using Application.Interfaces;
using Application.Interfaces.Approval;
using Core.Abstractions;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for asset request operations
    /// </summary>
    public class AssetRequestService : IAssetRequestService
    {
        private readonly IAssetRequestRepository _repository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IAssetsRepository _assetsRepository;
        private readonly IApprovalWorkflowService? _approvalWorkflowService;
        private readonly ILogger<AssetRequestService>? _logger;

        public AssetRequestService(
            IAssetRequestRepository repository,
            IEmployeesRepository employeesRepository,
            IAssetsRepository assetsRepository,
            IApprovalWorkflowService? approvalWorkflowService = null,
            ILogger<AssetRequestService>? logger = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
            _assetsRepository = assetsRepository ?? throw new ArgumentNullException(nameof(assetsRepository));
            _approvalWorkflowService = approvalWorkflowService;
            _logger = logger;
        }

        public async Task<Result<AssetRequestDetailDto>> GetByIdAsync(Guid id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null)
                return Error.NotFound("Asset request not found");

            var dto = await MapToDetailDtoAsync(request);

            // Get approval workflow status if available
            if (_approvalWorkflowService != null)
            {
                var approvalStatus = await _approvalWorkflowService.GetActivityApprovalStatusAsync(
                    ActivityTypes.AssetRequest, id);

                if (approvalStatus.IsSuccess && approvalStatus.Value != null)
                {
                    dto.ApprovalRequestId = approvalStatus.Value.Id;
                    dto.HasApprovalWorkflow = true;
                    dto.CurrentApprovalStep = approvalStatus.Value.CurrentStep;
                    dto.TotalApprovalSteps = approvalStatus.Value.Steps?.Count ?? 0;
                }
            }

            return Result<AssetRequestDetailDto>.Success(dto);
        }

        public async Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetByCompanyAsync(Guid companyId, string? status = null)
        {
            var requests = await _repository.GetByCompanyAsync(companyId, status);
            var dtos = await MapToSummaryDtosAsync(requests);
            return Result<IEnumerable<AssetRequestSummaryDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetByEmployeeAsync(Guid employeeId, string? status = null)
        {
            var requests = await _repository.GetByEmployeeAsync(employeeId, status);
            var dtos = await MapToSummaryDtosAsync(requests);
            return Result<IEnumerable<AssetRequestSummaryDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetPendingForCompanyAsync(Guid companyId)
        {
            return await GetByCompanyAsync(companyId, AssetRequestStatus.Pending);
        }

        public async Task<Result<IEnumerable<AssetRequestSummaryDto>>> GetApprovedUnfulfilledAsync(Guid companyId)
        {
            var requests = await _repository.GetApprovedUnfulfilledAsync(companyId);
            var dtos = await MapToSummaryDtosAsync(requests);
            return Result<IEnumerable<AssetRequestSummaryDto>>.Success(dtos);
        }

        public async Task<Result<AssetRequestDetailDto>> CreateAsync(Guid employeeId, Guid companyId, CreateAssetRequestDto dto)
        {
            // Validate employee exists
            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee == null)
                return Error.NotFound("Employee not found");

            // Create the request
            var request = new Core.Entities.AssetRequest
            {
                CompanyId = companyId,
                EmployeeId = employeeId,
                AssetType = dto.AssetType,
                Category = dto.Category,
                Title = dto.Title,
                Description = dto.Description,
                Justification = dto.Justification,
                Specifications = dto.Specifications,
                Priority = dto.Priority,
                Status = AssetRequestStatus.Pending,
                Quantity = dto.Quantity,
                EstimatedBudget = dto.EstimatedBudget,
                RequestedByDate = dto.RequestedByDate
            };

            var created = await _repository.AddAsync(request);

            // Start approval workflow if service is available
            if (_approvalWorkflowService != null)
            {
                var workflowResult = await _approvalWorkflowService.StartWorkflowAsync(created);
                if (workflowResult.IsFailure)
                {
                    _logger?.LogWarning(
                        "Failed to start approval workflow for asset request {RequestId}: {Error}. Falling back to legacy approval.",
                        created.Id, workflowResult.Error?.Message);
                }
                else
                {
                    _logger?.LogInformation(
                        "Started approval workflow {WorkflowRequestId} for asset request {RequestId}",
                        workflowResult.Value?.Id, created.Id);
                }
            }

            return await GetByIdAsync(created.Id);
        }

        public async Task<Result<AssetRequestDetailDto>> UpdateAsync(Guid employeeId, Guid requestId, UpdateAssetRequestDto dto)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            if (request.EmployeeId != employeeId)
                return Error.Forbidden("You can only update your own asset requests");

            if (!request.CanEdit)
                return Error.Validation("Only pending requests can be updated");

            // Apply updates
            if (dto.AssetType != null) request.AssetType = dto.AssetType;
            if (dto.Category != null) request.Category = dto.Category;
            if (dto.Title != null) request.Title = dto.Title;
            if (dto.Description != null) request.Description = dto.Description;
            if (dto.Justification != null) request.Justification = dto.Justification;
            if (dto.Specifications != null) request.Specifications = dto.Specifications;
            if (dto.Priority != null) request.Priority = dto.Priority;
            if (dto.Quantity.HasValue) request.Quantity = dto.Quantity.Value;
            if (dto.EstimatedBudget.HasValue) request.EstimatedBudget = dto.EstimatedBudget;
            if (dto.RequestedByDate.HasValue) request.RequestedByDate = dto.RequestedByDate;

            await _repository.UpdateAsync(request);
            return await GetByIdAsync(requestId);
        }

        public async Task<Result<AssetRequestDetailDto>> ApproveAsync(Guid requestId, Guid approvedBy, ApproveAssetRequestDto dto)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            if (request.Status != AssetRequestStatus.Pending && request.Status != AssetRequestStatus.InProgress)
                return Error.Validation("Only pending or in-progress requests can be approved");

            await _repository.UpdateStatusAsync(requestId, AssetRequestStatus.Approved, approvedBy);
            return await GetByIdAsync(requestId);
        }

        public async Task<Result<AssetRequestDetailDto>> RejectAsync(Guid requestId, Guid rejectedBy, RejectAssetRequestDto dto)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            if (request.Status != AssetRequestStatus.Pending && request.Status != AssetRequestStatus.InProgress)
                return Error.Validation("Only pending or in-progress requests can be rejected");

            await _repository.UpdateStatusAsync(requestId, AssetRequestStatus.Rejected, rejectedBy, dto.Reason);
            return await GetByIdAsync(requestId);
        }

        public async Task<Result<AssetRequestDetailDto>> CancelAsync(Guid requestId, Guid cancelledBy, CancelAssetRequestDto dto)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            if (!request.CanCancel)
                return Error.Validation("This request cannot be cancelled");

            await _repository.UpdateStatusAsync(requestId, AssetRequestStatus.Cancelled, null, dto.Reason);

            // If using workflow, cancel it too
            if (_approvalWorkflowService != null)
            {
                var approvalStatus = await _approvalWorkflowService.GetActivityApprovalStatusAsync(
                    ActivityTypes.AssetRequest, requestId);
                if (approvalStatus.IsSuccess && approvalStatus.Value != null)
                {
                    await _approvalWorkflowService.CancelAsync(approvalStatus.Value.Id, cancelledBy);
                }
            }

            return await GetByIdAsync(requestId);
        }

        public async Task<Result<AssetRequestDetailDto>> FulfillAsync(Guid requestId, Guid fulfilledBy, FulfillAssetRequestDto dto)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            if (!request.CanFulfill)
                return Error.Validation("Only approved and unfulfilled requests can be fulfilled");

            // Verify assigned asset exists if provided
            if (dto.AssignedAssetId.HasValue)
            {
                var asset = await _assetsRepository.GetByIdAsync(dto.AssignedAssetId.Value);
                if (asset == null)
                    return Error.NotFound("Assigned asset not found");
            }

            await _repository.FulfillAsync(requestId, fulfilledBy, dto.AssignedAssetId, dto.Notes);
            return await GetByIdAsync(requestId);
        }

        public async Task<Result> WithdrawAsync(Guid employeeId, Guid requestId, string? reason = null)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            if (request.EmployeeId != employeeId)
                return Error.Forbidden("You can only withdraw your own asset requests");

            if (request.Status != AssetRequestStatus.Pending)
                return Error.Validation("Only pending requests can be withdrawn");

            await _repository.UpdateStatusAsync(requestId, AssetRequestStatus.Cancelled, null, reason);
            return Result.Success();
        }

        public async Task<Result<AssetRequestStatsDto>> GetStatsAsync(Guid companyId)
        {
            var stats = await _repository.GetStatsAsync(companyId);
            return Result<AssetRequestStatsDto>.Success(new AssetRequestStatsDto
            {
                TotalRequests = stats.TotalRequests,
                PendingRequests = stats.PendingRequests,
                ApprovedRequests = stats.ApprovedRequests,
                RejectedRequests = stats.RejectedRequests,
                FulfilledRequests = stats.FulfilledRequests,
                UnfulfilledApproved = stats.UnfulfilledApproved
            });
        }

        public async Task<Result> DeleteAsync(Guid requestId)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            await _repository.DeleteAsync(requestId);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> UpdateStatusAsync(Guid requestId, string status, string? reason = null)
        {
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound("Asset request not found");

            // Map string status to the appropriate status constant
            var newStatus = status.ToLowerInvariant() switch
            {
                "approved" => AssetRequestStatus.Approved,
                "rejected" => AssetRequestStatus.Rejected,
                "in_progress" => AssetRequestStatus.InProgress,
                "pending" => AssetRequestStatus.Pending,
                "fulfilled" => AssetRequestStatus.Fulfilled,
                "cancelled" => AssetRequestStatus.Cancelled,
                _ => status
            };

            // Update status with reason if rejected
            if (newStatus == AssetRequestStatus.Rejected || newStatus == AssetRequestStatus.Cancelled)
            {
                await _repository.UpdateStatusAsync(requestId, newStatus, null, reason);
            }
            else if (newStatus == AssetRequestStatus.Approved)
            {
                await _repository.UpdateStatusAsync(requestId, newStatus, null);
            }
            else
            {
                await _repository.UpdateStatusAsync(requestId, newStatus, null);
            }

            _logger?.LogInformation(
                "Asset request {RequestId} status updated to {Status}{Reason}",
                requestId, newStatus, reason != null ? $" with reason: {reason}" : "");

            return Result.Success();
        }

        // ==================== Mapping Methods ====================

        private async Task<AssetRequestDetailDto> MapToDetailDtoAsync(Core.Entities.AssetRequest request)
        {
            var employee = await _employeesRepository.GetByIdAsync(request.EmployeeId);
            var approver = request.ApprovedBy.HasValue
                ? await _employeesRepository.GetByIdAsync(request.ApprovedBy.Value)
                : null;
            var fulfiller = request.FulfilledBy.HasValue
                ? await _employeesRepository.GetByIdAsync(request.FulfilledBy.Value)
                : null;
            var assignedAsset = request.AssignedAssetId.HasValue
                ? await _assetsRepository.GetByIdAsync(request.AssignedAssetId.Value)
                : null;

            return new AssetRequestDetailDto
            {
                Id = request.Id,
                CompanyId = request.CompanyId,
                EmployeeId = request.EmployeeId,
                EmployeeName = employee?.EmployeeName ?? string.Empty,
                EmployeeCode = employee?.EmployeeId,
                Department = employee?.Department,
                AssetType = request.AssetType,
                Category = request.Category,
                Title = request.Title,
                Description = request.Description,
                Justification = request.Justification,
                Specifications = request.Specifications,
                Priority = request.Priority,
                Status = request.Status,
                Quantity = request.Quantity,
                EstimatedBudget = request.EstimatedBudget,
                RequestedByDate = request.RequestedByDate,
                RequestedAt = request.RequestedAt,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                ApprovedBy = request.ApprovedBy,
                ApprovedByName = approver?.EmployeeName,
                ApprovedAt = request.ApprovedAt,
                RejectionReason = request.RejectionReason,
                CancelledAt = request.CancelledAt,
                CancellationReason = request.CancellationReason,
                AssignedAssetId = request.AssignedAssetId,
                AssignedAssetName = assignedAsset?.Name,
                FulfilledBy = request.FulfilledBy,
                FulfilledByName = fulfiller?.EmployeeName,
                FulfilledAt = request.FulfilledAt,
                FulfillmentNotes = request.FulfillmentNotes,
                CanEdit = request.CanEdit,
                CanCancel = request.CanCancel,
                CanFulfill = request.CanFulfill
            };
        }

        private async Task<IEnumerable<AssetRequestSummaryDto>> MapToSummaryDtosAsync(IEnumerable<Core.Entities.AssetRequest> requests)
        {
            var result = new List<AssetRequestSummaryDto>();
            foreach (var request in requests)
            {
                var employee = await _employeesRepository.GetByIdAsync(request.EmployeeId);
                result.Add(new AssetRequestSummaryDto
                {
                    Id = request.Id,
                    EmployeeId = request.EmployeeId,
                    EmployeeName = employee?.EmployeeName ?? string.Empty,
                    EmployeeCode = employee?.EmployeeId,
                    AssetType = request.AssetType,
                    Category = request.Category,
                    Title = request.Title,
                    Priority = request.Priority,
                    Status = request.Status,
                    Quantity = request.Quantity,
                    EstimatedBudget = request.EstimatedBudget,
                    RequestedAt = request.RequestedAt,
                    ApprovedAt = request.ApprovedAt,
                    FulfilledAt = request.FulfilledAt
                });
            }
            return result;
        }
    }
}
