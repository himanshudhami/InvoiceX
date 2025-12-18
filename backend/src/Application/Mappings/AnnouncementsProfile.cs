using AutoMapper;
using Application.DTOs.Announcements;
using Core.Entities;

namespace Application.Mappings;

/// <summary>
/// AutoMapper profile for Announcements mappings
/// </summary>
public class AnnouncementsProfile : Profile
{
    public AnnouncementsProfile()
    {
        // DTO -> Entity
        CreateMap<CreateAnnouncementDto, Announcement>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateAnnouncementDto, Announcement>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Entity -> DTO
        CreateMap<Announcement, AnnouncementSummaryDto>();
        CreateMap<Announcement, AnnouncementDetailDto>();
    }
}
