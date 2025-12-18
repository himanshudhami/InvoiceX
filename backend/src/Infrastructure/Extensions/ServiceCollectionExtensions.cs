using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dapper;

namespace Infrastructure.Extensions
{
    /// <summary>
    /// Dependency injection extensions for Infrastructure layer
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Infrastructure layer services to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

            // Ensure Dapper maps snake_case columns (e.g., study_id) to PascalCase properties (StudyId)
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            // Register all repositories
services.AddScoped<Core.Interfaces.ICompaniesRepository>(sp =>
            new Infrastructure.Data.CompaniesRepository(connectionString));
services.AddScoped<Core.Interfaces.ICustomersRepository>(sp =>
            new Infrastructure.Data.CustomersRepository(connectionString));
services.AddScoped<Core.Interfaces.IInvoiceItemsRepository>(sp =>
            new Infrastructure.Data.InvoiceItemsRepository(connectionString));
services.AddScoped<Core.Interfaces.IInvoiceTemplatesRepository>(sp =>
            new Infrastructure.Data.InvoiceTemplatesRepository(connectionString));
services.AddScoped<Core.Interfaces.IInvoicesRepository>(sp =>
            new Infrastructure.Data.InvoicesRepository(connectionString));
services.AddScoped<Core.Interfaces.IPaymentsRepository>(sp =>
            new Infrastructure.Data.PaymentsRepository(connectionString));
services.AddScoped<Core.Interfaces.IProductsRepository>(sp =>
            new Infrastructure.Data.ProductsRepository(connectionString));
services.AddScoped<Core.Interfaces.ITaxRatesRepository>(sp =>
            new Infrastructure.Data.TaxRatesRepository(connectionString));
services.AddScoped<Core.Interfaces.IDashboardRepository>(sp =>
            new Infrastructure.Data.DashboardRepository(connectionString));
services.AddScoped<Core.Interfaces.IQuotesRepository>(sp =>
                new Infrastructure.Data.QuotesRepository(connectionString));
services.AddScoped<Core.Interfaces.IQuoteItemsRepository>(sp =>
                new Infrastructure.Data.QuoteItemsRepository(connectionString));
services.AddScoped<Core.Interfaces.IEmployeesRepository>(sp =>
                new Infrastructure.Data.EmployeesRepository(connectionString));
services.AddScoped<Core.Interfaces.IEmployeeSalaryTransactionsRepository>(sp =>
                new Infrastructure.Data.EmployeeSalaryTransactionsRepository(connectionString));
services.AddScoped<Core.Interfaces.IAssetsRepository>(sp =>
                new Infrastructure.Data.AssetsRepository(connectionString));
services.AddScoped<Core.Interfaces.ISubscriptionsRepository>(sp =>
                new Infrastructure.Data.SubscriptionsRepository(connectionString));
services.AddScoped<Core.Interfaces.ILoansRepository>(sp =>
                new Infrastructure.Data.LoansRepository(connectionString));
services.AddScoped<Core.Interfaces.ICashFlowRepository>(sp =>
                new Infrastructure.Data.CashFlowRepository(connectionString));

            // Bank integration repositories
            services.AddScoped<Core.Interfaces.IBankAccountRepository>(sp =>
                new Infrastructure.Data.BankAccountRepository(connectionString));
            services.AddScoped<Core.Interfaces.IBankTransactionRepository>(sp =>
                new Infrastructure.Data.BankTransactionRepository(connectionString));

            // TDS Receivable repository
            services.AddScoped<Core.Interfaces.ITdsReceivableRepository>(sp =>
                new Infrastructure.Data.TdsReceivableRepository(connectionString));

