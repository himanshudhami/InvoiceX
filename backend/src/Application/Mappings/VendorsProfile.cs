using AutoMapper;
using Application.DTOs.Vendors;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Vendors mappings
    /// </summary>
    public class VendorsProfile : Profile
    {
        public VendorsProfile()
        {
            // DTO -> Entity
            CreateMap<CreateVendorsDto, Vendors>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateVendorsDto, Vendors>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
