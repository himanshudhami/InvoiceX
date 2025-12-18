using Application.DTOs.Approval;
using Application.Interfaces.Approval;
using Core.Common;
using Core.Entities.Approval;
using Core.Interfaces;
using Core.Interfaces.Approval;

namespace Application.Services.Approval
{
    public class ApprovalTemplateService : IApprovalTemplateService
    {
        private readonly IApprovalTemplateRepository _templateRepository;
        private readonly IEmployeesRepository _employeesRepository;

        public ApprovalTemplateService(
            IApprovalTemplateRepository templateRepository,
            IEmployeesRepository employeesRepository)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        #region Template Operations

        public async Task<Result<IEnumerable<ApprovalWorkflowTemplateDto>>> GetByCompanyAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            var templates = await _templateRepository.GetByCompanyAsync(companyId);
            var result = new List<ApprovalWorkflowTemplateDto>();

            foreach (var template in templates)
            {
                var steps = await _templateRepository.GetStepsByTemplateAsync(template.Id);
                result.Add(MapToTemplateDto(template, steps.Count()));
            }

            return Result<IEnumerable<ApprovalWorkflowTemplateDto>>.Success(result);
        }

        public async Task<Result<IEnumerable<ApprovalWorkflowTemplateDto>>> GetByCompanyAndActivityTypeAsync(Guid companyId, string activityType)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");
            if (string.IsNullOrEmpty(activityType))
                return Error.Validation("Activity type is required");

            var templates = await _templateRepository.GetByCompanyAndActivityTypeAsync(companyId, activityType);
            var result = new List<ApprovalWorkflowTemplateDto>();

            foreach (var template in templates)
            {
                var steps = await _templateRepository.GetStepsByTemplateAsync(template.Id);
                result.Add(MapToTemplateDto(template, steps.Count()));
            }

