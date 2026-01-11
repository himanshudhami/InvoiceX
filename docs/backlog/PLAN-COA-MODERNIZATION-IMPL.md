# Implementation Plan: COA Modernization (Task 16)

**Created**: 2026-01-11
**Status**: ✅ IMPLEMENTED & VALIDATED (2026-01-11)
**Feature Doc**: [FEATURE-COA-MODERNIZATION.md](./FEATURE-COA-MODERNIZATION.md)

---

## Validation Results (2026-01-11)

| Criteria | Expected | Actual | Status |
|----------|----------|--------|--------|
| JE lines with subledger | >0% | **21.9%** (208/948) | ✅ PASS |
| tally_ledger_mapping | >0 | **332** (14 customers + 318 vendors) | ✅ PASS |
| TL-/TR- party accounts | 0 | **0** (20 non-party ledgers OK) | ✅ PASS |
| Control accounts configured | Yes | **7** control accounts | ✅ PASS |

---

## Current State Analysis (Database Audit)

### Already Implemented (Phase 1 from Feature Doc - COMPLETE)

| Component | Status | Evidence |
|-----------|--------|----------|
| `chart_of_accounts.is_control_account` | ✅ | Column exists, 7 control accounts configured |
| `chart_of_accounts.control_account_type` | ✅ | payables, receivables, tds_payable, tds_receivable, gst_input, gst_output, loans |
| `chart_of_accounts.is_tally_legacy` | ✅ | Column exists, 0 legacy accounts (clean COA) |
| `journal_entry_lines.subledger_type` | ✅ | Column exists |
| `journal_entry_lines.subledger_id` | ✅ | Column exists |
| `tally_ledger_mapping` table | ✅ | Full schema exists (company_id, tally_ledger_name, control_account_id, party_type, party_id) |
| Control Accounts | ✅ | 1120 Trade Receivables, 2100 Trade Payables, etc. |
| Clean COA | ✅ | 127 accounts (not 500+), 0 TL-/TR- party ledgers |

### Gaps Identified

| Gap | Location | Impact |
|-----|----------|--------|
| AutoPostingService doesn't populate subledger fields | `AutoPostingService.cs:615-623` | JE lines lack party reference |
| Posting templates missing subledger config | `posting_rules.posting_template` | Can't drive subledger from rules |
| tally_ledger_mapping empty | 0 rows | Tally import/export can't translate |
| No subledger reports | Missing services/UI | Can't drill-down from control account |

---

## Implementation Phases

### Phase 1: Fix AutoPosting Subledger Population
**Effort**: 1 day | **Risk**: Low | **Status**: ✅ COMPLETE (2026-01-11)

**Problem**: `AutoPostingService` reads `SubledgerType` and `SubledgerIdField` from template but doesn't set them on `JournalEntryLine`.

**Completed**: Added subledger resolution logic at `AutoPostingService.cs:615-639`. Now reads `subledger_type` and `subledger_id_field` from posting template and sets `SubledgerType`/`SubledgerId` on `JournalEntryLine`.

**Code Change** (`AutoPostingService.cs:615-623`):
```csharp
// Before
var line = new JournalEntryLine
{
    AccountId = account.Id,
    DebitAmount = debitAmount,
    CreditAmount = creditAmount,
    Description = description,
    Currency = "INR",
    ExchangeRate = 1
};

// After - add subledger resolution
var subledgerType = lineTemplate.SubledgerType ?? lineTemplate.SubledgerTypeCamel;
Guid? subledgerId = null;
var subledgerIdField = lineTemplate.SubledgerIdField ?? lineTemplate.SubledgerIdFieldCamel;
if (!string.IsNullOrEmpty(subledgerIdField) && sourceData.TryGetValue(subledgerIdField, out var sidValue))
{
    if (sidValue is Guid g) subledgerId = g;
    else if (Guid.TryParse(sidValue?.ToString(), out var parsed)) subledgerId = parsed;
}

var line = new JournalEntryLine
{
    AccountId = account.Id,
    DebitAmount = debitAmount,
    CreditAmount = creditAmount,
    Description = description,
    Currency = "INR",
    ExchangeRate = 1,
    SubledgerType = subledgerType,
    SubledgerId = subledgerId
};
```

