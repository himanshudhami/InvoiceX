using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using AutoMapper;
using Core.Common;
using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services.Payroll
{
    /// <summary>
    /// Service for managing tax declarations with workflow and audit trail support
    /// </summary>
    public class TaxDeclarationService : ITaxDeclarationService
    {
        private readonly IEmployeeTaxDeclarationRepository _repository;
        private readonly IEmployeeTaxDeclarationHistoryRepository _historyRepository;
        private readonly ITaxDeclarationValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<TaxDeclarationService> _logger;

        public TaxDeclarationService(
            IEmployeeTaxDeclarationRepository repository,
            IEmployeeTaxDeclarationHistoryRepository historyRepository,
            ITaxDeclarationValidationService validationService,
            IMapper mapper,
            ILogger<TaxDeclarationService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<EmployeeTaxDeclarationDto>> CreateAsync(
            CreateEmployeeTaxDeclarationDto dto,
            string createdBy)
        {
            _logger.LogInformation("Creating tax declaration for employee {EmployeeId}, FY {FY}", dto.EmployeeId, dto.FinancialYear);

            // Check if declaration already exists for this employee and year
            var existing = await _repository.ExistsForEmployeeAndYearAsync(dto.EmployeeId, dto.FinancialYear);
            if (existing)
            {
                return Error.Conflict($"Tax declaration already exists for employee in {dto.FinancialYear}");
            }

            // Validate business rules
            var validationResult = _validationService.ValidateAndCalculateSummary(dto);
            if (validationResult.IsFailure)
            {
                return Error.Validation(validationResult.Error!.Message);
            }

            var entity = _mapper.Map<EmployeeTaxDeclaration>(dto);
            entity.Status = "draft";

            var created = await _repository.AddAsync(entity);

            // Log audit trail
            await LogHistoryAsync(created.Id, "created", createdBy, null, entity);

            _logger.LogInformation("Tax declaration {Id} created successfully", created.Id);
            return Result<EmployeeTaxDeclarationDto>.Success(_mapper.Map<EmployeeTaxDeclarationDto>(created));
        }

        public async Task<Result> UpdateAsync(
            Guid id,
            UpdateEmployeeTaxDeclarationDto dto,
            string updatedBy)
        {
            _logger.LogInformation("Updating tax declaration {Id}", id);

            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            // Only draft or rejected declarations can be updated
            if (!CanUpdate(declaration.Status))
            {
                return Error.Validation($"Cannot update declaration with status '{declaration.Status}'. Only draft or rejected declarations can be updated.");
            }

            var previousValues = SerializeEntity(declaration);

            // Apply updates
            ApplyUpdates(declaration, dto);

            await _repository.UpdateAsync(declaration);

            // Log audit trail
            await LogHistoryAsync(id, "updated", updatedBy, previousValues, declaration);

            _logger.LogInformation("Tax declaration {Id} updated successfully", id);
            return Result.Success();
        }

        public async Task<Result> SubmitAsync(Guid id, string submittedBy)
        {
            _logger.LogInformation("Submitting tax declaration {Id}", id);

            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            if (declaration.Status != "draft" && declaration.Status != "rejected")
            {
                return Error.Validation($"Cannot submit declaration with status '{declaration.Status}'. Only draft or rejected declarations can be submitted.");
            }

            var previousStatus = declaration.Status;
            await _repository.UpdateStatusAsync(id, "submitted");

            // Log audit trail
            await LogHistoryAsync(id, "submitted", submittedBy,
                JsonSerializer.Serialize(new { status = previousStatus }),
                new { status = "submitted" });

            _logger.LogInformation("Tax declaration {Id} submitted successfully", id);
            return Result.Success();
        }

        public async Task<Result> VerifyAsync(Guid id, string verifiedBy)
        {
            _logger.LogInformation("Verifying tax declaration {Id}", id);

            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            if (declaration.Status != "submitted")
            {
                return Error.Validation($"Cannot verify declaration with status '{declaration.Status}'. Only submitted declarations can be verified.");
            }

            await _repository.UpdateStatusAsync(id, "verified", verifiedBy);

            // Log audit trail
            await LogHistoryAsync(id, "verified", verifiedBy,
                JsonSerializer.Serialize(new { status = "submitted" }),
                new { status = "verified", verifiedBy });

            _logger.LogInformation("Tax declaration {Id} verified by {VerifiedBy}", id, verifiedBy);
            return Result.Success();
        }

        public async Task<Result> RejectAsync(Guid id, RejectDeclarationDto rejectDto, string rejectedBy)
        {
            _logger.LogInformation("Rejecting tax declaration {Id}", id);

            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            if (declaration.Status != "submitted")
            {
                return Error.Validation($"Cannot reject declaration with status '{declaration.Status}'. Only submitted declarations can be rejected.");
            }

            if (string.IsNullOrWhiteSpace(rejectDto.Reason))
            {
                return Error.Validation("Rejection reason is required");
            }

            await _repository.UpdateStatusWithRejectionAsync(id, "rejected", rejectedBy, rejectDto.Reason);

            // Log audit trail with rejection details
            var history = new EmployeeTaxDeclarationHistory
            {
                DeclarationId = id,
                Action = "rejected",
                ChangedBy = rejectedBy,
                RejectionReason = rejectDto.Reason,
                RejectionComments = rejectDto.Comments,
                PreviousValues = JsonSerializer.Serialize(new { status = "submitted" }),
                NewValues = JsonSerializer.Serialize(new { status = "rejected", rejectedBy, reason = rejectDto.Reason })
            };
            await _historyRepository.AddAsync(history);

            _logger.LogInformation("Tax declaration {Id} rejected by {RejectedBy}. Reason: {Reason}", id, rejectedBy, rejectDto.Reason);
            return Result.Success();
        }

        public async Task<Result> ReviseAndResubmitAsync(
            Guid id,
            UpdateEmployeeTaxDeclarationDto dto,
            string submittedBy)
        {
            _logger.LogInformation("Revising and resubmitting tax declaration {Id}", id);

            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            if (declaration.Status != "rejected")
            {
                return Error.Validation($"Cannot revise declaration with status '{declaration.Status}'. Only rejected declarations can be revised.");
            }

            var previousValues = SerializeEntity(declaration);

            // Apply updates
            ApplyUpdates(declaration, dto);

            // Clear rejection and increment revision count
            await _repository.ClearRejectionAsync(id);
            await _repository.IncrementRevisionCountAsync(id);
            await _repository.UpdateAsync(declaration);
            await _repository.UpdateStatusAsync(id, "submitted");

            // Log audit trail
            await LogHistoryAsync(id, "revised", submittedBy, previousValues, declaration);

            _logger.LogInformation("Tax declaration {Id} revised and resubmitted. Revision count: {RevisionCount}",
                id, declaration.RevisionCount + 1);
            return Result.Success();
        }

        public async Task<Result> LockAsync(Guid id, string lockedBy)
        {
            _logger.LogInformation("Locking tax declaration {Id}", id);

            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            if (declaration.Status != "verified")
            {
                return Error.Validation($"Cannot lock declaration with status '{declaration.Status}'. Only verified declarations can be locked.");
            }

            await _repository.UpdateStatusAsync(id, "locked");

            // Log audit trail
            await LogHistoryAsync(id, "locked", lockedBy,
                JsonSerializer.Serialize(new { status = "verified" }),
                new { status = "locked" });

            _logger.LogInformation("Tax declaration {Id} locked by {LockedBy}", id, lockedBy);
            return Result.Success();
        }

        public async Task<Result> UnlockAsync(Guid id, string unlockedBy)
        {
            _logger.LogInformation("Unlocking tax declaration {Id}", id);

            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            if (declaration.Status != "locked")
            {
                return Error.Validation($"Cannot unlock declaration with status '{declaration.Status}'. Only locked declarations can be unlocked.");
            }

            await _repository.UpdateStatusAsync(id, "verified");

            // Log audit trail
            await LogHistoryAsync(id, "unlocked", unlockedBy,
                JsonSerializer.Serialize(new { status = "locked" }),
                new { status = "verified" });

            _logger.LogWarning("Tax declaration {Id} unlocked by {UnlockedBy} - this is an admin action", id, unlockedBy);
            return Result.Success();
        }

        public async Task<Result<TaxDeclarationSummaryDto>> GetSummaryAsync(Guid id)
        {
            var declaration = await _repository.GetByIdAsync(id);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {id} not found");
            }

            var createDto = _mapper.Map<CreateEmployeeTaxDeclarationDto>(declaration);
            var result = _validationService.ValidateAndCalculateSummary(createDto);

            if (result.IsSuccess)
            {
                result.Value!.DeclarationId = id;
            }

            return result;
        }

        public async Task<Result<TaxDeclarationSummaryDto>> ValidateAsync(
            CreateEmployeeTaxDeclarationDto dto,
            Guid? employeeId = null)
        {
            // Check for duplicate if employeeId provided
            if (employeeId.HasValue)
            {
                var existing = await _repository.ExistsForEmployeeAndYearAsync(employeeId.Value, dto.FinancialYear);
                if (existing)
                {
                    return Error.Conflict($"Tax declaration already exists for employee in {dto.FinancialYear}");
                }
            }

            return _validationService.ValidateAndCalculateSummary(dto);
        }

        public async Task<Result<IEnumerable<DeclarationHistoryDto>>> GetHistoryAsync(Guid declarationId)
        {
            var declaration = await _repository.GetByIdAsync(declarationId);
            if (declaration == null)
            {
                return Error.NotFound($"Tax declaration {declarationId} not found");
            }

            var history = await _historyRepository.GetByDeclarationIdAsync(declarationId);
            var historyDtos = _mapper.Map<IEnumerable<DeclarationHistoryDto>>(history);

            return Result<IEnumerable<DeclarationHistoryDto>>.Success(historyDtos);
        }

        private static bool CanUpdate(string? status)
        {
            return status is "draft" or "rejected";
        }

        private async Task LogHistoryAsync(Guid declarationId, string action, string changedBy, string? previousValues, object newValues)
        {
            var history = new EmployeeTaxDeclarationHistory
            {
                DeclarationId = declarationId,
                Action = action,
                ChangedBy = changedBy,
                PreviousValues = previousValues,
                NewValues = newValues is string s ? s : JsonSerializer.Serialize(newValues)
            };

            await _historyRepository.AddAsync(history);
        }

        private static string SerializeEntity(EmployeeTaxDeclaration entity)
        {
            return JsonSerializer.Serialize(new
            {
                entity.Status,
                entity.TaxRegime,
                entity.Sec80cPpf,
                entity.Sec80cElss,
                entity.Sec80cLifeInsurance,
                entity.Sec80cHomeLoanPrincipal,
                entity.Sec80cChildrenTuition,
                entity.Sec80cNsc,
                entity.Sec80cSukanyaSamriddhi,
                entity.Sec80cFixedDeposit,
                entity.Sec80cOthers,
                entity.Sec80ccdNps,
                entity.Sec80dSelfFamily,
                entity.Sec80dParents,
                entity.Sec80dPreventiveCheckup,
                entity.Sec80eEducationLoan,
                entity.Sec24HomeLoanInterest,
                entity.Sec80gDonations,
                entity.Sec80ttaSavingsInterest,
                entity.HraRentPaidAnnual,
                entity.HraMetroCity,
                entity.HraLandlordPan,
                entity.HraLandlordName,
                entity.OtherIncomeAnnual,
                entity.PrevEmployerIncome,
                entity.PrevEmployerTds
            });
        }

        private static void ApplyUpdates(EmployeeTaxDeclaration entity, UpdateEmployeeTaxDeclarationDto dto)
        {
            if (dto.TaxRegime != null) entity.TaxRegime = dto.TaxRegime;
            if (dto.Sec80cPpf.HasValue) entity.Sec80cPpf = dto.Sec80cPpf.Value;
            if (dto.Sec80cElss.HasValue) entity.Sec80cElss = dto.Sec80cElss.Value;
            if (dto.Sec80cLifeInsurance.HasValue) entity.Sec80cLifeInsurance = dto.Sec80cLifeInsurance.Value;
            if (dto.Sec80cHomeLoanPrincipal.HasValue) entity.Sec80cHomeLoanPrincipal = dto.Sec80cHomeLoanPrincipal.Value;
            if (dto.Sec80cChildrenTuition.HasValue) entity.Sec80cChildrenTuition = dto.Sec80cChildrenTuition.Value;
            if (dto.Sec80cNsc.HasValue) entity.Sec80cNsc = dto.Sec80cNsc.Value;
            if (dto.Sec80cSukanyaSamriddhi.HasValue) entity.Sec80cSukanyaSamriddhi = dto.Sec80cSukanyaSamriddhi.Value;
            if (dto.Sec80cFixedDeposit.HasValue) entity.Sec80cFixedDeposit = dto.Sec80cFixedDeposit.Value;
            if (dto.Sec80cOthers.HasValue) entity.Sec80cOthers = dto.Sec80cOthers.Value;
            if (dto.Sec80ccdNps.HasValue) entity.Sec80ccdNps = dto.Sec80ccdNps.Value;
            if (dto.Sec80dSelfFamily.HasValue) entity.Sec80dSelfFamily = dto.Sec80dSelfFamily.Value;
            if (dto.Sec80dParents.HasValue) entity.Sec80dParents = dto.Sec80dParents.Value;
            if (dto.Sec80dPreventiveCheckup.HasValue) entity.Sec80dPreventiveCheckup = dto.Sec80dPreventiveCheckup.Value;
            if (dto.Sec80dSelfSeniorCitizen.HasValue) entity.Sec80dSelfSeniorCitizen = dto.Sec80dSelfSeniorCitizen.Value;
            if (dto.Sec80dParentsSeniorCitizen.HasValue) entity.Sec80dParentsSeniorCitizen = dto.Sec80dParentsSeniorCitizen.Value;
            if (dto.Sec80eEducationLoan.HasValue) entity.Sec80eEducationLoan = dto.Sec80eEducationLoan.Value;
            if (dto.Sec24HomeLoanInterest.HasValue) entity.Sec24HomeLoanInterest = dto.Sec24HomeLoanInterest.Value;
            if (dto.Sec80gDonations.HasValue) entity.Sec80gDonations = dto.Sec80gDonations.Value;
            if (dto.Sec80ttaSavingsInterest.HasValue) entity.Sec80ttaSavingsInterest = dto.Sec80ttaSavingsInterest.Value;
            if (dto.HraRentPaidAnnual.HasValue) entity.HraRentPaidAnnual = dto.HraRentPaidAnnual.Value;
            if (dto.HraMetroCity.HasValue) entity.HraMetroCity = dto.HraMetroCity.Value;
            if (dto.HraLandlordPan != null) entity.HraLandlordPan = dto.HraLandlordPan;
            if (dto.HraLandlordName != null) entity.HraLandlordName = dto.HraLandlordName;
            if (dto.OtherIncomeAnnual.HasValue) entity.OtherIncomeAnnual = dto.OtherIncomeAnnual.Value;
            if (dto.PrevEmployerIncome.HasValue) entity.PrevEmployerIncome = dto.PrevEmployerIncome.Value;
            if (dto.PrevEmployerTds.HasValue) entity.PrevEmployerTds = dto.PrevEmployerTds.Value;
            if (dto.PrevEmployerPf.HasValue) entity.PrevEmployerPf = dto.PrevEmployerPf.Value;
            if (dto.PrevEmployerPt.HasValue) entity.PrevEmployerPt = dto.PrevEmployerPt.Value;
            if (dto.ProofDocuments != null) entity.ProofDocuments = dto.ProofDocuments;
        }
    }
}
