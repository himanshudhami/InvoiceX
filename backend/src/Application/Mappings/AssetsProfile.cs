using Application.DTOs.Assets;
using AutoMapper;
using Core.Entities;

namespace Application.Mappings;

public class AssetsProfile : Profile
{
    public AssetsProfile()
    {
        CreateMap<CreateAssetDto, Assets>();
        CreateMap<UpdateAssetDto, Assets>();
    }
}