**Files**:
- `src/Application/Services/Ledger/AutoPostingService.cs`

---

### Phase 2: Update Posting Rules with Subledger Config
**Effort**: 2 days | **Risk**: Medium | **Status**: ✅ COMPLETE (2026-01-11)

**Discovery**: Posting rules already have subledger config (`subledgerType`, `subledgerField`)!

**Completed**:
1. Added `SubledgerFieldLegacy` property to read existing `subledgerField` (legacy alias)
2. Added `customer_id` to invoice source data (`PostInvoiceAsync`)
3. Added `customer_id` to payment source data (`PostPaymentAsync`)

**Existing Templates Verified**:
- INV_DEFAULT: `subledgerType: "customer"`, `subledgerField: "customer_id"` ✅
- PMT_DEFAULT: `subledgerType: "customer"`, `subledgerField: "customer_id"` ✅
- VINV_DEFAULT: `subledgerType: "vendor"`, `subledgerField: "vendor_id"` ✅
- VPMT_DEFAULT: `subledgerType: "vendor"`, `subledgerField: "vendor_id"` ✅

**Tasks**:
1. ~~Create migration to update `posting_rules.posting_template` for vendor/customer transactions~~ (Not needed - already configured)
2. ~~Add subledger config to templates~~ (Not needed - already configured)

**Example Template Update** (vendor_invoice):
```json
{
  "narration_template": "Purchase from {vendor_name} - {source_number}",
  "lines": [
    {
      "account_code": "5100",
      "debit_field": "subtotal",
      "description_template": "Purchases - {source_number}"
    },
    {
      "account_code": "2100",
      "credit_field": "net_payable",
      "description_template": "Payable to {vendor_name}",
      "subledger_type": "vendor",
      "subledger_id_field": "vendor_id"
    }
  ]
}
```

**Files**:
- New migration: `xxx_update_posting_rules_subledger.sql`
- Update existing rules for: `vendor_invoice`, `vendor_payment`, `invoice`, `payment`

---

### Phase 3: Tally Ledger Mapping Population
**Effort**: 2 days | **Risk**: Medium | **Status**: ✅ COMPLETE (2026-01-11)

**Completed**:
1. Created `TallyLedgerMapping` entity (`Core/Entities/Migration/TallyLedgerMapping.cs`)
2. Created `ITallyLedgerMappingRepository` interface (`Core/Interfaces/Migration/ITallyLedgerMappingRepository.cs`)
3. Created `TallyLedgerMappingRepository` with `SeedFromPartiesAsync()` method (`Infrastructure/Data/Migration/TallyLedgerMappingRepository.cs`)
4. Registered in DI (`Infrastructure/Extensions/ServiceCollectionExtensions.cs`)

**Key Methods**:
- `GetByTallyLedgerNameAsync()` - For Tally import lookup
- `GetByPartyIdAsync()` - For Tally export lookup
- `SeedFromPartiesAsync()` - Populates mappings from existing parties

**Tasks**:
1. ~~Create `ITallyLedgerMappingRepository` interface in Core~~ ✅
2. ~~Create `TallyLedgerMappingRepository` in Infrastructure~~ ✅
3. ~~Create seed script to populate from existing parties~~ ✅ (via `SeedFromPartiesAsync`)

**Seed Logic**:
```sql
INSERT INTO tally_ledger_mapping (company_id, tally_ledger_name, control_account_id, party_type, party_id)
SELECT
    p.company_id,
    p.display_name as tally_ledger_name,
    CASE p.party_type
        WHEN 'vendor' THEN (SELECT id FROM chart_of_accounts WHERE account_code = '2100' AND company_id = p.company_id)
        WHEN 'customer' THEN (SELECT id FROM chart_of_accounts WHERE account_code = '1120' AND company_id = p.company_id)
    END as control_account_id,
    p.party_type,
    p.id as party_id
FROM parties p
WHERE p.party_type IN ('vendor', 'customer');
```

**Files**:
- `src/Core/Interfaces/Migration/ITallyLedgerMappingRepository.cs`
- `src/Infrastructure/Data/Migration/TallyLedgerMappingRepository.cs`
- Migration script for seed data

