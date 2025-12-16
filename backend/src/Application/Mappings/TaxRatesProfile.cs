using AutoMapper;
using Application.DTOs.TaxRates;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for TaxRates mappings
    /// </summary>
    public class TaxRatesProfile : Profile
    {
        public TaxRatesProfile()
        {
            // DTO -> Entity
            CreateMap<CreateTaxRatesDto, TaxRates>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateTaxRatesDto, TaxRates>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<TaxRates, TaxRatesResponseDto>();
        }
    }
}


