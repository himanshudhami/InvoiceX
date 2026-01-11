using Application.Interfaces.Ledger;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;

namespace Application.Services.Ledger
{
    /// <summary>
    /// Service for generating trial balance and financial statements
    /// Follows SOLID principles - SQL queries are in the repository
    /// </summary>
    public class TrialBalanceService : ITrialBalanceService
    {
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly ILedgerReportRepository _reportRepository;
        private readonly ILogger<TrialBalanceService> _logger;

        public TrialBalanceService(
            IChartOfAccountRepository accountRepository,
            ILedgerReportRepository reportRepository,
            ILogger<TrialBalanceService> logger)
        {
            _accountRepository = accountRepository;
            _reportRepository = reportRepository;
            _logger = logger;
        }

        public async Task<TrialBalanceReport> GetTrialBalanceAsync(
            Guid companyId,
            DateOnly asOfDate,
            bool includeZeroBalances = false)
        {
            var financialYear = GetFinancialYear(asOfDate);
            var data = await _reportRepository.GetTrialBalanceDataAsync(companyId, asOfDate, includeZeroBalances);

            var rows = data.Select(d => new TrialBalanceRow
            {
                AccountId = d.AccountId,
                AccountCode = d.AccountCode,
                AccountName = d.AccountName,
                AccountType = d.AccountType,
                DepthLevel = d.DepthLevel,
                OpeningBalance = d.OpeningBalance,
                Debits = d.Debits,
                Credits = d.Credits,
                ClosingBalance = d.ClosingBalance
            }).ToList();

            var report = new TrialBalanceReport
            {
                CompanyId = companyId,
                AsOfDate = asOfDate,
                FinancialYear = financialYear,
                Rows = rows,
                TotalDebits = rows.Sum(r => r.DebitBalance),
                TotalCredits = rows.Sum(r => r.CreditBalance)
            };

            _logger.LogInformation(
                "Generated trial balance for company {CompanyId} as of {AsOfDate}. Balanced: {IsBalanced}",
                companyId, asOfDate, report.IsBalanced);

            return report;
        }

        public async Task<TrialBalanceReport> GetTrialBalanceByPeriodAsync(
            Guid companyId,
            string financialYear,
            int? periodMonth = null)
        {
            var (startYear, _) = ParseFinancialYear(financialYear);
            DateOnly asOfDate;

            if (periodMonth.HasValue)
            {
                var calendarMonth = periodMonth.Value >= 10 ? periodMonth.Value - 9 : periodMonth.Value + 3;
                var year = calendarMonth <= 3 ? startYear + 1 : startYear;
                asOfDate = new DateOnly(year, calendarMonth, DateTime.DaysInMonth(year, calendarMonth));
            }
            else
            {
                asOfDate = new DateOnly(startYear + 1, 3, 31);
            }

            return await GetTrialBalanceAsync(companyId, asOfDate);
        }

        public async Task<AccountLedgerReport> GetAccountLedgerAsync(
            Guid accountId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new InvalidOperationException($"Account {accountId} not found");
            }

            var openingBalance = await _reportRepository.GetAccountOpeningBalanceAsync(
                accountId, fromDate, account.OpeningBalance, account.NormalBalance);

            var data = await _reportRepository.GetAccountLedgerDataAsync(accountId, fromDate, toDate);

            var entries = data.Select(d => new AccountLedgerEntry
            {
                Date = d.Date,
                JournalNumber = d.JournalNumber,
                JournalEntryId = d.JournalEntryId,
                Description = d.Description,
                Debit = d.Debit,
                Credit = d.Credit
            }).ToList();

            // Calculate running balance
            var runningBalance = openingBalance;
            var isDebitNormal = account.NormalBalance == "debit";

            foreach (var entry in entries)
            {
                if (isDebitNormal)
                {
                    runningBalance += entry.Debit - entry.Credit;
                }
                else
                {
                    runningBalance += entry.Credit - entry.Debit;
                }
                entry.RunningBalance = runningBalance;
            }

