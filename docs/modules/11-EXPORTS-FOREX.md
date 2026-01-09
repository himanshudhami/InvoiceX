# Exports & Forex Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: EDPMS auto-reporting pending

---

## Overview

The Exports & Forex module handles export invoice tracking, FIRC (Foreign Inward Remittance Certificate) management, LUT (Letter of Undertaking) registration, and forex gain/loss computation. Critical for exporters to maintain RBI/FEMA compliance.

### Key Features
- Export invoice tracking with foreign currency
- FIRC tracking and invoice allocation
- LUT register for zero-rated exports
- Forex gain/loss computation
- EDPMS reporting readiness
- Export receivables aging
- FEMA compliance tracking

### Key Entities
- **FIRC Tracking** - Foreign remittance certificates
- **FIRC Invoice Links** - FIRC-to-invoice allocation
- **Forex Transactions** - Currency conversion records
- **LUT Register** - Letter of Undertaking records

---

## Database Schema

### firc_tracking
Foreign Inward Remittance Certificate tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `firc_number` | VARCHAR | FIRC number from bank |
| `firc_date` | DATE | FIRC date |
| **Bank Details** |
| `bank_name` | VARCHAR | Receiving bank |
| `bank_branch` | VARCHAR | Branch |
| `bank_swift_code` | VARCHAR | SWIFT code |
| `purpose_code` | VARCHAR | RBI purpose code |
| **Currency** |
| `foreign_currency` | VARCHAR | Currency code (USD, EUR) |
| `foreign_amount` | NUMERIC | Foreign currency amount |
| `inr_amount` | NUMERIC | INR equivalent |
| `exchange_rate` | NUMERIC | Applied exchange rate |
| **Remitter** |
| `remitter_name` | VARCHAR | Sender name |
| `remitter_country` | VARCHAR | Sender country |
| `remitter_bank` | VARCHAR | Sender's bank |
| **Beneficiary** |
| `beneficiary_name` | VARCHAR | Our company name |
| `beneficiary_account` | VARCHAR | Our bank account |
| **Links** |
| `payment_id` | UUID | FK to payments |
| **EDPMS** |
| `edpms_reported` | BOOLEAN | Reported to EDPMS |
| `edpms_report_date` | DATE | Report date |
| `edpms_reference` | VARCHAR | EDPMS reference |
| **Status** |
| `status` | VARCHAR | `received`, `allocated`, `reported` |
| `notes` | TEXT | Notes |
| `created_by` | UUID | Created by user |

### firc_invoice_links
Allocation of FIRC amounts to export invoices.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `firc_id` | UUID | FK to firc_tracking |
| `invoice_id` | UUID | FK to invoices |
| `allocated_amount` | NUMERIC | Amount in foreign currency |
| `allocated_amount_inr` | NUMERIC | Amount in INR |
| `created_at` | TIMESTAMP | Created timestamp |

### forex_transactions
Foreign exchange transactions and gain/loss tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `transaction_date` | DATE | Transaction date |
| `financial_year` | VARCHAR | FY |
| **Source** |
| `source_type` | VARCHAR | `invoice`, `payment`, `firc`, `vendor_payment` |
| `source_id` | UUID | FK to source document |
| `source_number` | VARCHAR | Document number |
| **Currency** |
| `currency` | VARCHAR | Foreign currency code |
| `foreign_amount` | NUMERIC | Foreign currency amount |
| `exchange_rate` | NUMERIC | Exchange rate used |
| `inr_amount` | NUMERIC | INR equivalent |
| **Transaction** |
| `transaction_type` | VARCHAR | `booking`, `realization`, `revaluation` |
| **Gain/Loss** |
| `forex_gain_loss` | NUMERIC | Gain (+) or Loss (-) |
| `gain_loss_type` | VARCHAR | `realized`, `unrealized` |
| `related_forex_id` | UUID | Related booking transaction |
| **Ledger** |
| `journal_entry_id` | UUID | FK to journal_entries |
| `is_posted` | BOOLEAN | Posted to ledger |
| `created_by` | UUID | Created by user |

### lut_register
Letter of Undertaking for zero-rated exports.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `lut_number` | VARCHAR | LUT number |
| `financial_year` | VARCHAR | FY (valid for one year) |
| `gstin` | VARCHAR | Company GSTIN |
| `valid_from` | DATE | Validity start |
| `valid_to` | DATE | Validity end |
| `filing_date` | DATE | Filing date |
| `arn` | VARCHAR | GST ARN number |
| `status` | VARCHAR | `draft`, `filed`, `active`, `expired` |
| `notes` | TEXT | Notes |
| `created_by` | UUID | Created by user |

---

## Backend Structure

### Controllers
- `WebApi/Controllers/Forex/FircController.cs`
- `WebApi/Controllers/Forex/LutController.cs`

### Entities
- `Core/Entities/Forex/FircTracking.cs`
- `Core/Entities/Forex/FircInvoiceLink.cs`
- `Core/Entities/Forex/ForexTransaction.cs`
- `Core/Entities/Forex/LutRegister.cs`

---

## Frontend Structure

### Pages
- `pages/finance/exports/ExportDashboard.tsx` - Overview
- `pages/finance/exports/FircManagement.tsx` - FIRC tracking
- `pages/finance/exports/LutRegister.tsx` - LUT management
- `pages/finance/exports/ReceivablesAgeing.tsx` - Export aging
- `pages/finance/exports/FemaCompliance.tsx` - FEMA tracking

