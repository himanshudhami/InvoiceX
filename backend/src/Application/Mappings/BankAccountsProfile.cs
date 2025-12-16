using AutoMapper;
using Application.DTOs.BankAccounts;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for BankAccount mappings
    /// </summary>
    public class BankAccountsProfile : Profile
    {
        public BankAccountsProfile()
        {
            // DTO -> Entity
            CreateMap<CreateBankAccountDto, BankAccount>();

            // DTO -> Entity (for updates)
            CreateMap<UpdateBankAccountDto, BankAccount>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
