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
                    coa.opening_balance +
                        CASE WHEN coa.normal_balance = 'debit'
                            THEN COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0)
                            ELSE COALESCE(SUM(jel.credit_amount), 0) - COALESCE(SUM(jel.debit_amount), 0)
                        END as closing_balance
                FROM chart_of_accounts coa
                LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
                LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id
                    AND je.status = 'posted'
                    AND je.journal_date <= @asOfDate
                WHERE coa.company_id = @companyId
                    AND coa.is_active = TRUE
                GROUP BY coa.id, coa.account_code, coa.account_name, coa.account_type,
                         coa.depth_level, coa.opening_balance, coa.normal_balance
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
    }
}