            return Result<IEnumerable<ApprovalWorkflowTemplateDto>>.Success(result);
        }

        public async Task<Result<ApprovalWorkflowTemplateDetailDto>> GetByIdAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
                return Error.Validation("Template ID is required");

            var template = await _templateRepository.GetByIdWithStepsAsync(templateId);
            if (template == null)
                return Error.NotFound("Template not found");

            return Result<ApprovalWorkflowTemplateDetailDto>.Success(await MapToTemplateDetailDtoAsync(template));
        }

        public async Task<Result<ApprovalWorkflowTemplateDto>> CreateAsync(CreateApprovalTemplateDto dto)
        {
            if (dto.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");
            if (string.IsNullOrWhiteSpace(dto.ActivityType))
                return Error.Validation("Activity type is required");
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Error.Validation("Name is required");

            var template = new ApprovalWorkflowTemplate
            {
                CompanyId = dto.CompanyId,
                ActivityType = dto.ActivityType,
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                IsDefault = dto.IsDefault
            };

            // If setting as default, need to handle existing defaults
            if (dto.IsDefault)
            {
                var existingDefault = await _templateRepository.GetDefaultTemplateAsync(dto.CompanyId, dto.ActivityType);
                if (existingDefault != null)
                {
                    existingDefault.IsDefault = false;
                    await _templateRepository.UpdateAsync(existingDefault);
                }
            }

            var created = await _templateRepository.AddAsync(template);
            return Result<ApprovalWorkflowTemplateDto>.Success(MapToTemplateDto(created, 0));
        }

        public async Task<Result<ApprovalWorkflowTemplateDto>> UpdateAsync(Guid templateId, UpdateApprovalTemplateDto dto)
        {
            if (templateId == Guid.Empty)
                return Error.Validation("Template ID is required");

            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                return Error.NotFound("Template not found");

            template.Name = dto.Name;
            template.Description = dto.Description;
            template.IsActive = dto.IsActive;

            // Handle default flag changes
            if (dto.IsDefault && !template.IsDefault)
            {
                await _templateRepository.SetAsDefaultAsync(templateId, template.CompanyId, template.ActivityType);
            }
            else if (!dto.IsDefault && template.IsDefault)
            {
                template.IsDefault = false;
            }

            await _templateRepository.UpdateAsync(template);

            var steps = await _templateRepository.GetStepsByTemplateAsync(templateId);
            return Result<ApprovalWorkflowTemplateDto>.Success(MapToTemplateDto(template, steps.Count()));
        }

        public async Task<Result> DeleteAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
                return Error.Validation("Template ID is required");

            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                return Error.NotFound("Template not found");

            await _templateRepository.DeleteAsync(templateId);
            return Result.Success();
        }

        public async Task<Result> SetAsDefaultAsync(Guid templateId)
        {
            if (templateId == Guid.Empty)
                return Error.Validation("Template ID is required");

            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                return Error.NotFound("Template not found");

            await _templateRepository.SetAsDefaultAsync(templateId, template.CompanyId, template.ActivityType);
            return Result.Success();
        }

        public async Task<Result> SeedDefaultTemplatesAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            // Check if templates already exist for this company
            var existingTemplates = await _templateRepository.GetByCompanyAsync(companyId);
            if (existingTemplates.Any())
            {
                // Templates already exist, skip seeding
                return Result.Success();
            }

            try
            {
                // Create default leave workflow template
                var leaveTemplate = new ApprovalWorkflowTemplate
                {
                    CompanyId = companyId,
                    ActivityType = "leave",
                    Name = "Default Leave Approval",
                    Description = "Standard leave approval workflow requiring manager approval",
                    IsActive = true,
                    IsDefault = true
                };
                var createdLeaveTemplate = await _templateRepository.AddAsync(leaveTemplate);

                // Add step: Direct Manager Approval
                var leaveStep = new ApprovalWorkflowStep
                {
                    TemplateId = createdLeaveTemplate.Id,
                    StepOrder = 1,
                    Name = "Manager Approval",
                    ApproverType = ApproverTypes.DirectManager,
                    IsRequired = true,
                    CanSkip = false
                };
                await _templateRepository.AddStepAsync(leaveStep);

                // Create default asset request workflow template
                var assetTemplate = new ApprovalWorkflowTemplate
                {
                    CompanyId = companyId,
                    ActivityType = "asset_request",
                    Name = "Default Asset Request Approval",
                    Description = "Standard asset request workflow requiring manager and HR approval",
                    IsActive = true,
                    IsDefault = true
                };
                var createdAssetTemplate = await _templateRepository.AddAsync(assetTemplate);

                // Add step 1: Direct Manager Approval
                var assetStep1 = new ApprovalWorkflowStep
                {
                    TemplateId = createdAssetTemplate.Id,
                    StepOrder = 1,
                    Name = "Manager Approval",
                    ApproverType = ApproverTypes.DirectManager,
                    IsRequired = true,
                    CanSkip = false
                };
                await _templateRepository.AddStepAsync(assetStep1);

                // Add step 2: HR Approval
                var assetStep2 = new ApprovalWorkflowStep
                {
                    TemplateId = createdAssetTemplate.Id,
                    StepOrder = 2,
                    Name = "HR Approval",
                    ApproverType = ApproverTypes.Role,
                    ApproverRole = "HR",
                    IsRequired = true,
                    CanSkip = false
                };
                await _templateRepository.AddStepAsync(assetStep2);

                return Result.Success();
            }
            catch (Exception)
            {
                return Error.Internal("Failed to seed default templates");
            }
        }

        #endregion

        #region Step Operations

        public async Task<Result<ApprovalWorkflowStepDto>> AddStepAsync(Guid templateId, CreateApprovalStepDto dto)
        {
            if (templateId == Guid.Empty)
                return Error.Validation("Template ID is required");
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Error.Validation("Step name is required");
            if (string.IsNullOrWhiteSpace(dto.ApproverType))
                return Error.Validation("Approver type is required");
            if (!ApproverTypes.IsValid(dto.ApproverType))
                return Error.Validation($"Invalid approver type: {dto.ApproverType}");

            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                return Error.NotFound("Template not found");

            // Get max step order and increment
            var maxOrder = await _templateRepository.GetMaxStepOrderAsync(templateId);

            var step = new ApprovalWorkflowStep
            {
                TemplateId = templateId,
                StepOrder = maxOrder + 1,
                Name = dto.Name,
                ApproverType = dto.ApproverType,
                ApproverRole = dto.ApproverRole,
                ApproverUserId = dto.ApproverUserId,
                IsRequired = dto.IsRequired,
                CanSkip = dto.CanSkip,
                AutoApproveAfterDays = dto.AutoApproveAfterDays,
                ConditionsJson = dto.ConditionsJson
            };

            var created = await _templateRepository.AddStepAsync(step);
            return Result<ApprovalWorkflowStepDto>.Success(await MapToStepDtoAsync(created));
        }

        public async Task<Result<ApprovalWorkflowStepDto>> UpdateStepAsync(Guid stepId, UpdateApprovalStepDto dto)
        {
            if (stepId == Guid.Empty)
                return Error.Validation("Step ID is required");
            if (!ApproverTypes.IsValid(dto.ApproverType))
                return Error.Validation($"Invalid approver type: {dto.ApproverType}");

            var step = await _templateRepository.GetStepByIdAsync(stepId);
            if (step == null)
                return Error.NotFound("Step not found");

            step.Name = dto.Name;
            step.ApproverType = dto.ApproverType;
            step.ApproverRole = dto.ApproverRole;
            step.ApproverUserId = dto.ApproverUserId;
            step.IsRequired = dto.IsRequired;
            step.CanSkip = dto.CanSkip;
            step.AutoApproveAfterDays = dto.AutoApproveAfterDays;
            step.ConditionsJson = dto.ConditionsJson;

            await _templateRepository.UpdateStepAsync(step);
            return Result<ApprovalWorkflowStepDto>.Success(await MapToStepDtoAsync(step));
        }

        public async Task<Result> DeleteStepAsync(Guid stepId)
        {
            if (stepId == Guid.Empty)
                return Error.Validation("Step ID is required");

            var step = await _templateRepository.GetStepByIdAsync(stepId);
            if (step == null)
                return Error.NotFound("Step not found");

            await _templateRepository.DeleteStepAsync(stepId);
            return Result.Success();
        }

        public async Task<Result> ReorderStepsAsync(Guid templateId, ReorderStepsDto dto)
        {
            if (templateId == Guid.Empty)
                return Error.Validation("Template ID is required");
            if (dto.StepIds == null || !dto.StepIds.Any())
                return Error.Validation("Step IDs are required");

            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
                return Error.NotFound("Template not found");

            await _templateRepository.ReorderStepsAsync(templateId, dto.StepIds);
            return Result.Success();
        }

        #endregion

        #region Mapping Helpers

        private static ApprovalWorkflowTemplateDto MapToTemplateDto(ApprovalWorkflowTemplate template, int stepCount)
        {
            return new ApprovalWorkflowTemplateDto
            {
                Id = template.Id,
                CompanyId = template.CompanyId,
                ActivityType = template.ActivityType,
                Name = template.Name,
                Description = template.Description,
                IsActive = template.IsActive,
                IsDefault = template.IsDefault,
                StepCount = stepCount,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        private async Task<ApprovalWorkflowTemplateDetailDto> MapToTemplateDetailDtoAsync(ApprovalWorkflowTemplate template)
        {
            var stepDtos = new List<ApprovalWorkflowStepDto>();
            if (template.Steps != null)
            {
                foreach (var step in template.Steps.OrderBy(s => s.StepOrder))
                {
                    stepDtos.Add(await MapToStepDtoAsync(step));
                }
            }

            return new ApprovalWorkflowTemplateDetailDto
            {
                Id = template.Id,
                CompanyId = template.CompanyId,
                ActivityType = template.ActivityType,
                Name = template.Name,
                Description = template.Description,
                IsActive = template.IsActive,
                IsDefault = template.IsDefault,
                StepCount = stepDtos.Count,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Steps = stepDtos
            };
        }

        private async Task<ApprovalWorkflowStepDto> MapToStepDtoAsync(ApprovalWorkflowStep step)
        {
            string? approverUserName = null;
            if (step.ApproverUserId.HasValue)
            {
                var user = await _employeesRepository.GetByIdAsync(step.ApproverUserId.Value);
                approverUserName = user?.EmployeeName;
            }

            return new ApprovalWorkflowStepDto
            {
                Id = step.Id,
                TemplateId = step.TemplateId,
                StepOrder = step.StepOrder,
                Name = step.Name,
                ApproverType = step.ApproverType,
                ApproverRole = step.ApproverRole,
                ApproverUserId = step.ApproverUserId,
                ApproverUserName = approverUserName,
                IsRequired = step.IsRequired,
                CanSkip = step.CanSkip,
                AutoApproveAfterDays = step.AutoApproveAfterDays,
                ConditionsJson = step.ConditionsJson
            };
        }

        #endregion
    }
}
