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

            // Bank transaction specialized services (refactored for SRP)
            services.AddScoped<Application.Interfaces.IReconciliationService,
                              Application.Services.ReconciliationService>();
            services.AddScoped<Application.Interfaces.IBankStatementImportService,
                              Application.Services.BankStatementImportService>();
            services.AddScoped<Application.Interfaces.IBrsService,
                              Application.Services.BrsService>();
            services.AddScoped<Application.Interfaces.IReversalDetectionService,
                              Application.Services.ReversalDetectionService>();
            services.AddScoped<Application.Interfaces.IOutgoingPaymentsService,
                              Application.Services.OutgoingPaymentsService>();
            services.AddScoped<Application.Interfaces.IJournalEntryLinkingService,
                              Application.Services.JournalEntryLinkingService>();

            // TDS Receivable service
            services.AddScoped<Application.Interfaces.ITdsReceivableService,
                              Application.Services.TdsReceivableService>();

            // Payment allocation service
            services.AddScoped<Application.Interfaces.IPaymentAllocationService,
                              Application.Services.PaymentAllocationService>();

            // Payroll calculation services
            services.AddScoped<Application.Services.Payroll.PfCalculationService>();
            services.AddScoped<Application.Services.Payroll.EsiCalculationService>();
            services.AddScoped<Application.Services.Payroll.ProfessionalTaxCalculationService>();
            services.AddScoped<Application.Services.Payroll.TdsCalculationService>();
            services.AddScoped<Application.Services.Payroll.PayrollCalculationService>();

            // Calculation rules engine
            services.AddScoped<Application.Services.Payroll.FormulaEvaluator>();
            services.AddScoped<Application.Services.Payroll.CalculationRuleEngine>();

            // Payroll configuration services
            services.AddScoped<Application.Interfaces.Payroll.IProfessionalTaxSlabService,
                              Application.Services.Payroll.ProfessionalTaxSlabService>();

            // Tax declaration services
            services.AddScoped<Application.Services.Payroll.ITaxDeclarationValidationService,
                              Application.Services.Payroll.TaxDeclarationValidationService>();
            services.AddScoped<Application.Interfaces.Payroll.ITaxDeclarationService,
                              Application.Services.Payroll.TaxDeclarationService>();

            // Register FluentValidation validators
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register AutoMapper manually to avoid ambiguous extension methods
            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });
            services.AddSingleton(sp => mapperConfiguration.CreateMapper());

            // Authentication service
            services.AddScoped<Application.Interfaces.IAuthService,
                              Application.Services.AuthService>();

            // Employee Portal service
            services.AddScoped<Application.Interfaces.IEmployeePortalService,
                              Application.Services.EmployeePortalService>();

            // Leave management service
            services.AddScoped<Application.Interfaces.Leave.ILeaveService,
                              Application.Services.Leave.LeaveService>();

            // Portal feature services
            services.AddScoped<Application.Interfaces.IAnnouncementsService,
                              Application.Services.AnnouncementsService>();
            services.AddScoped<Application.Interfaces.ISupportTicketsService,
                              Application.Services.SupportTicketsService>();
            services.AddScoped<Application.Interfaces.IEmployeeDocumentsService,
                              Application.Services.EmployeeDocumentsService>();

            // Employee hierarchy service
            services.AddScoped<Application.Interfaces.Hierarchy.IEmployeeHierarchyService,
                              Application.Services.Hierarchy.EmployeeHierarchyService>();

            // Approval workflow services
            services.AddScoped<Application.Interfaces.Approval.IApprovalTemplateService,
                              Application.Services.Approval.ApprovalTemplateService>();
            services.AddScoped<Application.Interfaces.Approval.IApprovalWorkflowService,
                              Application.Services.Approval.ApprovalWorkflowService>();

            // Approver resolver implementations (registered for factory consumption)
            services.AddScoped<Core.Interfaces.Approval.IApproverResolver,
                              Application.Services.Approval.Resolvers.DirectManagerApproverResolver>();
            services.AddScoped<Core.Interfaces.Approval.IApproverResolver,
                              Application.Services.Approval.Resolvers.SkipLevelManagerApproverResolver>();
            services.AddScoped<Core.Interfaces.Approval.IApproverResolver,
                              Application.Services.Approval.Resolvers.RoleBasedApproverResolver>();
            services.AddScoped<Core.Interfaces.Approval.IApproverResolver,
                              Application.Services.Approval.Resolvers.SpecificUserApproverResolver>();
            services.AddScoped<Core.Interfaces.Approval.IApproverResolver,
                              Application.Services.Approval.Resolvers.DepartmentHeadApproverResolver>();

            // Approver resolver factory
            services.AddScoped<Core.Abstractions.IApproverResolverFactory,
                              Application.Services.Approval.ApproverResolverFactory>();

            // Activity status handlers (for workflow completion callbacks)
            services.AddScoped<Application.Interfaces.Approval.IActivityStatusHandler,
                              Application.Services.Approval.Handlers.LeaveStatusHandler>();
            services.AddScoped<Application.Interfaces.Approval.IActivityStatusHandler,
                              Application.Services.Approval.Handlers.AssetRequestStatusHandler>();
            services.AddScoped<Application.Interfaces.Approval.IActivityStatusHandler,
                              Application.Services.Approval.Handlers.ExpenseStatusHandler>();

            // Activity status handler registry
            services.AddScoped<Application.Interfaces.Approval.IActivityStatusHandlerRegistry,
                              Application.Services.Approval.ActivityStatusHandlerRegistry>();

            // Asset request service
            services.AddScoped<Application.Interfaces.IAssetRequestService,
                              Application.Services.AssetRequestService>();

            // File upload service
            services.AddScoped<Application.Interfaces.IFileUploadService,
                              Application.Services.FileStorage.FileUploadService>();

            // Document category service
            services.AddScoped<Application.Interfaces.Document.IDocumentCategoryService,
                              Application.Services.Document.DocumentCategoryService>();

            // Expense services
            services.AddScoped<Application.Interfaces.Expense.IExpenseCategoryService,
                              Application.Services.Expense.ExpenseCategoryService>();
            services.AddScoped<Application.Interfaces.Expense.IExpenseClaimService,
                              Application.Services.Expense.ExpenseClaimService>();

            // General Ledger services
            services.AddScoped<Application.Interfaces.Ledger.IAutoPostingService,
                              Application.Services.Ledger.AutoPostingService>();

            // Forex/Export services
            services.AddScoped<Application.Interfaces.Forex.IFircReconciliationService,
                              Application.Services.Forex.FircReconciliationService>();
            services.AddScoped<Application.Interfaces.Forex.ILutService,
                              Application.Services.Forex.LutService>();
            services.AddScoped<Application.Interfaces.Forex.IForexService,
                              Application.Services.Forex.ForexService>();
            services.AddScoped<Application.Interfaces.Reports.IExportReportingService,
                              Application.Services.Reports.ExportReportingService>();

            // FVU File Generation services (TDS Return text file generation for NSDL)
            services.AddScoped<Application.Services.Tax.IFvuRecordBuilder,
                              Application.Services.Tax.FvuRecordBuilder>();
            services.AddScoped<Core.Interfaces.Tax.IFvuFileGeneratorService,
                              Application.Services.Tax.FvuFileGeneratorService>();

            // Add other application services here
            // services.AddScoped<IEmailService, EmailService>();
            // services.AddScoped<INotificationService, NotificationService>();

            return services;
        }
    }
}