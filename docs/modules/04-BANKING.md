# Banking Module

## Status
- **Current State**: Working (Active Development)
- **Last Updated**: 2026-01-09
- **Active Issues**: Bank feed integration not implemented

---

## Overview

The Banking module manages bank accounts, imports bank statements, and handles reconciliation between book entries (payments, receipts) and actual bank transactions. Critical for maintaining accurate cash position and BRS (Bank Reconciliation Statement).

### Key Features
- Bank account master
- Bank statement import (CSV, manual)
- Transaction categorization
- Auto-matching with payments/receipts
- Manual reconciliation workflow
- BRS report generation

### Related Implementation Plans
- `docs/PLAN_3_BANK_TRANSACTIONS_IMPORT.md` - Tally import for bank transactions

---

## Database Schema

### bank_accounts
Bank account master with ledger linking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `account_name` | VARCHAR | Display name |
| `account_number` | VARCHAR | Bank account number |
| `bank_name` | VARCHAR | Bank name |
| `ifsc_code` | VARCHAR | IFSC code |
| `branch_name` | VARCHAR | Branch name |
| `account_type` | VARCHAR | `current`, `savings`, `od`, `cc` |
| `currency` | VARCHAR | Account currency (default INR) |
| **Balances** |
| `opening_balance` | NUMERIC | Opening balance |
| `current_balance` | NUMERIC | Current balance |
| `as_of_date` | DATE | Balance as of date |
| **Flags** |
| `is_primary` | BOOLEAN | Primary account flag |
| `is_active` | BOOLEAN | Active flag |
| `notes` | TEXT | Notes |
| **Ledger Link** |
| `linked_account_id` | UUID | FK to chart_of_accounts |
| **Tally Migration** |
| `tally_ledger_guid` | VARCHAR | Tally GUID |
| `tally_ledger_name` | VARCHAR | Tally ledger name |
| `tally_migration_batch_id` | UUID | Migration batch |

### bank_transactions
Individual bank transactions (from statement or Tally import).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `bank_account_id` | UUID | FK to bank_accounts |
| `transaction_date` | DATE | Transaction date |
| `value_date` | DATE | Value date |
| `description` | TEXT | Bank description/narration |
| `reference_number` | VARCHAR | Reference/UTR number |
| `cheque_number` | VARCHAR | Cheque number |
| `transaction_type` | VARCHAR | `debit`, `credit` |
| `amount` | NUMERIC | Transaction amount |
| `balance_after` | NUMERIC | Balance after transaction |
| `category` | VARCHAR | Transaction category |
| **Reconciliation** |
| `is_reconciled` | BOOLEAN | Reconciled flag |
| `reconciled_type` | VARCHAR | Type of matched entity |
| `reconciled_id` | UUID | FK to matched entity |
| `reconciled_at` | TIMESTAMP | Reconciliation timestamp |
| `reconciled_by` | VARCHAR | Reconciled by user |
| `reconciled_journal_entry_id` | UUID | FK to journal_entries |
| `reconciled_je_line_id` | UUID | FK to specific JE line |
| **Entity Matching** |
| `matched_entity_type` | VARCHAR | `vendor_payments`, `payments`, `contractor_payments`, etc. |
| `matched_entity_id` | UUID | FK to matched entity |
| `source_voucher_type` | VARCHAR | Source voucher type |
| **Import** |
| `import_source` | VARCHAR | `csv`, `tally`, `manual`, `api` |
| `import_batch_id` | UUID | Import batch ID |
| `raw_data` | JSONB | Original import data |
| `transaction_hash` | VARCHAR | Dedup hash |
| **Pairing** (for internal transfers) |
| `paired_transaction_id` | UUID | FK to paired transaction |
| `pair_type` | VARCHAR | `internal_transfer`, `reversal` |
| **Reversal** |
| `is_reversal_transaction` | BOOLEAN | Is reversal |
| `reversal_journal_entry_id` | UUID | FK to reversal JE |
| **Reconciliation Adjustments** |
| `reconciliation_difference_amount` | NUMERIC | Difference amount |
| `reconciliation_difference_type` | VARCHAR | `tds`, `bank_charge`, `forex` |
| `reconciliation_difference_notes` | VARCHAR | Difference notes |
| `reconciliation_tds_section` | VARCHAR | TDS section if applicable |
| `reconciliation_adjustment_journal_id` | UUID | Adjustment JE |
| **Tally** |
| `tally_voucher_guid` | VARCHAR | Tally GUID |
| `tally_voucher_number` | VARCHAR | Tally voucher number |
| `tally_migration_batch_id` | UUID | Migration batch |

### bank_transaction_matches
Many-to-many matching for split transactions.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `bank_transaction_id` | UUID | FK to bank_transactions |
| `matched_type` | VARCHAR | Entity type |
| `matched_id` | UUID | Entity ID |
| `matched_amount` | NUMERIC | Matched amount |
| `matched_at` | TIMESTAMP | Match timestamp |
| `matched_by` | VARCHAR | Matched by user |
| `match_method` | VARCHAR | `auto`, `manual`, `rule` |
| `confidence_score` | NUMERIC | Auto-match confidence (0-1) |
| `notes` | TEXT | Match notes |
| `journal_entry_id` | UUID | Related JE |
| `journal_entry_line_id` | UUID | Related JE line |

