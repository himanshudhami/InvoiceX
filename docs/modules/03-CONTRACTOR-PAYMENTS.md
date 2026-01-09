# Contractor Payments Module

## Status
- **Current State**: Working (Active Development)
- **Last Updated**: 2026-01-09
- **Active Issues**: Party migration in progress (Plan 4)

---

## Overview

The Contractor Payments module handles payments to contractors/consultants with TDS tracking (Section 194C/194J). Separate from vendor payments as contractors typically receive monthly retainer-style payments without formal invoicing.

### Key Features
- Monthly contractor payment tracking
- TDS calculation (194C for contractors, 194J for professionals)
- GST handling for registered contractors
- Bank reconciliation integration
- Ledger posting (accrual + disbursement)

### Related Implementation Plans
- `docs/PLAN_1_CONTRACTOR_PAYMENTS_IMPORT.md` - Tally import
- `docs/PLAN_4_CONTRACTOR_TO_PARTY_MIGRATION.md` - Employee to Party migration

---

## Database Schema

### contractor_payments
Primary table for contractor payment tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `party_id` | UUID | FK to parties (contractor) |
| **Period** |
| `payment_month` | INTEGER | Payment month (1-12) |
| `payment_year` | INTEGER | Payment year |
| **Invoice Details** |
| `invoice_number` | VARCHAR | Contractor invoice reference |
| `contract_reference` | VARCHAR | Contract/PO reference |
| **Amounts** |
| `gross_amount` | NUMERIC | Gross payment before deductions |
| `tds_rate` | NUMERIC | TDS rate applied |
| `tds_amount` | NUMERIC | TDS deducted |
| `other_deductions` | NUMERIC | Other deductions |
| `net_payable` | NUMERIC | Net amount to contractor |
| **GST** |
| `gst_applicable` | BOOLEAN | Contractor GST registered |
| `gst_rate` | NUMERIC | GST rate |
| `gst_amount` | NUMERIC | GST amount |
| `total_invoice_amount` | NUMERIC | Gross + GST |
| **TDS Details** |
| `tds_section` | VARCHAR | `194C`, `194J`, etc. |
| `contractor_pan` | VARCHAR | Contractor PAN |
| `pan_verified` | BOOLEAN | PAN verification status |
| `rule_pack_id` | UUID | FK to tax_rule_packs |
| **Payment** |
| `status` | VARCHAR | `draft`, `approved`, `paid`, `cancelled` |
| `payment_date` | DATE | Actual payment date |
| `payment_method` | VARCHAR | `neft`, `rtgs`, `imps`, `upi`, `cheque` |
| `payment_reference` | VARCHAR | Bank reference |
| `bank_account_id` | UUID | FK to bank_accounts |
| `description` | TEXT | Payment description |
| `remarks` | TEXT | Additional remarks |
| **Bank Reconciliation** |
| `bank_transaction_id` | UUID | FK to bank_transactions |
| `reconciled_at` | TIMESTAMP | Reconciliation timestamp |
| `reconciled_by` | VARCHAR | Reconciled by user |
| **Ledger Posting** |
| `accrual_journal_entry_id` | UUID | FK for accrual JE |
| `disbursement_journal_entry_id` | UUID | FK for payment JE |
| **Tally Migration** |
| `tally_voucher_guid` | VARCHAR | Original Tally GUID |
| `tally_voucher_number` | VARCHAR | Tally voucher number |
| `tally_migration_batch_id` | UUID | Migration batch |

---

## Backend Structure

### Entities
- `Core/Entities/Payroll/ContractorPayment.cs`

### Repositories
- `Infrastructure/Data/Payroll/ContractorPaymentRepository.cs`

### DTOs
- `Application/DTOs/Payroll/ContractorPaymentDto.cs`
- `Application/DTOs/Payroll/CreateContractorPaymentDto.cs`
- `Application/DTOs/Payroll/UpdateContractorPaymentDto.cs`

### Controllers
- `WebApi/Controllers/Payroll/ContractorPaymentsController.cs`

---

## Frontend Structure

### Pages
- `pages/hr/payroll/ContractorPaymentsPage.tsx` - Payment list
- `pages/hr/payroll/ContractorPaymentDetailPage.tsx` - Payment detail

### Features/Hooks
```
features/payroll/hooks/
├── useContractorPayments.ts
├── useContractorPaymentsPaged.ts
├── useCreateContractorPayment.ts
├── useUpdateContractorPayment.ts
└── contractorPaymentKeys.ts
```

### Services
- `services/api/hr/payroll/payrollService.ts` (includes contractor payments)

### Forms
- `components/forms/ContractorPaymentForm.tsx`

### Types
- `features/payroll/types/payroll.ts`

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/contractor-payments` | List all payments |
| GET | `/api/contractor-payments/paged` | Paginated with filters |
| GET | `/api/contractor-payments/{id}` | Get payment by ID |
| POST | `/api/contractor-payments` | Create payment |
| PUT | `/api/contractor-payments/{id}` | Update payment |
| DELETE | `/api/contractor-payments/{id}` | Delete payment |
| POST | `/api/contractor-payments/{id}/approve` | Approve payment |
| POST | `/api/contractor-payments/{id}/process` | Mark as paid |

### Query Parameters (Paged)
- `companyId` - Filter by company
- `partyId` - Filter by contractor
- `month`, `year` - Filter by period
- `status` - Filter by status
- `searchTerm` - Search
- `pageNumber`, `pageSize` - Pagination

---

## Business Rules

### Payment Status Flow
```
draft → approved → paid → [cancelled]
```

### TDS Calculation
| Section | Description | Rate (Individual) | Rate (Company) |
|---------|-------------|-------------------|----------------|
| 194C | Contractors | 1% | 2% |
| 194J | Professional fees | 10% | 10% |
| 194H | Commission | 5% | 5% |

**TDS Rules**:
- Threshold: Rs. 30,000 per payment / Rs. 1,00,000 p.a. for 194C
- PAN verification required for correct rate
- Higher rate (20%) if PAN not available
- LDC (Lower Deduction Certificate) support

### GST Handling
- If contractor is GST registered, GST added to gross
- ITC can be claimed if eligible
- RCM applicable for unregistered contractors (threshold based)

### Ledger Posting

**Accrual Entry** (when approved):
- DR: Contractor Expense account
- DR: GST Input Credit (if applicable)
- CR: Contractor Payable account
- CR: TDS Payable account

**Disbursement Entry** (when paid):
- DR: Contractor Payable account
- CR: Bank account

### Bank Reconciliation
- Payment links to `bank_transactions` via `bank_transaction_id`
- Auto-matching by amount and date
- Manual reconciliation for mismatches

---

## Migration Notes

### Current State (Plan 4 in progress)
- `party_id` now links to `parties` table
- Previously used `employee_id` (deprecated)
- Contractors managed as parties with `is_vendor = true`

### Party Profile
Contractors use `party_vendor_profiles` for:
- Default TDS section and rate
- Bank account details
- GST registration info

---

## Current Gaps / TODO

- [ ] Bulk payment processing
- [ ] Payment approval workflow integration
- [ ] Contractor statement/ledger view
- [ ] TDS certificate generation (Form 16A)
- [ ] Monthly payment schedule automation

---

## Related Modules

- [Accounts Payable](02-ACCOUNTS-PAYABLE.md) - For formal vendor invoices
- [TDS/TCS](10-TDS-TCS.md) - TDS returns and challans
- [Banking](04-BANKING.md) - Bank reconciliation
- [Billing](01-BILLING.md) - Party master (shared)

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Party migration (Plan 4) in progress
- Bank reconciliation integration active on `ap_ar` branch
