using Application.Interfaces.Ledger;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;

namespace Application.Services.Ledger
{
    /// <summary>
    /// Service for subledger reports - party-wise breakdown of control accounts.
    /// Part of COA Modernization (Task 16).
    /// </summary>
    public class SubledgerReportService : ISubledgerReportService
    {
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _coaRepository;
        private readonly IPartyRepository _partyRepository;
        private readonly ILogger<SubledgerReportService> _logger;

        public SubledgerReportService(
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository coaRepository,
            IPartyRepository partyRepository,
            ILogger<SubledgerReportService> logger)
        {
            _journalRepository = journalRepository;
            _coaRepository = coaRepository;
            _partyRepository = partyRepository;
            _logger = logger;
        }

        public async Task<SubledgerAgingReport> GetApAgingAsync(
            Guid companyId,
            DateOnly asOfDate,
            AgingBuckets? customBuckets = null)
        {
            return await GetAgingReportAsync(companyId, "vendor", "AP", asOfDate, customBuckets);
        }

        public async Task<SubledgerAgingReport> GetArAgingAsync(
            Guid companyId,
            DateOnly asOfDate,
            AgingBuckets? customBuckets = null)
        {
            return await GetAgingReportAsync(companyId, "customer", "AR", asOfDate, customBuckets);
        }

        private async Task<SubledgerAgingReport> GetAgingReportAsync(
            Guid companyId,
            string partyType,
            string reportType,
            DateOnly asOfDate,
            AgingBuckets? customBuckets)
        {
            var buckets = customBuckets ?? new AgingBuckets();
            var report = new SubledgerAgingReport
            {
                CompanyId = companyId,
                ReportType = reportType,
                AsOfDate = asOfDate,
                Buckets = buckets
            };

            // Get all parties with their subledger balances
            var partyBalances = await _journalRepository.GetSubledgerBalancesByPartyAsync(
                companyId, partyType, asOfDate);

            // Get party details
            var parties = await _partyRepository.GetByTypeAsync(companyId, partyType);
            var partyDict = parties.ToDictionary(p => p.Id, p => p);

            foreach (var balance in partyBalances)
            {
                if (!partyDict.TryGetValue(balance.PartyId, out var party))
                    continue;

                // For aging, we need to analyze by invoice/document date
                // Simplified: Use current balance as "Current" bucket
                // Full implementation would require document-level aging
                var row = new SubledgerAgingRow
                {
                    PartyId = balance.PartyId,
                    PartyName = party.DisplayName ?? party.LegalName ?? "Unknown",
                    PartyCode = party.PartyCode,
                    Current = balance.Balance,
                    TotalOutstanding = balance.Balance
                };

                report.Rows.Add(row);
            }

            // Calculate totals
            report.Totals = new SubledgerAgingTotals
            {
                Current = report.Rows.Sum(r => r.Current),
                Days1To30 = report.Rows.Sum(r => r.Days1To30),
                Days31To60 = report.Rows.Sum(r => r.Days31To60),
                Days61To90 = report.Rows.Sum(r => r.Days61To90),
                Over90Days = report.Rows.Sum(r => r.Over90Days),
                TotalOutstanding = report.Rows.Sum(r => r.TotalOutstanding)
            };

            return report;
        }

        public async Task<PartyLedgerReport> GetPartyLedgerAsync(
            Guid companyId,
            string partyType,
            Guid partyId,
            DateOnly fromDate,
            DateOnly toDate)
        {
            var party = await _partyRepository.GetByIdAsync(partyId);
            if (party == null)
            {
                throw new ArgumentException($"Party {partyId} not found");
            }

            var report = new PartyLedgerReport
            {
                CompanyId = companyId,
                PartyId = partyId,
                PartyName = party.DisplayName ?? party.LegalName ?? "Unknown",
                PartyType = partyType,
                FromDate = fromDate,
                ToDate = toDate
            };

            // Get opening balance (sum of all transactions before fromDate)
            report.OpeningBalance = await _journalRepository.GetSubledgerBalanceAsync(
                companyId, partyType, partyId, fromDate.AddDays(-1));

            // Get transactions in date range
            var transactions = await _journalRepository.GetSubledgerTransactionsAsync(
                companyId, partyType, partyId, fromDate, toDate);

            var runningBalance = report.OpeningBalance;
            foreach (var tx in transactions.OrderBy(t => t.Date).ThenBy(t => t.JournalNumber))
            {
                runningBalance += tx.Debit - tx.Credit;
                report.Entries.Add(new PartyLedgerEntry
                {
                    Date = tx.Date,
                    JournalEntryId = tx.JournalEntryId,
                    JournalNumber = tx.JournalNumber,
                    SourceType = tx.SourceType,
                    SourceNumber = tx.SourceNumber,
                    Description = tx.Description,
                    Debit = tx.Debit,
                    Credit = tx.Credit,
                    RunningBalance = runningBalance
                });
            }

            report.ClosingBalance = runningBalance;
            report.TotalDebits = report.Entries.Sum(e => e.Debit);
            report.TotalCredits = report.Entries.Sum(e => e.Credit);

            return report;
        }

