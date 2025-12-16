using AutoMapper;
using Application.DTOs.InvoiceItems;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for InvoiceItems mappings
    /// </summary>
    public class InvoiceItemsProfile : Profile
    {
        public InvoiceItemsProfile()
        {
            // DTO -> Entity
            CreateMap<CreateInvoiceItemsDto, InvoiceItems>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateInvoiceItemsDto, InvoiceItems>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<InvoiceItems, InvoiceItemsResponseDto>();
        }
    }
}


