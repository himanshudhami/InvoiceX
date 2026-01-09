# Accounts Payable Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: MSME compliance reporting not implemented

---

## Overview

The Accounts Payable (AP) module handles vendor-side transactions: receiving vendor invoices, processing payments, and tracking payables. Integrates with GST (ITC, RCM) and TDS compliance.

### Key Entities
- **Parties** (Vendors) - Vendor master with GST and TDS config
- **Vendor Invoices** - Bills received from vendors
- **Vendor Payments** - Outgoing payments with TDS deduction
- **Vendor Payment Allocations** - Bill-wise settlement

---

## Database Schema

### vendor_invoices
Bills received from vendors with GST ITC tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `party_id` | UUID | FK to parties (vendor) |
| `invoice_number` | VARCHAR | Vendor invoice number |
| `internal_reference` | VARCHAR | Internal bill reference |
| `invoice_date` | DATE | Vendor invoice date |
| `due_date` | DATE | Payment due date |
| `received_date` | DATE | Date received |
| `status` | VARCHAR | `draft`, `pending`, `approved`, `paid`, `cancelled` |
| **Amounts** |
| `subtotal` | NUMERIC | Pre-tax total |
| `tax_amount` | NUMERIC | Total tax |
| `discount_amount` | NUMERIC | Discounts |
| `total_amount` | NUMERIC | Grand total |
| `paid_amount` | NUMERIC | Amount paid |
| `currency` | VARCHAR | Invoice currency |
| `po_number` | VARCHAR | Purchase order reference |
| **GST Fields** |
| `invoice_type` | VARCHAR | `regular`, `import`, `rcm` |
| `supply_type` | VARCHAR | `intra_state`, `inter_state` |
| `place_of_supply` | VARCHAR | POS state code |
| `reverse_charge` | BOOLEAN | RCM applicable (legacy) |
| `rcm_applicable` | BOOLEAN | RCM flag |
| `total_cgst` | NUMERIC | Central GST |
| `total_sgst` | NUMERIC | State GST |
| `total_igst` | NUMERIC | Integrated GST |
| `total_cess` | NUMERIC | Cess |
| **ITC Tracking** |
| `itc_eligible` | BOOLEAN | ITC can be claimed |
| `itc_claimed_amount` | NUMERIC | ITC amount claimed |
| `itc_ineligible_reason` | VARCHAR | Reason if blocked |
| `matched_with_gstr2b` | BOOLEAN | Matched with GSTR-2B |
| `gstr2b_period` | VARCHAR | GSTR-2B period |
| **TDS** |
| `tds_applicable` | BOOLEAN | TDS applicable |
| `tds_section` | VARCHAR | TDS section (194C, 194J, etc.) |
| `tds_rate` | NUMERIC | TDS rate |
| `tds_amount` | NUMERIC | TDS amount |
| **Import** |
| `bill_of_entry_number` | VARCHAR | BOE number |
| `bill_of_entry_date` | DATE | BOE date |
| `port_code` | VARCHAR | Import port |
| `foreign_currency_amount` | NUMERIC | Foreign amount |
| `foreign_currency` | VARCHAR | Currency code |
| `exchange_rate` | NUMERIC | Exchange rate |
| **Posting** |
| `is_posted` | BOOLEAN | Posted to ledger |
| `posted_journal_id` | UUID | FK to journal_entries |
| `posted_at` | TIMESTAMP | Posting timestamp |
| `expense_account_id` | UUID | Default expense account |
| **Approval** |
| `approved_by` | UUID | Approver |
| `approved_at` | TIMESTAMP | Approval time |
| `approval_notes` | TEXT | Approval comments |
| **Tally Migration** |
| `tally_voucher_guid` | VARCHAR | Tally GUID |
| `tally_voucher_number` | VARCHAR | Tally voucher number |
| `tally_migration_batch_id` | UUID | Migration batch |

