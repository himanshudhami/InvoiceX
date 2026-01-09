# TDS/TCS Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: Form 16/16A generation pending

---

## Overview

The TDS/TCS module handles tax deduction/collection at source compliance. TDS is deducted on vendor payments, contractor payments, and salary; TCS is collected on sales. Supports statutory payments, return filing, and Form 26AS reconciliation.

### Key Features
- TDS section master with rates
- TCS section master with rates
- TDS receivable tracking (from customers)
- TCS transaction tracking
- Statutory payment tracking (challans)
- Tax rule packs (FY-wise rates)
- Form 24Q/26Q/27Q/27EQ data preparation
- Form 26AS reconciliation

### Key Entities
- **TDS Sections** - TDS section master (194C, 194J, etc.)
- **TCS Sections** - TCS section master (206C)
- **TDS Receivable** - TDS deducted by customers on our invoices
- **TCS Transactions** - TCS collected on sales
- **Statutory Payments** - TDS/TCS/PF/ESI challans
- **Tax Rule Packs** - FY-wise tax configurations

---

## Database Schema

### tds_sections
TDS section master with applicable rates.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `section_code` | VARCHAR | Section code (194C, 194J) |
| `section_name` | VARCHAR | Section description |
| `description` | TEXT | Detailed description |
| **Rates** |
| `default_rate` | NUMERIC | Default rate |
| `individual_rate` | NUMERIC | Rate for individuals |
| `company_rate` | NUMERIC | Rate for companies |
| `no_pan_rate` | NUMERIC | Rate if no PAN (20%) |
| **Thresholds** |
| `threshold_per_transaction` | NUMERIC | Per-transaction threshold |
| `threshold_annual` | NUMERIC | Annual threshold |
| **Applicability** |
| `applicable_to` | ARRAY | Payee types |
| `deductor_type` | ARRAY | Deductor types |
| **Ledger** |
| `payable_account_code` | VARCHAR | TDS payable account |
| `receivable_account_code` | VARCHAR | TDS receivable account |
| **Validity** |
| `effective_from` | DATE | Effective from date |
| `effective_to` | DATE | Effective to date |
| `is_active` | BOOLEAN | Active flag |

### tds_section_rates
TDS rates by rule pack (FY-wise).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `rule_pack_id` | UUID | FK to tax_rule_packs |
| `section_code` | VARCHAR | TDS section |
| `section_name` | VARCHAR | Section name |
| `rate_individual` | NUMERIC | Individual rate |
| `rate_company` | NUMERIC | Company rate |
| `rate_no_pan` | NUMERIC | No-PAN rate |
| `threshold_amount` | NUMERIC | Threshold amount |
| `threshold_type` | VARCHAR | `per_transaction`, `annual` |
| `payee_types` | ARRAY | Applicable payee types |
| `effective_from` | DATE | Effective from |
| `effective_to` | DATE | Effective to |
| `is_active` | BOOLEAN | Active flag |
| `notes` | TEXT | Notes |

### tds_receivable
TDS deducted by customers on company's invoices.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `financial_year` | VARCHAR | FY (e.g., `2024-25`) |
| `quarter` | VARCHAR | Quarter (Q1, Q2, Q3, Q4) |
| `customer_id` | UUID | FK to parties |
| **Deductor Details** |
| `deductor_name` | VARCHAR | Customer name |
| `deductor_tan` | VARCHAR | Customer TAN |
| `deductor_pan` | VARCHAR | Customer PAN |
| **Transaction** |
| `payment_date` | DATE | Payment date |
| `tds_section` | VARCHAR | TDS section applied |
| `gross_amount` | NUMERIC | Gross invoice amount |
| `tds_rate` | NUMERIC | TDS rate |
| `tds_amount` | NUMERIC | TDS deducted |
| `net_received` | NUMERIC | Net amount received |
| **Certificate** |
| `certificate_number` | VARCHAR | Form 16A number |
| `certificate_date` | DATE | Certificate date |
| `certificate_downloaded` | BOOLEAN | Downloaded flag |
| **Links** |
| `payment_id` | UUID | FK to payments |
| `invoice_id` | UUID | FK to invoices |
| **Form 26AS Match** |
| `matched_with_26as` | BOOLEAN | Matched with 26AS |
| `form_26as_amount` | NUMERIC | Amount in 26AS |
| `amount_difference` | NUMERIC | Difference |
| `matched_at` | TIMESTAMP | Match timestamp |
| **Status** |
| `status` | VARCHAR | `pending`, `verified`, `claimed` |
| `claimed_in_return` | VARCHAR | ITR claim reference |
| `notes` | TEXT | Notes |

