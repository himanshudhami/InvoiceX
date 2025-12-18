using AutoMapper;
using Application.DTOs.SupportTickets;
using Core.Entities;

namespace Application.Mappings;

/// <summary>
/// AutoMapper profile for Support Tickets mappings
/// </summary>
public class SupportTicketsProfile : Profile
{
    public SupportTicketsProfile()
    {
        // Support Ticket mappings
        CreateMap<CreateSupportTicketDto, SupportTicket>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TicketNumber, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedTo, opt => opt.Ignore())
            .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ResolutionNotes, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateSupportTicketDto, SupportTicket>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
            .ForMember(dest => dest.TicketNumber, opt => opt.Ignore())
            .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<SupportTicket, SupportTicketSummaryDto>();
        CreateMap<SupportTicket, SupportTicketDetailDto>();

        // Ticket Message mappings
        CreateMap<SupportTicketMessage, TicketMessageDto>();

        // FAQ mappings
        CreateMap<CreateFaqDto, FaqItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateFaqDto, FaqItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<FaqItem, FaqItemDto>();
    }
}
