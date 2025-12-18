using Application.DTOs.Approval;
using Application.Interfaces.Approval;
using Core.Abstractions;
using Core.Common;
using Core.Entities.Approval;
using Core.Interfaces;
using Core.Interfaces.Approval;

namespace Application.Services.Approval
{
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly IApprovalWorkflowRepository _workflowRepository;
        private readonly IApprovalTemplateRepository _templateRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IApproverResolverFactory _resolverFactory;

        public ApprovalWorkflowService(
            IApprovalWorkflowRepository workflowRepository,
            IApprovalTemplateRepository templateRepository,
            IEmployeesRepository employeesRepository,
            IApproverResolverFactory resolverFactory)
        {
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
            _resolverFactory = resolverFactory ?? throw new ArgumentNullException(nameof(resolverFactory));
        }

        public async Task<Result<ApprovalRequestDetailDto>> StartWorkflowAsync(IApprovableActivity activity)
        {
            // Get default template for this activity type
            var template = await _templateRepository.GetDefaultTemplateAsync(activity.CompanyId, activity.ActivityType);
            if (template == null)
            {
                return Error.NotFound($"No active approval workflow template found for activity type '{activity.ActivityType}'");
            }

            // Get template steps
            var templateSteps = (await _templateRepository.GetStepsByTemplateAsync(template.Id)).ToList();
            if (!templateSteps.Any())
            {
                return Error.Validation($"Approval workflow template '{template.Name}' has no steps configured");
            }

            // Create approval request
            var request = new ApprovalRequest
            {
                CompanyId = activity.CompanyId,
                TemplateId = template.Id,
                ActivityType = activity.ActivityType,
                ActivityId = activity.ActivityId,
                RequestorId = activity.RequestorId,
                CurrentStep = 1,
                Status = ApprovalRequestStatus.InProgress
            };

            // Create request steps and resolve approvers
            var requestSteps = new List<ApprovalRequestStep>();
            foreach (var templateStep in templateSteps.OrderBy(s => s.StepOrder))
            {
                var approverIdResult = await _resolverFactory.ResolveApproverAsync(activity.RequestorId, templateStep);

                var requestStep = new ApprovalRequestStep
                {
                    StepOrder = templateStep.StepOrder,
                    StepName = templateStep.Name,
                    ApproverType = templateStep.ApproverType,
                    AssignedToId = approverIdResult,
                    Status = templateStep.StepOrder == 1 ? ApprovalStepStatus.Pending : ApprovalStepStatus.Pending
                };

                requestSteps.Add(requestStep);
            }

            // Save request with all steps
            var createdRequest = await _workflowRepository.CreateRequestWithStepsAsync(request, requestSteps);

            return await GetRequestStatusAsync(createdRequest.Id);
        }

        public async Task<Result<IEnumerable<PendingApprovalDto>>> GetPendingApprovalsForUserAsync(Guid employeeId)
        {
            if (employeeId == Guid.Empty)
                return Error.Validation("Employee ID is required");

            var pendingSteps = await _workflowRepository.GetPendingApprovalsForUserAsync(employeeId);
            var result = new List<PendingApprovalDto>();

            foreach (var step in pendingSteps)
            {
                var request = await _workflowRepository.GetByIdAsync(step.RequestId);
                if (request == null) continue;

                var requestor = await _employeesRepository.GetByIdAsync(request.RequestorId);
                var allSteps = await _workflowRepository.GetStepsByRequestAsync(request.Id);

                result.Add(new PendingApprovalDto
                {
                    RequestId = request.Id,
                    StepId = step.Id,
                    ActivityType = request.ActivityType,
                    ActivityId = request.ActivityId,
                    ActivityTitle = $"{request.ActivityType} Request",  // Would be fetched from activity
                    RequestorId = request.RequestorId,
                    RequestorName = requestor?.EmployeeName ?? "Unknown",
                    RequestorDepartment = requestor?.Department ?? "",
                    StepName = step.StepName,
                    StepOrder = step.StepOrder,
                    TotalSteps = allSteps.Count(),
                    RequestedAt = request.CreatedAt
                });
            }

            return Result<IEnumerable<PendingApprovalDto>>.Success(result);
        }