### tcs_sections
TCS section master (206C variants).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `section_code` | VARCHAR | Section code (206C(1H)) |
| `section_name` | VARCHAR | Section description |
| `description` | TEXT | Detailed description |
| **Rates** |
| `default_rate` | NUMERIC | Default rate |
| `rate_without_pan` | NUMERIC | Rate without PAN |
| `rate_non_filer` | NUMERIC | Non-filer rate |
| **Thresholds** |
| `threshold_per_transaction` | NUMERIC | Per-transaction |
| `threshold_annual` | NUMERIC | Annual (Rs. 50L for 1H) |
| **Applicability** |
| `applicable_goods` | ARRAY | Goods categories |
| `hsn_codes` | ARRAY | Applicable HSN codes |
| **Ledger** |
| `payable_account_code` | VARCHAR | TCS payable account |
| `receivable_account_code` | VARCHAR | TCS receivable account |
| **Validity** |
| `effective_from` | DATE | Effective from |
| `effective_to` | DATE | Effective to |
| `is_active` | BOOLEAN | Active flag |

### tcs_transactions
TCS collected on sales.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `transaction_type` | VARCHAR | `sale`, `receipt` |
| `section_code` | VARCHAR | TCS section |
| `section_id` | UUID | FK to tcs_sections |
| `transaction_date` | DATE | Transaction date |
| `financial_year` | VARCHAR | FY |
| `quarter` | VARCHAR | Quarter |
| **Party** |
| `party_type` | VARCHAR | `customer`, `vendor` |
| `party_id` | UUID | FK to parties |
| `party_name` | VARCHAR | Party name |
| `party_pan` | VARCHAR | Party PAN |
| `party_gstin` | VARCHAR | Party GSTIN |
| **Amounts** |
| `transaction_value` | NUMERIC | Sale value |
| `tcs_rate` | NUMERIC | TCS rate |
| `tcs_amount` | NUMERIC | TCS collected |
| `cumulative_value_fy` | NUMERIC | YTD value |
| `threshold_amount` | NUMERIC | Threshold |
| **Links** |
| `invoice_id` | UUID | FK to invoices |
| `payment_id` | UUID | FK to payments |
| `journal_entry_id` | UUID | FK to journal_entries |
| **Remittance** |
| `status` | VARCHAR | `collected`, `remitted`, `filed` |
| `collected_at` | TIMESTAMP | Collection timestamp |
| `remitted_at` | TIMESTAMP | Remittance timestamp |
| `challan_number` | VARCHAR | Challan number |
| `bsr_code` | VARCHAR | BSR code |
| **Form 27EQ** |
| `form_27eq_quarter` | VARCHAR | Filing quarter |
| `form_27eq_filed` | BOOLEAN | Filed flag |
| `form_27eq_acknowledgement` | VARCHAR | Filing ack |
| `notes` | TEXT | Notes |