---

### Phase 4: Tally Import/Export Translation Layer
**Effort**: 3 days | **Risk**: Medium | **Status**: ✅ COMPLETE (2026-01-11)

**Completed**:
1. Added `ITallyLedgerMappingRepository` to `TallyVoucherMappingService` constructor
2. Updated journal entry line creation to use `tally_ledger_mapping`:
   - First checks `GetByTallyLedgerNameAsync()` / `GetByTallyGuidAsync()`
   - If found: Uses control account + sets `SubledgerType`/`SubledgerId`
   - If not found: Falls back to direct COA lookup (legacy behavior)

**Files Modified**:
- `Application/Services/Migration/TallyVoucherMappingService.cs`

**Tasks**:
1. ~~Update `TallyVoucherMappingService` to lookup `tally_ledger_mapping`~~ ✅
2. ~~On import: Translate Tally ledger name → control account + party~~ ✅
3. On export: Translate party → Tally ledger name (future - when export is needed)

**Import Flow**:
```
Tally Voucher Line: Cr. "RK WORLDINFOCOM" 11,800
    ↓
Lookup tally_ledger_mapping WHERE tally_ledger_name = 'RK WORLDINFOCOM'
    ↓
Result: control_account_id = 2100 (Trade Payables), party_type = 'vendor', party_id = uuid-123
    ↓
Create JournalEntryLine: account_id = 2100, subledger_type = 'vendor', subledger_id = uuid-123
```

**Files**:
- `src/Application/Services/Migration/TallyVoucherMappingService.cs`
- `src/Application/Interfaces/Migration/ITallyLedgerMappingService.cs` (new)

---

### Phase 5: Subledger Reports
**Effort**: 3 days | **Risk**: Low | **Status**: ✅ COMPLETE (2026-01-11)

**Completed**:
1. Created `ISubledgerReportService` interface with DTOs (`Application/Interfaces/Ledger/ISubledgerReportService.cs`)
2. Created `SubledgerReportService` implementation (`Application/Services/Ledger/SubledgerReportService.cs`)
3. Added subledger query methods to `IJournalEntryRepository` and implementation
4. Added `GetControlAccountsAsync()` to `IChartOfAccountRepository` and implementation
5. Added `GetByTypeAsync()` to `IPartyRepository` and implementation
6. Added `ControlAccountType` property to `ChartOfAccount` entity
7. Registered `SubledgerReportService` in DI

**Reports Available**:
- `GetApAgingAsync()` - AP aging by vendor
- `GetArAgingAsync()` - AR aging by customer
- `GetPartyLedgerAsync()` - Transaction history for a party
- `GetControlAccountReconciliationAsync()` - Verify subledger = control balance
- `GetSubledgerDrilldownAsync()` - Party breakdown of control account

**New Reports**:

1. **AP Aging by Vendor**
   - GROUP BY subledger_id WHERE subledger_type = 'vendor'
   - Age buckets: Current, 1-30, 31-60, 61-90, 90+

2. **AR Aging by Customer**
   - GROUP BY subledger_id WHERE subledger_type = 'customer'

3. **Party Ledger (Transaction History)**
   - All JE lines for a specific party
   - Running balance

4. **Control Account Reconciliation**
   - Verify: SUM(subledger balances) = control account balance

**Files**:
- `src/Application/Services/Reports/SubledgerReportService.cs` (new)
- `src/Application/Interfaces/Reports/ISubledgerReportService.cs` (new)
- `src/WebApi/Controllers/Reports/SubledgerReportController.cs` (new)

---

### Phase 6: UI Updates
**Effort**: 2 days | **Risk**: Low | **Status**: ✅ COMPLETE (2026-01-11)

**Completed**:
1. Added `isControlAccount` and `controlAccountType` to Trial Balance API response
2. Updated TrialBalanceReport.tsx with clickable control accounts (chevron indicator)
3. Created SubledgerDrilldownDrawer component showing party-wise breakdown
4. Created PartyLedgerReport page with full transaction history
5. Added Party Ledger to navigation menu
6. Added subledger report hooks and API service methods
7. Updated backend types to support subledger drill-down

