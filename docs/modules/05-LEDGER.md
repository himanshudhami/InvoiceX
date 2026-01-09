# Ledger Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Ledger module provides double-entry accounting capabilities: chart of accounts, journal entries, and financial reports. All business transactions (invoices, payments, payroll) post to this module. Supports Indian accounting standards with schedule references.

### Key Entities
- **Chart of Accounts** - Hierarchical account structure
- **Journal Entries** - Double-entry transactions
- **Journal Entry Lines** - Individual debits/credits
- **Account Period Balances** - Pre-computed monthly balances

### Database Views
- `v_trial_balance` - Trial balance report
- `v_income_statement` - Profit & Loss
- `v_balance_sheet` - Balance sheet
- `v_account_ledger` - Account-wise ledger

---

## Database Schema

### chart_of_accounts
Hierarchical chart of accounts with Indian accounting support.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `account_code` | VARCHAR | Account code (e.g., `1001`, `2001.01`) |
| `account_name` | VARCHAR | Account name |
| `account_type` | VARCHAR | `asset`, `liability`, `equity`, `revenue`, `expense` |
| `account_subtype` | VARCHAR | Subtype (e.g., `current_asset`, `fixed_asset`) |
| **Hierarchy** |
| `parent_account_id` | UUID | FK to parent account |
| `depth_level` | INTEGER | Nesting depth (0 = root) |
| `full_path` | TEXT | Full path (e.g., `Assets > Current > Bank`) |
| `sort_order` | INTEGER | Display order |
| **Indian Accounting** |
| `schedule_reference` | VARCHAR | Schedule III reference (e.g., `Schedule III.A.1`) |
| `gst_treatment` | VARCHAR | `input`, `output`, `exempt`, `nil_rated` |
| **Control Flags** |
| `is_control_account` | BOOLEAN | Subledger control (AR/AP) |
| `is_system_account` | BOOLEAN | Protected system account |
| `is_bank_account` | BOOLEAN | Bank account flag |
| `linked_bank_account_id` | UUID | FK to bank_accounts |
| **Balances** |
| `normal_balance` | VARCHAR | `debit` or `credit` |
| `opening_balance` | NUMERIC | Opening balance |
| `current_balance` | NUMERIC | Current balance |
| `is_active` | BOOLEAN | Active flag |
| **Tally Migration** |
| `tally_ledger_guid` | VARCHAR | Tally GUID |
| `tally_ledger_name` | VARCHAR | Tally ledger name |
| `tally_group_name` | VARCHAR | Tally parent group |

### Account Types & Subtypes

| Type | Subtypes | Normal Balance |
|------|----------|----------------|
| `asset` | `current_asset`, `fixed_asset`, `investment`, `intangible` | Debit |
| `liability` | `current_liability`, `long_term_liability`, `provisions` | Credit |
| `equity` | `share_capital`, `reserves`, `retained_earnings` | Credit |
| `revenue` | `operating_revenue`, `other_income`, `interest_income` | Credit |
| `expense` | `operating_expense`, `financial_expense`, `depreciation` | Debit |

### journal_entries
Double-entry journal entry header with source tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `journal_number` | VARCHAR | Auto-generated number |
| `journal_date` | DATE | Transaction date |
| `financial_year` | VARCHAR | FY (e.g., `2024-25`) |
| `period_month` | INTEGER | Month (1-12) |
| **Classification** |
| `entry_type` | VARCHAR | `standard`, `adjusting`, `closing`, `opening`, `reversal` |
| `source_type` | VARCHAR | `invoice`, `payment`, `payroll`, `manual`, `migration` |
| `source_id` | UUID | FK to source entity |
| `source_number` | VARCHAR | Source reference number |
| **Content** |
| `description` | TEXT | Entry description |
| `narration` | TEXT | Detailed narration |
| `total_debit` | NUMERIC | Total debits |
| `total_credit` | NUMERIC | Total credits |
| **Status & Workflow** |
| `status` | VARCHAR | `draft`, `posted`, `approved`, `reversed` |
| `posted_at` | TIMESTAMP | Posting timestamp |
| `posted_by` | UUID | Posted by user |
| `approved_by` | UUID | Approved by user |
| `approved_at` | TIMESTAMP | Approval timestamp |
| **Reversal** |
| `is_reversed` | BOOLEAN | Reversed flag |
| `reversal_of_id` | UUID | FK to original entry |
| `reversed_by_id` | UUID | FK to reversal entry |
| `reversal_reason` | TEXT | Reversal reason |
| **Posting Rules** |
| `posting_rule_id` | UUID | FK to posting_rules |
| `rule_pack_version` | VARCHAR | Rule pack version |
| `rule_code` | VARCHAR | Rule code used |
| **Correction** |
| `correction_of_id` | UUID | FK to corrected entry |
| `idempotency_key` | VARCHAR | Prevent duplicates |

