using AutoMapper;
using Application.DTOs.Party;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Party and related entity mappings
    /// </summary>
    public class PartyProfile : Profile
    {
        public PartyProfile()
        {
            // ==================== Party Mappings ====================

            // Entity -> DTO
            CreateMap<Party, PartyDto>();
            CreateMap<Party, PartyListDto>();

            // CreateDTO -> Entity
            CreateMap<CreatePartyDto, Party>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.VendorProfile, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerProfile, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.Ignore());

            // UpdateDTO -> Entity
            CreateMap<UpdatePartyDto, Party>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.TallyLedgerGuid, opt => opt.Ignore())
                .ForMember(dest => dest.TallyLedgerName, opt => opt.Ignore())
                .ForMember(dest => dest.TallyGroupName, opt => opt.Ignore())
                .ForMember(dest => dest.TallyMigrationBatchId, opt => opt.Ignore())
                .ForMember(dest => dest.VendorProfile, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerProfile, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.Ignore());

            // ==================== Vendor Profile Mappings ====================

            // Entity -> DTO
            CreateMap<PartyVendorProfile, PartyVendorProfileDto>();

            // CreateDTO -> Entity
            CreateMap<CreatePartyVendorProfileDto, PartyVendorProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PartyId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Party, opt => opt.Ignore());

            // UpdateDTO -> Entity
            CreateMap<UpdatePartyVendorProfileDto, PartyVendorProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PartyId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Party, opt => opt.Ignore());

            // ==================== Customer Profile Mappings ====================

            // Entity -> DTO
            CreateMap<PartyCustomerProfile, PartyCustomerProfileDto>();

            // CreateDTO -> Entity
            CreateMap<CreatePartyCustomerProfileDto, PartyCustomerProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PartyId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Party, opt => opt.Ignore());

            // UpdateDTO -> Entity
            CreateMap<UpdatePartyCustomerProfileDto, PartyCustomerProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PartyId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Party, opt => opt.Ignore());

            // ==================== Party Tag Mappings ====================

            // Entity -> DTO
            CreateMap<PartyTag, PartyTagDto>();

            // Note: TdsTagRule mappings are handled manually in TdsDetectionService
            // using MapToDto() methods for explicit control over the mapping
        }
    }
}