            return new AccountLedgerReport
            {
                AccountId = accountId,
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                FromDate = fromDate,
                ToDate = toDate,
                OpeningBalance = openingBalance,
                Entries = entries,
                ClosingBalance = runningBalance
            };
        }

        public async Task<IncomeStatementReport> GetIncomeStatementAsync(
            Guid companyId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            var data = await _reportRepository.GetIncomeExpenseDataAsync(companyId, fromDate, toDate);

            var incomeSections = new List<IncomeStatementSection>();
            var expenseSections = new List<IncomeStatementSection>();

            var incomeData = data.Where(d => d.AccountType == "income").ToList();
            var expenseData = data.Where(d => d.AccountType == "expense").ToList();

            // Group by subtype for income
            foreach (var group in incomeData.GroupBy(d => d.AccountSubtype ?? "Other Income"))
            {
                incomeSections.Add(new IncomeStatementSection
                {
                    SectionName = group.Key,
                    Rows = group.Select(d => new IncomeStatementRow
                    {
                        AccountId = d.AccountId,
                        AccountCode = d.AccountCode,
                        AccountName = d.AccountName,
                        Amount = d.Amount
                    }).ToList(),
                    SectionTotal = group.Sum(d => d.Amount)
                });
            }

            // Group by subtype for expenses
            foreach (var group in expenseData.GroupBy(d => d.AccountSubtype ?? "Other Expenses"))
            {
                expenseSections.Add(new IncomeStatementSection
                {
                    SectionName = group.Key,
                    Rows = group.Select(d => new IncomeStatementRow
                    {
                        AccountId = d.AccountId,
                        AccountCode = d.AccountCode,
                        AccountName = d.AccountName,
                        Amount = d.Amount
                    }).ToList(),
                    SectionTotal = group.Sum(d => d.Amount)
                });
            }

            return new IncomeStatementReport
            {
                CompanyId = companyId,
                FromDate = fromDate,
                ToDate = toDate,
                IncomeSections = incomeSections,
                ExpenseSections = expenseSections,
                TotalIncome = incomeSections.Sum(s => s.SectionTotal),
                TotalExpenses = expenseSections.Sum(s => s.SectionTotal)
            };
        }

        public async Task<BalanceSheetReport> GetBalanceSheetAsync(
            Guid companyId,
            DateOnly asOfDate)
        {
            var data = await _reportRepository.GetBalanceSheetDataAsync(companyId, asOfDate);

            var assetSections = BuildBalanceSheetSections(data.Where(d => d.AccountType == "asset"));
            var liabilitySections = BuildBalanceSheetSections(data.Where(d => d.AccountType == "liability"));
            var equitySections = BuildBalanceSheetSections(data.Where(d => d.AccountType == "equity"));

            return new BalanceSheetReport
            {
                CompanyId = companyId,
                AsOfDate = asOfDate,
                AssetSections = assetSections,
                LiabilitySections = liabilitySections,
                EquitySections = equitySections,
                TotalAssets = assetSections.Sum(s => s.SectionTotal),
                TotalLiabilities = liabilitySections.Sum(s => s.SectionTotal),
                TotalEquity = equitySections.Sum(s => s.SectionTotal)
            };
        }

        public async Task RecalculatePeriodBalancesAsync(Guid companyId, string financialYear)
        {
            _logger.LogInformation(
                "Recalculating period balances for company {CompanyId}, FY {FinancialYear}",
                companyId, financialYear);

            await _reportRepository.RecalculatePeriodBalancesAsync(companyId, financialYear);

            _logger.LogInformation("Period balances recalculated successfully");
        }

        // ==================== Helper Methods ====================

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static (int StartYear, int EndYear) ParseFinancialYear(string financialYear)
        {
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = parts[1].Length == 2
                ? startYear / 100 * 100 + int.Parse(parts[1])
                : int.Parse(parts[1]);
            return (startYear, endYear);
        }