### vendor_invoice_items
Line items with per-line GST and ITC tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `vendor_invoice_id` | UUID | FK to vendor_invoices |
| `product_id` | UUID | FK to products (optional) |
| `description` | TEXT | Line description |
| `quantity` | NUMERIC | Quantity |
| `unit_price` | NUMERIC | Unit price |
| `tax_rate` | NUMERIC | Tax rate |
| `discount_rate` | NUMERIC | Discount rate |
| `line_total` | NUMERIC | Line total |
| `sort_order` | INTEGER | Display order |
| `hsn_sac_code` | VARCHAR | HSN/SAC code |
| `is_service` | BOOLEAN | Service flag |
| `cgst_rate/amount`, `sgst_rate/amount`, `igst_rate/amount`, `cess_rate/amount` | NUMERIC | GST breakup |
| `itc_eligible` | BOOLEAN | Line ITC eligible |
| `itc_category` | VARCHAR | ITC category |

### vendor_payments
Outgoing payments to vendors with TDS tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `party_id` | UUID | FK to parties (vendor) |
| `bank_account_id` | UUID | FK to bank_accounts |
| `payment_date` | DATE | Payment date |
| `amount` | NUMERIC | Net payment amount |
| `gross_amount` | NUMERIC | Gross before TDS |
| `amount_in_inr` | NUMERIC | INR equivalent |
| `currency` | VARCHAR | Payment currency |
| **Payment Details** |
| `payment_method` | VARCHAR | `neft`, `rtgs`, `imps`, `upi`, `cheque` |
| `reference_number` | VARCHAR | Bank reference |
| `cheque_number` | VARCHAR | Cheque number |
| `cheque_date` | DATE | Cheque date |
| `description` | TEXT | Description |
| `notes` | TEXT | Notes |
| `payment_type` | VARCHAR | `against_invoice`, `advance`, `on_account` |
| `status` | VARCHAR | `draft`, `approved`, `processed`, `cancelled` |
| **TDS** |
| `tds_applicable` | BOOLEAN | TDS deducted |
| `tds_section` | VARCHAR | TDS section |
| `tds_rate` | NUMERIC | TDS rate |
| `tds_amount` | NUMERIC | TDS deducted |
| `tds_deposited` | BOOLEAN | TDS deposited to govt |
| `tds_challan_number` | VARCHAR | Challan number |
| `tds_deposit_date` | DATE | Deposit date |
| `financial_year` | VARCHAR | FY for TDS |
| **Posting** |
| `is_posted` | BOOLEAN | Posted to ledger |
| `posted_journal_id` | UUID | FK to journal_entries |
| `posted_at` | TIMESTAMP | Posting timestamp |
| **Bank Reconciliation** |
| `bank_transaction_id` | UUID | FK to bank_transactions |
| `is_reconciled` | BOOLEAN | Reconciled with bank |
| `reconciled_at` | TIMESTAMP | Reconciliation time |
| **Approval** |
| `approved_by` | UUID | Approver |
| `approved_at` | TIMESTAMP | Approval time |

### vendor_payment_allocations
Bill-wise allocation of payments (Tally-style).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `vendor_payment_id` | UUID | FK to vendor_payments |
| `vendor_invoice_id` | UUID | FK to vendor_invoices |
| `allocated_amount` | NUMERIC | Amount allocated to bill |
| `tds_allocated` | NUMERIC | TDS portion |
| `allocation_type` | VARCHAR | `against_invoice`, `advance_adjustment` |
| `tally_bill_ref` | VARCHAR | Tally bill reference |

---

## Backend Structure

### Entities
- `Core/Entities/VendorInvoice.cs`
- `Core/Entities/VendorInvoiceItem.cs`
- `Core/Entities/VendorPayment.cs`
- `Core/Entities/VendorPaymentAllocation.cs`

### Repositories
- `Infrastructure/Data/VendorInvoiceRepository.cs`
- `Infrastructure/Data/VendorPaymentRepository.cs`

### Services
- `Application/Services/VendorInvoiceService.cs`
- `Application/Services/VendorPaymentService.cs`

