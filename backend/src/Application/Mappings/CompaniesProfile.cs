using AutoMapper;
using Application.DTOs.Companies;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Companies mappings
    /// </summary>
    public class CompaniesProfile : Profile
    {
        public CompaniesProfile()
        {
            // DTO -> Entity
            CreateMap<CreateCompaniesDto, Companies>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateCompaniesDto, Companies>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<Companies, CompaniesResponseDto>();
        }
    }
}


