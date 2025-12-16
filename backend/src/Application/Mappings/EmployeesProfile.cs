using AutoMapper;
using Application.DTOs.Employees;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Employees mappings
    /// </summary>
    public class EmployeesProfile : Profile
    {
        public EmployeesProfile()
        {
            // DTO -> Entity
            CreateMap<CreateEmployeesDto, Employees>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // DTO -> Entity (for updates)
            // Only map non-null values to prevent overwriting existing data with null
            CreateMap<UpdateEmployeesDto, Employees>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ContractType, opt => opt.Condition(src => src.ContractType != null));

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<Employees, EmployeesResponseDto>();
        }
    }
}