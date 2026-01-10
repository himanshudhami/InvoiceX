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
services.AddScoped<Core.Interfaces.ICreditNotesRepository>(sp =>
            new Infrastructure.Data.CreditNotesRepository(connectionString));
services.AddScoped<Core.Interfaces.ICreditNoteItemsRepository>(sp =>
            new Infrastructure.Data.CreditNoteItemsRepository(connectionString));
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

            // GST Input Credit repository
            services.AddScoped<Core.Interfaces.Ledger.IGstInputCreditRepository>(sp =>
                new Infrastructure.Data.Ledger.GstInputCreditRepository(connectionString));

            // Payment allocation repository
            services.AddScoped<Core.Interfaces.IPaymentAllocationRepository>(sp =>
                new Infrastructure.Data.PaymentAllocationRepository(connectionString));

            // Vendor payment allocation repository
            services.AddScoped<Core.Interfaces.IVendorPaymentAllocationRepository>(sp =>
                new Infrastructure.Data.VendorPaymentAllocationRepository(connectionString));

            // Bank transaction match repository
            services.AddScoped<Core.Interfaces.IBankTransactionMatchRepository>(sp =>
                new Infrastructure.Data.BankTransactionMatchRepository(connectionString));

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

            // Calculation rules repositories
            services.AddScoped<Core.Interfaces.Payroll.ICalculationRuleRepository>(sp =>
                new Infrastructure.Data.Payroll.CalculationRuleRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.IFormulaVariableRepository>(sp =>
                new Infrastructure.Data.Payroll.FormulaVariableRepository(connectionString));
            services.AddScoped<Core.Interfaces.Payroll.ICalculationRuleTemplateRepository>(sp =>
                new Infrastructure.Data.Payroll.CalculationRuleTemplateRepository(connectionString));

            // Authentication repositories
            services.AddScoped<Core.Interfaces.IUserRepository>(sp =>
                new Infrastructure.Data.UserRepository(connectionString));
            services.AddScoped<Core.Interfaces.IRefreshTokenRepository>(sp =>
                new Infrastructure.Data.RefreshTokenRepository(connectionString));
            services.AddScoped<Core.Interfaces.IUserCompanyAssignmentRepository>(sp =>
                new Infrastructure.Data.UserCompanyAssignmentRepository(connectionString));

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

            // Document category repository
            services.AddScoped<Core.Interfaces.Document.IDocumentCategoryRepository>(sp =>
                new Infrastructure.Data.Document.DocumentCategoryRepository(connectionString));

            // Expense repositories
            services.AddScoped<Core.Interfaces.Expense.IExpenseCategoryRepository>(sp =>
                new Infrastructure.Data.Expense.ExpenseCategoryRepository(connectionString));
            services.AddScoped<Core.Interfaces.Expense.IExpenseClaimRepository>(sp =>
                new Infrastructure.Data.Expense.ExpenseClaimRepository(connectionString));
            services.AddScoped<Core.Interfaces.Expense.IExpenseAttachmentRepository>(sp =>
                new Infrastructure.Data.Expense.ExpenseAttachmentRepository(connectionString));

            // File storage repositories
            services.AddScoped<Core.Interfaces.FileStorage.IFileStorageRepository>(sp =>
                new Infrastructure.Data.FileStorage.FileStorageRepository(connectionString));
            services.AddScoped<Core.Interfaces.FileStorage.IDocumentAuditLogRepository>(sp =>
                new Infrastructure.Data.FileStorage.DocumentAuditLogRepository(connectionString));

            // File storage service (local implementation - will be replaced with S3 in production)
            services.AddScoped<Core.Interfaces.FileStorage.IFileStorageService, Infrastructure.FileStorage.LocalFileStorageService>();

            // General Ledger repositories
            services.AddScoped<Core.Interfaces.Ledger.IChartOfAccountRepository>(sp =>
                new Infrastructure.Data.Ledger.ChartOfAccountRepository(connectionString));
            services.AddScoped<Core.Interfaces.Ledger.IJournalEntryRepository>(sp =>
                new Infrastructure.Data.Ledger.JournalEntryRepository(connectionString));
            services.AddScoped<Core.Interfaces.Ledger.IPostingRuleRepository>(sp =>
                new Infrastructure.Data.Ledger.PostingRuleRepository(connectionString));
            services.AddScoped<Core.Interfaces.Ledger.ILedgerReportRepository>(sp =>
                new Infrastructure.Data.Ledger.LedgerReportRepository(connectionString));

            // Trial Balance service (uses repository pattern - SQL in repository, logic in service)
            services.AddScoped<Application.Interfaces.Ledger.ITrialBalanceService>(sp =>
                new Application.Services.Ledger.TrialBalanceService(
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.ILedgerReportRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Ledger.TrialBalanceService>>()
                ));

            // Auto-Posting service (for invoices, payments, vendor invoices, vendor payments, etc.)
            // Note: Expense claim posting uses dedicated ExpensePostingService
            services.AddScoped<Application.Interfaces.Ledger.IAutoPostingService>(sp =>
                new Application.Services.Ledger.AutoPostingService(
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IJournalEntryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IPostingRuleRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IInvoicesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IPaymentsRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IVendorInvoicesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IVendorPaymentsRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Ledger.AutoPostingService>>()
                ));

            // Statutory Payment repository
            services.AddScoped<Core.Interfaces.Payroll.IStatutoryPaymentRepository>(sp =>
                new Infrastructure.Data.Payroll.StatutoryPaymentRepository(connectionString));

            // Payroll Posting service (three-stage journal entries for payroll)
            services.AddScoped<Core.Interfaces.Payroll.IPayrollPostingService>(sp =>
                new Application.Services.Payroll.PayrollPostingService(
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollRunRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IJournalEntryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IStatutoryPaymentRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Payroll.PayrollPostingService>>()
                ));

            // Contractor Posting service (two-stage journal entries for contractor payments)
            services.AddScoped<Core.Interfaces.Payroll.IContractorPostingService>(sp =>
                new Application.Services.Payroll.ContractorPostingService(
                    sp.GetRequiredService<Core.Interfaces.Payroll.IContractorPaymentRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IJournalEntryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Payroll.ContractorPostingService>>()
                ));

            // Expense Posting service (journal entries for expense claims)
            services.AddScoped<Core.Interfaces.Expense.IExpensePostingService>(sp =>
                new Application.Services.Expense.ExpensePostingService(
                    sp.GetRequiredService<Core.Interfaces.Expense.IExpenseClaimRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Expense.IExpenseCategoryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IJournalEntryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Expense.ExpensePostingService>>()
                ));

            // E-Invoice repositories
            services.AddScoped<Core.Interfaces.EInvoice.IEInvoiceCredentialsRepository>(sp =>
                new Infrastructure.Data.EInvoice.EInvoiceCredentialsRepository(connectionString));
            services.AddScoped<Core.Interfaces.EInvoice.IEInvoiceAuditLogRepository>(sp =>
                new Infrastructure.Data.EInvoice.EInvoiceAuditLogRepository(connectionString));
            services.AddScoped<Core.Interfaces.EInvoice.IEInvoiceQueueRepository>(sp =>
                new Infrastructure.Data.EInvoice.EInvoiceQueueRepository(connectionString));

            // E-Invoice GSP clients (using AddHttpClient for proper HttpClient DI)
            services.AddHttpClient<Infrastructure.EInvoice.ClearTaxGspClient>();
            services.AddScoped<Core.Interfaces.EInvoice.IEInvoiceGspClient, Infrastructure.EInvoice.ClearTaxGspClient>();

            // E-Invoice GSP client factory
            services.AddScoped<Application.Services.EInvoice.IEInvoiceGspClientFactory, Infrastructure.EInvoice.EInvoiceGspClientFactory>();

            // E-Invoice service
            services.AddScoped<Application.Services.EInvoice.EInvoiceService>();

            // Tax Rule Pack repository and service
            services.AddScoped<Core.Interfaces.ITaxRulePackRepository>(sp =>
                new Infrastructure.Data.TaxRulePackRepository(connectionString));
            services.AddScoped<Application.Services.TaxRulePackService>();

            // Tax Rate Providers (for integrating Rule Packs with TDS calculations)
            // Register individual providers
            services.AddScoped<Infrastructure.Data.Payroll.TaxRulePackTaxRateProvider>(sp =>
                new Infrastructure.Data.Payroll.TaxRulePackTaxRateProvider(
                    sp.GetRequiredService<Core.Interfaces.ITaxRulePackRepository>()));

            services.AddScoped<Infrastructure.Data.Payroll.LegacyTaxRateProvider>(sp =>
                new Infrastructure.Data.Payroll.LegacyTaxRateProvider(
                    sp.GetRequiredService<Core.Interfaces.Payroll.ITaxSlabRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.ITaxParameterRepository>()));

            // Register HybridTaxRateProvider as the default ITaxRateProvider
            // This tries Rule Packs first, then falls back to Legacy tables
            services.AddScoped<Core.Interfaces.Payroll.ITaxRateProvider>(sp =>
                new Infrastructure.Data.Payroll.HybridTaxRateProvider(
                    sp.GetRequiredService<Infrastructure.Data.Payroll.TaxRulePackTaxRateProvider>(),
                    sp.GetRequiredService<Infrastructure.Data.Payroll.LegacyTaxRateProvider>(),
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<Infrastructure.Data.Payroll.HybridTaxRateProvider>>(),
                    preferRulePacks: true));

            // Register factory for cases where caller wants to choose provider
            services.AddScoped<Infrastructure.Data.Payroll.TaxRateProviderFactory>();

            // Forex/Export repositories
            services.AddScoped<Core.Interfaces.Forex.IFircTrackingRepository>(sp =>
                new Infrastructure.Data.Forex.FircTrackingRepository(connectionString));
            services.AddScoped<Core.Interfaces.Forex.ILutRegisterRepository>(sp =>
                new Infrastructure.Data.Forex.LutRegisterRepository(connectionString));
            services.AddScoped<Core.Interfaces.Forex.IForexTransactionRepository>(sp =>
                new Infrastructure.Data.Forex.ForexTransactionRepository(connectionString));

            // RCM Transaction repository (Reverse Charge Mechanism)
            services.AddScoped<Core.Interfaces.Gst.IRcmTransactionRepository>(sp =>
                new Infrastructure.Data.Gst.RcmTransactionRepository(connectionString));

            // TCS Transaction repository (Tax Collected at Source)
            services.AddScoped<Core.Interfaces.Tax.ITcsTransactionRepository>(sp =>
                new Infrastructure.Data.Tax.TcsTransactionRepository(connectionString));

            // Lower Deduction Certificate repository (Form 13)
            services.AddScoped<Core.Interfaces.Tax.ILowerDeductionCertificateRepository>(sp =>
                new Infrastructure.Data.Tax.LowerDeductionCertificateRepository(connectionString));

            // Form 16 repository (TDS Certificate for Salary)
            services.AddScoped<Core.Interfaces.Tax.IForm16Repository>(sp =>
                new Infrastructure.Data.Tax.Form16Repository(connectionString));

            // Form 24Q Filing repository (Quarterly TDS Return for Salary)
            services.AddScoped<Core.Interfaces.Tax.IForm24QFilingRepository>(sp =>
                new Infrastructure.Data.Tax.Form24QFilingRepository(connectionString));

            // Vendors (AP) repositories
            services.AddScoped<Core.Interfaces.IVendorsRepository>(sp =>
                new Infrastructure.Data.VendorsRepository(connectionString));
            services.AddScoped<Core.Interfaces.IVendorInvoicesRepository>(sp =>
                new Infrastructure.Data.VendorInvoicesRepository(connectionString));
            services.AddScoped<Core.Interfaces.IVendorPaymentsRepository>(sp =>
                new Infrastructure.Data.VendorPaymentsRepository(connectionString));

            // RCM Posting service (two-stage journal entries for RCM)
            services.AddScoped<Core.Interfaces.Gst.IRcmPostingService>(sp =>
                new Application.Services.Gst.RcmPostingService(
                    sp.GetRequiredService<Core.Interfaces.Gst.IRcmTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IJournalEntryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Expense.IExpenseClaimRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Gst.RcmPostingService>>()
                ));

            // TCS service (Tax Collected at Source)
            services.AddScoped<Core.Interfaces.Tax.ITcsService>(sp =>
                new Application.Services.Tax.TcsService(
                    sp.GetRequiredService<Core.Interfaces.Tax.ITcsTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IJournalEntryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Tax.TcsService>>()
                ));

            // GST Posting service (ITC Blocked, Credit/Debit Notes, ITC Reversal)
            services.AddScoped<Core.Interfaces.Gst.IGstPostingService>(sp =>
                new Application.Services.Gst.GstPostingService(
                    sp.GetRequiredService<Core.Interfaces.Ledger.IJournalEntryRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Ledger.IChartOfAccountRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Gst.GstPostingService>>()
                ));

            // TDS Return service (Form 26Q and Form 24Q preparation)
            services.AddScoped<Core.Interfaces.Tax.ITdsReturnService>(sp =>
                new Application.Services.Tax.TdsReturnService(
                    sp.GetRequiredService<Core.Interfaces.Payroll.IContractorPaymentRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IEmployeesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.ICompaniesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Tax.ILowerDeductionCertificateRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IEmployeeTaxDeclarationRepository>()
                ));

            // Form 16 service (TDS Certificate for Salary - Section 192)
            services.AddScoped<Core.Interfaces.Tax.IForm16Service>(sp =>
                new Application.Services.Tax.Form16GenerationService(
                    sp.GetRequiredService<Core.Interfaces.Tax.IForm16Repository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollRunRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IEmployeesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IEmployeePayrollInfoRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IEmployeeTaxDeclarationRepository>(),
                    sp.GetRequiredService<Core.Interfaces.ICompaniesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.ICompanyStatutoryConfigRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IStatutoryPaymentRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Tax.Form16GenerationService>>()
                ));

            // Form 24Q Filing service (Quarterly TDS Return for Salary - Section 192)
            services.AddScoped<Core.Interfaces.Tax.IForm24QFilingService>(sp =>
                new Application.Services.Tax.Form24QFilingService(
                    sp.GetRequiredService<Core.Interfaces.Tax.IForm24QFilingRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Tax.ITdsReturnService>(),
                    sp.GetRequiredService<Core.Interfaces.Tax.IFvuFileGeneratorService>(),
                    sp.GetRequiredService<Core.Interfaces.ICompaniesRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Tax.Form24QFilingService>>()
                ));

            // TDS Challan 281 service (TDS deposit management)
            services.AddScoped<Core.Interfaces.Statutory.ITdsChallanService>(sp =>
                new Application.Services.Statutory.TdsChallanService(
                    sp.GetRequiredService<Core.Interfaces.Payroll.IStatutoryPaymentRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollRunRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IContractorPaymentRepository>(),
                    sp.GetRequiredService<Core.Interfaces.ICompaniesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.ICompanyStatutoryConfigRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IEmployeesRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Statutory.TdsChallanService>>()
                ));

            // PF ECR service (EPFO ECR generation and filing)
            services.AddScoped<Core.Interfaces.Statutory.IPfEcrService>(sp =>
                new Application.Services.Statutory.PfEcrService(
                    sp.GetRequiredService<Core.Interfaces.Payroll.IStatutoryPaymentRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollRunRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IEmployeePayrollInfoRepository>(),
                    sp.GetRequiredService<Core.Interfaces.ICompaniesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.ICompanyStatutoryConfigRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Statutory.PfEcrService>>()
                ));

            // ESI Return service (ESIC monthly return generation and filing)
            services.AddScoped<Core.Interfaces.Statutory.IEsiReturnService>(sp =>
                new Application.Services.Statutory.EsiReturnService(
                    sp.GetRequiredService<Core.Interfaces.Payroll.IStatutoryPaymentRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollTransactionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IPayrollRunRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.IEmployeePayrollInfoRepository>(),
                    sp.GetRequiredService<Core.Interfaces.ICompaniesRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Payroll.ICompanyStatutoryConfigRepository>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Services.Statutory.EsiReturnService>>()
                ));

            // Tags & Attribution System
            services.AddScoped<Core.Interfaces.Tags.ITagRepository>(sp =>
                new Infrastructure.Data.Tags.TagRepository(connectionString));
            services.AddScoped<Core.Interfaces.Tags.ITransactionTagRepository>(sp =>
                new Infrastructure.Data.Tags.TransactionTagRepository(connectionString));
            services.AddScoped<Core.Interfaces.Tags.IAttributionRuleRepository>(sp =>
                new Infrastructure.Data.Tags.AttributionRuleRepository(connectionString));

            services.AddScoped<Application.Interfaces.Tags.ITagService>(sp =>
                new Application.Services.Tags.TagService(
                    sp.GetRequiredService<Core.Interfaces.Tags.ITagRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Tags.ITransactionTagRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Tags.IAttributionRuleRepository>()
                ));
            services.AddScoped<Application.Interfaces.Tags.IAttributionRuleService>(sp =>
                new Application.Services.Tags.AttributionRuleService(
                    sp.GetRequiredService<Core.Interfaces.Tags.IAttributionRuleRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Tags.ITagRepository>()
                ));

            // Inventory repositories
            services.AddScoped<Core.Interfaces.Inventory.IWarehouseRepository>(sp =>
                new Infrastructure.Data.Inventory.WarehouseRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IStockGroupRepository>(sp =>
                new Infrastructure.Data.Inventory.StockGroupRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IUnitOfMeasureRepository>(sp =>
                new Infrastructure.Data.Inventory.UnitOfMeasureRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IStockItemRepository>(sp =>
                new Infrastructure.Data.Inventory.StockItemRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IUnitConversionRepository>(sp =>
                new Infrastructure.Data.Inventory.UnitConversionRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IStockBatchRepository>(sp =>
                new Infrastructure.Data.Inventory.StockBatchRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IStockMovementRepository>(sp =>
                new Infrastructure.Data.Inventory.StockMovementRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IStockTransferRepository>(sp =>
                new Infrastructure.Data.Inventory.StockTransferRepository(connectionString));
            services.AddScoped<Core.Interfaces.Inventory.IStockTransferItemRepository>(sp =>
                new Infrastructure.Data.Inventory.StockTransferItemRepository(connectionString));

            // Inventory services
            services.AddScoped<Application.Interfaces.Inventory.IWarehouseService>(sp =>
                new Application.Services.Inventory.WarehouseService(
                    sp.GetRequiredService<Core.Interfaces.Inventory.IWarehouseRepository>(),
                    sp.GetRequiredService<AutoMapper.IMapper>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.CreateWarehouseDto>>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.UpdateWarehouseDto>>()
                ));
            services.AddScoped<Application.Interfaces.Inventory.IStockGroupService>(sp =>
                new Application.Services.Inventory.StockGroupService(
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockGroupRepository>(),
                    sp.GetRequiredService<AutoMapper.IMapper>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.CreateStockGroupDto>>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.UpdateStockGroupDto>>()
                ));
            services.AddScoped<Application.Interfaces.Inventory.IStockItemService>(sp =>
                new Application.Services.Inventory.StockItemService(
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IUnitConversionRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockMovementRepository>(),
                    sp.GetRequiredService<AutoMapper.IMapper>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.CreateStockItemDto>>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.UpdateStockItemDto>>()
                ));
            services.AddScoped<Application.Interfaces.Inventory.IStockMovementService>(sp =>
                new Application.Services.Inventory.StockMovementService(
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockMovementRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockBatchRepository>(),
                    sp.GetRequiredService<AutoMapper.IMapper>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.CreateStockMovementDto>>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.UpdateStockMovementDto>>()
                ));
            services.AddScoped<Application.Interfaces.Inventory.IStockTransferService>(sp =>
                new Application.Services.Inventory.StockTransferService(
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockTransferRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockTransferItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockMovementRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockBatchRepository>(),
                    sp.GetRequiredService<AutoMapper.IMapper>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.CreateStockTransferDto>>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Application.DTOs.Inventory.UpdateStockTransferDto>>()
                ));

            // Serial Number repository (Inventory)
            services.AddScoped<Core.Interfaces.Inventory.ISerialNumberRepository>(sp =>
                new Infrastructure.Data.Inventory.SerialNumberRepository(connectionString));

            // Manufacturing repositories
            services.AddScoped<Core.Interfaces.Manufacturing.IBomRepository>(sp =>
                new Infrastructure.Data.Manufacturing.BomRepository(connectionString));
            services.AddScoped<Core.Interfaces.Manufacturing.IBomItemRepository>(sp =>
                new Infrastructure.Data.Manufacturing.BomItemRepository(connectionString));
            services.AddScoped<Core.Interfaces.Manufacturing.IProductionOrderRepository>(sp =>
                new Infrastructure.Data.Manufacturing.ProductionOrderRepository(connectionString));
            services.AddScoped<Core.Interfaces.Manufacturing.IProductionOrderItemRepository>(sp =>
                new Infrastructure.Data.Manufacturing.ProductionOrderItemRepository(connectionString));

            // Manufacturing services
            services.AddScoped<Application.Services.Manufacturing.IBomService>(sp =>
                new Application.Services.Manufacturing.BomService(
                    sp.GetRequiredService<Core.Interfaces.Manufacturing.IBomRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Manufacturing.IBomItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockItemRepository>()
                ));
            services.AddScoped<Application.Services.Manufacturing.IProductionOrderService>(sp =>
                new Application.Services.Manufacturing.ProductionOrderService(
                    sp.GetRequiredService<Core.Interfaces.Manufacturing.IProductionOrderRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Manufacturing.IProductionOrderItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Manufacturing.IBomRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Manufacturing.IBomItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockMovementRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IWarehouseRepository>()
                ));
            services.AddScoped<Application.Services.Inventory.ISerialNumberService>(sp =>
                new Application.Services.Inventory.SerialNumberService(
                    sp.GetRequiredService<Core.Interfaces.Inventory.ISerialNumberRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IStockItemRepository>(),
                    sp.GetRequiredService<Core.Interfaces.Inventory.IWarehouseRepository>()
                ));

            // Tally Migration repositories
            services.AddScoped<Core.Interfaces.Migration.ITallyMigrationBatchRepository>(sp =>
                new Infrastructure.Data.Migration.TallyMigrationBatchRepository(connectionString));
            services.AddScoped<Core.Interfaces.Migration.ITallyMigrationLogRepository>(sp =>
                new Infrastructure.Data.Migration.TallyMigrationLogRepository(connectionString));
            services.AddScoped<Core.Interfaces.Migration.ITallyFieldMappingRepository>(sp =>
                new Infrastructure.Data.Migration.TallyFieldMappingRepository(connectionString));

            // Tally Parser services
            services.AddScoped<Application.Interfaces.Migration.ITallyParserService, Application.Services.Migration.TallyXmlParserService>();
            services.AddScoped<Application.Interfaces.Migration.ITallyParserService, Application.Services.Migration.TallyJsonParserService>();
            services.AddScoped<Application.Interfaces.Migration.ITallyParserFactory, Application.Services.Migration.TallyParserFactory>();

            // Tally Mapping services
            services.AddScoped<Application.Interfaces.Migration.ITallyMasterMappingService, Application.Services.Migration.TallyMasterMappingService>();
            services.AddScoped<Application.Interfaces.Migration.ITallyPaymentClassifier, Application.Services.Migration.TallyPaymentClassifier>();
            services.AddScoped<Application.Interfaces.Migration.ITallyContractorPaymentMapper, Application.Services.Migration.TallyContractorPaymentMapper>();
            services.AddScoped<Application.Interfaces.Migration.ITallyStatutoryPaymentMapper, Application.Services.Migration.TallyStatutoryPaymentMapper>();
            services.AddScoped<Application.Interfaces.Migration.ITallyBankTransactionMapper, Application.Services.Migration.TallyBankTransactionMapper>();
            services.AddScoped<Application.Interfaces.Migration.ITallyVoucherMappingService, Application.Services.Migration.TallyVoucherMappingService>();

            // Tally Validation and Rollback services
            services.AddScoped<Application.Interfaces.Migration.ITallyValidationService, Application.Services.Migration.TallyValidationService>();
            services.AddScoped<Application.Interfaces.Migration.ITallyRollbackService, Application.Services.Migration.TallyRollbackService>();

            // Tally Import Orchestrator (main service)
            services.AddScoped<Application.Interfaces.Migration.ITallyImportService, Application.Services.Migration.TallyImportOrchestrator>();

            // Unified Party Management repositories
            services.AddScoped<Core.Interfaces.IPartyRepository>(sp =>
                new Infrastructure.Data.PartyRepository(connectionString));

            // Tag-driven TDS repositories (replaces old TdsSectionRule)
            services.AddScoped<Core.Interfaces.ITdsTagRuleRepository>(sp =>
                new Infrastructure.Data.TdsTagRuleRepository(connectionString));

            // Unified Party Management services (uses tag-driven TDS)
            services.AddScoped<Application.Interfaces.IPartyService>(sp =>
                new Application.Services.PartyService(
                    sp.GetRequiredService<Core.Interfaces.IPartyRepository>(),
                    sp.GetRequiredService<Core.Interfaces.ITdsTagRuleRepository>(),
                    sp.GetRequiredService<AutoMapper.IMapper>()
                ));

            // Tag-driven TDS Detection Service (new approach)
            services.AddScoped<Application.Interfaces.ITdsDetectionService>(sp =>
                new Application.Services.TdsDetectionService(
                    sp.GetRequiredService<Core.Interfaces.ITdsTagRuleRepository>(),
                    sp.GetRequiredService<Core.Interfaces.IPartyRepository>(),
                    sp.GetRequiredService<AutoMapper.IMapper>()
                ));

            // Add other infrastructure services here
            // services.AddScoped<IEmailProvider, SmtpEmailProvider>();
            // services.AddScoped<IFileStorage, LocalFileStorage>();
            // services.AddScoped<ICacheService, RedisCacheService>();

            return services;
        }
    }
}