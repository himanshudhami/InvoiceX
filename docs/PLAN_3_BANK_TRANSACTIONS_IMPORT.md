# Plan 3: Bank Transactions Import (For Reconciliation)

## Overview

Create `bank_transactions` records for ALL Payment vouchers to enable bank reconciliation workflows.

### Current State
- **260 payment vouchers** in XML
- **0 bank transactions** in database
- Bank accounts exist (Axis, Kotak)
- No reconciliation data available

### Target State
- All payments create corresponding `bank_transactions` records
- Transactions linked to source entity (vendor_payment, contractor_payment, statutory_payment, journal_entry)
- Bank reconciliation workflow enabled
- Match against bank statement imports

---

## Design Philosophy

**Key Insight:** Every Tally Payment voucher represents an outgoing bank transaction, regardless of what business entity it maps to.

```
┌─────────────────────────────────────────────────────────────────┐
│                     Tally Payment Voucher                       │
│                                                                 │
│  Creates TWO records:                                           │
│  1. Business Entity (vendor_payment, contractor_payment, etc.)  │
│  2. Bank Transaction (for reconciliation)                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Architecture Design (SOLID/SRP/SOC)

```
TallyVoucherMappingService.ImportPaymentVouchersAsync()
    │
    ├──▶ [Business Entity Mapper] (Plan 1 & 2)
    │         │
    │         ▼
    │    vendor_payments / contractor_payments / statutory_payments
    │
    └──▶ [Bank Transaction Mapper] (This Plan)
              │
              ▼
         bank_transactions
              │
              ▼
         bank_transaction_matches (link to source entity)
```

### Separation of Concerns

| Component | Responsibility |
|-----------|----------------|
| `TallyPaymentClassifier` | Determine business entity type |
| `TallyVendorPaymentMapper` | Create vendor_payments |
| `TallyContractorPaymentMapper` | Create contractor_payments |
| `TallyStatutoryPaymentMapper` | Create statutory_payments |
| `TallyBankTransactionMapper` | Create bank_transactions for ALL payments |

---

## Implementation Steps

### Step 1: Add Tally Tracking Fields to bank_transactions

**File:** `backend/migrations/137_bank_transactions_tally_fields.sql`

```sql
-- Add Tally migration tracking fields to bank_transactions
ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_voucher_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id),
ADD COLUMN IF NOT EXISTS matched_entity_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS matched_entity_id UUID;

