using FluentValidation;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.Extensions
{
    /// <summary>
    /// Dependency injection extensions for Application layer
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Application layer services to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register all application services
services.AddScoped<Application.Interfaces.ICompaniesService,
                              Application.Services.CompaniesService>();
services.AddScoped<Application.Interfaces.ICustomersService,
                              Application.Services.CustomersService>();
services.AddScoped<Application.Interfaces.IInvoiceItemsService,
                              Application.Services.InvoiceItemsService>();
services.AddScoped<Application.Interfaces.IInvoiceTemplatesService,
                              Application.Services.InvoiceTemplatesService>();
services.AddScoped<Application.Interfaces.IInvoicesService,
                              Application.Services.InvoicesService>();
services.AddScoped<Application.Interfaces.IPaymentsService,
                              Application.Services.PaymentsService>();
services.AddScoped<Application.Interfaces.IProductsService,
                              Application.Services.ProductsService>();
services.AddScoped<Application.Interfaces.ITaxRatesService,
                              Application.Services.TaxRatesService>();
services.AddScoped<Application.Interfaces.IDashboardService,
                              Application.Services.DashboardService>();
services.AddScoped<Application.Interfaces.IQuotesService,
                              Application.Services.QuotesService>();
services.AddScoped<Application.Interfaces.IQuoteItemsService,
                              Application.Services.QuoteItemsService>();
services.AddScoped<Application.Interfaces.IEmployeesService,
                              Application.Services.EmployeesService>();
services.AddScoped<Application.Interfaces.IEmployeeSalaryTransactionsService,
                              Application.Services.EmployeeSalaryTransactionsService>();
            // Asset services - register specialized services first, then main service
            services.AddScoped<Application.Services.Assets.AssetAssignmentService>();
            services.AddScoped<Application.Services.Assets.AssetDocumentService>();
            services.AddScoped<Application.Services.Assets.AssetMaintenanceService>();
            services.AddScoped<Application.Services.Assets.AssetDisposalService>();
            services.AddScoped<Application.Services.Assets.AssetCostService>();
            services.AddScoped<Application.Services.Assets.AssetBulkService>();
            services.AddScoped<Application.Interfaces.IAssetsService,
                              Application.Services.AssetsService>();
services.AddScoped<Application.Interfaces.ISubscriptionsService,
                              Application.Services.SubscriptionsService>();
            services.AddScoped<Application.Services.Subscriptions.SubscriptionExpenseService>();
            // Loan services
            services.AddScoped<Application.Services.Loans.EmiCalculationService>();
            services.AddScoped<Application.Interfaces.ILoansService,
                              Application.Services.LoansService>();
            // Cash Flow service
            services.AddScoped<Application.Interfaces.ICashFlowService,
                              Application.Services.CashFlowService>();

            // Bank integration services
            services.AddScoped<Application.Interfaces.IBankAccountService,
                              Application.Services.BankAccountService>();
            services.AddScoped<Application.Interfaces.IBankTransactionService,
                              Application.Services.BankTransactionService>();

            // TDS Receivable service
            services.AddScoped<Application.Interfaces.ITdsReceivableService,
                              Application.Services.TdsReceivableService>();

            // Payroll calculation services
            services.AddScoped<Application.Services.Payroll.PfCalculationService>();
            services.AddScoped<Application.Services.Payroll.EsiCalculationService>();
            services.AddScoped<Application.Services.Payroll.ProfessionalTaxCalculationService>();
            services.AddScoped<Application.Services.Payroll.TdsCalculationService>();
            services.AddScoped<Application.Services.Payroll.PayrollCalculationService>();

            // Payroll configuration services
            services.AddScoped<Application.Interfaces.Payroll.IProfessionalTaxSlabService,
                              Application.Services.Payroll.ProfessionalTaxSlabService>();

            // Register FluentValidation validators
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register AutoMapper manually to avoid ambiguous extension methods
            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });
            services.AddSingleton(sp => mapperConfiguration.CreateMapper());

            // Add other application services here
            // services.AddScoped<IEmailService, EmailService>();
            // services.AddScoped<INotificationService, NotificationService>();

            return services;
        }
    }
}