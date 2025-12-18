using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data;

/// <summary>
/// Repository for cash flow data operations
/// </summary>
public class CashFlowRepository : ICashFlowRepository
{
    private readonly string _connectionString;

    public CashFlowRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<decimal> GetCashReceiptsFromCustomersAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(p.amount), 0)
            FROM payments p
            INNER JOIN invoices i ON p.invoice_id = i.id
            WHERE i.status = 'paid'
                AND (@companyId IS NULL OR i.company_id = @companyId)
                AND (@fromDate IS NULL OR p.payment_date >= @fromDate)
                AND (@toDate IS NULL OR p.payment_date <= @toDate)";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetCashPaidToEmployeesAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Combine employee salaries from payroll_transactions and contractor payments
        var sql = @"
            SELECT COALESCE(SUM(net_payable), 0)
            FROM (
                SELECT pt.net_payable, pt.payment_date, pr.company_id
                FROM payroll_transactions pt
                INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id
                WHERE pt.status = 'paid'
                    AND pt.payment_date IS NOT NULL
                    AND pt.payroll_type = 'employee'
                    AND (@companyId IS NULL OR pr.company_id = @companyId)
                    AND (@fromDate IS NULL OR pt.payment_date >= @fromDate)
                    AND (@toDate IS NULL OR pt.payment_date <= @toDate)
                
                UNION ALL
                
                SELECT net_payable, payment_date, company_id
                FROM contractor_payments
                WHERE status = 'paid'
                    AND payment_date IS NOT NULL
                    AND (@companyId IS NULL OR company_id = @companyId)
                    AND (@fromDate IS NULL OR payment_date >= @fromDate)
                    AND (@toDate IS NULL OR payment_date <= @toDate)
            ) AS all_payments";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetCashPaidForSubscriptionsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        // Subscriptions are typically monthly expenses - we'll use subscription_monthly_expenses view if available
        // For now, return 0 as subscriptions may not have explicit payment dates
        // This can be enhanced when subscription payment tracking is added
        return 0;
    }

    public async Task<decimal> GetCashPaidForOpexAssetsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(purchase_cost), 0)
            FROM assets
            WHERE purchase_type = 'opex'
                AND purchase_date IS NOT NULL
                AND (@companyId IS NULL OR company_id = @companyId)
                AND (@fromDate IS NULL OR purchase_date >= @fromDate)
                AND (@toDate IS NULL OR purchase_date <= @toDate)";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetCashPaidForMaintenanceAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        // Maintenance costs are typically tracked in asset_cost_report
        // For cash flow, we need actual payment dates which may not be tracked
        // Return 0 for now - can be enhanced when maintenance payment tracking is added
        return 0;
    }

    public async Task<decimal> GetTdsPaymentsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Combine TDS from payroll_transactions and contractor_payments
        var sql = @"
            SELECT COALESCE(SUM(tds_amount), 0)
            FROM (
                SELECT pt.tds_deducted AS tds_amount, pt.payment_date, pr.company_id
                FROM payroll_transactions pt
                INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id
                WHERE pt.status = 'paid'
                    AND pt.payment_date IS NOT NULL
                    AND pt.tds_deducted > 0
                    AND (@companyId IS NULL OR pr.company_id = @companyId)
                    AND (@fromDate IS NULL OR pt.payment_date >= @fromDate)
                    AND (@toDate IS NULL OR pt.payment_date <= @toDate)
                
                UNION ALL
                
                SELECT tds_amount, payment_date, company_id
                FROM contractor_payments
                WHERE status = 'paid'
                    AND payment_date IS NOT NULL
                    AND tds_amount > 0
                    AND (@companyId IS NULL OR company_id = @companyId)
                    AND (@fromDate IS NULL OR payment_date >= @fromDate)
                    AND (@toDate IS NULL OR payment_date <= @toDate)
            ) AS all_tds";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetCapexAssetPurchasesAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(purchase_cost), 0)
            FROM assets
            WHERE purchase_type = 'capex'
                AND purchase_date IS NOT NULL
                AND (@companyId IS NULL OR company_id = @companyId)
                AND (@fromDate IS NULL OR purchase_date >= @fromDate)
                AND (@toDate IS NULL OR purchase_date <= @toDate)";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetAssetDisposalsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        // Asset disposals may be tracked in asset_disposals table
        // For now, return 0 - can be enhanced when disposal tracking is added
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(disposal_amount), 0)
            FROM asset_disposals
            WHERE disposal_date IS NOT NULL
                AND (@companyId IS NULL OR EXISTS (
                    SELECT 1 FROM assets a WHERE a.id = asset_disposals.asset_id AND a.company_id = @companyId
                ))
                AND (@fromDate IS NULL OR disposal_date >= @fromDate)
                AND (@toDate IS NULL OR disposal_date <= @toDate)";

        try
        {
            return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
        }
        catch
        {
            // Table may not exist, return 0
            return 0;
        }
    }

    public async Task<decimal> GetLoanDisbursementsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(amount), 0)
            FROM loan_transactions
            WHERE transaction_type = 'disbursement'
                AND (@companyId IS NULL OR EXISTS (
                    SELECT 1 FROM loans l WHERE l.id = loan_transactions.loan_id AND l.company_id = @companyId
                ))
                AND (@fromDate IS NULL OR transaction_date >= @fromDate)
                AND (@toDate IS NULL OR transaction_date <= @toDate)";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetLoanPrincipalRepaymentsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(principal_amount), 0)
            FROM loan_transactions
            WHERE transaction_type IN ('emi_payment', 'prepayment', 'foreclosure')
                AND principal_amount > 0
                AND (@companyId IS NULL OR EXISTS (
                    SELECT 1 FROM loans l WHERE l.id = loan_transactions.loan_id AND l.company_id = @companyId
                ))
                AND (@fromDate IS NULL OR transaction_date >= @fromDate)
                AND (@toDate IS NULL OR transaction_date <= @toDate)";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetLoanInterestPaymentsAsync(Guid? companyId, DateOnly? fromDate, DateOnly? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(interest_amount), 0)
            FROM loan_transactions
            WHERE transaction_type IN ('emi_payment', 'interest_accrual')
                AND interest_amount > 0
                AND (@companyId IS NULL OR EXISTS (
                    SELECT 1 FROM loans l WHERE l.id = loan_transactions.loan_id AND l.company_id = @companyId
                ))
                AND (@fromDate IS NULL OR transaction_date >= @fromDate)
                AND (@toDate IS NULL OR transaction_date <= @toDate)";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, fromDate, toDate });
    }

    public async Task<decimal> GetAccountsReceivableAsync(Guid? companyId, DateOnly asOfDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var sql = @"
            SELECT COALESCE(SUM(total_amount - COALESCE(paid_amount, 0)), 0)
            FROM invoices
            WHERE status IN ('sent', 'viewed', 'overdue')
                AND (@companyId IS NULL OR company_id = @companyId)
                AND invoice_date <= @asOfDate";

        return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { companyId, asOfDate });
    }

    public async Task<decimal> GetAccountsPayableAsync(Guid? companyId, DateOnly asOfDate)
    {
        // Accounts payable tracking not fully implemented yet
        // Return 0 for now - can be enhanced when payable tracking is added
        return 0;
    }
}