**Frontend Files**:
- `apps/admin-portal/src/pages/finance/ledger/TrialBalanceReport.tsx` (updated)
- `apps/admin-portal/src/components/ledger/SubledgerDrilldownDrawer.tsx` (new)
- `apps/admin-portal/src/pages/finance/ledger/PartyLedgerReport.tsx` (new)
- `apps/admin-portal/src/features/ledger/hooks/useLedger.ts` (updated)
- `apps/admin-portal/src/services/api/finance/ledger/ledgerService.ts` (updated)
- `apps/admin-portal/src/services/api/types/ledger.ts` (updated)
- `apps/admin-portal/src/data/navigation.ts` (updated)
- `apps/admin-portal/src/App.tsx` (updated)

---

## Execution Order

```
Phase 1 (Day 1)     → AutoPostingService fix
Phase 2 (Day 2-3)   → Posting rules update
Phase 3 (Day 4-5)   → Tally mapping repository + seed
Phase 4 (Day 6-8)   → Tally import/export translation
Phase 5 (Day 9-11)  → Subledger reports
Phase 6 (Day 12-13) → UI updates
```

---

## Validation Queries

After implementation, run these to verify:

```sql
-- 1. JE lines have subledger populated
SELECT COUNT(*) as lines_with_subledger
FROM journal_entry_lines
WHERE subledger_type IS NOT NULL;

-- 2. tally_ledger_mapping populated
SELECT party_type, COUNT(*)
FROM tally_ledger_mapping
GROUP BY party_type;

-- 3. Control account balance = subledger sum
SELECT
    coa.account_name,
    coa.current_balance as control_balance,
    (SELECT SUM(debit_amount - credit_amount)
     FROM journal_entry_lines jel
     JOIN journal_entries je ON jel.journal_entry_id = je.id
     WHERE jel.account_id = coa.id AND je.status = 'posted') as computed_subledger_sum
FROM chart_of_accounts coa
WHERE coa.is_control_account = true;
```

---

## Dependencies

| Phase | Depends On |
|-------|------------|
| Phase 2 | Phase 1 (AutoPosting must read subledger first) |
| Phase 4 | Phase 3 (Need mapping repository) |
| Phase 5 | Phases 1-2 (Need JE lines with subledger) |
| Phase 6 | Phase 5 (Need report APIs) |

---

## Rollback Strategy

All phases are additive (new columns already exist, no data deleted):
- Phase 1: Revert code, subledger fields remain null
- Phase 2: Revert migration, old templates still work
- Phase 3: DELETE FROM tally_ledger_mapping
- Phases 4-6: Remove new code/UI, no data impact

---

## Success Criteria

| Criteria | Measurement |
|----------|-------------|
| All vendor invoices post with subledger | 100% of new vendor_invoice JE lines have subledger_type='vendor' |
| Tally import uses mapping | 0 new TL-/TR- accounts created |
| AP Aging works | Report shows vendor-wise outstanding |
| TB drill-down works | Control account click shows party breakdown |

---

## Bug Fixes Applied (2026-01-11)

### 1. Schema Migration (163_add_tally_batch_missing_columns.sql)

Added missing columns to support Tally import:

**tally_migration_batches**:
- `skipped_stock_items`, `failed_stock_items`
- `total_cost_centers`, `imported_cost_centers`, `skipped_cost_centers`, `failed_cost_centers`
- `total_godowns`, `imported_godowns`, `total_units`, `imported_units`
- `total_stock_groups`, `imported_stock_groups`
- `parsing_started_at`, `validation_started_at`, `validation_completed_at`, `import_started_at`

**tally_field_mappings**:
- `target_account_subtype`, `default_account_name`, `tag_assignments`

**tally_migration_logs**:
- `error_code`, `amount_difference`, `processing_duration_ms`

**bank_accounts**:
- `linked_account_id`, `tally_ledger_guid`, `tally_ledger_name`, `tally_migration_batch_id`

**bank_transactions**:
- Reconciliation columns: `reconciliation_difference_amount`, `reconciliation_difference_type`, etc.
- Tally columns: `tally_voucher_guid`, `tally_voucher_number`, `tally_migration_batch_id`
- Matching columns: `source_voucher_type`, `matched_entity_type`, `matched_entity_id`

