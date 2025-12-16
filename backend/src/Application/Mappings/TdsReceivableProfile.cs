using AutoMapper;
using Application.DTOs.TdsReceivable;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for TdsReceivable mappings
    /// </summary>
    public class TdsReceivableProfile : Profile
    {
        public TdsReceivableProfile()
        {
            // DTO -> Entity
            CreateMap<CreateTdsReceivableDto, TdsReceivable>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateTdsReceivableDto, TdsReceivable>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore());
        }
    }
}