        public async Task<Result<int>> GetPendingApprovalsCountAsync(Guid employeeId)
        {
            if (employeeId == Guid.Empty)
                return Error.Validation("Employee ID is required");

            var count = await _workflowRepository.GetPendingApprovalsCountForUserAsync(employeeId);
            return Result<int>.Success(count);
        }

        public async Task<Result<ApprovalRequestDetailDto>> ApproveAsync(Guid requestId, Guid approverId, ApproveRequestDto dto)
        {
            if (requestId == Guid.Empty)
                return Error.Validation("Request ID is required");
            if (approverId == Guid.Empty)
                return Error.Validation("Approver ID is required");

            var request = await _workflowRepository.GetByIdWithStepsAsync(requestId);
            if (request == null)
                return Error.NotFound($"Approval request not found");

            if (request.Status != ApprovalRequestStatus.InProgress)
                return Error.Validation($"Request is already {request.Status}");

            // Get current step
            var currentStep = request.Steps?.FirstOrDefault(s => s.StepOrder == request.CurrentStep);
            if (currentStep == null)
                return Error.Internal("Current step not found");

            // Validate approver is assigned to this step
            if (currentStep.AssignedToId != approverId)
                return Error.Validation("You are not authorized to approve this step");

            if (currentStep.Status != ApprovalStepStatus.Pending)
                return Error.Validation("This step has already been processed");

            // Update step status
            await _workflowRepository.UpdateStepStatusAsync(
                currentStep.Id,
                ApprovalStepStatus.Approved,
                approverId,
                dto.Comments);

            // Check if there are more steps
            var nextStep = request.Steps?.FirstOrDefault(s => s.StepOrder == request.CurrentStep + 1);

            if (nextStep != null)
            {
                // Move to next step
                request.CurrentStep++;
                await _workflowRepository.UpdateAsync(request);
            }
            else
            {
                // All steps complete - approve the request
                request.Status = ApprovalRequestStatus.Approved;
                request.CompletedAt = DateTime.UtcNow;
                await _workflowRepository.UpdateAsync(request);

                // Note: The activity's OnApprovedAsync should be called by the service layer that owns the activity
            }

            return await GetRequestStatusAsync(requestId);
        }

        public async Task<Result<ApprovalRequestDetailDto>> RejectAsync(Guid requestId, Guid approverId, RejectRequestDto dto)
        {
            if (requestId == Guid.Empty)
                return Error.Validation("Request ID is required");
            if (approverId == Guid.Empty)
                return Error.Validation("Approver ID is required");
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return Error.Validation("Rejection reason is required");

            var request = await _workflowRepository.GetByIdWithStepsAsync(requestId);
            if (request == null)
                return Error.NotFound($"Approval request not found");

            if (request.Status != ApprovalRequestStatus.InProgress)
                return Error.Validation($"Request is already {request.Status}");

            // Get current step
            var currentStep = request.Steps?.FirstOrDefault(s => s.StepOrder == request.CurrentStep);
            if (currentStep == null)
                return Error.Internal("Current step not found");

            // Validate approver
            if (currentStep.AssignedToId != approverId)
                return Error.Validation("You are not authorized to reject this request");

            if (currentStep.Status != ApprovalStepStatus.Pending)
                return Error.Validation("This step has already been processed");

            // Update step status
            await _workflowRepository.UpdateStepStatusAsync(
                currentStep.Id,
                ApprovalStepStatus.Rejected,
                approverId,
                dto.Reason);

            // Reject the entire request
            request.Status = ApprovalRequestStatus.Rejected;
            request.CompletedAt = DateTime.UtcNow;
            await _workflowRepository.UpdateAsync(request);

            // Note: The activity's OnRejectedAsync should be called by the service layer that owns the activity

            return await GetRequestStatusAsync(requestId);
        }

        public async Task<Result> CancelAsync(Guid requestId, Guid requestorId)
        {
            if (requestId == Guid.Empty)
                return Error.Validation("Request ID is required");

            var request = await _workflowRepository.GetByIdAsync(requestId);
            if (request == null)
                return Error.NotFound($"Approval request not found");

            if (request.RequestorId != requestorId)
                return Error.Validation("Only the requestor can cancel this request");

            if (request.Status != ApprovalRequestStatus.InProgress)
                return Error.Validation($"Cannot cancel a request that is already {request.Status}");

            request.Status = ApprovalRequestStatus.Cancelled;
            request.CompletedAt = DateTime.UtcNow;
            await _workflowRepository.UpdateAsync(request);

            return Result.Success();
        }

