using Application.DTOs.CashFlow;
using Application.Interfaces;
using Application.Services.Assets;
using Core.Common;
using Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service for cash flow statement calculations (AS-3 compliant)
/// </summary>
public class CashFlowService : ICashFlowService
{
    private readonly ICashFlowRepository _cashFlowRepository;
    private readonly IInvoicesRepository _invoicesRepository;
    private readonly IEmployeeSalaryTransactionsRepository _salaryRepository;
    private readonly IAssetsRepository _assetsRepository;
    private readonly ILoansRepository _loansRepository;
    private readonly AssetCostService _assetCostService;

    public CashFlowService(
        ICashFlowRepository cashFlowRepository,
        IInvoicesRepository invoicesRepository,
        IEmployeeSalaryTransactionsRepository salaryRepository,
        IAssetsRepository assetsRepository,
        ILoansRepository loansRepository,
        AssetCostService assetCostService)
    {
        _cashFlowRepository = cashFlowRepository ?? throw new ArgumentNullException(nameof(cashFlowRepository));
        _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
        _salaryRepository = salaryRepository ?? throw new ArgumentNullException(nameof(salaryRepository));
        _assetsRepository = assetsRepository ?? throw new ArgumentNullException(nameof(assetsRepository));
        _loansRepository = loansRepository ?? throw new ArgumentNullException(nameof(loansRepository));
        _assetCostService = assetCostService ?? throw new ArgumentNullException(nameof(assetCostService));
    }

    public async Task<Result<CashFlowStatementDto>> GetCashFlowStatementAsync(Guid? companyId, int year, int? month = null)
    {
        try
        {
            // Calculate date range
            var (fromDate, toDate) = CalculateDateRange(year, month);

            // Get asset cost report for depreciation
            var assetCostReportResult = await _assetCostService.GetCostReportAsync(companyId);
            if (assetCostReportResult.IsFailure)
            {
                return Error.Internal("Failed to retrieve asset cost report");
            }

            var assetCostReport = assetCostReportResult.Value;
            var depreciation = assetCostReport.TotalAccumulatedDepreciation;

            // Calculate net profit before tax (simplified - using accrual basis)
            // In a real scenario, this would be calculated from P&L
            // For now, we'll use a simplified calculation
            var netProfitBeforeTax = await CalculateNetProfitBeforeTaxAsync(companyId, year, month, depreciation);

            // Get all cash flow components
            var operatingDetails = new OperatingActivitiesDetail();
            var investingDetails = new InvestingActivitiesDetail();
            var financingDetails = new FinancingActivitiesDetail();

            // Operating Activities - Cash Receipts and Payments
            operatingDetails.CashReceiptsFromCustomers = await _cashFlowRepository.GetCashReceiptsFromCustomersAsync(companyId, fromDate, toDate);
            operatingDetails.CashPaidToEmployees = await _cashFlowRepository.GetCashPaidToEmployeesAsync(companyId, fromDate, toDate);
            operatingDetails.CashPaidForSubscriptions = await _cashFlowRepository.GetCashPaidForSubscriptionsAsync(companyId, fromDate, toDate);
            operatingDetails.CashPaidForOpexAssets = await _cashFlowRepository.GetCashPaidForOpexAssetsAsync(companyId, fromDate, toDate);
            operatingDetails.CashPaidForMaintenance = await _cashFlowRepository.GetCashPaidForMaintenanceAsync(companyId, fromDate, toDate);
            operatingDetails.TdsPayments = await _cashFlowRepository.GetTdsPaymentsAsync(companyId, fromDate, toDate);
            operatingDetails.LoanInterestAddedBack = await _cashFlowRepository.GetLoanInterestPaymentsAsync(companyId, fromDate, toDate);
            operatingDetails.DepreciationAddedBack = depreciation; // Add back depreciation (non-cash expense)

            // Investing Activities
            investingDetails.CapexAssetPurchases = await _cashFlowRepository.GetCapexAssetPurchasesAsync(companyId, fromDate, toDate);
            investingDetails.AssetDisposals = await _cashFlowRepository.GetAssetDisposalsAsync(companyId, fromDate, toDate);

            // Financing Activities
            financingDetails.LoanDisbursementsReceived = await _cashFlowRepository.GetLoanDisbursementsAsync(companyId, fromDate, toDate);
            financingDetails.LoanPrincipalRepayments = await _cashFlowRepository.GetLoanPrincipalRepaymentsAsync(companyId, fromDate, toDate);
            financingDetails.LoanInterestPayments = operatingDetails.LoanInterestAddedBack; // Same value, different classification

            // Calculate Operating Cash Flow (Indirect Method)
            // Start with Net Profit Before Tax
            // Add back non-cash items (depreciation, loan interest)
            var adjustmentsForNonCashItems = operatingDetails.DepreciationAddedBack + operatingDetails.LoanInterestAddedBack;
            var operatingCashBeforeWorkingCapital = netProfitBeforeTax + adjustmentsForNonCashItems;

            // Calculate changes in working capital
            var arAtStart = await _cashFlowRepository.GetAccountsReceivableAsync(companyId, fromDate ?? DateOnly.FromDateTime(DateTime.Today));
            var arAtEnd = await _cashFlowRepository.GetAccountsReceivableAsync(companyId, toDate ?? DateOnly.FromDateTime(DateTime.Today));
            var changeInAR = arAtEnd - arAtStart; // Increase in AR is negative for cash flow

            var apAtStart = await _cashFlowRepository.GetAccountsPayableAsync(companyId, fromDate ?? DateOnly.FromDateTime(DateTime.Today));
            var apAtEnd = await _cashFlowRepository.GetAccountsPayableAsync(companyId, toDate ?? DateOnly.FromDateTime(DateTime.Today));
            var changeInAP = apAtEnd - apAtStart; // Increase in AP is positive for cash flow

            var changesInWorkingCapital = changeInAP - changeInAR; // Net change

            // Direct method calculation (alternative approach)
            var cashFromOperatingDirect = operatingDetails.CashReceiptsFromCustomers
                - operatingDetails.CashPaidToEmployees
                - operatingDetails.CashPaidForSubscriptions
                - operatingDetails.CashPaidForOpexAssets
                - operatingDetails.CashPaidForMaintenance
                - operatingDetails.TdsPayments;

            // Use indirect method for AS-3 compliance
            var cashFromOperatingActivities = operatingCashBeforeWorkingCapital + changesInWorkingCapital;

            // Investing Activities
            var cashFromInvestingActivities = investingDetails.AssetDisposals - investingDetails.CapexAssetPurchases;

            // Financing Activities
            var cashFromFinancingActivities = financingDetails.LoanDisbursementsReceived
                - financingDetails.LoanPrincipalRepayments;

            // Net Cash Flow
            var netIncreaseDecreaseInCash = cashFromOperatingActivities
                + cashFromInvestingActivities
                + cashFromFinancingActivities;

            // Opening and Closing Cash Balance
            // For now, opening balance is 0 (can be enhanced with actual cash balance tracking)
            var openingCashBalance = 0m;
            var closingCashBalance = openingCashBalance + netIncreaseDecreaseInCash;

            var statement = new CashFlowStatementDto
            {
                NetProfitBeforeTax = netProfitBeforeTax,
                AdjustmentsForNonCashItems = adjustmentsForNonCashItems,
                OperatingCashBeforeWorkingCapital = operatingCashBeforeWorkingCapital,
                ChangesInWorkingCapital = changesInWorkingCapital,
                CashFromOperatingActivities = cashFromOperatingActivities,
                PurchaseOfFixedAssets = investingDetails.CapexAssetPurchases,
                SaleOfFixedAssets = investingDetails.AssetDisposals,
                CashFromInvestingActivities = cashFromInvestingActivities,
                LoanDisbursements = financingDetails.LoanDisbursementsReceived,
                LoanRepayments = financingDetails.LoanPrincipalRepayments,
                CashFromFinancingActivities = cashFromFinancingActivities,
                NetIncreaseDecreaseInCash = netIncreaseDecreaseInCash,
                OpeningCashBalance = openingCashBalance,
                ClosingCashBalance = closingCashBalance,
                OperatingDetails = operatingDetails,
                InvestingDetails = investingDetails,
                FinancingDetails = financingDetails
            };

            return Result<CashFlowStatementDto>.Success(statement);
        }
        catch (Exception ex)
        {
            return Error.Internal($"Failed to calculate cash flow statement: {ex.Message}");
        }
    }