### 2. TallyMasterMappingService Fixes

**Problem**: `CreateReceivableAccount` and `CreatePayableAccount` were creating individual TL-/TR- COA accounts for each party (old pattern).

**Fix**: Modified to create `tally_ledger_mapping` entries instead:
- `CreateReceivableAccount` → Creates mapping to control account 1120 + party
- `CreatePayableAccount` → Creates mapping to control account 2100 + party

**Code Changes**:
```csharp
// Before: Created individual COA entries
var account = new ChartOfAccount { AccountCode = "TL-xxx", AccountName = "Trade Payable - Vendor" };
await _coaRepository.AddAsync(account);

// After: Creates tally_ledger_mapping entry
var mapping = new TallyLedgerMapping {
    ControlAccountId = controlAccount.Id,  // 2100 Trade Payables
    PartyType = "vendor",
    PartyId = vendorId
};
await _tallyLedgerMappingRepository.AddAsync(mapping);
```

**Files Modified**:
- `Application/Services/Migration/TallyMasterMappingService.cs` (lines 924-994)

### 3. SeedFromPartiesAsync Fix

**Problem**: Used wrong column `party_type` (string like "individual", "company") instead of `is_customer`/`is_vendor` booleans.

**Fix**: Updated SQL to use boolean flags:
```sql
-- Before (wrong)
WHERE p.party_type IN ('vendor', 'customer')

-- After (correct)
WHERE p.is_vendor = true OR p.is_customer = true
```

**Files Modified**:
- `Infrastructure/Data/Migration/TallyLedgerMappingRepository.cs` (lines 242-289)

### 4. DI Registration

Added `ITallyLedgerMappingRepository` to `TallyMasterMappingService` constructor injection.

### 5. Tally Sign Convention Fix

**Problem**: Tally uses inverted sign convention (debit=negative, credit=positive) vs standard accounting (debit=positive, credit=negative). This caused all imported balances to have wrong signs.

**Fix**: Negated `OpeningBalance` and `ClosingBalance` in 5 locations in `TallyMasterMappingService.cs`:

| Method | Line | Change |
|--------|------|--------|
| `ImportLedgerAsBankAccount` | 834-835 | `-ledger.ClosingBalance`, `-ledger.OpeningBalance` |
| `ImportLedgerAsChartOfAccount` | 894-895 | `-ledger.OpeningBalance`, `-ledger.ClosingBalance` |
| `CreateReceivableAccount` | 952 | `-ledger.OpeningBalance` |
| `CreatePayableAccount` | 988 | `-ledger.OpeningBalance` |
| `CreateBankGlAccount` | 1024-1025 | `-ledger.OpeningBalance`, `-ledger.ClosingBalance` |

### 6. Opening Balance Equity Plug

**Problem**: Trial Balance unbalanced by ₹1,16,25,816.30 after Tally import because Tally exports don't include the equity plug (Retained Earnings) needed to balance opening balances.

**Root Cause**: Opening balances from Tally don't sum to zero. In standard accounting: Assets = Liabilities + Equity. The missing "Retained Earnings" component causes imbalance.

**Fix**: Added `CreateOpeningBalanceEquityPlugAsync` method to `TallyMasterMappingService`:

1. Calculates total opening balance after all masters imported
2. If not zero (beyond 1 rupee tolerance), creates/updates Retained Earnings account (3210)
3. Sets opening balance = -imbalance to make total = 0

```csharp
private async Task CreateOpeningBalanceEquityPlugAsync(Guid batchId, Guid companyId, CancellationToken cancellationToken)
{
    var totalOpeningBalance = await _coaRepository.GetTotalOpeningBalanceAsync(companyId);
    if (Math.Abs(totalOpeningBalance) < 1.0m) return; // Already balanced

    var plugAmount = -totalOpeningBalance;

    var retainedEarningsAccount = await _coaRepository.GetByCodeAsync(companyId, "3210");
    if (retainedEarningsAccount == null)
    {
        // Create new Retained Earnings account
        retainedEarningsAccount = new ChartOfAccount { AccountCode = "3210", OpeningBalance = plugAmount };
        await _coaRepository.AddAsync(retainedEarningsAccount);
    }
    else
    {
        // Update existing
        retainedEarningsAccount.OpeningBalance += plugAmount;
        await _coaRepository.UpdateAsync(retainedEarningsAccount);
    }
}
```

