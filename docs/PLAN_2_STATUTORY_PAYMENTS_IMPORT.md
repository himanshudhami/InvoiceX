# Plan 2: Statutory Payments Import (EPF/ESI/TDS/PT)

## Overview

Import Tally Payment vouchers for statutory compliance payments into the `statutory_payments` table.

### Current State
- **17+ statutory payments** in XML (7 EPF + 9 TDS + 1 PT)
- Currently imported as `journal_entries` (losing compliance context)
- `statutory_payments` table exists with proper structure
- `statutory_payment_types` reference table is populated

### Target State
- Statutory payments properly tracked in `statutory_payments`
- Linked to correct payment type (PF, ESI, TDS_192, TDS_194C, PT_KA)
- Challan/reference numbers captured for compliance
- Visible in Statutory Compliance dashboard

---

## Payment Type Detection Matrix

| Narration Pattern | Ledger Pattern | Payment Type Code |
|-------------------|----------------|-------------------|
| `epf`, `provident fund` | EPF Payable | `PF` |
| `esi` | ESI Payable | `ESI` |
| `tds on salary`, `tds 192` | TDS on Salary Payable | `TDS_192` |
| `tds`, `cbdt`, `tin 2.0` | TDS Payable | `TDS_194C` (default) |
| `professional tax`, `gok e-khajane` | PT Payable | `PT_KA` |

---

## Architecture Design (SOLID/SRP/SOC)

```
TallyVoucherMappingService.ImportPaymentVouchersAsync()
    │
    ▼
TallyPaymentClassifier (from Plan 1)
    │
    ▼ (if Statutory)
┌─────────────────────────────────────────────────────────────┐
│           TallyStatutoryPaymentMapper (NEW)                 │
│                                                             │
│  Single Responsibility: Map voucher to StatutoryPayment     │
│                                                             │
│  - DeterminePaymentType(voucher) : string                   │
│  - ExtractChallanDetails(narration) : ChallanInfo           │
│  - MapToStatutoryPayment(voucher) : StatutoryPayment        │
│  - DeterminePeriod(voucher) : (month, year, quarter)        │
└─────────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### Step 1: Add Tally Tracking Fields to statutory_payments

**File:** `backend/migrations/136_statutory_payments_tally_fields.sql`

```sql
-- Add Tally migration tracking fields to statutory_payments
ALTER TABLE statutory_payments
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