            // Payroll repositories
            services.AddScoped<Core.Interfaces.Payroll.ITaxSlabRepository>(sp =>
                new Infrastructure.Data.Payroll.TaxSlabRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IProfessionalTaxSlabRepository>(sp =>
                new Infrastructure.Data.Payroll.ProfessionalTaxSlabRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.ICompanyStatutoryConfigRepository>(sp =>
                new Infrastructure.Data.Payroll.CompanyStatutoryConfigRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IEmployeePayrollInfoRepository>(sp =>
                new Infrastructure.Data.Payroll.EmployeePayrollInfoRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IEmployeeSalaryStructureRepository>(sp =>
                new Infrastructure.Data.Payroll.EmployeeSalaryStructureRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IEmployeeTaxDeclarationRepository>(sp =>
                new Infrastructure.Data.Payroll.EmployeeTaxDeclarationRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IPayrollRunRepository>(sp =>
                new Infrastructure.Data.Payroll.PayrollRunRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IPayrollTransactionRepository>(sp =>
                new Infrastructure.Data.Payroll.PayrollTransactionRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IContractorPaymentRepository>(sp =>
                new Infrastructure.Data.Payroll.ContractorPaymentRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.ITaxParameterRepository>(sp =>
                new Infrastructure.Data.Payroll.TaxParameterRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.ISalaryComponentRepository>(sp =>
                new Infrastructure.Data.Payroll.SalaryComponentRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IPayrollCalculationLineRepository>(sp =>
                new Infrastructure.Data.Payroll.PayrollCalculationLineRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IEsiEligibilityRepository>(sp =>
                new Infrastructure.Data.Payroll.EsiEligibilityRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IEmployeeTaxDeclarationHistoryRepository>(sp =>
                new Infrastructure.Data.Payroll.EmployeeTaxDeclarationHistoryRepository(connectionString));

            // Authentication repositories
            services.AddScoped<Core.Interfaces.IUserRepository>(sp =>
                new Infrastructure.Data.UserRepository(connectionString));
            services.AddScoped<Core.Interfaces.IRefreshTokenRepository>(sp =>
                new Infrastructure.Data.RefreshTokenRepository(connectionString));

            // Leave management repositories
            services.AddScoped<Core.Interfaces.Leave.ILeaveTypeRepository>(sp =>
                new Infrastructure.Data.Leave.LeaveTypeRepository(connectionString));
            services.AddScoped<Core.Interfaces.Leave.IEmployeeLeaveBalanceRepository>(sp =>
                new Infrastructure.Data.Leave.EmployeeLeaveBalanceRepository(connectionString));
            services.AddScoped<Core.Interfaces.Leave.ILeaveApplicationRepository>(sp =>
                new Infrastructure.Data.Leave.LeaveApplicationRepository(connectionString));
            services.AddScoped<Core.Interfaces.Leave.IHolidayRepository>(sp =>
                new Infrastructure.Data.Leave.HolidayRepository(connectionString));

            // Portal feature repositories
            services.AddScoped<Core.Interfaces.IAnnouncementsRepository>(sp =>
                new Infrastructure.Data.AnnouncementsRepository(connectionString));
            services.AddScoped<Core.Interfaces.ISupportTicketsRepository>(sp =>
                new Infrastructure.Data.SupportTicketsRepository(connectionString));
            services.AddScoped<Core.Interfaces.IEmployeeDocumentsRepository>(sp =>
                new Infrastructure.Data.EmployeeDocumentsRepository(connectionString));

            // Employee hierarchy repository
            services.AddScoped<Core.Interfaces.Hierarchy.IEmployeeHierarchyRepository>(sp =>
                new Infrastructure.Data.Hierarchy.EmployeeHierarchyRepository(connectionString));

            // Approval workflow repositories
            services.AddScoped<Core.Interfaces.Approval.IApprovalTemplateRepository>(sp =>
                new Infrastructure.Data.Approval.ApprovalTemplateRepository(connectionString));
            services.AddScoped<Core.Interfaces.Approval.IApprovalWorkflowRepository>(sp =>
                new Infrastructure.Data.Approval.ApprovalWorkflowRepository(connectionString));

            // Asset request repository
            services.AddScoped<Core.Interfaces.IAssetRequestRepository>(sp =>
                new Infrastructure.Data.AssetRequestRepository(connectionString));

            // Add other infrastructure services here
            // services.AddScoped<IEmailProvider, SmtpEmailProvider>();
            // services.AddScoped<IFileStorage, LocalFileStorage>();
            // services.AddScoped<ICacheService, RedisCacheService>();

            return services;
        }
    }
}