    private static (DateOnly? fromDate, DateOnly? toDate) CalculateDateRange(int year, int? month)
    {
        DateOnly? fromDate = null;
        DateOnly? toDate = null;

        if (month.HasValue)
        {
            fromDate = new DateOnly(year, month.Value, 1);
            toDate = new DateOnly(year, month.Value, DateTime.DaysInMonth(year, month.Value));
        }
        else
        {
            fromDate = new DateOnly(year, 1, 1);
            toDate = new DateOnly(year, 12, 31);
        }

        return (fromDate, toDate);
    }

    private async Task<decimal> CalculateNetProfitBeforeTaxAsync(Guid? companyId, int year, int? month, decimal depreciation)
    {
        // Simplified net profit calculation
        // In production, this should use the actual P&L service or calculation
        
        // Get income from paid invoices
        var invoices = await _invoicesRepository.GetAllAsync();
        var filteredInvoices = invoices.Where(i =>
            i.Status?.ToLower() == "paid" &&
            (companyId == null || i.CompanyId == companyId) &&
            i.InvoiceDate.Year == year &&
            (!month.HasValue || i.InvoiceDate.Month == month.Value)
        );

        var totalIncome = filteredInvoices.Sum(i => i.TotalAmount);

        // Get expenses from salary transactions
        var salaryTransactions = await _salaryRepository.GetAllAsync();
        var filteredSalaries = salaryTransactions.Where(s =>
            (companyId == null || s.CompanyId == companyId) &&
            s.SalaryYear == year &&
            (!month.HasValue || s.SalaryMonth == month.Value)
        );

        var salaryExpense = filteredSalaries.Sum(s => s.GrossSalary);

        // Get loan interest expense
        var loans = await _loansRepository.GetAllAsync(companyId);
        var loanInterestExpense = 0m; // Simplified - would need loan transaction details

        // Calculate net profit
        var totalExpenses = salaryExpense + loanInterestExpense;
        var ebitda = totalIncome - totalExpenses;
        var netProfit = ebitda - depreciation;

        return netProfit;
    }
}




