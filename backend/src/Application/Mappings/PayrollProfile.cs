using AutoMapper;
using Application.DTOs.Payroll;
using Core.Entities.Payroll;

namespace Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Payroll domain mappings
    /// </summary>
    public class PayrollProfile : Profile
    {
        public PayrollProfile()
        {
            // ==================== EmployeePayrollInfo ====================

            CreateMap<CreateEmployeePayrollInfoDto, EmployeePayrollInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore());

            CreateMap<UpdateEmployeePayrollInfoDto, EmployeePayrollInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<EmployeePayrollInfo, EmployeePayrollInfoDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeName : null))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

            // ==================== EmployeeSalaryStructure ====================

            CreateMap<CreateEmployeeSalaryStructureDto, EmployeeSalaryStructure>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EffectiveTo, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.MonthlyGross, opt => opt.MapFrom(src => CalculateMonthlyGross(src)))
                .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore());

            CreateMap<UpdateEmployeeSalaryStructureDto, EmployeeSalaryStructure>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<EmployeeSalaryStructure, EmployeeSalaryStructureDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeName : null))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

            // ==================== EmployeeTaxDeclaration ====================

            CreateMap<CreateEmployeeTaxDeclarationDto, EmployeeTaxDeclaration>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "draft"))
                .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
                .ForMember(dest => dest.VerifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.VerifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LockedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ProofDocuments, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());

            CreateMap<UpdateEmployeeTaxDeclarationDto, EmployeeTaxDeclaration>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.FinancialYear, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
                .ForMember(dest => dest.VerifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.VerifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LockedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<EmployeeTaxDeclaration, EmployeeTaxDeclarationDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeName : null))
                .ForMember(dest => dest.Total80cDeduction, opt => opt.MapFrom(src => Calculate80cTotal(src)))
                .ForMember(dest => dest.Total80dDeduction, opt => opt.MapFrom(src => Calculate80dTotal(src)))
                .ForMember(dest => dest.TotalDeductions, opt => opt.MapFrom(src => CalculateTotalDeductions(src)));

            // ==================== PayrollRun ====================

            CreateMap<CreatePayrollRunDto, PayrollRun>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FinancialYear, opt => opt.MapFrom(src => GetFinancialYear(src.PayrollMonth, src.PayrollYear)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "draft"))
                .ForMember(dest => dest.TotalEmployees, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.TotalContractors, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.TotalGrossSalary, opt => opt.MapFrom(_ => 0m))
                .ForMember(dest => dest.TotalDeductions, opt => opt.MapFrom(_ => 0m))
                .ForMember(dest => dest.TotalNetSalary, opt => opt.MapFrom(_ => 0m))
                .ForMember(dest => dest.TotalEmployerPf, opt => opt.MapFrom(_ => 0m))
                .ForMember(dest => dest.TotalEmployerEsi, opt => opt.MapFrom(_ => 0m))
                .ForMember(dest => dest.TotalEmployerCost, opt => opt.MapFrom(_ => 0m))
                .ForMember(dest => dest.ComputedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ComputedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PaidBy, opt => opt.Ignore())
                .ForMember(dest => dest.PaidAt, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentReference, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMode, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<UpdatePayrollRunDto, PayrollRun>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.PayrollMonth, opt => opt.Ignore())
                .ForMember(dest => dest.PayrollYear, opt => opt.Ignore())
                .ForMember(dest => dest.FinancialYear, opt => opt.Ignore())
                .ForMember(dest => dest.TotalEmployees, opt => opt.Ignore())
                .ForMember(dest => dest.TotalContractors, opt => opt.Ignore())
                .ForMember(dest => dest.TotalGrossSalary, opt => opt.Ignore())
                .ForMember(dest => dest.TotalDeductions, opt => opt.Ignore())
                .ForMember(dest => dest.TotalNetSalary, opt => opt.Ignore())
                .ForMember(dest => dest.TotalEmployerPf, opt => opt.Ignore())
                .ForMember(dest => dest.TotalEmployerEsi, opt => opt.Ignore())
                .ForMember(dest => dest.TotalEmployerCost, opt => opt.Ignore())
                .ForMember(dest => dest.ComputedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ComputedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PaidBy, opt => opt.Ignore())
                .ForMember(dest => dest.PaidAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<PayrollRun, PayrollRunDto>()
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

            // ==================== PayrollTransaction ====================

            CreateMap<PayrollTransaction, PayrollTransactionDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeName : null));

            // ==================== ContractorPayment ====================

            CreateMap<CreateContractorPaymentDto, ContractorPayment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TdsAmount, opt => opt.Ignore()) // Calculated
                .ForMember(dest => dest.NetPayable, opt => opt.Ignore()) // Calculated
                .ForMember(dest => dest.GstAmount, opt => opt.Ignore()) // Calculated
                .ForMember(dest => dest.TotalInvoiceAmount, opt => opt.Ignore()) // Calculated
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "pending"))
                .ForMember(dest => dest.PaymentDate, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentReference, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore());

            CreateMap<UpdateContractorPaymentDto, ContractorPayment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMonth, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentYear, opt => opt.Ignore())
                .ForMember(dest => dest.TdsAmount, opt => opt.Ignore()) // Recalculated
                .ForMember(dest => dest.NetPayable, opt => opt.Ignore()) // Recalculated
                .ForMember(dest => dest.GstAmount, opt => opt.Ignore()) // Recalculated
                .ForMember(dest => dest.TotalInvoiceAmount, opt => opt.Ignore()) // Recalculated
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ContractorPayment, ContractorPaymentDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeName : null))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

            // ==================== CompanyStatutoryConfig ====================

            CreateMap<CreateCompanyStatutoryConfigDto, CompanyStatutoryConfig>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore());

            CreateMap<UpdateCompanyStatutoryConfigDto, CompanyStatutoryConfig>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CompanyStatutoryConfig, CompanyStatutoryConfigDto>()
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

            // ==================== TaxParameter ====================

            CreateMap<CreateTaxParameterDto, TaxParameter>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EffectiveTo, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            CreateMap<UpdateTaxParameterDto, TaxParameter>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FinancialYear, opt => opt.Ignore())
                .ForMember(dest => dest.Regime, opt => opt.Ignore())
                .ForMember(dest => dest.ParameterCode, opt => opt.Ignore())
                .ForMember(dest => dest.ParameterType, opt => opt.Ignore())
                .ForMember(dest => dest.EffectiveFrom, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<TaxParameter, TaxParameterDto>();

            // ==================== SalaryComponent ====================

            CreateMap<CreateSalaryComponentDto, SalaryComponent>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            CreateMap<UpdateSalaryComponentDto, SalaryComponent>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.ComponentCode, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<SalaryComponent, SalaryComponentDto>();
            CreateMap<SalaryComponent, SalaryComponentWageFlagsDto>();

            // ==================== PayrollCalculationLine ====================

            CreateMap<CreatePayrollCalculationLineDto, PayrollCalculationLine>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Transaction, opt => opt.Ignore());

            CreateMap<PayrollCalculationLine, PayrollCalculationLineDto>();

            CreateMap<PayrollCalculationLine, CalculationLineItemDto>()
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.ComputedAmount));
        }

        // Helper methods
        private static decimal CalculateMonthlyGross(CreateEmployeeSalaryStructureDto src)
        {
            // LTA is paid monthly in Indian payroll (tax exemption is claimed annually)
            return src.BasicSalary + src.Hra + src.DearnessAllowance +
                   src.ConveyanceAllowance + src.MedicalAllowance +
                   src.SpecialAllowance + src.OtherAllowances +
                   (src.LtaAnnual / 12);
        }

        private static decimal Calculate80cTotal(EmployeeTaxDeclaration src)
        {
            return src.Sec80cPpf + src.Sec80cElss + src.Sec80cLifeInsurance +
                   src.Sec80cHomeLoanPrincipal + src.Sec80cChildrenTuition +
                   src.Sec80cNsc + src.Sec80cSukanyaSamriddhi +
                   src.Sec80cFixedDeposit + src.Sec80cOthers;
        }

        private static decimal Calculate80dTotal(EmployeeTaxDeclaration src)
        {
            return src.Sec80dSelfFamily + src.Sec80dParents + src.Sec80dPreventiveCheckup;
        }

        private static decimal CalculateTotalDeductions(EmployeeTaxDeclaration src)
        {
            return Math.Min(Calculate80cTotal(src), 150000m) + // 80C cap
                   Math.Min(src.Sec80ccdNps, 50000m) + // 80CCD(1B) cap
                   Calculate80dTotal(src) +
                   src.Sec80eEducationLoan +
                   Math.Min(src.Sec24HomeLoanInterest, 200000m) + // Section 24 cap
                   src.Sec80gDonations +
                   Math.Min(src.Sec80ttaSavingsInterest, 10000m); // 80TTA cap
        }

        private static string GetFinancialYear(int month, int year)
        {
            if (month >= 4)
            {
                return $"{year}-{(year + 1) % 100:D2}";
            }
            else
            {
                return $"{year - 1}-{year % 100:D2}";
            }
        }
    }
}
