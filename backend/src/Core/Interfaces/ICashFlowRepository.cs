namespace Core.Interfaces;

/// <summary>
/// Repository interface for cash flow data operations
/// </summary>
public interface ICashFlowRepository
{
    /// <summary>
    /// Get cash receipts from customers (paid invoices) within date range
    /// </summary>
    Task<decimal> GetCashReceiptsFromCustomersAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get cash paid to employees (salary payments) within date range
    /// </summary>
    Task<decimal> GetCashPaidToEmployeesAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get cash paid for subscriptions within date range
    /// </summary>
    Task<decimal> GetCashPaidForSubscriptionsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get cash paid for OPEX assets within date range
    /// </summary>
    Task<decimal> GetCashPaidForOpexAssetsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get cash paid for asset maintenance within date range
    /// </summary>
    Task<decimal> GetCashPaidForMaintenanceAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get TDS payments within date range
    /// </summary>
    Task<decimal> GetTdsPaymentsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get CAPEX asset purchases (cash outflow) within date range
    /// </summary>
    Task<decimal> GetCapexAssetPurchasesAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get asset disposals (cash inflow) within date range
    /// </summary>
    Task<decimal> GetAssetDisposalsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get loan disbursements (cash inflow) within date range
    /// </summary>
    Task<decimal> GetLoanDisbursementsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get loan principal repayments (cash outflow) within date range
    /// </summary>
    Task<decimal> GetLoanPrincipalRepaymentsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get loan interest payments (for operating activities adjustment) within date range
    /// </summary>
    Task<decimal> GetLoanInterestPaymentsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate);
    
    /// <summary>
    /// Get accounts receivable (unpaid invoices) as of date
    /// </summary>
    Task<decimal> GetAccountsReceivableAsync(Guid? companyId, DateOnly asOfDate);
    
    /// <summary>
    /// Get accounts payable (unpaid expenses) as of date - placeholder for future
    /// </summary>
    Task<decimal> GetAccountsPayableAsync(Guid? companyId, DateOnly asOfDate);
}