-- Index for efficient lookups
CREATE INDEX IF NOT EXISTS idx_bank_transactions_tally_guid
ON bank_transactions(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_bank_transactions_matched_entity
ON bank_transactions(matched_entity_type, matched_entity_id)
WHERE matched_entity_id IS NOT NULL;

-- Add comment
COMMENT ON COLUMN bank_transactions.matched_entity_type IS 'Entity type: vendor_payments, contractor_payments, statutory_payments, journal_entries';
COMMENT ON COLUMN bank_transactions.matched_entity_id IS 'Reference to the matched business entity';
```

### Step 2: Create ITallyBankTransactionMapper Interface

**File:** `backend/src/Application/Interfaces/Migration/ITallyBankTransactionMapper.cs`

```csharp
using Application.DTOs.Migration;
using Core.Common;
using Core.Entities;

namespace Application.Interfaces.Migration
{
    /// <summary>
    /// Creates bank transaction records from Tally Payment vouchers
    /// for bank reconciliation purposes.
    /// </summary>
    public interface ITallyBankTransactionMapper
    {
        /// <summary>
        /// Creates a bank transaction record for a payment voucher.
        /// Should be called AFTER the business entity is created.
        /// </summary>
        Task<Result<BankTransaction>> CreateBankTransactionAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            string matchedEntityType,
            Guid? matchedEntityId,
            CancellationToken cancellationToken = default);
    }
}
```

### Step 3: Implement TallyBankTransactionMapper

**File:** `backend/src/Application/Services/Migration/TallyBankTransactionMapper.cs`

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
            _logger = logger;
            _bankTransactionRepository = bankTransactionRepository;
            _bankAccountRepository = bankAccountRepository;
        }

        public async Task<Result<BankTransaction>> CreateBankTransactionAsync(
            Guid batchId,
            Guid companyId,
            TallyVoucherDto voucher,
            string matchedEntityType,
            Guid? matchedEntityId,
            CancellationToken cancellationToken = default)
        {
            // Check for duplicate
            var existing = await _bankTransactionRepository.GetByTallyGuidAsync(voucher.Guid);
            if (existing != null)
            {
                _logger.LogDebug("Bank transaction already exists for voucher {VoucherNumber}", voucher.VoucherNumber);
                return Result<BankTransaction>.Success(existing);
            }

            // Resolve bank account from debit entry (source of funds)
            var bankAccountId = await ResolveBankAccountAsync(companyId, voucher);
            if (!bankAccountId.HasValue)
            {
                return Error.Validation($"Could not resolve bank account for voucher {voucher.VoucherNumber}");
            }

            // Extract transaction details from narration
            var txnDetails = ParseTransactionDetails(voucher.Narration ?? "");

            var bankTransaction = new BankTransaction
            {
                Id = Guid.NewGuid(),
                BankAccountId = bankAccountId.Value,
                TransactionDate = voucher.Date.ToDateTime(TimeOnly.MinValue),
                ValueDate = voucher.Date.ToDateTime(TimeOnly.MinValue),
                Description = BuildDescription(voucher),
                ReferenceNumber = txnDetails.ReferenceNumber ?? voucher.VoucherNumber,
                ChequeNumber = txnDetails.ChequeNumber,
                TransactionType = "debit",  // All payments are debits
                Amount = voucher.Amount,
                Status = "imported",  // Will be reconciled later
                Source = "tally_import",
                MatchedEntityType = matchedEntityType,
                MatchedEntityId = matchedEntityId,
                TallyVoucherGuid = voucher.Guid,
                TallyVoucherNumber = voucher.VoucherNumber,
                TallyVoucherType = voucher.VoucherType,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _bankTransactionRepository.AddAsync(bankTransaction);

            _logger.LogDebug(
                "Created bank transaction {TxnId} for voucher {VoucherNumber}, matched to {EntityType}:{EntityId}",
                bankTransaction.Id, voucher.VoucherNumber, matchedEntityType, matchedEntityId);

            return Result<BankTransaction>.Success(bankTransaction);
        }

        private async Task<Guid?> ResolveBankAccountAsync(Guid companyId, TallyVoucherDto voucher)
        {
            // In Tally Payment vouchers:
            // - PartyLedgerName is typically the bank account (debit side)
            // - Credit entries are the payees

            // First try PartyLedgerName
            if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
            {
                var bankAccount = await _bankAccountRepository.GetByNameAsync(companyId, voucher.PartyLedgerName);
                if (bankAccount != null)
                    return bankAccount.Id;
            }

            // Then try debit entries (positive amounts)
            foreach (var entry in voucher.LedgerEntries.Where(e => e.Amount > 0))
            {
                if (IsBankLedger(entry.LedgerName))
                {
                    var bankAccount = await _bankAccountRepository.GetByNameAsync(companyId, entry.LedgerName!);
                    if (bankAccount != null)
                        return bankAccount.Id;
                }
            }

            // Fallback: try PartyLedgerName with partial match
            if (!string.IsNullOrEmpty(voucher.PartyLedgerName))
            {
                var allBankAccounts = await _bankAccountRepository.GetAllByCompanyAsync(companyId);
                var match = allBankAccounts.FirstOrDefault(ba =>
                    voucher.PartyLedgerName.Contains(ba.AccountName, StringComparison.OrdinalIgnoreCase) ||
                    ba.AccountName.Contains(voucher.PartyLedgerName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return match.Id;
            }

            _logger.LogWarning("Could not resolve bank account for voucher {VoucherNumber}, party: {PartyLedger}",
                voucher.VoucherNumber, voucher.PartyLedgerName);

            return null;
        }

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
                   name.Contains("idfc");
        }

        private static TransactionDetails ParseTransactionDetails(string narration)
        {
            var details = new TransactionDetails();

            // Extract INB reference: INB/NEFT/AXOIC09099179414/...
            var inbMatch = Regex.Match(narration, @"INB[\/\s]*(?:NEFT|RTGS|IMPS)?[\/\s]*([A-Z0-9]{10,20})", RegexOptions.IgnoreCase);
            if (inbMatch.Success)
            {
                details.ReferenceNumber = inbMatch.Groups[1].Value;
                details.TransferMode = DetermineTransferMode(narration);
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

            // Extract MB (Mobile Banking) reference
            var mbMatch = Regex.Match(narration, @"MB:([^\t\/]+)", RegexOptions.IgnoreCase);
            if (mbMatch.Success)
            {
                details.Description = mbMatch.Groups[1].Value.Trim();
                details.TransferMode = "Mobile";
            }

            // Extract cheque number
            var chequeMatch = Regex.Match(narration, @"(?:CHQ|CHEQUE)[\/\s#]*(\d{6,10})", RegexOptions.IgnoreCase);
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

        private static string BuildDescription(TallyVoucherDto voucher)
        {
            var parts = new List<string>();

            // Add voucher type
            parts.Add($"[{voucher.VoucherType}]");

            // Add payee name (credit entry)
            var payee = voucher.LedgerEntries
                .Where(e => e.Amount < 0)
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
                var narration = voucher.Narration.Length > 100
                    ? voucher.Narration[..100] + "..."
                    : voucher.Narration;
                parts.Add(narration);
            }

            return string.Join(" | ", parts);
        }

        private class TransactionDetails
        {
            public string? ReferenceNumber { get; set; }
            public string? ChequeNumber { get; set; }
            public string? TransferMode { get; set; }
            public string? Description { get; set; }
        }
    }
}
```