### journal_entry_lines
Individual debit/credit lines with subledger support.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `journal_entry_id` | UUID | FK to journal_entries |
| `account_id` | UUID | FK to chart_of_accounts |
| `debit_amount` | NUMERIC | Debit amount (0 if credit) |
| `credit_amount` | NUMERIC | Credit amount (0 if debit) |
| `line_number` | INTEGER | Line sequence |
| **Multi-Currency** |
| `currency` | VARCHAR | Line currency |
| `exchange_rate` | NUMERIC | Exchange rate to INR |
| `foreign_debit` | NUMERIC | Foreign currency debit |
| `foreign_credit` | NUMERIC | Foreign currency credit |
| **Subledger** |
| `subledger_type` | VARCHAR | `customer`, `vendor`, `employee`, `bank` |
| `subledger_id` | UUID | FK to subledger entity |
| **Reference** |
| `description` | TEXT | Line description |
| `reference_type` | VARCHAR | Reference type |
| `reference_id` | UUID | Reference entity |

### account_period_balances
Pre-computed monthly balances for fast reporting.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `account_id` | UUID | FK to chart_of_accounts |
| `financial_year` | VARCHAR | Financial year |
| `period_month` | INTEGER | Month (1-12) |
| `opening_balance` | NUMERIC | Opening balance |
| `period_debit` | NUMERIC | Month's total debits |
| `period_credit` | NUMERIC | Month's total credits |
| `closing_balance` | NUMERIC | Closing balance |
| `transaction_count` | INTEGER | Number of transactions |
| `last_computed_at` | TIMESTAMP | Last recompute time |

---

## Backend Structure

### Entities
- `Core/Entities/ChartOfAccount.cs`
- `Core/Entities/JournalEntry.cs`
- `Core/Entities/JournalEntryLine.cs`
- `Core/Entities/AccountPeriodBalance.cs`
- `Core/Entities/PostingRule.cs`

### Repositories
- `Infrastructure/Data/ChartOfAccountRepository.cs`
- `Infrastructure/Data/JournalEntryRepository.cs`
- `Infrastructure/Data/AccountPeriodBalanceRepository.cs`

### Services
- `Application/Services/LedgerService.cs`
- `Application/Services/JournalEntryService.cs`
- `Application/Services/PostingService.cs`

### Controllers
- `WebApi/Controllers/LedgerController.cs`
- `WebApi/Controllers/ChartOfAccountsController.cs`
- `WebApi/Controllers/JournalEntriesController.cs`

---

## Frontend Structure

### Pages
- `pages/ledger/ChartOfAccountsPage.tsx` - Account list
- `pages/ledger/JournalEntriesPage.tsx` - Journal list
- `pages/ledger/JournalEntryDetailPage.tsx` - Entry view
- `pages/ledger/CreateJournalEntryPage.tsx` - Manual entry
- `pages/ledger/TrialBalancePage.tsx` - Trial balance report
- `pages/ledger/IncomeStatementPage.tsx` - P&L report
- `pages/ledger/BalanceSheetPage.tsx` - Balance sheet
- `pages/ledger/AccountLedgerPage.tsx` - Account drill-down

