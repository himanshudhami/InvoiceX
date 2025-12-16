namespace Application.DTOs.CashFlow;

/// <summary>
/// Cash Flow Statement DTO compliant with Indian Accounting Standard 3 (AS-3)
/// </summary>
public class CashFlowStatementDto
{
    // Operating Activities
    public decimal NetProfitBeforeTax { get; set; }
    public decimal AdjustmentsForNonCashItems { get; set; }
    public decimal OperatingCashBeforeWorkingCapital { get; set; }
    public decimal ChangesInWorkingCapital { get; set; }
    public decimal CashFromOperatingActivities { get; set; }
    
    // Investing Activities
    public decimal PurchaseOfFixedAssets { get; set; }
    public decimal SaleOfFixedAssets { get; set; }
    public decimal CashFromInvestingActivities { get; set; }
    
    // Financing Activities
    public decimal LoanDisbursements { get; set; }
    public decimal LoanRepayments { get; set; }
    public decimal CashFromFinancingActivities { get; set; }
    
    // Net Cash Flow
    public decimal NetIncreaseDecreaseInCash { get; set; }
    public decimal OpeningCashBalance { get; set; }
    public decimal ClosingCashBalance { get; set; }
    
    // Detailed breakdowns for transparency
    public OperatingActivitiesDetail OperatingDetails { get; set; } = new();
    public InvestingActivitiesDetail InvestingDetails { get; set; } = new();
    public FinancingActivitiesDetail FinancingDetails { get; set; } = new();
}

public class OperatingActivitiesDetail
{
    public decimal CashReceiptsFromCustomers { get; set; }
    public decimal CashPaidToEmployees { get; set; }
    public decimal CashPaidForSubscriptions { get; set; }
    public decimal CashPaidForOpexAssets { get; set; }
    public decimal CashPaidForMaintenance { get; set; }
    public decimal TdsPayments { get; set; }
    public decimal DepreciationAddedBack { get; set; }
    public decimal LoanInterestAddedBack { get; set; }
}

public class InvestingActivitiesDetail
{
    public decimal CapexAssetPurchases { get; set; }
    public decimal AssetDisposals { get; set; }
}

public class FinancingActivitiesDetail
{
    public decimal LoanDisbursementsReceived { get; set; }
    public decimal LoanPrincipalRepayments { get; set; }
    public decimal LoanInterestPayments { get; set; }
}