### Step 4: Update TallyVoucherMappingService to Create Bank Transactions

Modify `ImportPaymentVouchersAsync` to call bank transaction mapper after business entity creation:

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
            // Step 1: Classify the payment
            var classification = await _paymentClassifier.ClassifyAsync(companyId, voucher, cancellationToken);

            // Step 2: Create business entity based on classification
            var (entityType, entityId) = classification.Type switch
            {
                TallyPaymentType.Vendor => await ImportVendorPaymentAsync(batchId, companyId, voucher, classification),
                TallyPaymentType.Contractor => await ImportContractorPaymentAsync(batchId, companyId, voucher, classification),
                TallyPaymentType.Statutory => await ImportStatutoryPaymentAsync(batchId, companyId, voucher, classification),
                TallyPaymentType.LoanEmi => await ImportLoanEmiPaymentAsync(batchId, companyId, voucher, classification),
                _ => await ImportAsJournalAsync(batchId, companyId, voucher)
            };

            // Step 3: ALWAYS create bank transaction (regardless of business entity type)
            await _bankTransactionMapper.CreateBankTransactionAsync(
                batchId,
                companyId,
                voucher,
                entityType,
                entityId,
                cancellationToken);

            counts.Imported++;
            await LogVoucherMigration(batchId, voucher, "success", null, entityId, processingOrder, entityType);
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

// Helper method that returns (entityType, entityId)
private async Task<(string entityType, Guid? entityId)> ImportVendorPaymentAsync(...)
{
    var result = await _vendorPaymentMapper.MapAndSaveAsync(...);
    return ("vendor_payments", result.IsSuccess ? result.Value!.Id : null);
}

// Similar for other payment types...
```

### Step 5: Add Repository Methods

**File:** `backend/src/Core/Interfaces/IBankTransactionRepository.cs`

```csharp
public interface IBankTransactionRepository
{
    Task<BankTransaction?> GetByTallyGuidAsync(string tallyGuid);
    Task<IEnumerable<BankTransaction>> GetByCompanyAsync(Guid companyId);
    Task<BankTransaction> AddAsync(BankTransaction transaction);
    // ... other methods
}
```

**File:** `backend/src/Infrastructure/Data/BankTransactionRepository.cs`

```csharp
public async Task<BankTransaction?> GetByTallyGuidAsync(string tallyGuid)
{
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QueryFirstOrDefaultAsync<BankTransaction>(
        "SELECT * FROM bank_transactions WHERE tally_voucher_guid = @tallyGuid",
        new { tallyGuid });
}
```

### Step 6: Register Service in DI

**File:** `backend/src/Infrastructure/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<ITallyBankTransactionMapper, TallyBankTransactionMapper>();
```

---

## Data Flow Diagram

```
Tally Payment Voucher
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│                    Classification                              │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┐   │
│  │   Vendor    │ Contractor  │  Statutory  │   Other     │   │
│  └─────┬───────┴──────┬──────┴──────┬──────┴──────┬──────┘   │
└────────┼──────────────┼─────────────┼─────────────┼───────────┘
         │              │             │             │
         ▼              ▼             ▼             ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│vendor_      │ │contractor_  │ │statutory_   │ │journal_     │
│payments     │ │payments     │ │payments     │ │entries      │
└──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘
       │               │              │               │
       └───────────────┴──────────────┴───────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │bank_transactions │  (ALL payments create this)
                    │                 │
                    │ matched_entity_ │
                    │ type + id       │  (links back to business entity)
                    └─────────────────┘
```

---

## Bank Transaction Status Workflow

```
imported → pending_match → matched → reconciled
    │           │             │           │
    │           │             │           └──▶ Statement matches & verified
    │           │             │
    │           │             └──▶ Auto-matched or manually matched
    │           │
    │           └──▶ Bank statement imported, waiting for match
    │
    └──▶ Created from Tally import (no statement yet)
```

---

## Testing Checklist

- [ ] Bank account resolved correctly for all payments
- [ ] Transaction reference numbers extracted from narration
- [ ] Matched entity linked correctly (vendor_payment, etc.)
- [ ] Duplicate imports prevented (idempotent)
- [ ] Status set to "imported" for reconciliation workflow
- [ ] All 260 payment vouchers create bank transactions

---

## Expected Results After Implementation

| Metric | Before | After |
|--------|--------|-------|
| `bank_transactions` records | 0 | ~260 |
| Transactions linked to vendor_payments | 0 | ~41 |
| Transactions linked to contractor_payments | 0 | ~75 |
| Transactions linked to statutory_payments | 0 | ~17 |
| Transactions linked to journal_entries | 0 | ~127 |

---

## Future Enhancement: Bank Reconciliation UI

With bank transactions now populated, the Bank Reconciliation page (`/finance/banking/reconciliation`) can:

1. Show all imported transactions with status "imported"
2. Allow matching against bank statement imports
3. Highlight discrepancies
4. Mark as reconciled when matched
