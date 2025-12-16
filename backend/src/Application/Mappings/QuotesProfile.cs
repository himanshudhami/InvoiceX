using AutoMapper;
using Application.DTOs.Quotes;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Quotes mappings
    /// </summary>
    public class QuotesProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the QuotesProfile
        /// </summary>
        public QuotesProfile()
        {
            CreateMap<CreateQuotesDto, Quotes>();
            CreateMap<UpdateQuotesDto, Quotes>();
            // CreateMap<Quotes, QuotesResponseDto>();
        }
    }
}
