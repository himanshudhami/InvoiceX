using AutoMapper;
using Application.DTOs.CreditNotes;
using Core.Entities;

namespace Application.Mappings
{
    public class CreditNotesProfile : Profile
    {
        public CreditNotesProfile()
        {
            // DTO -> Entity (for creation)
            CreateMap<CreateCreditNotesDto, CreditNotes>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalInvoiceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalInvoiceDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TimeBarredDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsTimeBarred, opt => opt.Ignore());

            // DTO -> Entity (for updates)
            CreateMap<UpdateCreditNotesDto, CreditNotes>()
                .ForMember(dest => dest.OriginalInvoiceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalInvoiceDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TimeBarredDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsTimeBarred, opt => opt.Ignore());

            // Credit Note Item DTO -> Entity
            CreateMap<CreditNoteItemDto, CreditNoteItems>()
                .ForMember(dest => dest.CreditNoteId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }
    }
}