### statutory_payments
TDS/TCS/PF/ESI challan tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `payment_type` | VARCHAR | `tds_salary`, `tds_non_salary`, `tcs`, `pf`, `esi`, `pt`, `gst` |
| `payment_category` | VARCHAR | Sub-category |
| `reference_number` | VARCHAR | Internal reference |
| `financial_year` | VARCHAR | FY |
| **Period** |
| `period_month` | INTEGER | Month (1-12) |
| `period_year` | INTEGER | Year |
| `quarter` | VARCHAR | Quarter |
| **Amounts** |
| `principal_amount` | NUMERIC | Tax amount |
| `interest_amount` | NUMERIC | Interest (234B/C) |
| `penalty_amount` | NUMERIC | Penalty |
| `late_fee` | NUMERIC | Late filing fee |
| `total_amount` | NUMERIC | Total payable |
| **Payment** |
| `payment_date` | DATE | Actual payment date |
| `payment_mode` | VARCHAR | `online`, `bank` |
| `bank_name` | VARCHAR | Bank name |
| `bank_account_id` | UUID | FK to bank_accounts |
| `bank_reference` | VARCHAR | Bank ref |
| **Challan** |
| `bsr_code` | VARCHAR | BSR code |
| `receipt_number` | VARCHAR | Receipt number |
| `trrn` | VARCHAR | TRRN |
| `challan_number` | VARCHAR | Challan number |
| **Status** |
| `status` | VARCHAR | `pending`, `paid`, `verified`, `filed` |
| `due_date` | DATE | Due date |
| **Ledger** |
| `journal_entry_id` | UUID | FK to journal_entries |
| **Workflow** |
| `paid_by` | UUID | Paid by user |
| `paid_at` | TIMESTAMP | Payment timestamp |
| `verified_by` | UUID | Verified by |
| `verified_at` | TIMESTAMP | Verification time |
| `filed_by` | UUID | Filed by (return) |
| `filed_at` | TIMESTAMP | Filing time |
| **Tally** |
| `tally_voucher_guid` | VARCHAR | Tally GUID |
| `tally_voucher_number` | VARCHAR | Tally number |
| `tally_migration_batch_id` | UUID | Migration batch |
| `notes` | TEXT | Notes |

### tax_rule_packs
FY-wise tax configuration (comprehensive).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `pack_code` | VARCHAR | Pack identifier |
| `pack_name` | VARCHAR | Display name |
| `financial_year` | VARCHAR | FY (2024-25) |
| `version` | INTEGER | Version number |
| `source_notification` | VARCHAR | CBDT notification |
| `description` | TEXT | Description |
| `status` | VARCHAR | `draft`, `active`, `archived` |
| **Tax Slabs (JSONB)** |
| `income_tax_slabs` | JSONB | Income tax slabs (old/new regime) |
| `standard_deductions` | JSONB | Standard deduction amounts |
| `rebate_thresholds` | JSONB | 87A rebate thresholds |
| `cess_rates` | JSONB | Health & education cess |
| `surcharge_rates` | JSONB | Surcharge slabs |
| `tds_rates` | JSONB | TDS section rates |
| `pf_esi_rates` | JSONB | PF/ESI rates |
| `professional_tax_config` | JSONB | State-wise PT |
| `gst_rates` | JSONB | GST rates |
| **Audit** |
| `created_by` | VARCHAR | Created by |
| `activated_at` | TIMESTAMP | Activation time |
| `activated_by` | VARCHAR | Activated by |

---

## Backend Structure

### Controllers
- `WebApi/Controllers/TdsReceivableController.cs`
- `WebApi/Controllers/Tax/TdsReturnsController.cs`
- `WebApi/Controllers/Statutory/TdsChallanController.cs`
- `WebApi/Controllers/Payroll/StatutoryConfigController.cs`

### Entities
- `Core/Entities/Tax/TdsSection.cs`
- `Core/Entities/Tax/TcsSection.cs`
- `Core/Entities/Tax/TdsReceivable.cs`
- `Core/Entities/Tax/TcsTransaction.cs`
- `Core/Entities/Tax/StatutoryPayment.cs`
- `Core/Entities/Tax/TaxRulePack.cs`

---

## Frontend Structure

### Pages
- `pages/finance/statutory/StatutoryDashboard.tsx` - Overview
- `pages/finance/statutory/TdsChallanManagement.tsx` - TDS challans
- `pages/finance/statutory/Form16Management.tsx` - Form 16
- `pages/finance/statutory/Form24QFilings.tsx` - Form 24Q
- `pages/finance/statutory/PfEcrManagement.tsx` - PF ECR
- `pages/finance/statutory/EsiReturnManagement.tsx` - ESI returns

### Services
- `services/api/finance/statutory/tdsService.ts`
- `services/api/finance/statutory/tcsService.ts`
- `services/api/finance/statutory/statutoryPaymentService.ts`

---

## API Endpoints

### TDS Sections
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tds-sections` | List TDS sections |
| GET | `/api/tds-sections/{code}` | Get section by code |

### TDS Receivable
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tds-receivable` | List TDS receivables |
| GET | `/api/tds-receivable/paged` | Paginated list |
| POST | `/api/tds-receivable` | Record TDS receivable |
| PUT | `/api/tds-receivable/{id}` | Update |
| POST | `/api/tds-receivable/{id}/match-26as` | Match with 26AS |

