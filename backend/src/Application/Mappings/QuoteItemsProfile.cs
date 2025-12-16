using AutoMapper;
using Application.DTOs.QuoteItems;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for QuoteItems mappings
    /// </summary>
    public class QuoteItemsProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the QuoteItemsProfile
        /// </summary>
        public QuoteItemsProfile()
        {
            CreateMap<CreateQuoteItemsDto, QuoteItems>();
            CreateMap<UpdateQuoteItemsDto, QuoteItems>();
            // CreateMap<QuoteItems, QuoteItemsResponseDto>();
        }
    }
}
