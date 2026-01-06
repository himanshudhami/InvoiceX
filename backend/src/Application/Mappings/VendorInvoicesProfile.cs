using AutoMapper;
using Application.DTOs.VendorInvoices;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Vendor Invoice mappings
    /// </summary>
    public class VendorInvoicesProfile : Profile
    {
        public VendorInvoicesProfile()
        {
            // DTO -> Entity
            CreateMap<CreateVendorInvoiceDto, VendorInvoice>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateVendorInvoiceDto, VendorInvoice>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Item DTO -> Entity
            CreateMap<CreateVendorInvoiceItemDto, VendorInvoiceItem>();
            CreateMap<UpdateVendorInvoiceItemDto, VendorInvoiceItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
