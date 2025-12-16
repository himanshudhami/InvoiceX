using AutoMapper;
using Application.DTOs.InvoiceTemplates;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for InvoiceTemplates mappings
    /// </summary>
    public class InvoiceTemplatesProfile : Profile
    {
        public InvoiceTemplatesProfile()
        {
            // DTO -> Entity
            CreateMap<CreateInvoiceTemplatesDto, InvoiceTemplates>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateInvoiceTemplatesDto, InvoiceTemplates>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<InvoiceTemplates, InvoiceTemplatesResponseDto>();
        }
    }
}