CREATE INDEX IF NOT EXISTS idx_statutory_payments_tally_guid
ON statutory_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;
```

### Step 2: Create ITallyStatutoryPaymentMapper Interface

**File:** `backend/src/Application/Interfaces/Migration/ITallyStatutoryPaymentMapper.cs`

```csharp
using Application.DTOs.Migration;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces.Migration
{
    public interface ITallyStatutoryPaymentMapper
    {
        Task<Result<StatutoryPayment>> MapAndSaveAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            TallyPaymentClassificationResult classification,
            CancellationToken cancellationToken = default);
    }
}
```

### Step 3: Implement TallyStatutoryPaymentMapper

**File:** `backend/src/Application/Services/Migration/TallyStatutoryPaymentMapper.cs`

```csharp
using System.Text.RegularExpressions;
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
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

            // Determine payment type
            var paymentType = DeterminePaymentType(voucher);

            // Extract challan/reference details
            var challanInfo = ExtractChallanDetails(voucher.Narration ?? "");

            // Determine period (statutory payments are usually for previous month)
            var (periodMonth, periodYear, quarter) = DeterminePeriod(voucher.Date);

            // Resolve bank account
            var bankAccountId = await ResolveBankAccountAsync(companyId, voucher);

            var statutoryPayment = new StatutoryPayment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                PaymentType = paymentType,
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
                PaymentDate = voucher.Date.ToDateTime(TimeOnly.MinValue),
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

            // EPF
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

            // TDS on Salary (192)
            if (narration.Contains("tds on salary") || narration.Contains("tds 192") ||
                (narration.Contains("tds") && narration.Contains("salary")))
            {
                return "TDS_192";
            }

            // TDS on Contractors (194C) / Professional Fees (194J)
            if (narration.Contains("tds") || narration.Contains("cbdt") || narration.Contains("tin 2.0"))
            {
                // Check if it's consulting/professional fees
                if (narration.Contains("consulting") || narration.Contains("professional"))
                {
                    return "TDS_194J";
                }
                return "TDS_194C";
            }

            // Professional Tax
            if (narration.Contains("professional tax") || narration.Contains("pt ") ||
                narration.Contains("gok e-khajane"))
            {
                return "PT_KA";  // Karnataka PT
            }

            // Default to general TDS
            return "TDS_194C";
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

            // Determine quarter
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
                .FirstOrDefault(e => e.Amount > 0 &&
                    (e.LedgerName?.ToLower().Contains("bank") ?? false ||
                     e.LedgerName?.ToLower().Contains("axis") ?? false ||
                     e.LedgerName?.ToLower().Contains("kotak") ?? false));

            if (bankEntry == null) return null;

            var bankAccount = await _bankAccountRepository.GetByNameAsync(companyId, bankEntry.LedgerName!);
            return bankAccount?.Id;
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
```

### Step 4: Add Repository Method for Tally GUID Lookup

**File:** `backend/src/Core/Interfaces/IStatutoryPaymentRepository.cs`

Add method:

```csharp
Task<StatutoryPayment?> GetByTallyGuidAsync(Guid companyId, string tallyGuid);
```

**File:** `backend/src/Infrastructure/Data/StatutoryPaymentRepository.cs`

Implement:

```csharp
public async Task<StatutoryPayment?> GetByTallyGuidAsync(Guid companyId, string tallyGuid)
{
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QueryFirstOrDefaultAsync<StatutoryPayment>(
        @"SELECT * FROM statutory_payments
          WHERE company_id = @companyId AND tally_voucher_guid = @tallyGuid",
        new { companyId, tallyGuid });
}
```

### Step 5: Update TallyVoucherMappingService

Add to `ImportPaymentVouchersAsync` switch statement (already shown in Plan 1):

```csharp
TallyPaymentType.Statutory => await ImportStatutoryPaymentAsync(
    batchId, companyId, voucher, classification, processingOrder),
```

Add the handler method:

```csharp
private async Task<ImportResult> ImportStatutoryPaymentAsync(
    Guid batchId,
    Guid companyId,
    TallyVoucherDto voucher,
    TallyPaymentClassificationResult classification,
    int processingOrder)
{
    var result = await _statutoryPaymentMapper.MapAndSaveAsync(
        batchId, companyId, voucher, classification);

    if (result.IsSuccess)
    {
        await LogVoucherMigration(batchId, voucher, "success", null,
            result.Value!.Id, processingOrder, "statutory_payments");
        return new ImportResult { Success = true };
    }

    await LogVoucherMigration(batchId, voucher, "failed",
        result.Error!.Message, null, processingOrder);
    return new ImportResult { Success = false };
}
```

### Step 6: Register Service in DI

**File:** `backend/src/Infrastructure/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<ITallyStatutoryPaymentMapper, TallyStatutoryPaymentMapper>();
```

---

## Mapping Summary

| Tally Data | StatutoryPayment Field |
|------------|------------------------|
| `Voucher.Date` | `payment_date` |
| `Voucher.Date - 1 month` | `period_month`, `period_year` |
| `LedgerEntry.Amount` (credit) | `principal_amount`, `total_amount` |
| Narration parsing | `reference_number`, `challan_number`, `trrn` |
| Bank ledger name | `bank_account_id` |
| Classification logic | `payment_type` |

---

## Testing Checklist

- [ ] EPF payments detected by narration pattern
- [ ] TDS payments correctly classified (192 vs 194C)
- [ ] Professional Tax (Karnataka) detected
- [ ] Challan/TRRN numbers extracted from narration
- [ ] Period calculated as previous month
- [ ] Bank account resolved from ledger entry
- [ ] Duplicate imports skipped (idempotent)

---

## Expected Results After Implementation

| Metric | Before | After |
|--------|--------|-------|
| `statutory_payments` records | 0 | ~17 |
| EPF payments tracked | 0 | 7 |
| TDS payments tracked | 0 | 9 |
| PT payments tracked | 0 | 1 |
