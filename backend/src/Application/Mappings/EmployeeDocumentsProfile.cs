using AutoMapper;
using Application.DTOs.EmployeeDocuments;
using Core.Entities;

namespace Application.Mappings;

/// <summary>
/// AutoMapper profile for Employee Documents mappings
/// </summary>
public class EmployeeDocumentsProfile : Profile
{
    public EmployeeDocumentsProfile()
    {
        // Employee Document mappings
        CreateMap<CreateEmployeeDocumentDto, EmployeeDocument>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateEmployeeDocumentDto, EmployeeDocument>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
            .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<EmployeeDocument, EmployeeDocumentSummaryDto>();
        CreateMap<EmployeeDocument, EmployeeDocumentDetailDto>();

        // Document Request mappings
        CreateMap<CreateDocumentRequestDto, DocumentRequest>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ProcessedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ProcessedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<DocumentRequest, DocumentRequestSummaryDto>();
    }
}
