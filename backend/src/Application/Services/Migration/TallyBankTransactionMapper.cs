using System.Text.RegularExpressions;
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Creates bank transaction records from Tally vouchers (Payment, Receipt, Contra, Journal).
    /// Single Responsibility: Map Tally voucher to BankTransaction entity.
    /// This enables bank reconciliation by creating expected bank entries (Bank Book).
    /// </summary>
    public class TallyBankTransactionMapper : ITallyBankTransactionMapper
    {
        private readonly ILogger<TallyBankTransactionMapper> _logger;
        private readonly IBankTransactionRepository _bankTransactionRepository;
        private readonly IBankAccountRepository _bankAccountRepository;

        public TallyBankTransactionMapper(
            ILogger<TallyBankTransactionMapper> logger,
            IBankTransactionRepository bankTransactionRepository,
            IBankAccountRepository bankAccountRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bankTransactionRepository = bankTransactionRepository ?? throw new ArgumentNullException(nameof(bankTransactionRepository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
        }

        public async Task<Result<BankTransaction>> CreateBankTransactionAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            string matchedEntityType,
            Guid? matchedEntityId,
            CancellationToken cancellationToken = default)
        {
            // Check for duplicate using Tally GUID
            var existing = await _bankTransactionRepository.GetByTallyGuidAsync(voucher.Guid);
            if (existing != null)
            {
                _logger.LogDebug("Bank transaction already exists for voucher {VoucherNumber}", voucher.VoucherNumber);
                return Result<BankTransaction>.Success(existing);
            }

            // Determine transaction type based on voucher type
            var transactionType = DetermineTransactionType(voucher);
            var sourceVoucherType = voucher.VoucherType?.ToLower() ?? "payment";

            // Resolve bank account (considers transaction type for debit/credit entries)
            var bankAccountId = await ResolveBankAccountAsync(companyId, voucher, transactionType);
            if (!bankAccountId.HasValue)
            {
                _logger.LogWarning(
                    "Could not resolve bank account for voucher {VoucherNumber}, skipping bank transaction",
                    voucher.VoucherNumber);
                return Error.Validation($"Could not resolve bank account for voucher {voucher.VoucherNumber}");
            }

            // Extract transaction details from narration
            var txnDetails = ParseTransactionDetails(voucher.Narration ?? "");

            var bankTransaction = new BankTransaction
            {
                Id = Guid.NewGuid(),
                BankAccountId = bankAccountId.Value,
                TransactionDate = voucher.Date,
                ValueDate = voucher.Date,
                Description = BuildDescription(voucher),
                ReferenceNumber = txnDetails.ReferenceNumber ?? voucher.VoucherNumber,
                ChequeNumber = txnDetails.ChequeNumber,
                TransactionType = transactionType,
                Amount = Math.Abs(voucher.Amount),
                Category = DetermineCategory(matchedEntityType),
                IsReconciled = false,  // Will be reconciled against bank statement later
                ImportSource = "tally_import",
                ImportBatchId = batchId,
                SourceVoucherType = sourceVoucherType,
                MatchedEntityType = matchedEntityType,
                MatchedEntityId = matchedEntityId,
                TallyVoucherGuid = voucher.Guid,
                TallyVoucherNumber = voucher.VoucherNumber,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _bankTransactionRepository.AddAsync(bankTransaction);

            _logger.LogDebug(
                "Created bank transaction {TxnId} for voucher {VoucherNumber}, matched to {EntityType}:{EntityId}",
                created.Id, voucher.VoucherNumber, matchedEntityType, matchedEntityId);

            return Result<BankTransaction>.Success(created);
        }

        /// <summary>
        /// Determines transaction type based on Tally voucher type.
        /// Payment = debit (money out), Receipt = credit (money in)
        /// </summary>
        private static string DetermineTransactionType(TallyVoucherDto voucher)
        {
            var voucherType = voucher.VoucherType?.ToLower() ?? "";
            return voucherType switch
            {
                "receipt" => "credit",   // Money coming IN
                "payment" => "debit",    // Money going OUT
                "contra" => "debit",     // Default, but Contra needs special handling (both legs)
                "journal" => "debit",    // Default, actual depends on entry
                _ => "debit"
            };
        }

        /// <summary>
        /// Resolves bank account from voucher.
        /// For Payment: look for debit entries (positive amounts)
        /// For Receipt: look for credit entries (negative amounts in Tally convention)
        /// </summary>
        private async Task<Guid?> ResolveBankAccountAsync(Guid companyId, TallyVoucherDto voucher, string transactionType)
        {
            // First try PartyLedgerName (usually the bank account in Payment/Receipt vouchers)
            if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
            {
                var bankAccount = await _bankAccountRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                if (bankAccount != null)
                    return bankAccount.Id;

                // Try by Tally GUID if available
                if (!string.IsNullOrEmpty(voucher.PartyLedgerGuid))
                {
                    bankAccount = await _bankAccountRepository.GetByTallyGuidAsync(companyId, voucher.PartyLedgerGuid);
                    if (bankAccount != null)
                        return bankAccount.Id;
                }
            }

            // For credits (Receipt), look for negative amount entries (credit to bank)
            // For debits (Payment), look for positive amount entries (debit from bank)
            Func<decimal, bool> amountFilter = transactionType == "credit"
                ? a => a < 0  // Credit entries (negative in Tally)
                : a => a > 0; // Debit entries (positive in Tally)

            foreach (var entry in voucher.LedgerEntries.Where(e => amountFilter(e.Amount)))
            {
                if (IsBankLedger(entry.LedgerName))
                {
                    var bankAccount = await _bankAccountRepository.GetByNameAsync(companyId, entry.LedgerName);
                    if (bankAccount != null)
                        return bankAccount.Id;

                    if (!string.IsNullOrEmpty(entry.LedgerGuid))
                    {
                        bankAccount = await _bankAccountRepository.GetByTallyGuidAsync(companyId, entry.LedgerGuid);
                        if (bankAccount != null)
                            return bankAccount.Id;
                    }
                }
            }

            // Fallback: try PartyLedgerName with partial match
            if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
            {
                var allBankAccounts = await _bankAccountRepository.GetByCompanyIdAsync(companyId);
                var match = allBankAccounts.FirstOrDefault(ba =>
                    voucher.PartyLedgerName.Contains(ba.AccountName, StringComparison.OrdinalIgnoreCase) ||
                    ba.AccountName.Contains(voucher.PartyLedgerName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return match.Id;
            }

            return null;
        }

        /// <summary>
        /// Determines if a ledger name is likely a bank account.
        /// </summary>
        private static bool IsBankLedger(string? ledgerName)
        {
            if (string.IsNullOrEmpty(ledgerName)) return false;

            var name = ledgerName.ToLower();
            return name.Contains("bank") ||
                   name.Contains("axis") ||
                   name.Contains("hdfc") ||
                   name.Contains("icici") ||
                   name.Contains("sbi") ||
                   name.Contains("kotak") ||
                   name.Contains("yes bank") ||
                   name.Contains("idfc") ||
                   name.Contains("canara") ||
                   name.Contains("union") ||
                   name.Contains("pnb") ||
                   name.Contains("bob") ||
                   name.Contains("indusind");
        }

        /// <summary>
        /// Parses transaction details from narration.
        /// Extracts NEFT/RTGS/IMPS/UPI references and cheque numbers.
        /// </summary>
        private static TransactionDetails ParseTransactionDetails(string narration)
        {
            var details = new TransactionDetails();

            // Extract INB reference: INB/NEFT/AXOIC09099179414/...
            var inbMatch = Regex.Match(narration, @"INB[\\/\s]*(?:NEFT|RTGS|IMPS)?[\\/\s]*([A-Z0-9]{10,20})", RegexOptions.IgnoreCase);
            if (inbMatch.Success)
            {
                details.ReferenceNumber = inbMatch.Groups[1].Value;
                details.TransferMode = DetermineTransferMode(narration);
            }

            // Extract NEFT/RTGS reference directly
            var neftMatch = Regex.Match(narration, @"(?:NEFT|RTGS)[/\s-]*([A-Z0-9]{12,22})", RegexOptions.IgnoreCase);
            if (neftMatch.Success && string.IsNullOrEmpty(details.ReferenceNumber))
            {
                details.ReferenceNumber = neftMatch.Groups[1].Value;
                details.TransferMode = narration.ToUpper().Contains("RTGS") ? "RTGS" : "NEFT";
            }

            // Extract IMPS reference: IMPS-514518413399
            var impsMatch = Regex.Match(narration, @"IMPS[- ]?(\d{12,15})", RegexOptions.IgnoreCase);
            if (impsMatch.Success)
            {
                details.ReferenceNumber ??= impsMatch.Groups[1].Value;
                details.TransferMode = "IMPS";
            }

            // Extract UPI reference: UPI-513714062933
            var upiMatch = Regex.Match(narration, @"UPI[- ]?(\d{12,15})", RegexOptions.IgnoreCase);
            if (upiMatch.Success)
            {
                details.ReferenceNumber ??= upiMatch.Groups[1].Value;
                details.TransferMode = "UPI";
            }

            // Extract cheque number
            var chequeMatch = Regex.Match(narration, @"(?:CHQ|CHEQUE|CHK)[\\/\s#]*(\d{6,10})", RegexOptions.IgnoreCase);
            if (chequeMatch.Success)
            {
                details.ChequeNumber = chequeMatch.Groups[1].Value;
            }

            // Extract EMI reference: PCR000909664727_EMI_10-04-2025
            var emiMatch = Regex.Match(narration, @"(PCR\d+)_EMI", RegexOptions.IgnoreCase);
            if (emiMatch.Success)
            {
                details.ReferenceNumber ??= emiMatch.Groups[1].Value;
                details.TransferMode = "Auto-Debit";
            }

            return details;
        }

        private static string DetermineTransferMode(string narration)
        {
            var lower = narration.ToLower();
            if (lower.Contains("neft")) return "NEFT";
            if (lower.Contains("rtgs")) return "RTGS";
            if (lower.Contains("imps")) return "IMPS";
            if (lower.Contains("upi")) return "UPI";
            return "Online";
        }

        /// <summary>
        /// Builds a description from the voucher for bank statement matching.
        /// </summary>
        private static string BuildDescription(TallyVoucherDto voucher)
        {
            var parts = new List<string>();

            // Add voucher type
            parts.Add($"[{voucher.VoucherType}]");

            // Add payee name (credit entry - where money is going)
            var payee = voucher.LedgerEntries
                .Where(e => e.Amount < 0)  // Credit entries
                .Select(e => e.LedgerName)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(payee))
            {
                // Clean up payee name
                payee = payee.Replace("\r", "").Replace("\n", "").Trim();
                if (payee.Length > 50) payee = payee[..50];
                parts.Add($"To: {payee}");
            }

            // Add narration if available
            if (!string.IsNullOrEmpty(voucher.Narration))
            {
                var narration = voucher.Narration.Replace("\r", " ").Replace("\n", " ").Trim();
                if (narration.Length > 100)
                    narration = narration[..100] + "...";
                parts.Add(narration);
            }

            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Determines category based on matched entity type.
        /// </summary>
        private static string DetermineCategory(string matchedEntityType)
        {
            return matchedEntityType switch
            {
                "vendor_payments" => "vendor_payment",
                "contractor_payments" => "contractor",
                "statutory_payments" => "tax",
                "journal_entries" => "transfer",
                _ => "other"
            };
        }

        private class TransactionDetails
        {
            public string? ReferenceNumber { get; set; }
            public string? ChequeNumber { get; set; }
            public string? TransferMode { get; set; }
        }

        /// <summary>
        /// Creates a bank transaction from a specific ledger entry.
        /// Used for Contra vouchers (creates TWO transactions) and Journal entries affecting bank.
        /// </summary>
        public async Task<Result<BankTransaction>> CreateBankTransactionFromLedgerEntryAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            TallyLedgerEntryDto ledgerEntry,
            string matchedEntityType,
            Guid? matchedEntityId,
            string transactionType,
            CancellationToken cancellationToken = default)
        {
            // For Contra/Journal, we may have multiple bank entries per voucher
            // Use a composite key: voucher GUID + ledger entry position
            var entryIndex = voucher.LedgerEntries.ToList().IndexOf(ledgerEntry);
            var compositeGuid = $"{voucher.Guid}_{entryIndex}";

            // Check for duplicate
            var existing = await _bankTransactionRepository.GetByTallyGuidAsync(compositeGuid);
            if (existing != null)
            {
                _logger.LogDebug("Bank transaction already exists for voucher {VoucherNumber} entry {Index}",
                    voucher.VoucherNumber, entryIndex);
                return Result<BankTransaction>.Success(existing);
            }

            // Resolve bank account from the specific ledger entry
            var bankAccountId = await ResolveBankAccountFromEntryAsync(companyId, ledgerEntry);
            if (!bankAccountId.HasValue)
            {
                _logger.LogWarning(
                    "Could not resolve bank account for ledger entry {LedgerName} in voucher {VoucherNumber}",
                    ledgerEntry.LedgerName, voucher.VoucherNumber);
                return Error.Validation($"Could not resolve bank account for {ledgerEntry.LedgerName}");
            }

            var txnDetails = ParseTransactionDetails(voucher.Narration ?? "");
            var sourceVoucherType = voucher.VoucherType?.ToLower() ?? "journal";

            var bankTransaction = new BankTransaction
            {
                Id = Guid.NewGuid(),
                BankAccountId = bankAccountId.Value,
                TransactionDate = voucher.Date,
                ValueDate = voucher.Date,
                Description = BuildDescriptionForEntry(voucher, ledgerEntry, transactionType),
                ReferenceNumber = txnDetails.ReferenceNumber ?? voucher.VoucherNumber,
                ChequeNumber = txnDetails.ChequeNumber,
                TransactionType = transactionType,
                Amount = Math.Abs(ledgerEntry.Amount),
                Category = DetermineCategory(matchedEntityType),
                IsReconciled = false,
                ImportSource = "tally_import",
                ImportBatchId = batchId,
                SourceVoucherType = sourceVoucherType,
                MatchedEntityType = matchedEntityType,
                MatchedEntityId = matchedEntityId,
                TallyVoucherGuid = compositeGuid,  // Composite for uniqueness
                TallyVoucherNumber = voucher.VoucherNumber,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _bankTransactionRepository.AddAsync(bankTransaction);

            _logger.LogDebug(
                "Created bank transaction {TxnId} ({Type}) for {VoucherType} voucher {VoucherNumber}, bank: {BankName}",
                created.Id, transactionType, sourceVoucherType, voucher.VoucherNumber, ledgerEntry.LedgerName);

            return Result<BankTransaction>.Success(created);
        }

        /// <summary>
        /// Resolves bank account from a specific ledger entry.
        /// </summary>
        private async Task<Guid?> ResolveBankAccountFromEntryAsync(Guid companyId, TallyLedgerEntryDto entry)
        {
            if (string.IsNullOrEmpty(entry.LedgerName)) return null;

            // Try exact name match
            var bankAccount = await _bankAccountRepository.GetByNameAsync(companyId, entry.LedgerName);
            if (bankAccount != null)
                return bankAccount.Id;

            // Try by Tally GUID
            if (!string.IsNullOrEmpty(entry.LedgerGuid))
            {
                bankAccount = await _bankAccountRepository.GetByTallyGuidAsync(companyId, entry.LedgerGuid);
                if (bankAccount != null)
                    return bankAccount.Id;
            }

            // Partial match fallback
            var allBankAccounts = await _bankAccountRepository.GetByCompanyIdAsync(companyId);
            var match = allBankAccounts.FirstOrDefault(ba =>
                entry.LedgerName.Contains(ba.AccountName, StringComparison.OrdinalIgnoreCase) ||
                ba.AccountName.Contains(entry.LedgerName, StringComparison.OrdinalIgnoreCase));

            return match?.Id;
        }

        /// <summary>
        /// Builds description for ledger entry-based transactions.
        /// </summary>
        private static string BuildDescriptionForEntry(TallyVoucherDto voucher, TallyLedgerEntryDto entry, string transactionType)
        {
            var parts = new List<string>();
            parts.Add($"[{voucher.VoucherType}]");

            // For contra, show transfer direction
            if (voucher.VoucherType?.ToLower() == "contra")
            {
                var direction = transactionType == "credit" ? "From" : "To";
                var otherBank = voucher.LedgerEntries
                    .Where(e => e != entry && IsBankLedger(e.LedgerName))
                    .Select(e => e.LedgerName)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(otherBank))
                {
                    parts.Add($"{direction}: {otherBank}");
                }
            }

            if (!string.IsNullOrEmpty(voucher.Narration))
            {
                var narration = voucher.Narration.Replace("\r", " ").Replace("\n", " ").Trim();
                if (narration.Length > 100)
                    narration = narration[..100] + "...";
                parts.Add(narration);
            }

            return string.Join(" | ", parts);
        }
    }
}
