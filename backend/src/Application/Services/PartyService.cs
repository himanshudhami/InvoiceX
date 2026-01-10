using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Audit;
using Application.DTOs.Party;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for Unified Party Management.
    /// Uses tag-driven TDS detection via ITdsTagRuleRepository.
    /// </summary>
    public class PartyService : IPartyService
    {
        private readonly IPartyRepository _repository;
        private readonly ITdsTagRuleRepository _tdsTagRuleRepository;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;

        public PartyService(
            IPartyRepository repository,
            ITdsTagRuleRepository tdsTagRuleRepository,
            IAuditService auditService,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _tdsTagRuleRepository = tdsTagRuleRepository ?? throw new ArgumentNullException(nameof(tdsTagRuleRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // ==================== Basic CRUD ====================

        public async Task<Result<PartyDto>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Party with ID {id} not found");

            return Result<PartyDto>.Success(_mapper.Map<PartyDto>(entity));
        }

        public async Task<Result<PartyDto>> GetByIdWithProfilesAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await _repository.GetByIdWithProfilesAsync(id);
            if (entity == null)
                return Error.NotFound($"Party with ID {id} not found");

            var dto = _mapper.Map<PartyDto>(entity);

            // Load tags
            var tags = await _repository.GetTagsAsync(id);
            dto.Tags = tags.Select(t => _mapper.Map<PartyTagDto>(t)).ToList();

            return Result<PartyDto>.Success(dto);
        }

        public async Task<Result<IEnumerable<PartyListDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return Result<IEnumerable<PartyListDto>>.Success(
                entities.Select(e => _mapper.Map<PartyListDto>(e)));
        }

        public async Task<Result<PartyDto>> CreateAsync(CreatePartyDto dto)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Error.Validation("Party name is required");

            if (dto.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            // Map DTO to entity
            var entity = _mapper.Map<Party>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var createdEntity = await _repository.AddAsync(entity);

            // Audit trail
            await _auditService.AuditCreateAsync(createdEntity, createdEntity.Id, dto.CompanyId, createdEntity.Name);

            // Create vendor profile if provided and party is a vendor
            if (dto.IsVendor && dto.VendorProfile != null)
            {
                var vendorProfile = _mapper.Map<PartyVendorProfile>(dto.VendorProfile);
                vendorProfile.PartyId = createdEntity.Id;
                vendorProfile.CompanyId = dto.CompanyId;
                await _repository.AddVendorProfileAsync(vendorProfile);
            }
            else if (dto.IsVendor)
            {
                // Create default vendor profile
                await _repository.AddVendorProfileAsync(new PartyVendorProfile
                {
                    PartyId = createdEntity.Id,
                    CompanyId = dto.CompanyId,
                    TdsApplicable = false,
                    MsmeRegistered = false
                });
            }

            // Create customer profile if provided and party is a customer
            if (dto.IsCustomer && dto.CustomerProfile != null)
            {
                var customerProfile = _mapper.Map<PartyCustomerProfile>(dto.CustomerProfile);
                customerProfile.PartyId = createdEntity.Id;
                customerProfile.CompanyId = dto.CompanyId;
                await _repository.AddCustomerProfileAsync(customerProfile);
            }
            else if (dto.IsCustomer)
            {
                // Create default customer profile
                await _repository.AddCustomerProfileAsync(new PartyCustomerProfile
                {
                    PartyId = createdEntity.Id,
                    CompanyId = dto.CompanyId,
                    EInvoiceApplicable = false,
                    EWayBillApplicable = false
                });
            }

            // Add tags if provided
            if (dto.TagIds?.Any() == true)
            {
                foreach (var tagId in dto.TagIds)
                {
                    await _repository.AddTagAsync(createdEntity.Id, tagId, "manual");
                }
            }

            return await GetByIdWithProfilesAsync(createdEntity.Id);
        }

        public async Task<Result> UpdateAsync(Guid id, UpdatePartyDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Party with ID {id} not found");

            // Capture state before update for audit trail
            var oldEntity = _mapper.Map<Party>(existingEntity);

            // Map DTO to existing entity
            _mapper.Map(dto, existingEntity);
            existingEntity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingEntity);

            // Audit trail
            await _auditService.AuditUpdateAsync(oldEntity, existingEntity, existingEntity.Id, existingEntity.CompanyId, existingEntity.Name);

            // Handle role changes - create/delete profiles as needed
            if (dto.IsVendor && await _repository.GetVendorProfileAsync(id) == null)
            {
                await _repository.AddVendorProfileAsync(new PartyVendorProfile
                {
                    PartyId = id,
                    CompanyId = existingEntity.CompanyId,
                    TdsApplicable = false,
                    MsmeRegistered = false
                });
            }
            else if (!dto.IsVendor)
            {
                await _repository.DeleteVendorProfileAsync(id);
            }

            if (dto.IsCustomer && await _repository.GetCustomerProfileAsync(id) == null)
            {
                await _repository.AddCustomerProfileAsync(new PartyCustomerProfile
                {
                    PartyId = id,
                    CompanyId = existingEntity.CompanyId,
                    EInvoiceApplicable = false,
                    EWayBillApplicable = false
                });
            }
            else if (!dto.IsCustomer)
            {
                await _repository.DeleteCustomerProfileAsync(id);
            }

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"Party with ID {id} not found");

            // Audit trail before delete
            await _auditService.AuditDeleteAsync(existingEntity, existingEntity.Id, existingEntity.CompanyId, existingEntity.Name);

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Company-scoped Queries ====================

        public async Task<Result<IEnumerable<PartyListDto>>> GetByCompanyIdAsync(
            Guid companyId,
            bool? isVendor = null,
            bool? isCustomer = null,
            bool? isEmployee = null)
        {
            var entities = await _repository.GetByCompanyIdAsync(companyId, isVendor, isCustomer, isEmployee);
            return Result<IEnumerable<PartyListDto>>.Success(
                entities.Select(e => _mapper.Map<PartyListDto>(e)));
        }

        public async Task<Result<(IEnumerable<PartyListDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            bool? isVendor = null,
            bool? isCustomer = null,
            bool? isEmployee = null,
            bool? isActive = null,
            Dictionary<string, object>? filters = null)
        {
            var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
            if (validation.IsFailure)
                return validation.Error!;

            var result = await _repository.GetPagedAsync(
                companyId, pageNumber, pageSize, searchTerm, sortBy, sortDescending,
                isVendor, isCustomer, isEmployee, isActive, filters);

            return Result<(IEnumerable<PartyListDto> Items, int TotalCount)>.Success((
                result.Items.Select(e => _mapper.Map<PartyListDto>(e)),
                result.TotalCount));
        }

        // ==================== Role-specific Queries ====================

        public async Task<Result<IEnumerable<PartyListDto>>> GetVendorsAsync(Guid companyId)
        {
            var entities = await _repository.GetVendorsAsync(companyId);
            return Result<IEnumerable<PartyListDto>>.Success(
                entities.Select(e => _mapper.Map<PartyListDto>(e)));
        }

        public async Task<Result<IEnumerable<PartyListDto>>> GetCustomersAsync(Guid companyId)
        {
            var entities = await _repository.GetCustomersAsync(companyId);
            return Result<IEnumerable<PartyListDto>>.Success(
                entities.Select(e => _mapper.Map<PartyListDto>(e)));
        }

        public async Task<Result<IEnumerable<PartyListDto>>> GetMsmeVendorsAsync(Guid companyId)
        {
            var entities = await _repository.GetMsmeVendorsAsync(companyId);
            return Result<IEnumerable<PartyListDto>>.Success(
                entities.Select(e => _mapper.Map<PartyListDto>(e)));
        }

        public async Task<Result<IEnumerable<PartyListDto>>> GetTdsApplicableVendorsAsync(Guid companyId)
        {
            var entities = await _repository.GetTdsApplicableVendorsAsync(companyId);
            return Result<IEnumerable<PartyListDto>>.Success(
                entities.Select(e => _mapper.Map<PartyListDto>(e)));
        }

        // ==================== Profile Management ====================

        public async Task<Result<PartyVendorProfileDto>> GetVendorProfileAsync(Guid partyId)
        {
            var profile = await _repository.GetVendorProfileAsync(partyId);
            if (profile == null)
                return Error.NotFound($"Vendor profile for party {partyId} not found");

            return Result<PartyVendorProfileDto>.Success(_mapper.Map<PartyVendorProfileDto>(profile));
        }

        public async Task<Result<PartyCustomerProfileDto>> GetCustomerProfileAsync(Guid partyId)
        {
            var profile = await _repository.GetCustomerProfileAsync(partyId);
            if (profile == null)
                return Error.NotFound($"Customer profile for party {partyId} not found");

            return Result<PartyCustomerProfileDto>.Success(_mapper.Map<PartyCustomerProfileDto>(profile));
        }

        public async Task<Result> UpdateVendorProfileAsync(Guid partyId, UpdatePartyVendorProfileDto dto)
        {
            var existingProfile = await _repository.GetVendorProfileAsync(partyId);
            if (existingProfile == null)
                return Error.NotFound($"Vendor profile for party {partyId} not found");

            _mapper.Map(dto, existingProfile);
            await _repository.UpdateVendorProfileAsync(existingProfile);
            return Result.Success();
        }

        public async Task<Result> UpdateCustomerProfileAsync(Guid partyId, UpdatePartyCustomerProfileDto dto)
        {
            var existingProfile = await _repository.GetCustomerProfileAsync(partyId);
            if (existingProfile == null)
                return Error.NotFound($"Customer profile for party {partyId} not found");

            _mapper.Map(dto, existingProfile);
            await _repository.UpdateCustomerProfileAsync(existingProfile);
            return Result.Success();
        }

        // ==================== Tag Management ====================

        public async Task<Result<IEnumerable<PartyTagDto>>> GetTagsAsync(Guid partyId)
        {
            var tags = await _repository.GetTagsAsync(partyId);
            return Result<IEnumerable<PartyTagDto>>.Success(
                tags.Select(t => _mapper.Map<PartyTagDto>(t)));
        }

        public async Task<Result> AddTagAsync(Guid partyId, AddPartyTagDto dto, Guid? userId = null)
        {
            var party = await _repository.GetByIdAsync(partyId);
            if (party == null)
                return Error.NotFound($"Party with ID {partyId} not found");

            await _repository.AddTagAsync(partyId, dto.TagId, dto.Source, userId);
            return Result.Success();
        }

        public async Task<Result> RemoveTagAsync(Guid partyId, Guid tagId)
        {
            await _repository.RemoveTagAsync(partyId, tagId);
            return Result.Success();
        }

        // ==================== TDS Detection ====================

        public async Task<Result<TdsConfigurationDto?>> DetectTdsConfigurationAsync(Guid partyId)
        {
            var party = await _repository.GetByIdWithProfilesAsync(partyId);
            if (party == null)
                return Error.NotFound($"Party with ID {partyId} not found");

            // First check if vendor profile has explicit TDS settings
            if (party.VendorProfile?.TdsApplicable == true && !string.IsNullOrEmpty(party.VendorProfile.DefaultTdsSection))
            {
                return Result<TdsConfigurationDto?>.Success(new TdsConfigurationDto
                {
                    TdsSection = party.VendorProfile.DefaultTdsSection,
                    TdsRate = party.VendorProfile.DefaultTdsRate ?? 0,
                    MatchedBy = "explicit_profile"
                });
            }

            // If vendor profile explicitly disables TDS
            if (party.VendorProfile?.TdsApplicable == false)
            {
                return Result<TdsConfigurationDto?>.Success(null);
            }

            // Use tag-driven TDS detection
            var tdsRule = await _tdsTagRuleRepository.GetRuleForPartyAsync(partyId);

            if (tdsRule == null || tdsRule.TdsSection == "EXEMPT")
            {
                return Result<TdsConfigurationDto?>.Success(null);
            }

            return Result<TdsConfigurationDto?>.Success(new TdsConfigurationDto
            {
                TdsSection = tdsRule.TdsSection,
                TdsRate = tdsRule.TdsRateWithPan,
                TdsRateNoPan = tdsRule.TdsRateWithoutPan,
                ThresholdAmount = tdsRule.ThresholdAnnual,
                SinglePaymentThreshold = tdsRule.ThresholdSinglePayment,
                MatchedBy = "tag",
                MatchedRuleName = tdsRule.Tag?.Name
            });
        }

        // ==================== Lookup Methods ====================

        public async Task<Result<PartyDto?>> GetByGstinAsync(Guid companyId, string gstin)
        {
            var entity = await _repository.GetByGstinAsync(companyId, gstin);
            return Result<PartyDto?>.Success(entity != null ? _mapper.Map<PartyDto>(entity) : null);
        }

        public async Task<Result<PartyDto?>> GetByPanAsync(Guid companyId, string panNumber)
        {
            var entity = await _repository.GetByPanAsync(companyId, panNumber);
            return Result<PartyDto?>.Success(entity != null ? _mapper.Map<PartyDto>(entity) : null);
        }

        public async Task<Result<PartyDto?>> GetByTallyGuidAsync(Guid companyId, string tallyLedgerGuid)
        {
            var entity = await _repository.GetByTallyGuidAsync(companyId, tallyLedgerGuid);
            return Result<PartyDto?>.Success(entity != null ? _mapper.Map<PartyDto>(entity) : null);
        }

        // ==================== Existence Check ====================

        public async Task<Result<bool>> ExistsAsync(Guid id)
        {
            if (id == default)
                return Result<bool>.Success(false);

            var entity = await _repository.GetByIdAsync(id);
            return Result<bool>.Success(entity != null);
        }
    }
}
