using AutoMapper;
using Application.DTOs.EmployeeSalaryTransactions;
using Core.Entities;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Employee Salary Transactions mappings
    /// </summary>
    public class EmployeeSalaryTransactionsProfile : Profile
    {
        public EmployeeSalaryTransactionsProfile()
        {
            // DTO -> Entity
            CreateMap<CreateEmployeeSalaryTransactionsDto, EmployeeSalaryTransactions>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => 
                    string.IsNullOrWhiteSpace(src.TransactionType) ? "salary" : src.TransactionType));

            // DTO -> Entity (for updates)
            CreateMap<UpdateEmployeeSalaryTransactionsDto, EmployeeSalaryTransactions>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.SalaryMonth, opt => opt.Ignore())
                .ForMember(dest => dest.SalaryYear, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());

            // Optionally: Entity -> DTOs if you add response DTOs later
            // CreateMap<EmployeeSalaryTransactions, EmployeeSalaryTransactionsResponseDto>();
        }
    }
}