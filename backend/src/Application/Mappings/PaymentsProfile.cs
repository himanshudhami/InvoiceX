using AutoMapper;
using Application.DTOs.Payments;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Payments mappings
    /// </summary>
    public class PaymentsProfile : Profile
    {
        public PaymentsProfile()
        {
            // DTO -> Entity
            CreateMap<CreatePaymentsDto, Payments>();

            // DTO -> Entity (for updates)
            CreateMap<UpdatePaymentsDto, Payments>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<Payments, PaymentsResponseDto>();
        }
    }
}