### Services
- `services/api/finance/ledger/ledgerService.ts`
- `services/api/finance/ledger/journalEntryService.ts`
- `services/api/finance/ledger/chartOfAccountsService.ts`

### Forms
- `components/forms/ChartOfAccountForm.tsx`
- `components/forms/JournalEntryForm.tsx`

---

## API Endpoints

### Chart of Accounts
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/chart-of-accounts` | List all accounts |
| GET | `/api/chart-of-accounts/tree` | Hierarchical tree |
| GET | `/api/chart-of-accounts/{id}` | Get account |
| POST | `/api/chart-of-accounts` | Create account |
| PUT | `/api/chart-of-accounts/{id}` | Update account |
| DELETE | `/api/chart-of-accounts/{id}` | Delete account |

### Journal Entries
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/journal-entries` | List entries |
| GET | `/api/journal-entries/paged` | Paginated list |
| GET | `/api/journal-entries/{id}` | Get entry with lines |
| POST | `/api/journal-entries` | Create entry |
| PUT | `/api/journal-entries/{id}` | Update draft entry |
| POST | `/api/journal-entries/{id}/post` | Post entry |
| POST | `/api/journal-entries/{id}/reverse` | Reverse entry |

### Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/ledger/trial-balance` | Trial balance |
| GET | `/api/ledger/income-statement` | P&L statement |
| GET | `/api/ledger/balance-sheet` | Balance sheet |
| GET | `/api/ledger/account-ledger/{id}` | Account transactions |
| GET | `/api/ledger/cash-flow` | Cash flow statement |

---

## Business Rules

### Double-Entry Validation
- Every entry must have equal debits and credits
- `total_debit == total_credit` enforced
- Entries rejected if unbalanced

### Account Hierarchy Rules
- Child accounts inherit parent's `account_type`
- Control accounts require subledger entries
- System accounts cannot be deleted

### Journal Entry Status Flow
```
draft → posted → [approved] → [reversed]
```

### Posting Rules
Automated posting creates journal entries from:
- **Invoice posting**: AR Dr / Revenue Cr / GST Cr
- **Payment posting**: Bank Dr / AR Cr
- **Vendor invoice**: Expense Dr / AP Cr / GST Dr
- **Vendor payment**: AP Dr / Bank Cr
- **Payroll**: Salary Expense Dr / Salary Payable Cr / Deductions Cr
- **Contractor payment**: Expense Dr / TDS Payable Cr / Bank Cr

### Financial Year Convention
- Indian FY: April to March
- Format: `2024-25` (starts April 2024)
- Period month: 1 (April) to 12 (March)

### Balance Recomputation
- Triggered on journal posting
- Updates `account_period_balances`
- Cascades to all subsequent months

---

## Report Calculations

### Trial Balance
```sql
SELECT account_code, account_name,
       SUM(debit_amount) as total_debit,
       SUM(credit_amount) as total_credit
FROM journal_entry_lines jel
JOIN journal_entries je ON je.id = jel.journal_entry_id
WHERE je.status = 'posted'
  AND je.journal_date BETWEEN @start AND @end
GROUP BY account_id
```

### Income Statement
- **Revenue** (account_type = 'revenue'): Credit balances
- **Expenses** (account_type = 'expense'): Debit balances
- **Net Income** = Total Revenue - Total Expenses

### Balance Sheet
- **Assets** (account_type = 'asset'): Debit balances
- **Liabilities** (account_type = 'liability'): Credit balances
- **Equity** (account_type = 'equity'): Credit balances + Net Income
- Assets = Liabilities + Equity (must balance)

---

## Current Gaps / TODO

- [ ] Year-end closing entries workflow
- [ ] Opening balance import wizard
- [ ] Cost center / department tagging
- [ ] Budget vs actual comparison
- [ ] Multi-currency revaluation
- [ ] Audit trail for balance changes
- [ ] Schedule III-compliant report formatting

---

## Session Notes

### 2026-01-09
- Initial documentation created
- All core ledger functionality operational
- Reports compute correctly from database views
