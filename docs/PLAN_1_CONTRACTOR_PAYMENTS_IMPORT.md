# Plan 1: Contractor/Consultant Payments Import

## Overview

Import Tally Payment vouchers where the payee belongs to the **CONSULTANTS** ledger group into the `contractor_payments` table.

### Current State
- **75+ payments** in XML are to consultants (CONSULTANTS group)
- Currently imported as `journal_entries` (losing business context)
- `contractor_payments` table exists but is empty

### Target State
- Contractor payments properly tracked in `contractor_payments`
- TDS details (section 194C/194J) properly captured
- Linkable to payroll/contractor management workflows

---

## Prerequisites

**IMPORTANT:** This plan relies on `Party.TallyGroupName` being populated during the **master import phase**. The Tally XML parser must:

1. Extract `<PARENT>` element from `<LEDGER>` records
2. Store it in `Party.TallyGroupName` when creating parties

This enables **generic classification** - the code doesn't hardcode company-specific names, but instead uses Tally's standard ledger groups:
- `CONSULTANTS` → Contractor payments
- `Sundry Creditors` → Vendor payments
- etc.

**Verify before implementation:**
```sql
-- Check that parties have TallyGroupName set
SELECT tally_group_name, COUNT(*)
FROM parties
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
GROUP BY tally_group_name;
```

---

## Architecture Design (SOLID/SRP/SOC)

```
TallyVoucherMappingService.ImportPaymentVouchersAsync()
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│              TallyPaymentClassifier (NEW)                   │
│                                                             │
│  Single Responsibility: Classify payment voucher type       │
│                                                             │
│  ClassifyPayment(voucher) returns:                          │
│    - PaymentType.Vendor                                     │
│    - PaymentType.Contractor    ◄── This plan                │
│    - PaymentType.Statutory     ◄── Plan 2                   │
│    - PaymentType.Salary                                     │
│    - PaymentType.LoanEmi                                    │
│    - PaymentType.BankCharge                                 │
│    - PaymentType.InternalTransfer                           │
│    - PaymentType.Other                                      │
└─────────────────────────────────────────────────────────────┘
    │
    ▼ (if Contractor)
┌─────────────────────────────────────────────────────────────┐
│           TallyContractorPaymentMapper (NEW)                │
│                                                             │
│  Single Responsibility: Map voucher to ContractorPayment    │
│                                                             │
│  - MapToContractorPayment(voucher) : ContractorPayment      │
│  - ExtractTdsInfo(voucher) : (section, rate, amount)        │
│  - ResolveContractor(voucher) : Guid                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### Step 1: Add Tally Tracking Fields to contractor_payments

**File:** `backend/migrations/135_contractor_payments_tally_fields.sql`

```sql
-- Add Tally migration tracking fields to contractor_payments
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

CREATE INDEX IF NOT EXISTS idx_contractor_payments_tally_guid
ON contractor_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;
```

### Step 2: Create Payment Classification Enum

**File:** `backend/src/Application/DTOs/Migration/TallyPaymentClassification.cs`

```csharp
namespace Application.DTOs.Migration
{
    public enum TallyPaymentType
    {
        Vendor,           // Sundry Creditors - existing flow
        Contractor,       // CONSULTANTS group - this plan
        Statutory,        // EPF/ESI/TDS/PT - Plan 2
        Salary,           // Salary Payable entries
        LoanEmi,          // EMI payments
        BankCharge,       // Bank service charges
        InternalTransfer, // Contra-like transfers
        Other             // Fallback to journal entry
    }