---

## Backend Structure

### Entities
- `Core/Entities/BankAccount.cs`
- `Core/Entities/BankTransaction.cs`
- `Core/Entities/BankTransactionMatch.cs`

### Repositories
- `Infrastructure/Data/BankAccountRepository.cs`
- `Infrastructure/Data/BankTransactionRepository.cs`

### Services
- `Application/Services/BankAccountService.cs`
- `Application/Services/BankReconciliationService.cs`

### Controllers
- `WebApi/Controllers/Banking/BankAccountsController.cs`
- `WebApi/Controllers/Banking/BankTransactionsController.cs`
- `WebApi/Controllers/Banking/BankReconciliationController.cs`

---

## Frontend Structure

### Pages
- `pages/finance/banking/BankAccountsPage.tsx` - Account list
- `pages/finance/banking/BankAccountDetailPage.tsx` - Account detail
- `pages/finance/banking/BankTransactionsPage.tsx` - Transaction list
- `pages/finance/banking/BankImportPage.tsx` - Statement import
- `pages/finance/banking/BankReconciliationPage.tsx` - BRS workflow

### Services
- `services/api/finance/banking/bankAccountService.ts`
- `services/api/finance/banking/bankTransactionService.ts`

### Forms
- `components/forms/BankAccountForm.tsx`
- `components/forms/BankTransactionForm.tsx`

---

## API Endpoints

### Bank Accounts
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/bank-accounts` | List accounts |
| GET | `/api/bank-accounts/{id}` | Get account |
| POST | `/api/bank-accounts` | Create account |
| PUT | `/api/bank-accounts/{id}` | Update account |

### Bank Transactions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/bank-transactions` | List transactions |
| GET | `/api/bank-transactions/paged` | Paginated list |
| GET | `/api/bank-transactions/{id}` | Get transaction |
| POST | `/api/bank-transactions/import` | Import statement |
| POST | `/api/bank-transactions/{id}/match` | Match to entity |
| POST | `/api/bank-transactions/{id}/unmatch` | Unmatch |
| POST | `/api/bank-transactions/{id}/categorize` | Set category |

### Reconciliation
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/bank-reconciliation/unmatched` | Unmatched transactions |
| GET | `/api/bank-reconciliation/suggestions/{id}` | Match suggestions |
| POST | `/api/bank-reconciliation/auto-match` | Run auto-matching |
| GET | `/api/bank-reconciliation/brs` | Generate BRS report |

---

## Business Rules

### Transaction Types
| Type | Description |
|------|-------------|
| `debit` | Outgoing payment (reduces balance) |
| `credit` | Incoming receipt (increases balance) |

### Matchable Entity Types
- `payments` - Customer receipts
- `vendor_payments` - Vendor payments
- `contractor_payments` - Contractor payments
- `statutory_payments` - Statutory payments
- `journal_entries` - Manual JE with bank line

### Auto-Matching Algorithm
1. Match by reference number (exact)
2. Match by amount + date (±3 days)
3. Match by description parsing (UTR, IMPS ref)
4. Confidence scoring based on match quality

### Reconciliation Workflow
```
unmatched → suggested → matched → reconciled
```

### BRS (Bank Reconciliation Statement)
```
Balance as per Bank Statement
(+) Deposits in transit (recorded but not cleared)
(-) Outstanding cheques (issued but not cleared)
(+/-) Bank errors
= Balance as per Books
```

### Adjustment Journal Entry
When reconciliation difference exists:
- TDS: DR TDS Receivable, CR Bank
- Bank Charge: DR Bank Charges, CR Bank
- Forex: DR/CR Forex Gain/Loss, CR/DR Bank

---

## Import Formats

### CSV Import
Required columns:
- `Date` - Transaction date
- `Description` - Narration
- `Debit` / `Credit` or `Amount` with sign
- `Balance` - Running balance (optional)
- `Reference` - UTR/Reference number (optional)

### Statement Parsing
Auto-extracts:
- UTR numbers: `UTR:XXXXX` or `UTRNO:XXXXX`
- IMPS references: `IMPS-XXXXX`
- NEFT references: `NEFT-XXXXX`
- Cheque numbers: `CHQ:XXXXX`

---

## Current Gaps / TODO

- [ ] Bank feed API integration (AA framework)
- [ ] Auto-categorization rules engine
- [ ] Multi-currency bank accounts
- [ ] Foreign currency revaluation
- [ ] Bulk matching operations
- [ ] Statement PDF parsing

---

## Related Modules

- [Ledger](05-LEDGER.md) - Bank account in chart of accounts
- [Accounts Payable](02-ACCOUNTS-PAYABLE.md) - Vendor payments
- [Contractor Payments](03-CONTRACTOR-PAYMENTS.md) - Contractor disbursements
- [Billing](01-BILLING.md) - Customer receipts

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Bank reconciliation actively being developed on `ap_ar` branch
- Entity matching enhanced with `matched_entity_type`/`matched_entity_id`
