using AutoMapper;
using Application.DTOs.Products;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Products mappings
    /// </summary>
    public class ProductsProfile : Profile
    {
        public ProductsProfile()
        {
            // DTO -> Entity
            CreateMap<CreateProductsDto, Products>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateProductsDto, Products>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<Products, ProductsResponseDto>();
        }
    }
}


