using AutoMapper;
using Application.DTOs.Invoices;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Invoices mappings
    /// </summary>
    public class InvoicesProfile : Profile
    {
        public InvoicesProfile()
        {
            // DTO -> Entity
            CreateMap<CreateInvoicesDto, Invoices>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateInvoicesDto, Invoices>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<Invoices, InvoicesResponseDto>();
        }
    }
}