        public async Task<Result<ApprovalRequestDetailDto>> GetRequestStatusAsync(Guid requestId)
        {
            var request = await _workflowRepository.GetByIdWithStepsAsync(requestId);
            if (request == null)
                return Error.NotFound($"Approval request not found");

            var requestor = await _employeesRepository.GetByIdAsync(request.RequestorId);

            var stepDtos = new List<ApprovalRequestStepDto>();
            if (request.Steps != null)
            {
                foreach (var step in request.Steps.OrderBy(s => s.StepOrder))
                {
                    var assignedTo = step.AssignedToId.HasValue
                        ? await _employeesRepository.GetByIdAsync(step.AssignedToId.Value)
                        : null;
                    var actionBy = step.ActionById.HasValue
                        ? await _employeesRepository.GetByIdAsync(step.ActionById.Value)
                        : null;

                    stepDtos.Add(new ApprovalRequestStepDto
                    {
                        Id = step.Id,
                        RequestId = step.RequestId,
                        StepOrder = step.StepOrder,
                        StepName = step.StepName,
                        ApproverType = step.ApproverType,
                        AssignedToId = step.AssignedToId,
                        AssignedToName = assignedTo?.EmployeeName,
                        Status = step.Status,
                        ActionById = step.ActionById,
                        ActionByName = actionBy?.EmployeeName,
                        ActionAt = step.ActionAt,
                        Comments = step.Comments,
                        CreatedAt = step.CreatedAt
                    });
                }
            }

            return Result<ApprovalRequestDetailDto>.Success(new ApprovalRequestDetailDto
            {
                Id = request.Id,
                CompanyId = request.CompanyId,
                ActivityType = request.ActivityType,
                ActivityId = request.ActivityId,
                ActivityTitle = $"{request.ActivityType} Request",
                RequestorId = request.RequestorId,
                RequestorName = requestor?.EmployeeName ?? "Unknown",
                CurrentStep = request.CurrentStep,
                TotalSteps = stepDtos.Count,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                CompletedAt = request.CompletedAt,
                Steps = stepDtos
            });
        }

        public async Task<Result<ApprovalRequestDetailDto?>> GetActivityApprovalStatusAsync(string activityType, Guid activityId)
        {
            var request = await _workflowRepository.GetByActivityAsync(activityType, activityId);
            if (request == null)
                return Result<ApprovalRequestDetailDto?>.Success(null);

            var result = await GetRequestStatusAsync(request.Id);
            if (result.IsFailure)
                return Error.Internal(result.Error!.Message);

            return Result<ApprovalRequestDetailDto?>.Success(result.Value);
        }

        public async Task<Result<IEnumerable<ApprovalRequestDto>>> GetRequestsByRequestorAsync(Guid requestorId, string? status = null)
        {
            var requests = await _workflowRepository.GetByRequestorAsync(requestorId, status);
            var result = new List<ApprovalRequestDto>();

            foreach (var request in requests)
            {
                var requestor = await _employeesRepository.GetByIdAsync(request.RequestorId);
                var steps = await _workflowRepository.GetStepsByRequestAsync(request.Id);

                result.Add(new ApprovalRequestDto
                {
                    Id = request.Id,
                    CompanyId = request.CompanyId,
                    ActivityType = request.ActivityType,
                    ActivityId = request.ActivityId,
                    ActivityTitle = $"{request.ActivityType} Request",
                    RequestorId = request.RequestorId,
                    RequestorName = requestor?.EmployeeName ?? "Unknown",
                    CurrentStep = request.CurrentStep,
                    TotalSteps = steps.Count(),
                    Status = request.Status,
                    CreatedAt = request.CreatedAt,
                    CompletedAt = request.CompletedAt
                });
            }

            return Result<IEnumerable<ApprovalRequestDto>>.Success(result);
        }
    }
}