    public class TallyPaymentClassificationResult
    {
        public TallyPaymentType Type { get; set; }
        public string? TargetLedgerName { get; set; }
        public string? TargetLedgerGuid { get; set; }
        public string? ParentGroup { get; set; }
        public decimal Amount { get; set; }
    }
}
```

### Step 3: Create ITallyPaymentClassifier Interface

**File:** `backend/src/Application/Interfaces/Migration/ITallyPaymentClassifier.cs`

```csharp
using Application.DTOs.Migration;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Classifies Tally Payment vouchers into specific payment types
    /// for routing to appropriate import handlers.
    /// </summary>
    public interface ITallyPaymentClassifier
    {
        /// <summary>
        /// Analyzes a payment voucher and determines its type based on
        /// party ledger group, narration patterns, and ledger entries.
        /// </summary>
        Task<TallyPaymentClassificationResult> ClassifyAsync(
            Guid companyId,
            TallyVoucherDto voucher,
            CancellationToken cancellationToken = default);
    }
}
```

### Step 4: Implement TallyPaymentClassifier

**File:** `backend/src/Application/Services/Migration/TallyPaymentClassifier.cs`

**Key Design Decision:** Classification uses `Party.TallyGroupName` which stores the original Tally ledger group (e.g., 'CONSULTANTS', 'Sundry Creditors'). This makes the classifier **generic** and works for any company's data.

```csharp
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
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
            _logger = logger;
            _partyRepository = partyRepository;
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
                return new TallyPaymentClassificationResult { Type = TallyPaymentType.Other };
            }

            var payeeName = creditEntry.LedgerName?.Trim() ?? "";
            var amount = Math.Abs(creditEntry.Amount);

            // 1. Check for Statutory Payments first (highest priority)
            if (IsStatutoryPayment(narration, voucher.LedgerEntries))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.Statutory,
                    TargetLedgerName = payeeName,
                    Amount = amount
                };
            }

            // 2. Check for Salary Payment
            if (IsSalaryPayment(narration, voucher.LedgerEntries))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.Salary,
                    TargetLedgerName = payeeName,
                    Amount = amount
                };
            }

            // 3. Check for Loan/EMI Payment
            if (IsLoanEmiPayment(narration))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.LoanEmi,
                    TargetLedgerName = payeeName,
                    Amount = amount
                };
            }

            // 4. Check for Bank Charges
            if (IsBankCharge(narration))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.BankCharge,
                    TargetLedgerName = payeeName,
                    Amount = amount
                };
            }

            // 5. Check for Internal Transfer
            if (IsInternalTransfer(narration))
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.InternalTransfer,
                    TargetLedgerName = payeeName,
                    Amount = amount
                };
            }

            // 6. Lookup the party and classify based on TallyGroupName
            // This is the GENERIC approach - uses the Party's stored TallyGroupName
            // which was set during master import from the Tally XML <PARENT> element
            var party = await _partyRepository.GetByTallyLedgerNameAsync(companyId, payeeName);

            if (party != null && !string.IsNullOrEmpty(party.TallyGroupName))
            {
                var tallyGroup = party.TallyGroupName;

                // Check if it's a contractor group
                if (ContractorGroups.Contains(tallyGroup))
                {
                    return new TallyPaymentClassificationResult
                    {
                        Type = TallyPaymentType.Contractor,
                        TargetLedgerName = payeeName,
                        TargetLedgerGuid = creditEntry.LedgerGuid,
                        ParentGroup = tallyGroup,
                        Amount = amount
                    };
                }

                // Check if it's a vendor group
                if (VendorGroups.Contains(tallyGroup))
                {
                    return new TallyPaymentClassificationResult
                    {
                        Type = TallyPaymentType.Vendor,
                        TargetLedgerName = payeeName,
                        TargetLedgerGuid = creditEntry.LedgerGuid,
                        ParentGroup = tallyGroup,
                        Amount = amount
                    };
                }
            }

            // 7. Fallback: Check if party has IsVendor flag set
            if (party?.IsVendor == true)
            {
                return new TallyPaymentClassificationResult
                {
                    Type = TallyPaymentType.Vendor,
                    TargetLedgerName = payeeName,
                    TargetLedgerGuid = creditEntry.LedgerGuid,
                    Amount = amount
                };
            }

            // 8. Default to Other
            return new TallyPaymentClassificationResult
            {
                Type = TallyPaymentType.Other,
                TargetLedgerName = payeeName,
                Amount = amount
            };
        }

        private static bool IsStatutoryPayment(string narration, List<TallyLedgerEntryDto> entries)
        {
            var ledgerNames = string.Join(" ", entries.Select(e => e.LedgerName ?? "")).ToLower();

            return narration.Contains("epf") || narration.Contains("provident fund") ||
                   narration.Contains("esi") ||
                   narration.Contains("tds") || narration.Contains("cbdt") ||
                   narration.Contains("professional tax") || narration.Contains("gok e-khajane") ||
                   ledgerNames.Contains("epf") || ledgerNames.Contains("tds payable");
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
```

### Step 5: Add Party Repository Method for Tally Ledger Lookup

**File:** `backend/src/Core/Interfaces/IPartyRepository.cs`

Add method to existing interface:

```csharp
/// <summary>
/// Gets a party by their original Tally ledger name
/// </summary>
Task<Party?> GetByTallyLedgerNameAsync(Guid companyId, string tallyLedgerName);
```

**File:** `backend/src/Infrastructure/Data/PartyRepository.cs`

Implement the method:

```csharp
public async Task<Party?> GetByTallyLedgerNameAsync(Guid companyId, string tallyLedgerName)
{
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QueryFirstOrDefaultAsync<Party>(
        @"SELECT * FROM parties
          WHERE company_id = @companyId
          AND (tally_ledger_name = @tallyLedgerName OR name = @tallyLedgerName)",
        new { companyId, tallyLedgerName });
}
```

---

### Step 6: Create ITallyContractorPaymentMapper Interface

**File:** `backend/src/Application/Interfaces/Migration/ITallyContractorPaymentMapper.cs`

```csharp
using Application.DTOs.Migration;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces.Migration
{
    public interface ITallyContractorPaymentMapper
    {
        Task<Result<ContractorPayment>> MapAndSaveAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            TallyPaymentClassificationResult classification,
            CancellationToken cancellationToken = default);
    }
}
```

### Step 7: Implement TallyContractorPaymentMapper

**File:** `backend/src/Application/Services/Migration/TallyContractorPaymentMapper.cs`

```csharp
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    public class TallyContractorPaymentMapper : ITallyContractorPaymentMapper
    {
        private readonly ILogger<TallyContractorPaymentMapper> _logger;
        private readonly IContractorPaymentRepository _contractorPaymentRepository;
        private readonly IEmployeesRepository _employeesRepository;

        public TallyContractorPaymentMapper(
            ILogger<TallyContractorPaymentMapper> logger,
            IContractorPaymentRepository contractorPaymentRepository,
            IEmployeesRepository employeesRepository)
        {
            _logger = logger;
            _contractorPaymentRepository = contractorPaymentRepository;
            _employeesRepository = employeesRepository;
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

            // Resolve or create contractor (as employee with contractor type)
            var contractorId = await ResolveOrCreateContractorAsync(
                companyId,
                classification.TargetLedgerName!,
                classification.TargetLedgerGuid);

            // Extract TDS details from ledger entries
            var tdsInfo = ExtractTdsInfo(voucher);

            var contractorPayment = new ContractorPayment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                EmployeeId = contractorId,
                PaymentMonth = voucher.Date.Month,
                PaymentYear = voucher.Date.Year,
                GrossAmount = classification.Amount + tdsInfo.TdsAmount,
                TdsSection = tdsInfo.Section,
                TdsRate = tdsInfo.Rate,
                TdsAmount = tdsInfo.TdsAmount,
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

            await _contractorPaymentRepository.AddAsync(contractorPayment);

            _logger.LogInformation(
                "Created contractor payment {PaymentId} for {Contractor} amount {Amount}",
                contractorPayment.Id, classification.TargetLedgerName, classification.Amount);

            return Result<ContractorPayment>.Success(contractorPayment);
        }

        private async Task<Guid> ResolveOrCreateContractorAsync(
            Guid companyId,
            string contractorName,
            string? tallyGuid)
        {
            // Clean name
            var cleanName = contractorName.Replace("\r", "").Replace("\n", "").Trim();

            // Try to find existing
            var existing = await _employeesRepository.GetByNameAsync(companyId, cleanName);
            if (existing != null)
                return existing.Id;

            // Create new contractor
            var contractor = new Employee
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                EmployeeName = cleanName,
                EmployeeId = $"CON-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Status = "active",
                EmploymentType = "contractor",
                TallyLedgerGuid = tallyGuid,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _employeesRepository.AddAsync(contractor);
            _logger.LogInformation("Created contractor {Name} with ID {Id}", cleanName, contractor.Id);

            return contractor.Id;
        }

        private static TdsInfo ExtractTdsInfo(TallyVoucherDto voucher)
        {
            // Look for TDS ledger entries
            var tdsEntry = voucher.LedgerEntries
                .FirstOrDefault(e =>
                    (e.LedgerName?.ToLower().Contains("tds") ?? false) ||
                    (e.TdsAmount ?? 0) > 0);

            if (tdsEntry == null)
            {
                return new TdsInfo { Section = "194C", Rate = 0, TdsAmount = 0 };
            }

            // Determine TDS section based on amount and context
            var amount = tdsEntry.TdsAmount ?? Math.Abs(tdsEntry.Amount);
            var grossAmount = voucher.LedgerEntries
                .Where(e => e.Amount < 0 && !e.LedgerName?.ToLower().Contains("tds") == true)
                .Sum(e => Math.Abs(e.Amount));

            var rate = grossAmount > 0 ? (amount / grossAmount * 100) : 0;

            // 194C is 1% or 2%, 194J is 10%
            var section = rate >= 9 ? "194J" : "194C";

            return new TdsInfo
            {
                Section = section,
                Rate = Math.Round(rate, 2),
                TdsAmount = amount
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

            return "bank_transfer";
        }

        private class TdsInfo
        {
            public string Section { get; set; } = "194C";
            public decimal Rate { get; set; }
            public decimal TdsAmount { get; set; }
        }
    }
}
```

### Step 8: Modify TallyVoucherMappingService

**File:** `backend/src/Application/Services/Migration/TallyVoucherMappingService.cs`

Replace the `ImportPaymentVouchersAsync` method:

```csharp
public async Task<Result<TallyImportCountsDto>> ImportPaymentVouchersAsync(
    Guid batchId,
    Guid companyId,
    List<TallyVoucherDto> vouchers,
    CancellationToken cancellationToken = default)
{
    var counts = new TallyImportCountsDto { Total = vouchers.Count };
    var processingOrder = 0;

    foreach (var voucher in vouchers)
    {
        cancellationToken.ThrowIfCancellationRequested();
        processingOrder++;

        try
        {
            // Classify the payment
            var classification = await _paymentClassifier.ClassifyAsync(companyId, voucher, cancellationToken);

            var result = classification.Type switch
            {
                TallyPaymentType.Vendor => await ImportVendorPaymentAsync(batchId, companyId, voucher, classification, processingOrder),
                TallyPaymentType.Contractor => await ImportContractorPaymentAsync(batchId, companyId, voucher, classification, processingOrder),
                TallyPaymentType.Statutory => await ImportStatutoryPaymentAsync(batchId, companyId, voucher, classification, processingOrder),
                TallyPaymentType.Salary => await ImportAsJournalAsync(batchId, companyId, voucher, processingOrder, "salary"),
                TallyPaymentType.LoanEmi => await ImportLoanEmiPaymentAsync(batchId, companyId, voucher, classification, processingOrder),
                _ => await ImportAsJournalAsync(batchId, companyId, voucher, processingOrder, classification.Type.ToString().ToLower())
            };

            if (result.Success)
                counts.Imported++;
            else
                counts.Failed++;
        }
        catch (Exception ex)
        {
            counts.Failed++;
            _logger.LogWarning(ex, "Failed to import payment voucher {Number}", voucher.VoucherNumber);
            await LogVoucherMigration(batchId, voucher, "failed", ex.Message, null, processingOrder);
        }
    }

    return Result<TallyImportCountsDto>.Success(counts);
}