        public async Task<ControlAccountReconciliation> GetControlAccountReconciliationAsync(
            Guid companyId,
            DateOnly asOfDate)
        {
            var report = new ControlAccountReconciliation
            {
                CompanyId = companyId,
                AsOfDate = asOfDate
            };

            // Get all control accounts
            var controlAccounts = await _coaRepository.GetControlAccountsAsync(companyId);

            foreach (var account in controlAccounts)
            {
                // Get control account balance from ledger
                var controlBalance = await _journalRepository.GetAccountBalanceAsync(
                    companyId, account.Id, asOfDate);

                // Get sum of subledger balances
                var subledgerType = account.ControlAccountType switch
                {
                    "payables" => "vendor",
                    "receivables" => "customer",
                    _ => null
                };

                decimal subledgerSum = 0;
                int partyCount = 0;

                if (!string.IsNullOrEmpty(subledgerType))
                {
                    var balances = await _journalRepository.GetSubledgerBalancesByPartyAsync(
                        companyId, subledgerType, asOfDate);
                    subledgerSum = balances.Sum(b => b.Balance);
                    partyCount = balances.Count();
                }

                report.Rows.Add(new ControlAccountReconciliationRow
                {
                    AccountId = account.Id,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    ControlAccountType = account.ControlAccountType,
                    ControlAccountBalance = controlBalance,
                    SubledgerSum = subledgerSum,
                    Difference = controlBalance - subledgerSum,
                    PartyCount = partyCount
                });
            }

            return report;
        }

        public async Task<SubledgerDrilldown> GetSubledgerDrilldownAsync(
            Guid companyId,
            Guid controlAccountId,
            DateOnly asOfDate)
        {
            var account = await _coaRepository.GetByIdAsync(controlAccountId);
            if (account == null || !account.IsControlAccount)
            {
                throw new ArgumentException($"Account {controlAccountId} is not a control account");
            }

            var report = new SubledgerDrilldown
            {
                CompanyId = companyId,
                ControlAccountId = controlAccountId,
                ControlAccountCode = account.AccountCode,
                ControlAccountName = account.AccountName,
                AsOfDate = asOfDate
            };

            // Get control account balance
            report.ControlAccountBalance = await _journalRepository.GetAccountBalanceAsync(
                companyId, controlAccountId, asOfDate);

            // Determine subledger type
            var subledgerType = account.ControlAccountType switch
            {
                "payables" => "vendor",
                "receivables" => "customer",
                _ => null
            };

            if (string.IsNullOrEmpty(subledgerType))
            {
                return report;
            }

            // Get party balances
            var balances = await _journalRepository.GetSubledgerBalancesByPartyAsync(
                companyId, subledgerType, asOfDate);

            // Get party details
            var partyIds = balances.Select(b => b.PartyId).Distinct().ToList();
            var parties = await _partyRepository.GetByIdsAsync(partyIds);
            var partyDict = parties.ToDictionary(p => p.Id, p => p);

            foreach (var balance in balances.Where(b => Math.Abs(b.Balance) >= 0.01m))
            {
                partyDict.TryGetValue(balance.PartyId, out var party);

                report.Parties.Add(new SubledgerDrilldownRow
                {
                    PartyId = balance.PartyId,
                    PartyName = party?.DisplayName ?? party?.LegalName ?? "Unknown",
                    PartyCode = party?.PartyCode,
                    PartyType = subledgerType,
                    Balance = balance.Balance,
                    TransactionCount = balance.TransactionCount,
                    LastTransactionDate = balance.LastTransactionDate
                });
            }

            report.SubledgerSum = report.Parties.Sum(p => p.Balance);

            return report;
        }
    }
}