        private static List<BalanceSheetSection> BuildBalanceSheetSections(IEnumerable<BalanceSheetData> data)
        {
            var sections = new List<BalanceSheetSection>();

            foreach (var group in data.GroupBy(d => d.AccountSubtype ?? "Other"))
            {
                sections.Add(new BalanceSheetSection
                {
                    SectionName = group.Key,
                    Rows = group.Select(d => new BalanceSheetRow
                    {
                        AccountId = d.AccountId,
                        AccountCode = d.AccountCode,
                        AccountName = d.AccountName,
                        Amount = d.Amount
                    }).ToList(),
                    SectionTotal = group.Sum(d => d.Amount)
                });
            }

            return sections;
        }

        public async Task<AbnormalBalanceReport> GetAbnormalBalancesAsync(Guid companyId)
        {
            _logger.LogInformation("Generating abnormal balance report for company {CompanyId}", companyId);

            var data = await _reportRepository.GetAbnormalBalancesAsync(companyId);
            var summary = await _reportRepository.GetAbnormalBalanceSummaryAsync(companyId);

            var items = data.Select(d => new AbnormalBalanceItem
            {
                AccountId = d.AccountId,
                AccountCode = d.AccountCode,
                AccountName = d.AccountName,
                AccountType = d.AccountType,
                AccountSubtype = d.AccountSubtype,
                ExpectedBalanceSide = d.NormalBalance,
                ActualBalanceSide = d.ActualBalanceSide,
                Amount = d.Amount,
                Category = d.Category,
                PossibleReason = d.PossibleReason,
                RecommendedAction = d.RecommendedAction,
                IsContraAccount = d.IsContraAccount
            }).ToList();

            var categorySummary = summary.Categories.Select(c => new AbnormalBalanceCategorySummary
            {
                CategoryName = c.CategoryName,
                Count = c.Count,
                TotalAmount = c.TotalAmount,
                Severity = c.Severity
            }).ToList();

            var report = new AbnormalBalanceReport
            {
                CompanyId = companyId,
                GeneratedAt = DateTime.UtcNow,
                TotalAbnormalAccounts = summary.TotalAbnormalAccounts,
                ActionableIssues = summary.TotalAbnormalAccounts - summary.ContraAccounts,
                TotalAbnormalAmount = summary.TotalAbnormalAmount,
                Items = items,
                CategorySummary = categorySummary
            };

            _logger.LogInformation(
                "Abnormal balance report: {TotalAccounts} accounts, {ActionableIssues} actionable issues, {Amount:C} total",
                report.TotalAbnormalAccounts, report.ActionableIssues, report.TotalAbnormalAmount);

            return report;
        }

        public async Task<AbnormalBalanceAlertSummary> GetAbnormalBalanceAlertAsync(Guid companyId)
        {
            var summary = await _reportRepository.GetAbnormalBalanceSummaryAsync(companyId);

            var actionableIssues = summary.TotalAbnormalAccounts - summary.ContraAccounts;
            var hasIssues = actionableIssues > 0;

            string alertMessage;
            string alertSeverity;

            if (!hasIssues)
            {
                alertMessage = "No abnormal balances detected";
                alertSeverity = "success";
            }
            else if (actionableIssues <= 3)
            {
                alertMessage = $"{actionableIssues} account(s) with abnormal balances need review";
                alertSeverity = "warning";
            }
            else
            {
                alertMessage = $"{actionableIssues} accounts with abnormal balances - data quality review recommended";
                alertSeverity = "error";
            }

            return new AbnormalBalanceAlertSummary
            {
                CompanyId = companyId,
                HasIssues = hasIssues,
                TotalIssues = actionableIssues,
                CriticalIssues = summary.LiabilitiesWithDebit + summary.AssetsWithCredit,
                TotalAmount = summary.TotalAbnormalAmount,
                AlertMessage = alertMessage,
                AlertSeverity = alertSeverity,
                TopCategories = summary.Categories
                    .Where(c => c.Severity == "warning")
                    .Take(3)
                    .Select(c => new AbnormalBalanceCategorySummary
                    {
                        CategoryName = c.CategoryName,
                        Count = c.Count,
                        TotalAmount = c.TotalAmount,
                        Severity = c.Severity
                    }).ToList()
            };
        }
    }
}
