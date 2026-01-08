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
    /// Maps Tally Payment vouchers to ContractorPayment entities.
    /// Links directly to parties table (unified vendor model).
    /// </summary>
    public class TallyContractorPaymentMapper : ITallyContractorPaymentMapper
    {
        private readonly ILogger<TallyContractorPaymentMapper> _logger;
        private readonly IContractorPaymentRepository _contractorPaymentRepository;
        private readonly IPartyRepository _partyRepository;

        public TallyContractorPaymentMapper(
            ILogger<TallyContractorPaymentMapper> logger,
            IContractorPaymentRepository contractorPaymentRepository,
            IPartyRepository partyRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contractorPaymentRepository = contractorPaymentRepository ?? throw new ArgumentNullException(nameof(contractorPaymentRepository));
            _partyRepository = partyRepository ?? throw new ArgumentNullException(nameof(partyRepository));
        }

        public async Task<Result<ContractorPayment>> MapAndSaveAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            TallyPaymentClassificationResult classification,
            CancellationToken cancellationToken = default)
        {
            // Check for duplicate
            var existing = await _contractorPaymentRepository.GetByTallyGuidAsync(companyId, voucher.Guid);
            if (existing != null)
            {
                _logger.LogDebug("Contractor payment already exists for voucher {VoucherNumber}", voucher.VoucherNumber);
                return Result<ContractorPayment>.Success(existing);
            }

            // Validate party exists (should be created by ledger import)
            if (!classification.PartyId.HasValue)
            {
                return Error.Validation($"No party found for contractor payment voucher {voucher.VoucherNumber}");
            }

            // Get party for PAN and TDS info
            var party = await _partyRepository.GetByIdAsync(classification.PartyId.Value);
            if (party == null)
            {
                return Error.NotFound($"Party {classification.PartyId} not found for voucher {voucher.VoucherNumber}");
            }

            // Extract TDS details from ledger entries
            var tdsInfo = ExtractTdsInfo(voucher, classification.Amount);

            // Get TDS info from party's vendor profile if available
            var vendorProfile = await _partyRepository.GetVendorProfileAsync(classification.PartyId.Value);
            if (vendorProfile?.TdsApplicable == true && !string.IsNullOrEmpty(vendorProfile.DefaultTdsSection))
            {
                tdsInfo.Section = vendorProfile.DefaultTdsSection;
                // Use profile rate if TDS wasn't detected in voucher
                if (tdsInfo.TdsAmount == 0 && vendorProfile.DefaultTdsRate.HasValue)
                {
                    tdsInfo.Rate = vendorProfile.DefaultTdsRate.Value;
                }
            }

            var contractorPayment = new ContractorPayment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                PartyId = classification.PartyId.Value,
                PaymentMonth = voucher.Date.Month,
                PaymentYear = voucher.Date.Year,
                GrossAmount = classification.Amount + tdsInfo.TdsAmount,
                TdsSection = tdsInfo.Section,
                TdsRate = tdsInfo.Rate,
                TdsAmount = tdsInfo.TdsAmount,
                ContractorPan = party.PanNumber,
                PanVerified = false,
                OtherDeductions = 0,
                NetPayable = classification.Amount,
                Status = "paid",
                PaymentDate = voucher.Date.ToDateTime(TimeOnly.MinValue),
                PaymentMethod = DeterminePaymentMethod(voucher),
                PaymentReference = voucher.VoucherNumber,
                Description = voucher.Narration,
                TallyVoucherGuid = voucher.Guid,
                TallyVoucherNumber = voucher.VoucherNumber,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _contractorPaymentRepository.AddAsync(contractorPayment);

            _logger.LogInformation(
                "Created contractor payment {PaymentId} for party {PartyName} (ID: {PartyId}) amount {Amount}",
                created.Id, party.Name, party.Id, classification.Amount);

            return Result<ContractorPayment>.Success(created);
        }

        private static TdsInfo ExtractTdsInfo(TallyVoucherDto voucher, decimal paymentAmount)
        {
            // Look for TDS ledger entries
            var tdsEntry = voucher.LedgerEntries
                .FirstOrDefault(e =>
                    (e.LedgerName?.ToLower().Contains("tds") ?? false) ||
                    (e.TdsAmount ?? 0) > 0);

            if (tdsEntry == null)
            {
                // No TDS found - default to 194J with 0%
                return new TdsInfo { Section = "194J", Rate = 0, TdsAmount = 0 };
            }

            // Calculate TDS amount
            var tdsAmount = tdsEntry.TdsAmount ?? Math.Abs(tdsEntry.Amount);

            // Calculate gross amount (net + tds)
            var grossAmount = paymentAmount + tdsAmount;

            // Calculate rate
            var rate = grossAmount > 0 ? (tdsAmount / grossAmount * 100) : 0;

            // Determine section based on rate
            // 194C is typically 1% or 2%, 194J is 10%
            string section;
            if (rate >= 9)
                section = "194J";  // Professional services - 10%
            else if (rate >= 1.5m)
                section = "194C"; // Contractors (individual) - 2%
            else if (rate > 0)
                section = "194C"; // Contractors (others) - 1%
            else
                section = "194J"; // Default for professionals

            // If TDS entry has section info, use it
            if (!string.IsNullOrEmpty(tdsEntry.TdsSection))
            {
                section = tdsEntry.TdsSection;
            }

            return new TdsInfo
            {
                Section = section,
                Rate = Math.Round(rate, 2),
                TdsAmount = tdsAmount
            };
        }

        private static string DeterminePaymentMethod(TallyVoucherDto voucher)
        {
            var narration = voucher.Narration?.ToLower() ?? "";

            if (narration.Contains("neft")) return "neft";
            if (narration.Contains("imps")) return "imps";
            if (narration.Contains("rtgs")) return "rtgs";
            if (narration.Contains("upi")) return "upi";
            if (narration.Contains("cheque") || narration.Contains("chq")) return "cheque";

            // Check ledger names for bank indicator
            foreach (var entry in voucher.LedgerEntries)
            {
                var name = entry.LedgerName?.ToLower() ?? "";
                if (name.Contains("cash")) return "cash";
                if (name.Contains("bank") || name.Contains("hdfc") || name.Contains("icici") ||
                    name.Contains("sbi") || name.Contains("axis")) return "bank_transfer";
            }

            return "bank_transfer";
        }

        private class TdsInfo
        {
            public string Section { get; set; } = "194J";
            public decimal Rate { get; set; }
            public decimal TdsAmount { get; set; }
        }
    }
}
