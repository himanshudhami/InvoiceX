using Application.Common;
using Application.Interfaces;
using Application.DTOs.Party;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;

namespace Application.Services
{
    /// <summary>
    /// Service for TDS detection using tag-driven approach
    /// Tags drive TDS behavior instead of hard-coded vendor types
    /// </summary>
    public class TdsDetectionService : ITdsDetectionService
    {
        private readonly ITdsTagRuleRepository _tdsTagRuleRepository;
        private readonly IPartyRepository _partyRepository;
        private readonly IMapper _mapper;

        public TdsDetectionService(
            ITdsTagRuleRepository tdsTagRuleRepository,
            IPartyRepository partyRepository,
            IMapper mapper)
        {
            _tdsTagRuleRepository = tdsTagRuleRepository ?? throw new ArgumentNullException(nameof(tdsTagRuleRepository));
            _partyRepository = partyRepository ?? throw new ArgumentNullException(nameof(partyRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<TdsDetectionResultDto>> DetectTdsForPartyAsync(
            Guid partyId,
            decimal? paymentAmount = null)
        {
            var party = await _partyRepository.GetByIdWithProfilesAsync(partyId);
            if (party == null)
                return Error.NotFound($"Party with ID {partyId} not found");

            return await DetectTdsInternalAsync(party, paymentAmount);
        }

        /// <inheritdoc />
        public async Task<Result<TdsDetectionResultDto>> DetectTdsForPartyByIdAsync(
            Guid companyId,
            Guid partyId,
            decimal? paymentAmount = null)
        {
            var party = await _partyRepository.GetByIdWithProfilesAsync(partyId);
            if (party == null)
                return Error.NotFound($"Party with ID {partyId} not found");

            if (party.CompanyId != companyId)
                return Error.Forbidden("Party does not belong to this company");

            return await DetectTdsInternalAsync(party, paymentAmount);
        }

        private async Task<Result<TdsDetectionResultDto>> DetectTdsInternalAsync(Party party, decimal? paymentAmount)
        {
            var result = new TdsDetectionResultDto
            {
                PartyId = party.Id,
                PartyName = party.Name,
                Pan = party.PanNumber
            };

            // Step 1: Check if vendor profile explicitly disables TDS
            if (party.VendorProfile?.TdsApplicable == false)
            {
                result.IsApplicable = false;
                result.DetectionMethod = "manual_exempt";
                result.Notes = "Manually marked as TDS not applicable";
                return Result<TdsDetectionResultDto>.Success(result);
            }

            // Step 2: Check for explicit TDS settings in vendor profile
            if (!string.IsNullOrEmpty(party.VendorProfile?.DefaultTdsSection))
            {
                result.IsApplicable = true;
                result.TdsSection = party.VendorProfile.DefaultTdsSection;
                result.DetectionMethod = "manual";

                // Check for lower TDS certificate
                var tdsRate = party.VendorProfile.DefaultTdsRate ?? 0;
                if (HasValidLowerCertificate(party.VendorProfile))
                {
                    tdsRate = party.VendorProfile.LowerTdsRate ?? tdsRate;
                    result.Notes = $"Lower TDS certificate applied: {party.VendorProfile.LowerTdsCertificate}";
                }

                result.TdsRate = tdsRate;
                return Result<TdsDetectionResultDto>.Success(result);
            }

            // Step 3: Find TDS section tag for this party
            var tdsRule = await _tdsTagRuleRepository.GetRuleForPartyAsync(party.Id);

            if (tdsRule == null)
            {
                result.IsApplicable = false;
                result.DetectionMethod = "no_tag";
                result.Notes = "No TDS section tag assigned to party";
                return Result<TdsDetectionResultDto>.Success(result);
            }

            // Check for TDS Exempt tag
            if (tdsRule.TdsSection == "EXEMPT")
            {
                result.IsApplicable = false;
                result.TdsSection = "EXEMPT";
                result.DetectionMethod = "tag_exempt";
                result.MatchedTagId = tdsRule.TagId;
                result.MatchedTagName = tdsRule.Tag?.Name;
                result.Notes = tdsRule.ExemptionNotes ?? "Party is TDS exempt";
                return Result<TdsDetectionResultDto>.Success(result);
            }

            // Step 4: Calculate rate based on PAN
            var tdsRate2 = CalculateTdsRate(party.PanNumber, tdsRule);

            // Check for lower TDS certificate
            if (HasValidLowerCertificate(party.VendorProfile))
            {
                tdsRate2 = Math.Min(tdsRate2, party.VendorProfile!.LowerTdsRate ?? tdsRate2);
                result.Notes = $"Lower TDS certificate applied: {party.VendorProfile.LowerTdsCertificate}";
            }

            // Step 5: Check thresholds
            var isBelowThreshold = false;
            if (paymentAmount.HasValue && tdsRule.ThresholdSinglePayment.HasValue)
            {
                isBelowThreshold = paymentAmount.Value < tdsRule.ThresholdSinglePayment.Value;
            }

            result.IsApplicable = true;
            result.TdsSection = tdsRule.TdsSection;
            result.TdsSectionClause = tdsRule.TdsSectionClause;
            result.TdsRate = tdsRate2;
            result.ThresholdAnnual = tdsRule.ThresholdAnnual;
            result.ThresholdSinglePayment = tdsRule.ThresholdSinglePayment;
            result.IsBelowThreshold = isBelowThreshold;
            result.DetectionMethod = "tag";
            result.MatchedTagId = tdsRule.TagId;
            result.MatchedTagName = tdsRule.Tag?.Name;
            result.ExemptionNotes = tdsRule.ExemptionNotes;

            if (isBelowThreshold)
            {
                result.Notes = $"Below single payment threshold of ₹{tdsRule.ThresholdSinglePayment:N0}";
            }

            return Result<TdsDetectionResultDto>.Success(result);
        }

        private decimal CalculateTdsRate(string? pan, TdsTagRule rule)
        {
            // No PAN or invalid PAN = higher rate (Section 206AA)
            if (string.IsNullOrEmpty(pan) || pan.Length < 10)
                return rule.TdsRateWithoutPan;

            // Determine entity type from PAN 4th character
            var entityType = pan[3];
            return entityType switch
            {
                'P' => rule.TdsRateIndividual ?? rule.TdsRateWithPan,  // Individual
                'H' => rule.TdsRateIndividual ?? rule.TdsRateWithPan,  // HUF
                'C' => rule.TdsRateCompany ?? rule.TdsRateWithPan,     // Company
                'F' => rule.TdsRateCompany ?? rule.TdsRateWithPan,     // Firm
                'L' => rule.TdsRateCompany ?? rule.TdsRateWithPan,     // LLP
                'T' => rule.TdsRateCompany ?? rule.TdsRateWithPan,     // Trust
                'A' => rule.TdsRateCompany ?? rule.TdsRateWithPan,     // AOP
                'B' => rule.TdsRateCompany ?? rule.TdsRateWithPan,     // BOI
                'G' => 0,                                               // Government (usually exempt)
                _ => rule.TdsRateWithPan
            };
        }

        private static bool HasValidLowerCertificate(PartyVendorProfile? profile)
        {
            if (profile == null) return false;
            if (string.IsNullOrEmpty(profile.LowerTdsCertificate)) return false;
            if (!profile.LowerTdsValidFrom.HasValue || !profile.LowerTdsValidTill.HasValue) return false;

            var today = DateOnly.FromDateTime(DateTime.Today);
            return profile.LowerTdsValidFrom.Value <= today &&
                   profile.LowerTdsValidTill.Value >= today;
        }

        // ==================== TDS Tag Rule Management ====================

        /// <inheritdoc />
        public async Task<Result<IEnumerable<TdsTagRuleDto>>> GetTdsTagRulesAsync(Guid companyId)
        {
            var rules = await _tdsTagRuleRepository.GetActiveByCompanyIdAsync(companyId);
            return Result<IEnumerable<TdsTagRuleDto>>.Success(
                rules.Select(r => MapToDto(r)));
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<TdsTagRuleDto> Items, int TotalCount)>> GetTdsTagRulesPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? tdsSection = null,
            bool? isActive = null,
            string? sortBy = null,
            bool sortDescending = false)
        {
            var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
            if (validation.IsFailure)
                return validation.Error!;

            var result = await _tdsTagRuleRepository.GetPagedAsync(
                companyId, pageNumber, pageSize, tdsSection, isActive, sortBy, sortDescending);

            return Result<(IEnumerable<TdsTagRuleDto> Items, int TotalCount)>.Success((
                result.Items.Select(r => MapToDto(r)),
                result.TotalCount));
        }

        /// <inheritdoc />
        public async Task<Result<TdsTagRuleDto>> GetTdsTagRuleByIdAsync(Guid ruleId)
        {
            var rule = await _tdsTagRuleRepository.GetByIdAsync(ruleId);
            if (rule == null)
                return Error.NotFound($"TDS tag rule with ID {ruleId} not found");

            return Result<TdsTagRuleDto>.Success(MapToDto(rule));
        }

        /// <inheritdoc />
        public async Task<Result<TdsTagRuleDto?>> GetTdsTagRuleByTagIdAsync(Guid tagId)
        {
            var rule = await _tdsTagRuleRepository.GetByTagIdAsync(tagId);
            return Result<TdsTagRuleDto?>.Success(rule != null ? MapToDto(rule) : null);
        }

        /// <inheritdoc />
        public async Task<Result<TdsTagRuleDto>> CreateTdsTagRuleAsync(
            Guid companyId,
            CreateTdsTagRuleDto dto,
            Guid? userId = null)
        {
            // Validation
            if (dto.TagId == Guid.Empty)
                return Error.Validation("Tag ID is required");

            if (string.IsNullOrWhiteSpace(dto.TdsSection))
                return Error.Validation("TDS section is required");

            if (dto.TdsRateWithPan < 0 || dto.TdsRateWithPan > 100)
                return Error.Validation("TDS rate must be between 0 and 100");

            // Check if rule already exists for this tag
            var existingRule = await _tdsTagRuleRepository.GetByTagIdAsync(dto.TagId);
            if (existingRule != null)
                return Error.Conflict($"A TDS rule already exists for this tag");

            var entity = new TdsTagRule
            {
                CompanyId = companyId,
                TagId = dto.TagId,
                TdsSection = dto.TdsSection,
                TdsSectionClause = dto.TdsSectionClause,
                TdsRateWithPan = dto.TdsRateWithPan,
                TdsRateWithoutPan = dto.TdsRateWithoutPan ?? 20.00m,
                TdsRateIndividual = dto.TdsRateIndividual,
                TdsRateCompany = dto.TdsRateCompany,
                ThresholdSinglePayment = dto.ThresholdSinglePayment,
                ThresholdAnnual = dto.ThresholdAnnual,
                ExemptionNotes = dto.ExemptionNotes,
                EffectiveFrom = dto.EffectiveFrom ?? new DateOnly(2024, 4, 1),
                EffectiveTo = dto.EffectiveTo,
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdEntity = await _tdsTagRuleRepository.AddAsync(entity);
            return Result<TdsTagRuleDto>.Success(MapToDto(createdEntity));
        }

        /// <inheritdoc />
        public async Task<Result> UpdateTdsTagRuleAsync(Guid ruleId, UpdateTdsTagRuleDto dto)
        {
            var validation = ServiceExtensions.ValidateGuid(ruleId);
            if (validation.IsFailure)
                return validation.Error!;

            var existingRule = await _tdsTagRuleRepository.GetByIdAsync(ruleId);
            if (existingRule == null)
                return Error.NotFound($"TDS tag rule with ID {ruleId} not found");

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.TdsSection))
                existingRule.TdsSection = dto.TdsSection;
            if (dto.TdsSectionClause != null)
                existingRule.TdsSectionClause = dto.TdsSectionClause;
            if (dto.TdsRateWithPan.HasValue)
                existingRule.TdsRateWithPan = dto.TdsRateWithPan.Value;
            if (dto.TdsRateWithoutPan.HasValue)
                existingRule.TdsRateWithoutPan = dto.TdsRateWithoutPan.Value;
            if (dto.TdsRateIndividual.HasValue)
                existingRule.TdsRateIndividual = dto.TdsRateIndividual;
            if (dto.TdsRateCompany.HasValue)
                existingRule.TdsRateCompany = dto.TdsRateCompany;
            if (dto.ThresholdSinglePayment.HasValue)
                existingRule.ThresholdSinglePayment = dto.ThresholdSinglePayment;
            if (dto.ThresholdAnnual.HasValue)
                existingRule.ThresholdAnnual = dto.ThresholdAnnual.Value;
            if (dto.ExemptionNotes != null)
                existingRule.ExemptionNotes = dto.ExemptionNotes;
            if (dto.EffectiveFrom.HasValue)
                existingRule.EffectiveFrom = dto.EffectiveFrom.Value;
            if (dto.EffectiveTo.HasValue)
                existingRule.EffectiveTo = dto.EffectiveTo;
            if (dto.IsActive.HasValue)
                existingRule.IsActive = dto.IsActive.Value;

            existingRule.UpdatedAt = DateTime.UtcNow;

            await _tdsTagRuleRepository.UpdateAsync(existingRule);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> DeleteTdsTagRuleAsync(Guid ruleId)
        {
            var validation = ServiceExtensions.ValidateGuid(ruleId);
            if (validation.IsFailure)
                return validation.Error!;

            var existingRule = await _tdsTagRuleRepository.GetByIdAsync(ruleId);
            if (existingRule == null)
                return Error.NotFound($"TDS tag rule with ID {ruleId} not found");

            await _tdsTagRuleRepository.DeleteAsync(ruleId);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> SeedTdsSystemAsync(Guid companyId)
        {
            var hasTags = await _tdsTagRuleRepository.HasTdsTagsAsync(companyId);
            if (hasTags)
                return Error.Conflict("TDS system already seeded for this company");

            await _tdsTagRuleRepository.SeedDefaultsAsync(companyId);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<bool> HasTdsSystemSeededAsync(Guid companyId)
        {
            return await _tdsTagRuleRepository.HasTdsTagsAsync(companyId);
        }

        // ==================== Mapping Helpers ====================

        private static TdsTagRuleDto MapToDto(TdsTagRule rule)
        {
            return new TdsTagRuleDto
            {
                Id = rule.Id,
                CompanyId = rule.CompanyId,
                TagId = rule.TagId,
                TagName = rule.Tag?.Name,
                TagCode = rule.Tag?.Code,
                TagColor = rule.Tag?.Color,
                TdsSection = rule.TdsSection,
                TdsSectionClause = rule.TdsSectionClause,
                TdsRateWithPan = rule.TdsRateWithPan,
                TdsRateWithoutPan = rule.TdsRateWithoutPan,
                TdsRateIndividual = rule.TdsRateIndividual,
                TdsRateCompany = rule.TdsRateCompany,
                ThresholdSinglePayment = rule.ThresholdSinglePayment,
                ThresholdAnnual = rule.ThresholdAnnual,
                AppliesToIndividual = rule.AppliesToIndividual,
                AppliesToHuf = rule.AppliesToHuf,
                AppliesToCompany = rule.AppliesToCompany,
                AppliesToFirm = rule.AppliesToFirm,
                AppliesToLlp = rule.AppliesToLlp,
                AppliesToTrust = rule.AppliesToTrust,
                AppliesToAopBoi = rule.AppliesToAopBoi,
                AppliesToGovernment = rule.AppliesToGovernment,
                LowerCertificateAllowed = rule.LowerCertificateAllowed,
                NilCertificateAllowed = rule.NilCertificateAllowed,
                ExemptionNotes = rule.ExemptionNotes,
                EffectiveFrom = rule.EffectiveFrom,
                EffectiveTo = rule.EffectiveTo,
                IsActive = rule.IsActive,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };
        }
    }
}
