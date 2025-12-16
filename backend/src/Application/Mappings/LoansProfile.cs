using Application.DTOs.Loans;
using AutoMapper;
using Core.Entities;

namespace Application.Mappings;

public class LoansProfile : Profile
{
    public LoansProfile()
    {
        CreateMap<CreateLoanDto, Loan>();
        CreateMap<UpdateLoanDto, Loan>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}