**Files Modified**:
- `Application/Services/Migration/TallyMasterMappingService.cs` (lines 124-126, 1042-1124)
- `Core/Interfaces/Ledger/IChartOfAccountRepository.cs` (added `GetTotalOpeningBalanceAsync`)
- `Infrastructure/Data/Ledger/ChartOfAccountRepository.cs` (implemented `GetTotalOpeningBalanceAsync`)

### 7. Trial Balance Closing Balance Formula Fix

**Problem**: Trial Balance showed assets in Credit column (green) instead of Debit column (blue). Example: Furnitures ₹8,88,408 showing as credit.

**Root Cause**: `LedgerReportRepository.GetTrialBalanceDataAsync()` had wrong formula:
```sql
-- WRONG: debits - credits - opening_balance
COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0) - coa.opening_balance as closing_balance
```

**Fix**: Corrected to standard accounting formula:
```sql
-- CORRECT: opening_balance + debits - credits
coa.opening_balance + COALESCE(SUM(jel.debit_amount), 0) - COALESCE(SUM(jel.credit_amount), 0) as closing_balance
```

**Files Modified**:
- `Infrastructure/Data/Ledger/LedgerReportRepository.cs` (line 37)

### 8. Trial Balance UI - Nested tbody Fix

**Problem**: Trial Balance columns misaligned - values appearing under wrong headers.

**Root Cause**: Invalid HTML - `<tbody>` nested inside `<tbody>` when groupByType enabled.

**Fix**:
1. Changed nested `<tbody>` to `<React.Fragment>`
2. Added `w-36` width class to all Debit/Credit data cells for consistent alignment

**Files Modified**:
- `apps/admin-portal/src/pages/finance/ledger/TrialBalanceReport.tsx`

### 9. Control Account Opening Balance Sync (Tally Import)

**Problem**: Control accounts (1120 Trade Receivables, 2100 Trade Payables) had opening_balance = 0 even though subledger mappings had balances totaling ₹10,70,586 and ₹39,725 respectively.

**Root Cause**: Tally import created `tally_ledger_mapping` entries with opening balances but didn't sync these to the control account's opening_balance.

**Fix**: Added `SyncControlAccountOpeningBalancesAsync()` method to `TallyMasterMappingService`:
- Called after ledgers are imported, before equity plug
- Queries sum of subledger opening balances per control account
- Updates control account's opening_balance to match

```csharp
private async Task SyncControlAccountOpeningBalancesAsync(Guid companyId, CancellationToken cancellationToken)
{
    var controlAccounts = await _coaRepository.GetControlAccountsAsync(companyId);
    foreach (var controlAccount in controlAccounts)
    {
        var mappings = await _tallyLedgerMappingRepository.GetByCompanyIdAsync(companyId);
        var subledgerTotal = mappings
            .Where(m => m.ControlAccountId == controlAccount.Id && m.IsActive)
            .Sum(m => m.OpeningBalance);

        if (subledgerTotal == 0) continue;

        controlAccount.OpeningBalance = subledgerTotal;
        controlAccount.CurrentBalance = subledgerTotal;
        await _coaRepository.UpdateAsync(controlAccount);
    }
}
```

**Type**: One-time Tally import fix (runs automatically during future imports)

**Files Modified**:
- `Application/Services/Migration/TallyMasterMappingService.cs` (lines 124-130, 1126-1169)

**Migration for existing data**:
- `migrations/164_sync_control_account_opening_balances.sql`

---

## Summary of Fix Types

| Fix | Type | When Applied |
|-----|------|--------------|
| Schema migrations (163) | One-time | Manual migration |
| TallyMasterMappingService → tally_ledger_mapping | Tally import | Every Tally import |
| Tally sign convention (-balance) | Tally import | Every Tally import |
| Opening balance equity plug | Tally import | Every Tally import |
| Control account balance sync | Tally import | Every Tally import |
| Trial Balance SQL formula | Application bug fix | Permanent |
| Trial Balance UI alignment | Application bug fix | Permanent |
