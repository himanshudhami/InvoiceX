using AutoMapper;
using Application.DTOs.VendorPayments;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Vendor Payments mappings
    /// </summary>
    public class VendorPaymentsProfile : Profile
    {
        public VendorPaymentsProfile()
        {
            // DTO -> Entity
            CreateMap<CreateVendorPaymentDto, VendorPayment>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateVendorPaymentDto, VendorPayment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Allocation DTO -> Entity
            CreateMap<CreateVendorPaymentAllocationDto, VendorPaymentAllocation>();
        }
    }
}