### Services
- `services/api/finance/exports/fircService.ts`
- `services/api/finance/exports/lutService.ts`
- `services/api/finance/exports/forexService.ts`

---

## API Endpoints

### FIRC
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/firc` | List FIRCs |
| GET | `/api/firc/paged` | Paginated list |
| GET | `/api/firc/{id}` | Get FIRC |
| POST | `/api/firc` | Create FIRC |
| PUT | `/api/firc/{id}` | Update FIRC |
| POST | `/api/firc/{id}/allocate` | Allocate to invoices |
| POST | `/api/firc/{id}/report-edpms` | Mark EDPMS reported |
| GET | `/api/firc/unallocated` | Unallocated FIRCs |

### LUT Register
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/lut` | List LUTs |
| GET | `/api/lut/active` | Current active LUT |
| POST | `/api/lut` | Create LUT |
| PUT | `/api/lut/{id}` | Update LUT |
| POST | `/api/lut/{id}/file` | Mark as filed |

### Forex Transactions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/forex-transactions` | List transactions |
| GET | `/api/forex-transactions/paged` | Paginated list |
| POST | `/api/forex-transactions/compute-gain-loss` | Compute realized gain/loss |
| POST | `/api/forex-transactions/revalue` | Month-end revaluation |

---

## Business Rules

### FIRC Allocation
1. FIRC received from bank after export payment
2. Link FIRC to outstanding export invoices
3. Multiple invoices can be linked to one FIRC
4. Single invoice can have multiple FIRCs
5. Total allocation cannot exceed FIRC amount

### Forex Gain/Loss Calculation
**Realized Gain/Loss** (on receipt):
```
Gain/Loss = FIRC INR Amount - Invoice INR Amount (at booking rate)
```

**Unrealized Gain/Loss** (month-end revaluation):
```
Gain/Loss = Outstanding Amount × (Current Rate - Booking Rate)
```

### Exchange Rate Sources
| Type | Rate Used |
|------|-----------|
| Invoice Booking | RBI reference rate on invoice date |
| Payment Receipt | Bank credit rate |
| Month-end Reval | RBI closing rate |

### LUT Compliance
- LUT required for zero-rated exports without IGST
- Valid for one financial year
- Must be renewed before expiry
- LUT number mandatory on export invoices

### FEMA Compliance Timeline
| Event | Timeline |
|-------|----------|
| Export Invoice | Shipped within 15 days |
| Receive Payment | Within 9 months of shipment |
| EDPMS Reporting | Within 15 days of receipt |
| FIRC Issuance | Bank issues within 15 days |

### RBI Purpose Codes (Common)
| Code | Description |
|------|-------------|
| P0101 | Software services |
| P0102 | Software products |
| P0103 | ITES/BPO |
| P0303 | Consultancy services |
| P0802 | Engineering services |

---

## Integration Points

### With Billing Module
- Export invoices marked with `is_export = true`
- Foreign currency invoicing
- LUT reference on zero-rated invoices

### With Banking Module
- FIRC linked to bank receipts
- Bank transactions in foreign currency

### With Ledger Module
- Forex gain/loss journal entries
- AR in foreign currency
- Month-end revaluation entries

### With GST Compliance
- LUT for zero-rated exports
- GSTR-1 export reporting

---

## Forex Journal Entries

### Export Invoice Booking
```
DR: Trade Receivable (Foreign)    Rs. X (at booking rate)
    CR: Export Revenue            Rs. X
```

### FIRC Receipt
```
DR: Bank Account                  Rs. Y (at receipt rate)
DR: Forex Loss (if loss)          Rs. Z
    CR: Trade Receivable (Foreign) Rs. X (original booking)
    CR: Forex Gain (if gain)      Rs. Z
```

### Month-end Revaluation (Unrealized)
```
DR/CR: Trade Receivable (Foreign)  Rs. Z
    CR/DR: Unrealized Forex Gain/Loss  Rs. Z
```

---

## Reports

### Export Receivables Aging
- Outstanding export invoices by age bucket
- Foreign currency and INR columns
- Customer-wise summary

### FIRC Utilization
- FIRCs received vs allocated
- Unallocated amounts
- Invoice-wise allocation

### Forex Gain/Loss Report
- Period-wise realized gain/loss
- Customer/currency-wise breakdown
- Unrealized position summary

### LUT Utilization
- Exports under LUT
- LUT validity status
- Renewal reminders

---

## Current Gaps / TODO

- [ ] EDPMS XML/API integration
- [ ] Automatic forex gain/loss on payment
- [ ] Multi-currency bank account support
- [ ] Forward contract tracking
- [ ] Hedging transaction tracking
- [ ] RBI rate auto-fetch
- [ ] Softex form generation
- [ ] Export obligation tracking (advance license)

---

## Related Modules

- [Billing](01-BILLING.md) - Export invoices
- [Banking](04-BANKING.md) - Foreign receipts
- [Ledger](05-LEDGER.md) - Forex posting
- [GST Compliance](09-GST-COMPLIANCE.md) - LUT, GSTR-1

---

## Session Notes

### 2026-01-09
- Initial documentation created
- FIRC tracking operational
- LUT register implemented
- Forex gain/loss computation functional
