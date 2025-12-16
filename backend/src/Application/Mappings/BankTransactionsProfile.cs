using AutoMapper;
using Application.DTOs.BankTransactions;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for BankTransaction mappings
    /// </summary>
    public class BankTransactionsProfile : Profile
    {
        public BankTransactionsProfile()
        {
            // DTO -> Entity
            CreateMap<CreateBankTransactionDto, BankTransaction>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateBankTransactionDto, BankTransaction>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Import DTO -> Entity
            CreateMap<ImportBankTransactionDto, BankTransaction>();
        }
    }
}