private async Task<ImportResult> ImportContractorPaymentAsync(
    Guid batchId,
    Guid companyId,
    TallyVoucherDto voucher,
    TallyPaymentClassificationResult classification,
    int processingOrder)
{
    var result = await _contractorPaymentMapper.MapAndSaveAsync(
        batchId, companyId, voucher, classification);

    if (result.IsSuccess)
    {
        await LogVoucherMigration(batchId, voucher, "success", null,
            result.Value!.Id, processingOrder, "contractor_payments");
        return new ImportResult { Success = true };
    }

    await LogVoucherMigration(batchId, voucher, "failed",
        result.Error!.Message, null, processingOrder);
    return new ImportResult { Success = false };
}
```

### Step 9: Register Services in DI

**File:** `backend/src/Infrastructure/Extensions/ServiceCollectionExtensions.cs`

Add to the migration services section:

```csharp
// Migration Services
services.AddScoped<ITallyPaymentClassifier, TallyPaymentClassifier>();
services.AddScoped<ITallyContractorPaymentMapper, TallyContractorPaymentMapper>();
```

---

## Testing Checklist

- [ ] Party lookup by `tally_ledger_name` works correctly
- [ ] Classifier correctly identifies parties with `TallyGroupName = 'CONSULTANTS'` as contractor payments
- [ ] Classifier correctly identifies parties with `TallyGroupName = 'Sundry Creditors'` as vendor payments
- [ ] TDS section (194C vs 194J) determined correctly based on rate
- [ ] Contractor created if not exists
- [ ] Duplicate imports skipped (idempotent)
- [ ] Journal entry also created for GL posting
- [ ] Migration log tracks all imports
- [ ] Works with any company's data (no hardcoded names)

---

## Expected Results After Implementation

| Metric | Before | After |
|--------|--------|-------|
| `contractor_payments` records | 0 | ~75 |
| Contractors in `employees` | 0 | ~20 |
| Payments with TDS tracking | 0 | ~75 |
