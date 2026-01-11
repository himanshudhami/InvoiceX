using Core.Interfaces.Ledger;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Ledger
{
    /// <summary>
    /// Repository implementation for ledger reporting queries
    /// All complex SQL for financial reports is centralized here
    /// </summary>
    public class LedgerReportRepository : ILedgerReportRepository
    {
        private readonly string _connectionString;

        public LedgerReportRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<TrialBalanceData>> GetTrialBalanceDataAsync(
            Guid companyId,
            DateOnly asOfDate,
            bool includeZeroBalances = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT
                    coa.id as account_id,
                    coa.account_code,
                    coa.account_name,
                    coa.account_type,
                    coa.depth_level,
                    coa.opening_balance,
                    COALESCE(SUM(jel.debit_amount), 0) as debits,
                    COALESCE(SUM(jel.credit_amount), 0) as credits,
                    coa.opening_balance + COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0) as closing_balance,
                    coa.is_control_account,
                    coa.control_account_type
                FROM chart_of_accounts coa
                LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
                LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id
                    AND je.status = 'posted'
                    AND je.journal_date <= @asOfDate
                WHERE coa.company_id = @companyId
                    AND coa.is_active = TRUE
                GROUP BY coa.id, coa.account_code, coa.account_name, coa.account_type,
                         coa.depth_level, coa.opening_balance, coa.normal_balance,
                         coa.is_control_account, coa.control_account_type
                HAVING @includeZeroBalances = TRUE OR (
                    coa.opening_balance != 0 OR
                    COALESCE(SUM(jel.debit_amount), 0) != 0 OR
                    COALESCE(SUM(jel.credit_amount), 0) != 0
                )
                ORDER BY coa.account_code";

            return await connection.QueryAsync<TrialBalanceData>(
                sql, new { companyId, asOfDate, includeZeroBalances });
        }

        public async Task<decimal> GetAccountOpeningBalanceAsync(
            Guid accountId,
            DateOnly beforeDate,
            decimal initialOpeningBalance,
            string normalBalance)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT
                    @initialOpeningBalance +
                    CASE WHEN @normalBalance = 'debit'
                        THEN COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0)
                        ELSE COALESCE(SUM(jel.credit_amount), 0) - COALESCE(SUM(jel.debit_amount), 0)
                    END
                FROM journal_entry_lines jel
                JOIN journal_entries je ON je.id = jel.journal_entry_id
                WHERE jel.account_id = @accountId
                    AND je.status = 'posted'
                    AND je.journal_date < @beforeDate";

            return await connection.ExecuteScalarAsync<decimal>(
                sql, new { accountId, beforeDate, initialOpeningBalance, normalBalance });
        }

        public async Task<IEnumerable<AccountLedgerData>> GetAccountLedgerDataAsync(
            Guid accountId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT
                    je.journal_date as date,
                    je.journal_number,
                    je.id as journal_entry_id,
                    je.description,
                    jel.debit_amount as debit,
                    jel.credit_amount as credit
                FROM journal_entry_lines jel
                JOIN journal_entries je ON je.id = jel.journal_entry_id
                WHERE jel.account_id = @accountId
                    AND je.status = 'posted'
                    AND je.journal_date >= @fromDate
                    AND je.journal_date <= @toDate
                ORDER BY je.journal_date, je.journal_number";

            return await connection.QueryAsync<AccountLedgerData>(
                sql, new { accountId, fromDate, toDate });
        }

        public async Task<IEnumerable<IncomeExpenseData>> GetIncomeExpenseDataAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT
                    coa.id as account_id,
                    coa.account_code,
                    coa.account_name,
                    coa.account_type,
                    coa.account_subtype,
                    CASE WHEN coa.normal_balance = 'credit'
                        THEN COALESCE(SUM(jel.credit_amount), 0) - COALESCE(SUM(jel.debit_amount), 0)
                        ELSE COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0)
                    END as amount
                FROM chart_of_accounts coa
                LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
                LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id
                    AND je.status = 'posted'
                    AND je.journal_date >= @fromDate
                    AND je.journal_date <= @toDate
                WHERE coa.company_id = @companyId
                    AND coa.account_type IN ('income', 'expense')
                    AND coa.is_active = TRUE
                GROUP BY coa.id, coa.account_code, coa.account_name,
                         coa.account_type, coa.account_subtype, coa.normal_balance
                HAVING COALESCE(SUM(jel.debit_amount), 0) != 0 OR COALESCE(SUM(jel.credit_amount), 0) != 0
                ORDER BY coa.account_type DESC, coa.account_code";

            return await connection.QueryAsync<IncomeExpenseData>(
                sql, new { companyId, fromDate, toDate });
        }

        public async Task<IEnumerable<BalanceSheetData>> GetBalanceSheetDataAsync(
            Guid companyId,
            DateOnly asOfDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT
                    coa.id as account_id,
                    coa.account_code,
                    coa.account_name,
                    coa.account_type,
                    coa.account_subtype,
                    coa.opening_balance +
                        CASE WHEN coa.normal_balance = 'debit'
                            THEN COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0)
                            ELSE COALESCE(SUM(jel.credit_amount), 0) - COALESCE(SUM(jel.debit_amount), 0)
                        END as amount
                FROM chart_of_accounts coa
                LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
                LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id
                    AND je.status = 'posted'
                    AND je.journal_date <= @asOfDate
                WHERE coa.company_id = @companyId
                    AND coa.account_type IN ('asset', 'liability', 'equity')
                    AND coa.is_active = TRUE
                GROUP BY coa.id, coa.account_code, coa.account_name,
                         coa.account_type, coa.account_subtype, coa.normal_balance,
                         coa.opening_balance
                HAVING coa.opening_balance +
                    CASE WHEN coa.normal_balance = 'debit'
                        THEN COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0)
                        ELSE COALESCE(SUM(jel.credit_amount), 0) - COALESCE(SUM(jel.debit_amount), 0)
                    END != 0
                ORDER BY coa.account_type, coa.account_code";

            return await connection.QueryAsync<BalanceSheetData>(
                sql, new { companyId, asOfDate });
        }

        public async Task RecalculatePeriodBalancesAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Delete existing period balances
            await connection.ExecuteAsync(
                @"DELETE FROM account_period_balances
                  WHERE company_id = @companyId AND financial_year = @financialYear",
                new { companyId, financialYear });

            // Recalculate from journal entries
            var sql = @"
                INSERT INTO account_period_balances (
                    company_id, account_id, financial_year, period_month,
                    opening_balance, total_debits, total_credits,
                    net_movement, closing_balance, last_updated_at
                )
                SELECT
                    @companyId,
                    coa.id,
                    @financialYear,
                    je.period_month,
                    0,
                    COALESCE(SUM(jel.debit_amount), 0),
                    COALESCE(SUM(jel.credit_amount), 0),
                    CASE WHEN coa.normal_balance = 'debit'
                        THEN COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0)
                        ELSE COALESCE(SUM(jel.credit_amount), 0) - COALESCE(SUM(jel.debit_amount), 0)
                    END,
                    0,
                    NOW()
                FROM chart_of_accounts coa
                JOIN journal_entry_lines jel ON jel.account_id = coa.id
                JOIN journal_entries je ON je.id = jel.journal_entry_id
                WHERE coa.company_id = @companyId
                    AND je.financial_year = @financialYear
                    AND je.status = 'posted'
                GROUP BY coa.id, coa.normal_balance, je.period_month";

            await connection.ExecuteAsync(sql, new { companyId, financialYear });
        }

        public async Task<IEnumerable<AbnormalBalanceData>> GetAbnormalBalancesAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                WITH account_balances AS (
                    SELECT
                        coa.id as account_id,
                        coa.account_code,
                        coa.account_name,
                        coa.account_type,
                        coa.account_subtype,
                        coa.normal_balance,
                        coa.opening_balance,
                        COALESCE(SUM(jel.debit_amount), 0) as total_debit,
                        COALESCE(SUM(jel.credit_amount), 0) as total_credit,
                        -- Net balance: (debits - credits) - opening_balance (Tally convention)
                        COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0) - coa.opening_balance as net_balance
                    FROM chart_of_accounts coa
                    LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
                    LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id
                        AND je.status = 'posted'
                    WHERE coa.company_id = @companyId
                        AND coa.is_active = TRUE
                    GROUP BY coa.id, coa.account_code, coa.account_name, coa.account_type,
                             coa.account_subtype, coa.normal_balance, coa.opening_balance
                )
                SELECT
                    account_id,
                    account_code,
                    account_name,
                    account_type,
                    account_subtype,
                    normal_balance,
                    CASE WHEN net_balance > 0 THEN 'debit' ELSE 'credit' END as actual_balance_side,
                    ABS(net_balance) as amount,
                    CASE
                        WHEN account_name ILIKE '%depreciation%' THEN 'Contra Account'
                        WHEN account_type = 'liability' AND net_balance > 0 THEN 'Liability with Debit Balance'
                        WHEN account_type = 'asset' AND net_balance < 0 THEN 'Asset with Credit Balance'
                        WHEN account_type = 'equity' AND net_balance > 0 THEN 'Equity with Debit Balance'
                        WHEN account_type = 'income' AND net_balance > 0 THEN 'Income with Debit Balance'
                        WHEN account_type = 'expense' AND net_balance < 0 THEN 'Expense with Credit Balance'
                        ELSE 'Other'
                    END as category,
                    CASE
                        WHEN account_name ILIKE '%depreciation%' THEN 'Normal for contra-asset accounts'
                        WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%payable%' THEN 'Advance paid or overpayment to vendor'
                        WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%loan%' THEN 'Loan given (not received) or overpayment'
                        WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%salary%' THEN 'Salary advance paid to employee'
                        WHEN account_type = 'liability' AND net_balance > 0 THEN 'Possible advance payment or data entry error'
                        WHEN account_type = 'asset' AND net_balance < 0 THEN 'Overdrawn or liability misclassified as asset'
                        WHEN account_type = 'income' AND net_balance > 0 THEN 'Sales return or reversal'
                        WHEN account_type = 'expense' AND net_balance < 0 THEN 'Expense refund or reversal'
                        ELSE 'Review required'
                    END as possible_reason,
                    CASE
                        WHEN account_name ILIKE '%depreciation%' THEN 'No action needed - this is correct'
                        WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%payable%' THEN 'Reclassify to Advance to Vendors (Asset)'
                        WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%loan%director%' THEN 'Verify if loan given TO director, reclassify to Loans & Advances'
                        WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%loan%' THEN 'Reclassify to Loans & Advances (Asset)'
                        WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%salary%' THEN 'Reclassify to Salary Advance (Asset)'
                        WHEN account_type = 'liability' AND net_balance > 0 THEN 'Review and reclassify or correct entry'
                        WHEN account_type = 'asset' AND net_balance < 0 THEN 'Review - may need reclassification to liability'
                        ELSE 'Review with accountant'
                    END as recommended_action,
                    CASE WHEN account_name ILIKE '%depreciation%' THEN TRUE ELSE FALSE END as is_contra_account
                FROM account_balances
                WHERE net_balance != 0
                    AND (
                        -- Liability/Income/Equity with debit balance (net > 0)
                        (account_type IN ('liability', 'income', 'equity') AND net_balance > 0)
                        OR
                        -- Asset/Expense with credit balance (net < 0)
                        (account_type IN ('asset', 'expense') AND net_balance < 0)
                    )
                ORDER BY
                    CASE WHEN account_name ILIKE '%depreciation%' THEN 1 ELSE 0 END,
                    ABS(net_balance) DESC";

            return await connection.QueryAsync<AbnormalBalanceData>(sql, new { companyId });
        }

        public async Task<AbnormalBalanceSummary> GetAbnormalBalanceSummaryAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                WITH account_balances AS (
                    SELECT
                        coa.account_type,
                        coa.account_name,
                        coa.opening_balance,
                        COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0) - coa.opening_balance as net_balance
                    FROM chart_of_accounts coa
                    LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
                    LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id
                        AND je.status = 'posted'
                    WHERE coa.company_id = @companyId
                        AND coa.is_active = TRUE
                    GROUP BY coa.id, coa.account_type, coa.account_name, coa.opening_balance
                ),
                abnormal AS (
                    SELECT
                        account_type,
                        account_name,
                        net_balance,
                        CASE WHEN account_name ILIKE '%depreciation%' THEN TRUE ELSE FALSE END as is_contra
                    FROM account_balances
                    WHERE net_balance != 0
                        AND (
                            (account_type IN ('liability', 'income', 'equity') AND net_balance > 0)
                            OR
                            (account_type IN ('asset', 'expense') AND net_balance < 0)
                        )
                )
                SELECT
                    COUNT(*) as total_abnormal_accounts,
                    COUNT(*) FILTER (WHERE account_type = 'liability' AND net_balance > 0 AND NOT is_contra) as liabilities_with_debit,
                    COUNT(*) FILTER (WHERE account_type = 'asset' AND net_balance < 0 AND NOT is_contra) as assets_with_credit,
                    COUNT(*) FILTER (WHERE is_contra) as contra_accounts,
                    COALESCE(SUM(ABS(net_balance)) FILTER (WHERE NOT is_contra), 0) as total_abnormal_amount
                FROM abnormal";

            var summary = await connection.QueryFirstAsync<AbnormalBalanceSummary>(sql, new { companyId });

            // Get category breakdown
            var categorySql = @"
                WITH account_balances AS (
                    SELECT
                        coa.account_type,
                        coa.account_name,
                        coa.opening_balance,
                        COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0) - coa.opening_balance as net_balance
                    FROM chart_of_accounts coa
                    LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
                    LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id
                        AND je.status = 'posted'
                    WHERE coa.company_id = @companyId
                        AND coa.is_active = TRUE
                    GROUP BY coa.id, coa.account_type, coa.account_name, coa.opening_balance
                ),
                abnormal AS (
                    SELECT
                        CASE
                            WHEN account_name ILIKE '%depreciation%' THEN 'Contra Accounts (OK)'
                            WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%payable%' THEN 'Vendor Advances/Overpayments'
                            WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%loan%' THEN 'Loans Given (Misclassified)'
                            WHEN account_type = 'liability' AND net_balance > 0 AND account_name ILIKE '%salary%' THEN 'Salary Advances'
                            WHEN account_type = 'liability' AND net_balance > 0 THEN 'Other Liability Issues'
                            WHEN account_type = 'asset' AND net_balance < 0 THEN 'Asset Overdrafts'
                            ELSE 'Other'
                        END as category_name,
                        ABS(net_balance) as amount,
                        CASE WHEN account_name ILIKE '%depreciation%' THEN 'info' ELSE 'warning' END as severity
                    FROM account_balances
                    WHERE net_balance != 0
                        AND (
                            (account_type IN ('liability', 'income', 'equity') AND net_balance > 0)
                            OR
                            (account_type IN ('asset', 'expense') AND net_balance < 0)
                        )
                )
                SELECT
                    category_name,
                    COUNT(*) as count,
                    SUM(amount) as total_amount,
                    MAX(severity) as severity
                FROM abnormal
                GROUP BY category_name
                ORDER BY
                    CASE WHEN category_name = 'Contra Accounts (OK)' THEN 1 ELSE 0 END,
                    SUM(amount) DESC";

            summary.Categories = (await connection.QueryAsync<AbnormalBalanceCategory>(categorySql, new { companyId })).ToList();

            return summary;
        }
    }
}