### Controllers
- `WebApi/Controllers/AP/VendorsController.cs`
- `WebApi/Controllers/AP/VendorInvoicesController.cs`
- `WebApi/Controllers/AP/VendorPaymentsController.cs`

---

## Frontend Structure

### Pages
- `pages/finance/ap/VendorsPage.tsx` - Vendor list
- `pages/finance/ap/VendorDetailPage.tsx` - Vendor profile
- `pages/finance/ap/VendorInvoicesPage.tsx` - Bill list
- `pages/finance/ap/VendorInvoiceDetailPage.tsx` - Bill detail
- `pages/finance/ap/VendorPaymentsPage.tsx` - Payment list
- `pages/finance/ap/CreateVendorPaymentPage.tsx` - New payment

### Services
- `services/api/finance/ap/vendorService.ts`
- `services/api/finance/ap/vendorInvoiceService.ts`
- `services/api/finance/ap/vendorPaymentService.ts`

### Forms
- `components/forms/VendorForm.tsx`
- `components/forms/VendorInvoiceForm.tsx`
- `components/forms/VendorPaymentForm.tsx`

---

## API Endpoints

### Vendors
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/vendors` | List vendors |
| GET | `/api/vendors/{id}` | Get vendor |
| POST | `/api/vendors` | Create vendor |
| PUT | `/api/vendors/{id}` | Update vendor |

### Vendor Invoices
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/vendor-invoices` | List bills |
| GET | `/api/vendor-invoices/paged` | Paginated bills |
| GET | `/api/vendor-invoices/{id}` | Get bill |
| POST | `/api/vendor-invoices` | Create bill |
| PUT | `/api/vendor-invoices/{id}` | Update bill |
| POST | `/api/vendor-invoices/{id}/approve` | Approve bill |
| POST | `/api/vendor-invoices/{id}/post` | Post to ledger |

### Vendor Payments
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/vendor-payments` | List payments |
| GET | `/api/vendor-payments/paged` | Paginated payments |
| GET | `/api/vendor-payments/{id}` | Get payment |
| POST | `/api/vendor-payments` | Create payment |
| POST | `/api/vendor-payments/{id}/allocate` | Allocate to bills |
| POST | `/api/vendor-payments/{id}/post` | Post to ledger |

---

## Business Rules

### Vendor Invoice Status Flow
```
draft → pending → approved → [paid | cancelled]
```

### TDS Deduction Rules
- TDS deducted at payment, not invoice
- Section determined by vendor PAN type and service category:
  - **194C**: Contractors (1%/2%)
  - **194J**: Professional fees (10%)
  - **194H**: Commission (5%)
  - **194I**: Rent (10%)
- Lower deduction certificate (LDC) support

### Payment Allocation
- Payments allocated FIFO or manually
- Advance payments tracked as `on_account`
- TDS proportionally allocated across bills

### ITC Eligibility
- Auto-flagged based on Section 17(5) rules
- GSTR-2B match required for claim
- Payment within 180 days required

### Posting Rules
**Vendor Invoice Posting**:
- DR: Expense account (or asset)
- DR: GST Input Credit (if ITC eligible)
- CR: Accounts Payable (vendor)

**Vendor Payment Posting**:
- DR: Accounts Payable (vendor)
- CR: Bank account
- CR: TDS Payable (if applicable)

---

## Current Gaps / TODO

- [ ] MSME 45-day payment compliance reporting
- [ ] Vendor aging report
- [ ] Bulk payment processing
- [ ] Payment approval workflow integration
- [ ] Vendor statement reconciliation
- [ ] Auto-matching with bank statement

---

## Related Modules

- [Billing](01-BILLING.md) - For party master (shared)
- [GST Compliance](09-GST-COMPLIANCE.md) - ITC tracking, RCM
- [TDS/TCS](10-TDS-TCS.md) - TDS deduction and returns
- [Banking](04-BANKING.md) - Bank reconciliation

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Core AP workflow operational
- TDS tracking functional