### TCS Transactions
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tcs-transactions` | List TCS transactions |
| GET | `/api/tcs-transactions/paged` | Paginated list |
| POST | `/api/tcs-transactions` | Record TCS |
| GET | `/api/tcs-transactions/quarterly-summary` | Quarter summary |

### Statutory Payments
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/statutory-payments` | List payments |
| GET | `/api/statutory-payments/paged` | Paginated list |
| POST | `/api/statutory-payments` | Create payment |
| PUT | `/api/statutory-payments/{id}` | Update |
| POST | `/api/statutory-payments/{id}/mark-paid` | Mark as paid |
| POST | `/api/statutory-payments/{id}/verify` | Verify payment |

### Tax Rule Packs
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tax-rule-packs` | List rule packs |
| GET | `/api/tax-rule-packs/active/{fy}` | Active pack for FY |
| POST | `/api/tax-rule-packs` | Create pack |
| POST | `/api/tax-rule-packs/{id}/activate` | Activate pack |

---

## Business Rules

### TDS Due Dates
| Deductor Type | Month | Due Date |
|---------------|-------|----------|
| Government | All | Same day |
| Others | April-Feb | 7th of next month |
| Others | March | 30th April |

### TDS Return Due Dates (Non-Govt)
| Form | Quarter | Due Date |
|------|---------|----------|
| 24Q | Q1 (Apr-Jun) | 31 Jul |
| 24Q | Q2 (Jul-Sep) | 31 Oct |
| 24Q | Q3 (Oct-Dec) | 31 Jan |
| 24Q | Q4 (Jan-Mar) | 31 May |
| 26Q | Same as 24Q | |
| 27Q | Same as 24Q | |
| 27EQ | Same as 24Q | |

### Common TDS Sections
| Section | Nature | Individual | Company | Threshold |
|---------|--------|------------|---------|-----------|
| 192 | Salary | Slab | - | - |
| 194A | Interest | 10% | 10% | 40K/50K |
| 194C | Contractor | 1% | 2% | 30K/1L |
| 194H | Commission | 5% | 5% | 15K |
| 194I | Rent | 10% | 10% | 2.4L |
| 194J | Professional | 10% | 10% | 30K |

### Common TCS Sections
| Section | Nature | Rate | Threshold |
|---------|--------|------|-----------|
| 206C(1) | Specified goods | 1%-5% | Various |
| 206C(1H) | Sale >50L | 0.1% | 50L p.a. |
| 206C(1G) | Foreign remit | 5%/20% | 7L |

### Interest Calculation
- **234B**: 1% per month for advance tax shortfall
- **234C**: 1% per month for deferment

### Form 26AS Reconciliation
1. Download Form 26AS from TRACES
2. Import/parse into system
3. Auto-match by TAN + amount + period
4. Flag mismatches for review
5. Follow up with deductor if missing

---

## Compliance Calendar Views

### Pending Statutory Payments
View: `v_pending_statutory_payments`
- Shows all pending challans with due dates
- Sorted by due date
- Amount and type breakdown

### TCS Quarterly Summary
View: `v_tcs_quarterly_summary`
- Quarter-wise TCS summary
- Section-wise breakdown
- Remittance status

---

## Current Gaps / TODO

- [ ] Form 16 generation (PDF)
- [ ] Form 16A generation
- [ ] Form 24Q data preparation
- [ ] Form 26Q data preparation
- [ ] Form 27EQ data preparation
- [ ] TRACES integration for Form 26AS
- [ ] Lower Deduction Certificate (LDC) tracking
- [ ] Auto-computation of interest (234B/234C)
- [ ] Advance tax schedule tracker

---

## Related Modules

- [Accounts Payable](02-ACCOUNTS-PAYABLE.md) - TDS on vendor payments
- [Contractor Payments](03-CONTRACTOR-PAYMENTS.md) - TDS on contractor payments
- [Payroll](06-PAYROLL.md) - TDS on salary (192)
- [Billing](01-BILLING.md) - TCS on sales
- [Ledger](05-LEDGER.md) - TDS/TCS posting

---

## Session Notes

### 2026-01-09
- Initial documentation created
- TDS section master and rates operational
- Statutory payment tracking active
- TCS transaction tracking implemented
