using AutoMapper;
using Application.DTOs.Customers;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Customers mappings
    /// </summary>
    public class CustomersProfile : Profile
    {
        public CustomersProfile()
        {
            // DTO -> Entity
            CreateMap<CreateCustomersDto, Customers>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateCustomersDto, Customers>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<Customers, CustomersResponseDto>();
        }
    }
}


