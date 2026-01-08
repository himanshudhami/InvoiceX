using System.Text.RegularExpressions;
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Maps Tally Payment vouchers to StatutoryPayment entities (EPF/ESI/TDS/PT)
    /// </summary>
    public class TallyStatutoryPaymentMapper : ITallyStatutoryPaymentMapper
    {
        private readonly ILogger<TallyStatutoryPaymentMapper> _logger;
        private readonly IStatutoryPaymentRepository _statutoryPaymentRepository;
        private readonly IBankAccountRepository _bankAccountRepository;

        public TallyStatutoryPaymentMapper(
            ILogger<TallyStatutoryPaymentMapper> logger,
            IStatutoryPaymentRepository statutoryPaymentRepository,
            IBankAccountRepository bankAccountRepository)
        {
            _logger = logger;
            _statutoryPaymentRepository = statutoryPaymentRepository;
            _bankAccountRepository = bankAccountRepository;
        }

        public async Task<Result<StatutoryPayment>> MapAndSaveAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            TallyPaymentClassificationResult classification,
            CancellationToken cancellationToken = default)
        {
            // Check for duplicate
            var existing = await _statutoryPaymentRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
            if (existing != null)
            {
                _logger.LogDebug("Statutory payment already exists for voucher {VoucherNumber}", voucher.VoucherNumber);
                return Result<StatutoryPayment>.Success(existing);
            }

            // Determine payment type from narration/ledger
            var paymentType = DeterminePaymentType(voucher);

            // Determine payment category (annual, arrear, penalty, etc.)
            var paymentCategory = DeterminePaymentCategory(voucher.Narration ?? "");

            // Extract challan/reference details
            var challanInfo = ExtractChallanDetails(voucher.Narration ?? "");

            // Determine period (statutory payments usually for previous month)
            var (periodMonth, periodYear, quarter) = DeterminePeriod(voucher.Date);

            // Resolve bank account
            var bankAccountId = await ResolveBankAccountAsync(companyId, voucher);

            var statutoryPayment = new StatutoryPayment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                PaymentType = paymentType,
                PaymentCategory = paymentCategory,
                ReferenceNumber = challanInfo.ReferenceNumber ?? voucher.VoucherNumber,
                FinancialYear = GetFinancialYear(voucher.Date),
                PeriodMonth = periodMonth,
                PeriodYear = periodYear,
                Quarter = quarter,
                PrincipalAmount = classification.Amount,
                InterestAmount = 0,
                PenaltyAmount = 0,
                LateFee = 0,
                TotalAmount = classification.Amount,
                PaymentDate = voucher.Date,
                PaymentMode = DeterminePaymentMode(voucher),
                BankAccountId = bankAccountId,
                BankReference = challanInfo.BankReference,
                ChallanNumber = challanInfo.ChallanNumber,
                Trrn = challanInfo.Trrn,
                Status = "paid",
                DueDate = CalculateDueDate(voucher.Date, paymentType),
                Notes = voucher.Narration,
                TallyVoucherGuid = voucher.Guid,
                TallyVoucherNumber = voucher.VoucherNumber,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _statutoryPaymentRepository.AddAsync(statutoryPayment);

            _logger.LogInformation(
                "Created statutory payment {PaymentId} type {Type} for period {Month}/{Year} amount {Amount}",
                statutoryPayment.Id, paymentType, periodMonth, periodYear, classification.Amount);

            return Result<StatutoryPayment>.Success(statutoryPayment);
        }

        private static string DeterminePaymentType(TallyVoucherDto voucher)
        {
            var narration = voucher.Narration?.ToLower() ?? "";
            var ledgerNames = string.Join(" ", voucher.LedgerEntries.Select(e => e.LedgerName ?? "")).ToLower();

            // Check ledger entry names FIRST - they have precise info like "TDS on Consulting Fee -194J"
            foreach (var entry in voucher.LedgerEntries)
            {
                var ledgerName = entry.LedgerName?.ToLower() ?? "";

                // TDS on Salary (Section 192)
                if (ledgerName.Contains("tds") && (ledgerName.Contains("salary") || ledgerName.Contains("192")))
                    return "TDS_192";

                // TDS on Professional Fees (Section 194J)
                if (ledgerName.Contains("tds") && (ledgerName.Contains("consulting") || ledgerName.Contains("194j")))
                    return "TDS_194J";

                // TDS on Contractors (Section 194C)
                if (ledgerName.Contains("tds") && ledgerName.Contains("194c"))
                    return "TDS_194C";

                // Professional Tax
                if (ledgerName.Contains("professional tax") && !ledgerName.Contains("tds"))
                    return "PT_KA";
            }

            // EPF / Provident Fund
            if (narration.Contains("epf") || narration.Contains("provident fund") ||
                ledgerNames.Contains("epf") || ledgerNames.Contains("provident fund"))
            {
                return "PF";
            }

            // ESI
            if (narration.Contains("esi") || ledgerNames.Contains("employee state insurance"))
            {
                return "ESI";
            }

            // TDS on Salary (Section 192) - from narration
            if (narration.Contains("tds on salary") || narration.Contains("tds 192") ||
                (narration.Contains("tds") && narration.Contains("salary")))
            {
                return "TDS_192";
            }

            // Professional Tax (Karnataka) - from narration
            if (narration.Contains("professional tax") || narration.Contains("pt ") ||
                narration.Contains("gok e-khajane"))
            {
                return "PT_KA";
            }

            // TDS on Contractors (194C) / Professional Fees (194J) - from narration
            if (narration.Contains("tds") || narration.Contains("cbdt") || narration.Contains("tin 2.0"))
            {
                // Check if it's consulting/professional fees
                if (narration.Contains("consulting") || narration.Contains("professional") ||
                    narration.Contains("194j"))
                {
                    return "TDS_194J";
                }
                return "TDS_194C";
            }

            // Default to general TDS
            return "TDS_194C";
        }

        private static string DeterminePaymentCategory(string narration)
        {
            var lower = narration.ToLower();

            // Annual payments (PT registration, annual filings)
            if (lower.Contains("annual") || lower.Contains("yearly"))
                return "annual";

            // Arrear payments
            if (lower.Contains("arrear") || lower.Contains("previous") || lower.Contains("prior"))
                return "arrear";

            // Penalty payments
            if (lower.Contains("penalty") || lower.Contains("fine"))
                return "penalty";

            // Interest payments
            if (lower.Contains("interest") && (lower.Contains("delay") || lower.Contains("late")))
                return "interest";

            // Revision/correction payments
            if (lower.Contains("revision") || lower.Contains("correction") || lower.Contains("additional"))
                return "revision";

            return "regular";
        }

        private static ChallanInfo ExtractChallanDetails(string narration)
        {
            var info = new ChallanInfo();

            // Pattern: INB/850252956//1968824102020250 EPF
            // Extract reference number (like 850252956)
            var refMatch = Regex.Match(narration, @"INB[\/\s]*(\d{9,12})");
            if (refMatch.Success)
            {
                info.BankReference = refMatch.Groups[1].Value;
            }

            // Extract TRRN/Challan number (like 1968824102020250)
            var trrnMatch = Regex.Match(narration, @"\/\/(\d{16,20})");
            if (trrnMatch.Success)
            {
                info.Trrn = trrnMatch.Groups[1].Value;
            }

            // TIN 2.0 CBDT reference
            var tinMatch = Regex.Match(narration, @"TIN 2\.0[\/\s]*(\d+)");
            if (tinMatch.Success)
            {
                info.ChallanNumber = tinMatch.Groups[1].Value;
            }

            // Generate reference number if not found
            if (string.IsNullOrEmpty(info.ReferenceNumber))
            {
                info.ReferenceNumber = info.BankReference ?? info.Trrn ?? info.ChallanNumber;
            }

            return info;
        }

        private static (int month, int year, string quarter) DeterminePeriod(DateOnly paymentDate)
        {
            // Statutory payments are typically for the previous month
            var periodDate = paymentDate.AddMonths(-1);
            var month = periodDate.Month;
            var year = periodDate.Year;

            // Determine quarter (Indian FY: Apr-Mar)
            var quarter = month switch
            {
                >= 4 and <= 6 => "Q1",   // Apr-Jun
                >= 7 and <= 9 => "Q2",   // Jul-Sep
                >= 10 and <= 12 => "Q3", // Oct-Dec
                _ => "Q4"                 // Jan-Mar
            };

            return (month, year, quarter);
        }

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static DateOnly CalculateDueDate(DateOnly paymentDate, string paymentType)
        {
            // Due dates based on payment type (from statutory_payment_types)
            var dueDay = paymentType switch
            {
                "PF" or "ESI" => 15,
                "TDS_192" or "TDS_194C" or "TDS_194J" => 7,
                "PT_KA" => 20,
                _ => 15
            };

            return new DateOnly(paymentDate.Year, paymentDate.Month, dueDay);
        }

        private async Task<Guid?> ResolveBankAccountAsync(Guid companyId, TallyVoucherDto voucher)
        {
            // Bank account is usually the debit entry (positive amount)
            var bankEntry = voucher.LedgerEntries
                .FirstOrDefault(e => e.Amount > 0 && IsBankLedger(e.LedgerName));

            if (bankEntry == null) return null;

            var bankAccount = await _bankAccountRepository.GetByNameAsync(companyId, bankEntry.LedgerName!);
            return bankAccount?.Id;
        }

        private static bool IsBankLedger(string? ledgerName)
        {
            if (string.IsNullOrEmpty(ledgerName)) return false;
            var name = ledgerName.ToLower();
            return name.Contains("bank") ||
                   name.Contains("axis") ||
                   name.Contains("kotak") ||
                   name.Contains("hdfc") ||
                   name.Contains("icici") ||
                   name.Contains("sbi");
        }

        private static string DeterminePaymentMode(TallyVoucherDto voucher)
        {
            var narration = voucher.Narration?.ToLower() ?? "";

            if (narration.Contains("neft")) return "neft";
            if (narration.Contains("rtgs")) return "rtgs";
            if (narration.Contains("online") || narration.Contains("inb")) return "online";

            return "online";
        }

        private class ChallanInfo
        {
            public string? ReferenceNumber { get; set; }
            public string? BankReference { get; set; }
            public string? ChallanNumber { get; set; }
            public string? Trrn { get; set; }
        }
    }
}
