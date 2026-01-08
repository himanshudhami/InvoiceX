using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Classifies Tally Payment vouchers into specific payment types
    /// based on party ledger group, narration patterns, and ledger entries.
    /// </summary>
    public class TallyPaymentClassifier : ITallyPaymentClassifier
    {
        private readonly ILogger<TallyPaymentClassifier> _logger;
        private readonly IPartyRepository _partyRepository;

        // Tally ledger groups that map to contractor payments
        // These are Tally's standard groups, not company-specific names
        private static readonly HashSet<string> ContractorGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            "CONSULTANTS",
            "Consultants",
            "Contractors",
            "Professional Services"
        };

        // Tally ledger groups that map to vendor payments
        private static readonly HashSet<string> VendorGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            "Sundry Creditors",
            "Trade Payables"
        };

        public TallyPaymentClassifier(
            ILogger<TallyPaymentClassifier> logger,
            IPartyRepository partyRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _partyRepository = partyRepository ?? throw new ArgumentNullException(nameof(partyRepository));
        }

        public async Task<TallyPaymentClassificationResult> ClassifyAsync(
            Guid companyId,
            TallyVoucherDto voucher,
            CancellationToken cancellationToken = default)
        {
            var narration = voucher.Narration?.ToLower() ?? "";

            // Find the credit entry (payee) - negative amount in Tally
            var creditEntry = voucher.LedgerEntries
                .Where(e => e.Amount < 0)
                .OrderByDescending(e => Math.Abs(e.Amount))
                .FirstOrDefault();

            if (creditEntry == null)
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.Other,
                    ClassificationReason = "No credit entry found"
                };
            }

            var payeeName = creditEntry.LedgerName?.Trim() ?? "";
            var amount = Math.Abs(creditEntry.Amount);

            // 1. Check for Statutory Payments ONLY if it's clearly a government remittance
            // This must be checked BEFORE contractor/vendor to avoid misclassifying TDS withholding
            if (IsStatutoryGovernmentRemittance(narration))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.Statutory,
                    TargetLedgerName = payeeName,
                    TargetLedgerGuid = creditEntry.LedgerGuid,
                    Amount = amount,
                    ClassificationReason = "Statutory government remittance (EPF/ESI/TDS/PT)"
                };
            }

            // 2. Lookup the party EARLY and classify based on TallyGroupName
            // This prevents misclassifying contractor payments as statutory just because they have TDS entries
            var party = await _partyRepository.GetByTallyLedgerNameAsync(companyId, payeeName);

            if (party != null && !string.IsNullOrEmpty(party.TallyGroupName))
            {
                var tallyGroup = party.TallyGroupName;

                // Check if it's a contractor group
                if (ContractorGroups.Contains(tallyGroup))
                {
                    _logger.LogDebug("Classified as Contractor: {Payee} (group: {Group})", payeeName, tallyGroup);
                    return new TallyPaymentClassificationResult
                    {
                        Type = TallyPaymentType.Contractor,
                        TargetLedgerName = payeeName,
                        TargetLedgerGuid = creditEntry.LedgerGuid,
                        ParentGroup = tallyGroup,
                        Amount = amount,
                        PartyId = party.Id,
                        ClassificationReason = $"Party TallyGroupName={tallyGroup}"
                    };
                }

                // Check if it's a vendor group
                if (VendorGroups.Contains(tallyGroup))
                {
                    _logger.LogDebug("Classified as Vendor: {Payee} (group: {Group})", payeeName, tallyGroup);
                    return new TallyPaymentClassificationResult
                    {
                        Type = TallyPaymentType.Vendor,
                        TargetLedgerName = payeeName,
                        TargetLedgerGuid = creditEntry.LedgerGuid,
                        ParentGroup = tallyGroup,
                        Amount = amount,
                        PartyId = party.Id,
                        ClassificationReason = $"Party TallyGroupName={tallyGroup}"
                    };
                }
            }

            // 3. Check for Salary Payment
            if (IsSalaryPayment(narration, voucher.LedgerEntries))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.Salary,
                    TargetLedgerName = payeeName,
                    TargetLedgerGuid = creditEntry.LedgerGuid,
                    Amount = amount,
                    ClassificationReason = "Salary payment"
                };
            }

            // 3. Check for Loan/EMI Payment
            if (IsLoanEmiPayment(narration))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.LoanEmi,
                    TargetLedgerName = payeeName,
                    TargetLedgerGuid = creditEntry.LedgerGuid,
                    Amount = amount,
                    ClassificationReason = "Loan EMI payment"
                };
            }

            // 4. Check for Bank Charges
            if (IsBankCharge(narration))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.BankCharge,
                    TargetLedgerName = payeeName,
                    TargetLedgerGuid = creditEntry.LedgerGuid,
                    Amount = amount,
                    ClassificationReason = "Bank charge"
                };
            }

            // 5. Check for Internal Transfer
            if (IsInternalTransfer(narration))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.InternalTransfer,
                    TargetLedgerName = payeeName,
                    TargetLedgerGuid = creditEntry.LedgerGuid,
                    Amount = amount,
                    ClassificationReason = "Internal bank transfer"
                };
            }

            // 6. Fallback: Check if party has IsVendor flag set
            if (party?.IsVendor == true)
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.Vendor,
                    TargetLedgerName = payeeName,
                    TargetLedgerGuid = creditEntry.LedgerGuid,
                    Amount = amount,
                    PartyId = party.Id,
                    ClassificationReason = "Party.IsVendor=true"
                };
            }

            // 7. Default to Other
            _logger.LogDebug("Classified as Other: {Payee} (no matching group)", payeeName);
            return new TallyPaymentClassificationResult
            {
                Type = TallyPaymentType.Other,
                TargetLedgerName = payeeName,
                TargetLedgerGuid = creditEntry.LedgerGuid,
                Amount = amount,
                PartyId = party?.Id,
                ClassificationReason = "No matching classification"
            };
        }

        /// <summary>
        /// Checks if the payment is a GOVERNMENT REMITTANCE (actual deposit to govt).
        /// This is stricter than just checking for TDS presence, which could be TDS withholding.
        /// </summary>
        private static bool IsStatutoryGovernmentRemittance(string narration)
        {
            // TDS remittance to government via CBDT/TIN 2.0
            if (narration.Contains("tin 2.0") || narration.Contains("cbdt tax payment"))
                return true;

            // EPF remittance patterns (EPFO payment, EPF TRRN)
            if (narration.Contains("epfo payment") || narration.Contains("epf") &&
                (narration.Contains("inb/") || narration.Contains("trrn")))
                return true;

            // ESI remittance (ESIC payment)
            if (narration.Contains("esic") || (narration.Contains("esi") && narration.Contains("challan")))
                return true;

            // Professional Tax to government (Karnataka: GOK E-KHAJANE)
            if (narration.Contains("gok e-khajane") || narration.Contains("e-khajane"))
                return true;

            // Generic government payment patterns with reference numbers
            if (narration.Contains("professional tax") && narration.Contains("inb/"))
                return true;

            return false;
        }

        private static bool IsSalaryPayment(string narration, List<TallyLedgerEntryDto> entries)
        {
            var ledgerNames = string.Join(" ", entries.Select(e => e.LedgerName ?? "")).ToLower();
            return narration.Contains("salary") || ledgerNames.Contains("salary payable");
        }

        private static bool IsLoanEmiPayment(string narration)
        {
            return narration.Contains("emi") || narration.Contains("pcr0009");
        }

        private static bool IsBankCharge(string narration)
        {
            return narration.Contains("service ch") || narration.Contains("gst @18%") ||
                   narration.Contains("bank charge");
        }

        private static bool IsInternalTransfer(string narration)
        {
            return narration.Contains("ift") || narration.Contains("tparty transfer");
        }
    }
}
