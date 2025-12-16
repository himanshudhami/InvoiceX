# Financial System Upgrade Plan
## Indian Tax Compliance & Bank Reconciliation

**Created:** 2025-12-15
**Status:** Draft - Pending Approval
**Branch:** Payroll_new

---

## Executive Summary

### Problem Statement
The current system determines income from invoice status (`status = 'paid'`) rather than actual payment receipts. This creates several issues:

1. **India-to-India payments** often happen outside the invoice system
2. **TDS deducted by customers** is not tracked (lost tax credits)
3. **No bank reconciliation** - cannot verify actual cash position
4. **GST not supported** for domestic invoices
5. **Financial reports show placeholders** for Cash & Bank, Accounts Receivable

### Solution Overview
Transform the system from "invoice-centric" to "payment-centric" with proper Indian tax compliance:

```
Current:  Invoice → Mark Paid → Income
Proposed: Bank Statement → Match/Categorize → Payment → Income (with TDS/GST tracking)
```

---

## Current State Analysis

### Database Tables Assessed

| Table | Purpose | Indian Compliance | Status |
|-------|---------|-------------------|--------|
| `payments` | Payment records | Missing TDS, GST, bank link | Needs Enhancement |
| `invoices` | Sales invoices | No GST breakup | Needs Enhancement |
| `invoice_items` | Line items | Generic tax_rate only | Needs Enhancement |
| `customers` | Customer data | Has tax_number, no GSTIN validation | Needs Enhancement |
| `companies` | Company data | No GSTIN, PAN | Needs Enhancement |
| `contractor_payments` | Outgoing payments | Has TDS, GST | Good |
| `company_statutory_configs` | Payroll compliance | PF, ESI, PT, TDS | Good |
| `payroll_transactions` | Salary payments | Full compliance | Good |

### Income Calculation (Current)

**Dashboard (`DashboardRepository.cs:26`):**
```sql
SUM(CASE WHEN status = 'paid' THEN total_amount ELSE 0 END) as TotalRevenue
```

**P&L (`pnlCalculation.ts:67`):**
```typescript
if (!inv.status || inv.status.toLowerCase() !== 'paid') return false;
```

### Key Gaps

1. **No independent payment recording** - payments require invoice_id
2. **No TDS on income** - when customers deduct TDS, we lose the credit
3. **No bank accounts** - cannot track actual cash
4. **No bank statement import** - cannot reconcile
5. **No GST on sales** - only on contractor payments and assets

---

## Proposed Solution

### Architecture Principles

1. **Payment-First Accounting** - All income flows through payments table
2. **Bank as Source of Truth** - Bank transactions drive reconciliation
3. **GST-Ready Invoicing** - Support both export and domestic invoices
4. **TDS Credit Tracking** - Capture TDS deducted by customers
5. **Backward Compatible** - Existing export invoicing continues to work

---

## Phase 1: Foundation (Database & Core Logic)

### 1.1 Enhance Companies Table

**Goal:** Add GST registration details to companies

**Schema Changes:**
```sql
ALTER TABLE companies ADD COLUMN gstin VARCHAR(20);
ALTER TABLE companies ADD COLUMN gst_state_code VARCHAR(5);
ALTER TABLE companies ADD COLUMN pan_number VARCHAR(15);
ALTER TABLE companies ADD COLUMN cin_number VARCHAR(25);
ALTER TABLE companies ADD COLUMN gst_registration_type VARCHAR(50); -- 'regular', 'composition', 'unregistered'
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Company form updated in frontend
- [ ] Can save and retrieve GSTIN for a company
- [ ] GSTIN validation (15 chars, state code matches)

**Effort:** Small

---

### 1.2 Enhance Customers Table

**Goal:** Support GST-registered and unregistered customers

**Schema Changes:**
```sql
ALTER TABLE customers ADD COLUMN gstin VARCHAR(20);
ALTER TABLE customers ADD COLUMN gst_state_code VARCHAR(5);
ALTER TABLE customers ADD COLUMN customer_type VARCHAR(20) DEFAULT 'overseas'; -- 'b2b', 'b2c', 'overseas', 'sez'
ALTER TABLE customers ADD COLUMN is_gst_registered BOOLEAN DEFAULT false;
ALTER TABLE customers ADD COLUMN pan_number VARCHAR(15);
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Customer form shows GST fields when country is India
- [ ] Customer type auto-detects based on country
- [ ] GSTIN validation works

**Effort:** Small

---

### 1.3 Create Bank Accounts Table

**Goal:** Track company bank accounts for reconciliation

**New Table:**
```sql
CREATE TABLE bank_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),
    account_name VARCHAR(255) NOT NULL,
    account_number VARCHAR(50) NOT NULL,
    bank_name VARCHAR(255) NOT NULL,
    ifsc_code VARCHAR(20),
    branch_name VARCHAR(255),
    account_type VARCHAR(50) DEFAULT 'current', -- 'current', 'savings', 'cc', 'foreign'
    currency VARCHAR(10) DEFAULT 'INR',
    opening_balance DECIMAL(18,2) DEFAULT 0,
    current_balance DECIMAL(18,2) DEFAULT 0,
    as_of_date DATE,
    is_primary BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bank_accounts_company ON bank_accounts(company_id);
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Can create bank account via API
- [ ] Can list bank accounts for a company
- [ ] Opening balance correctly stored

**Effort:** Medium

---

### 1.4 Enhance Payments Table

**Goal:** Support non-invoice payments, TDS tracking, bank linking

**Schema Changes:**
```sql
-- Make invoice_id truly optional and add customer link
ALTER TABLE payments ADD COLUMN company_id UUID REFERENCES companies(id);
ALTER TABLE payments ADD COLUMN customer_id UUID REFERENCES customers(id);

-- Bank linking
ALTER TABLE payments ADD COLUMN bank_account_id UUID REFERENCES bank_accounts(id);
ALTER TABLE payments ADD COLUMN bank_reference VARCHAR(255);

-- Payment categorization
ALTER TABLE payments ADD COLUMN payment_type VARCHAR(50) DEFAULT 'invoice_payment';
-- Values: 'invoice_payment', 'advance_received', 'direct_income', 'refund_received'
ALTER TABLE payments ADD COLUMN income_category VARCHAR(100);
-- Values: 'export_services', 'domestic_services', 'product_sale', 'interest', 'other'

-- TDS tracking (when customer deducts TDS)
ALTER TABLE payments ADD COLUMN tds_applicable BOOLEAN DEFAULT false;
ALTER TABLE payments ADD COLUMN tds_section VARCHAR(20); -- '194J', '194C', '194H', '194O'
ALTER TABLE payments ADD COLUMN tds_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE payments ADD COLUMN tds_amount DECIMAL(18,2) DEFAULT 0;
ALTER TABLE payments ADD COLUMN gross_amount DECIMAL(18,2); -- Amount before TDS deduction

-- GST on payment (for advance receipts)
ALTER TABLE payments ADD COLUMN gst_applicable BOOLEAN DEFAULT false;
ALTER TABLE payments ADD COLUMN cgst_amount DECIMAL(18,2) DEFAULT 0;
ALTER TABLE payments ADD COLUMN sgst_amount DECIMAL(18,2) DEFAULT 0;
ALTER TABLE payments ADD COLUMN igst_amount DECIMAL(18,2) DEFAULT 0;

-- Financial year tracking
ALTER TABLE payments ADD COLUMN financial_year VARCHAR(10); -- '2024-25'

-- Reconciliation status
ALTER TABLE payments ADD COLUMN is_reconciled BOOLEAN DEFAULT false;
ALTER TABLE payments ADD COLUMN reconciled_at TIMESTAMP;
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Existing payments still work (invoice-linked)
- [ ] Can create payment without invoice_id
- [ ] Can create payment with TDS details
- [ ] Can link payment to bank account

**Effort:** Medium

---

### 1.5 Create Bank Transactions Table

**Goal:** Import and track bank statement entries

**New Table:**
```sql
CREATE TABLE bank_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bank_account_id UUID NOT NULL REFERENCES bank_accounts(id),

    -- Transaction details
    transaction_date DATE NOT NULL,
    value_date DATE,
    description TEXT,
    reference_number VARCHAR(255),
    cheque_number VARCHAR(50),

    -- Amount
    transaction_type VARCHAR(20) NOT NULL, -- 'credit', 'debit'
    amount DECIMAL(18,2) NOT NULL,
    balance_after DECIMAL(18,2),

    -- Categorization
    category VARCHAR(100), -- 'customer_payment', 'vendor_payment', 'salary', 'tax', 'bank_charges', etc.

    -- Reconciliation
    is_reconciled BOOLEAN DEFAULT false,
    reconciled_type VARCHAR(50), -- 'payment', 'expense', 'payroll', 'tax_payment', 'transfer'
    reconciled_id UUID, -- ID of linked payment/expense/payroll record
    reconciled_at TIMESTAMP,
    reconciled_by VARCHAR(255),

    -- Import tracking
    import_source VARCHAR(100) DEFAULT 'manual', -- 'manual', 'csv', 'pdf', 'api'
    import_batch_id UUID,
    raw_data JSONB, -- Store original imported row

    -- Duplicate detection
    transaction_hash VARCHAR(64), -- Hash of date+amount+description for dedup

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bank_tx_account ON bank_transactions(bank_account_id);
CREATE INDEX idx_bank_tx_date ON bank_transactions(transaction_date);
CREATE INDEX idx_bank_tx_reconciled ON bank_transactions(is_reconciled);
CREATE INDEX idx_bank_tx_hash ON bank_transactions(transaction_hash);
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Can create bank transaction via API
- [ ] Can list transactions for a bank account
- [ ] Duplicate detection works (same hash = duplicate)
- [ ] Can mark transaction as reconciled

**Effort:** Medium

---

## Phase 2: GST Invoice Support

### 2.1 Enhance Invoice Items Table

**Goal:** Support HSN/SAC codes and GST breakup

**Schema Changes:**
```sql
ALTER TABLE invoice_items ADD COLUMN hsn_sac_code VARCHAR(20);
ALTER TABLE invoice_items ADD COLUMN is_service BOOLEAN DEFAULT true; -- true=SAC, false=HSN
ALTER TABLE invoice_items ADD COLUMN cgst_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN cgst_amount DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN sgst_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN sgst_amount DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN igst_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN igst_amount DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN cess_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN cess_amount DECIMAL(18,2) DEFAULT 0;
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Existing invoices still render correctly
- [ ] Can add line item with GST breakup
- [ ] Line total = base + CGST + SGST (or IGST)

**Effort:** Small

---

### 2.2 Enhance Invoices Table

**Goal:** Support GST invoice types and e-invoicing fields

**Schema Changes:**
```sql
-- GST classification
ALTER TABLE invoices ADD COLUMN invoice_type VARCHAR(50) DEFAULT 'export';
-- Values: 'export', 'domestic_b2b', 'domestic_b2c', 'sez', 'deemed_export'
ALTER TABLE invoices ADD COLUMN supply_type VARCHAR(20); -- 'intra_state', 'inter_state', 'export'
ALTER TABLE invoices ADD COLUMN place_of_supply VARCHAR(50); -- State code or 'export'
ALTER TABLE invoices ADD COLUMN reverse_charge BOOLEAN DEFAULT false;

-- GST totals
ALTER TABLE invoices ADD COLUMN total_cgst DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN total_sgst DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN total_igst DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN total_cess DECIMAL(18,2) DEFAULT 0;

-- E-invoicing (for B2B > 5cr threshold)
ALTER TABLE invoices ADD COLUMN e_invoice_applicable BOOLEAN DEFAULT false;
ALTER TABLE invoices ADD COLUMN e_invoice_irn VARCHAR(100);
ALTER TABLE invoices ADD COLUMN e_invoice_ack_number VARCHAR(100);
ALTER TABLE invoices ADD COLUMN e_invoice_ack_date TIMESTAMP;
ALTER TABLE invoices ADD COLUMN e_invoice_qr_code TEXT;

-- Shipping details (for goods)
ALTER TABLE invoices ADD COLUMN shipping_address TEXT;
ALTER TABLE invoices ADD COLUMN transporter_name VARCHAR(255);
ALTER TABLE invoices ADD COLUMN vehicle_number VARCHAR(50);
ALTER TABLE invoices ADD COLUMN eway_bill_number VARCHAR(50);
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Existing export invoices work (type='export')
- [ ] Can create domestic B2B invoice with GSTIN
- [ ] Place of supply auto-determines CGST/SGST vs IGST
- [ ] Invoice totals include GST breakdown

**Effort:** Medium

---

### 2.3 Enhance Products Table

**Goal:** Add HSN/SAC codes and default GST rates

**Schema Changes:**
```sql
ALTER TABLE products ADD COLUMN hsn_sac_code VARCHAR(20);
ALTER TABLE products ADD COLUMN is_service BOOLEAN DEFAULT true;
ALTER TABLE products ADD COLUMN default_gst_rate DECIMAL(5,2) DEFAULT 18; -- 0, 5, 12, 18, 28
ALTER TABLE products ADD COLUMN cess_rate DECIMAL(5,2) DEFAULT 0;
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Can set HSN/SAC code for product
- [ ] Product GST rate flows to invoice line item

**Effort:** Small

---

## Phase 3: TDS Credit Tracking

### 3.1 Create TDS Receivable Table

**Goal:** Track TDS deducted by customers for Form 26AS reconciliation

**New Table:**
```sql
CREATE TABLE tds_receivable (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    financial_year VARCHAR(10) NOT NULL, -- '2024-25'
    quarter VARCHAR(5) NOT NULL, -- 'Q1', 'Q2', 'Q3', 'Q4'

    -- Deductor (customer) details
    customer_id UUID REFERENCES customers(id),
    deductor_name VARCHAR(255) NOT NULL,
    deductor_tan VARCHAR(20), -- TAN of deductor
    deductor_pan VARCHAR(15),

    -- Transaction details
    payment_date DATE NOT NULL,
    tds_section VARCHAR(20) NOT NULL, -- '194J', '194C', etc.
    gross_amount DECIMAL(18,2) NOT NULL,
    tds_rate DECIMAL(5,2) NOT NULL,
    tds_amount DECIMAL(18,2) NOT NULL,
    net_received DECIMAL(18,2) NOT NULL,

    -- Certificate details (Form 16A)
    certificate_number VARCHAR(100),
    certificate_date DATE,
    certificate_downloaded BOOLEAN DEFAULT false,

    -- Linked records
    payment_id UUID REFERENCES payments(id),
    invoice_id UUID REFERENCES invoices(id),

    -- 26AS matching
    matched_with_26as BOOLEAN DEFAULT false,
    form_26as_amount DECIMAL(18,2),
    amount_difference DECIMAL(18,2),
    matched_at TIMESTAMP,

    -- Claiming status
    status VARCHAR(50) DEFAULT 'pending',
    -- Values: 'pending', 'matched', 'claimed', 'disputed', 'written_off'
    claimed_in_return VARCHAR(50), -- 'ITR-2024-25'

    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_tds_recv_company_fy ON tds_receivable(company_id, financial_year);
CREATE INDEX idx_tds_recv_customer ON tds_receivable(customer_id);
CREATE INDEX idx_tds_recv_status ON tds_receivable(status);
```

**Verification:**
- [ ] Migration runs successfully
- [ ] Can create TDS receivable entry
- [ ] Can link to payment record
- [ ] Can mark as matched with 26AS
- [ ] Summary shows total TDS credits by FY

**Effort:** Medium

---

## Phase 4: Backend Services

### 4.1 Bank Account Service

**New Files:**
- `Core/Entities/BankAccount.cs`
- `Core/Interfaces/IBankAccountRepository.cs`
- `Infrastructure/Data/BankAccountRepository.cs`
- `Application/DTOs/BankAccount/*.cs`
- `Application/Interfaces/IBankAccountService.cs`
- `Application/Services/BankAccountService.cs`
- `WebApi/Controllers/BankAccountsController.cs`

**API Endpoints:**
```
GET    /api/bank-accounts
GET    /api/bank-accounts/{id}
POST   /api/bank-accounts
PUT    /api/bank-accounts/{id}
DELETE /api/bank-accounts/{id}
GET    /api/bank-accounts/{id}/balance
POST   /api/bank-accounts/{id}/update-balance
```

**Verification:**
- [ ] All CRUD operations work
- [ ] Balance updates correctly
- [ ] Company filter works
- [ ] Primary account flag is unique per company

**Effort:** Medium

---

### 4.2 Bank Transaction Service

**New Files:**
- `Core/Entities/BankTransaction.cs`
- `Core/Interfaces/IBankTransactionRepository.cs`
- `Infrastructure/Data/BankTransactionRepository.cs`
- `Application/DTOs/BankTransaction/*.cs`
- `Application/Interfaces/IBankTransactionService.cs`
- `Application/Services/BankTransactionService.cs`
- `Application/Services/BankStatementImportService.cs`
- `WebApi/Controllers/BankTransactionsController.cs`

**API Endpoints:**
```
GET    /api/bank-transactions?accountId=&from=&to=
GET    /api/bank-transactions/{id}
POST   /api/bank-transactions
POST   /api/bank-transactions/import (multipart CSV)
POST   /api/bank-transactions/{id}/reconcile
GET    /api/bank-transactions/unreconciled
GET    /api/bank-transactions/reconciliation-suggestions/{id}
```

**Verification:**
- [ ] Can create manual transaction
- [ ] CSV import works (common bank formats)
- [ ] Duplicate detection prevents re-import
- [ ] Can reconcile with payment
- [ ] Suggestions API returns likely matches

**Effort:** Large

---

### 4.3 Enhanced Payment Service

**Modified Files:**
- `Core/Entities/Payments.cs` - Add new fields
- `Application/DTOs/Payments/*.cs` - Add TDS, GST, bank fields
- `Application/Services/PaymentsService.cs` - Support non-invoice payments
- `WebApi/Controllers/PaymentsController.cs` - New endpoints

**New API Endpoints:**
```
POST   /api/payments/record-direct  (non-invoice payment)
GET    /api/payments/by-customer/{customerId}
GET    /api/payments/by-bank-account/{bankAccountId}
GET    /api/payments/income-summary?year=&month=&companyId=
```

**Verification:**
- [ ] Existing invoice payments still work
- [ ] Can create payment without invoice
- [ ] TDS fields captured correctly
- [ ] Income summary uses payments (not invoices)
- [ ] Bank account linkage works

**Effort:** Medium

---

### 4.4 TDS Receivable Service

**New Files:**
- `Core/Entities/TdsReceivable.cs`
- `Core/Interfaces/ITdsReceivableRepository.cs`
- `Infrastructure/Data/TdsReceivableRepository.cs`
- `Application/DTOs/TdsReceivable/*.cs`
- `Application/Interfaces/ITdsReceivableService.cs`
- `Application/Services/TdsReceivableService.cs`
- `WebApi/Controllers/TdsReceivableController.cs`

**API Endpoints:**
```
GET    /api/tds-receivable?companyId=&fy=&status=
GET    /api/tds-receivable/{id}
POST   /api/tds-receivable
PUT    /api/tds-receivable/{id}
POST   /api/tds-receivable/{id}/match-26as
GET    /api/tds-receivable/summary?companyId=&fy=
```

**Verification:**
- [ ] Can create TDS entry when recording payment
- [ ] Summary shows total by quarter
- [ ] Can mark as matched/claimed
- [ ] Links correctly to payment and invoice

**Effort:** Medium

---

### 4.5 Updated Dashboard/P&L Logic

**Modified Files:**
- `Infrastructure/Data/DashboardRepository.cs`
- `frontend/src/lib/pnlCalculation.ts`
- `frontend/src/hooks/usePnLCalculation.ts`

**Changes:**
```typescript
// OLD: Income from paid invoices
const totalIncome = invoices.filter(inv => inv.status === 'paid')...

// NEW: Income from payments + TDS credits
const totalIncome = payments.reduce((sum, p) => {
  const grossIncome = p.grossAmount || p.amount;
  return sum + toInr(grossIncome, p.currency);
}, 0);
```

**Verification:**
- [ ] Dashboard revenue = sum of payments (not invoices)
- [ ] P&L income = sum of payments
- [ ] TDS receivable shows in "Tax Credits" section
- [ ] Cash balance shows from bank accounts

**Effort:** Medium

---

## Phase 5: Frontend Enhancements

### 5.1 Company Settings - GST Section

**Modified Files:**
- `frontend/src/components/forms/CompanyForm.tsx`
- `frontend/src/pages/CompaniesManagement.tsx`

**New Fields:**
- GSTIN input with validation
- GST State Code dropdown
- PAN Number input
- GST Registration Type dropdown

**Verification:**
- [ ] GST fields appear in company form
- [ ] GSTIN validates correctly (15 chars, checksum)
- [ ] State code dropdown populated
- [ ] Data saves and loads correctly

**Effort:** Small

---

### 5.2 Customer Form - GST Section

**Modified Files:**
- `frontend/src/components/forms/CustomerForm.tsx`

**Logic:**
- If country = India, show GST fields
- Customer type dropdown (B2B, B2C, Export)
- GSTIN input (required for B2B)
- State dropdown

**Verification:**
- [ ] GST fields show only for Indian customers
- [ ] B2B requires GSTIN
- [ ] B2C doesn't require GSTIN
- [ ] State auto-extracts from GSTIN

**Effort:** Small

---

### 5.3 Bank Accounts Page

**New Files:**
- `frontend/src/pages/BankAccountsManagement.tsx`
- `frontend/src/components/forms/BankAccountForm.tsx`
- `frontend/src/hooks/api/useBankAccounts.ts`
- `frontend/src/services/api/bankAccountService.ts`

**Features:**
- List bank accounts by company
- Add/Edit bank account
- Show current balance
- Mark as primary

**Verification:**
- [ ] Can add bank account
- [ ] Can edit bank account
- [ ] Balance displays correctly
- [ ] Primary flag toggles correctly

**Effort:** Medium

---

### 5.4 Bank Statement Import Page

**New Files:**
- `frontend/src/pages/BankStatementImport.tsx`
- `frontend/src/components/BankStatementPreview.tsx`
- `frontend/src/hooks/api/useBankTransactions.ts`
- `frontend/src/services/api/bankTransactionService.ts`

**Features:**
- Select bank account
- Upload CSV file
- Preview parsed transactions
- Show duplicates (already imported)
- Import selected transactions

**Verification:**
- [ ] Can upload CSV
- [ ] Preview shows parsed data
- [ ] Duplicates highlighted
- [ ] Import creates transactions
- [ ] Balance updates after import

**Effort:** Large

---

### 5.5 Reconciliation Dashboard

**New Files:**
- `frontend/src/pages/ReconciliationDashboard.tsx`
- `frontend/src/components/ReconciliationMatcher.tsx`

**Features:**
- Show unreconciled bank transactions
- Show unmatched payments
- Suggest matches (by amount, date)
- Manual matching interface
- Bulk reconcile

**Verification:**
- [ ] Lists unreconciled transactions
- [ ] Suggestions appear for matches
- [ ] Can link transaction to payment
- [ ] Status updates after reconciliation

**Effort:** Large

---

### 5.6 Payment Recording Enhancement

**Modified Files:**
- `frontend/src/components/invoice/payment-details.tsx` (or new component)

**New Features:**
- "Record Payment" without invoice
- TDS section (applicable?, section, rate, amount)
- Bank account selection
- Income category dropdown

**Verification:**
- [ ] Can record payment without invoice
- [ ] TDS fields calculate correctly
- [ ] Net = Gross - TDS
- [ ] Links to bank account

**Effort:** Medium

---

### 5.7 GST Invoice Form

**Modified Files:**
- `frontend/src/components/invoice/form-context.tsx`
- `frontend/src/components/invoice/line-items.tsx`
- `frontend/src/components/invoice/summary.tsx`

**New Features:**
- Invoice type selector (Export/Domestic B2B/B2C)
- Place of supply dropdown
- Auto-calculate CGST/SGST vs IGST
- GST summary section
- HSN/SAC code on line items

**Verification:**
- [ ] Export invoices work as before
- [ ] Domestic B2B calculates GST
- [ ] Intra-state = CGST + SGST
- [ ] Inter-state = IGST
- [ ] Summary shows GST breakup

**Effort:** Large

---

### 5.8 Financial Reports Update

**Modified Files:**
- `frontend/src/hooks/usePnLCalculation.ts`
- `frontend/src/components/financial-report/BalanceSheetView.tsx`
- `frontend/src/components/financial-report/KPICards.tsx`

**Changes:**
- Income from payments table
- Cash & Bank from bank_accounts.current_balance
- Accounts Receivable from unpaid invoices
- TDS Receivable section

**Verification:**
- [ ] Dashboard shows payment-based revenue
- [ ] Balance sheet shows real cash balance
- [ ] AR calculated from outstanding invoices
- [ ] TDS credits appear

**Effort:** Medium

---

## Phase 6: Reports & Compliance

### 6.1 TDS Summary Report

**New Files:**
- `frontend/src/pages/TdsSummaryReport.tsx`

**Features:**
- Filter by FY, quarter, company
- Show TDS deducted (contractor payments)
- Show TDS credits (received payments)
- Export for ITR filing

**Effort:** Medium

---

### 6.2 GST Summary Report

**New Files:**
- `frontend/src/pages/GstSummaryReport.tsx`

**Features:**
- GSTR-1 summary (outward supplies)
- GSTR-3B summary
- HSN-wise summary
- Export to JSON/Excel

**Effort:** Large

---

### 6.3 Bank Reconciliation Report

**New Files:**
- `frontend/src/pages/BankReconciliationReport.tsx`

**Features:**
- Select bank account and period
- Book balance vs Bank balance
- List of unreconciled items
- Reconciliation statement

**Effort:** Medium

---

## Implementation Phases

### MVP (Minimum Viable Product)
**Goal:** Fix the core income tracking problem

| Item | Description | Effort |
|------|-------------|--------|
| 1.4 | Enhance payments table | Medium |
| 4.3 | Enhanced payment service | Medium |
| 5.6 | Payment recording enhancement | Medium |
| 4.5 | Update P&L logic | Medium |

**Outcome:** Can record non-invoice payments with TDS, income calculated from payments

---

### Phase A: Bank Integration
**Goal:** Enable bank reconciliation

| Item | Description | Effort |
|------|-------------|--------|
| 1.3 | Bank accounts table | Medium |
| 1.5 | Bank transactions table | Medium |
| 4.1 | Bank account service | Medium |
| 4.2 | Bank transaction service | Large |
| 5.3 | Bank accounts page | Medium |
| 5.4 | Bank statement import | Large |
| 5.5 | Reconciliation dashboard | Large |

**Outcome:** Can import bank statements and reconcile with payments

---

### Phase B: GST Compliance
**Goal:** Support domestic GST invoicing

| Item | Description | Effort |
|------|-------------|--------|
| 1.1 | Enhance companies (GSTIN) | Small |
| 1.2 | Enhance customers (GSTIN) | Small |
| 2.1 | Enhance invoice items (GST) | Small |
| 2.2 | Enhance invoices (GST) | Medium |
| 2.3 | Enhance products (HSN/SAC) | Small |
| 5.1 | Company settings GST | Small |
| 5.2 | Customer form GST | Small |
| 5.7 | GST invoice form | Large |

**Outcome:** Can create GST-compliant domestic invoices

---

### Phase C: TDS & Reporting
**Goal:** Complete tax compliance and reporting

| Item | Description | Effort |
|------|-------------|--------|
| 3.1 | TDS receivable table | Medium |
| 4.4 | TDS receivable service | Medium |
| 5.8 | Financial reports update | Medium |
| 6.1 | TDS summary report | Medium |
| 6.2 | GST summary report | Large |
| 6.3 | Bank reconciliation report | Medium |

**Outcome:** Full tax compliance with proper reporting

---

## Verification Checklist

### MVP Success Criteria
- [ ] Can record a payment received from Indian customer without invoice
- [ ] Can capture TDS deducted by customer (10% u/s 194J)
- [ ] Dashboard revenue shows sum of payments (not invoices)
- [ ] P&L income matches sum of payments
- [ ] Existing export invoice workflow unchanged

### Phase A Success Criteria
- [ ] Can add company bank accounts
- [ ] Can import bank statement CSV
- [ ] Can match bank credit with payment record
- [ ] Balance sheet shows real cash balance
- [ ] Unreconciled items clearly visible

### Phase B Success Criteria
- [ ] Can create domestic B2B invoice with GSTIN
- [ ] CGST/SGST calculated for intra-state
- [ ] IGST calculated for inter-state
- [ ] Invoice PDF shows GST breakup
- [ ] Products have HSN/SAC codes

### Phase C Success Criteria
- [ ] TDS credits tracked per customer
- [ ] Can generate TDS summary by FY/quarter
- [ ] Can generate GST summary for GSTR-1
- [ ] Bank reconciliation statement available

---

## Risk Mitigation

### Data Migration
- All schema changes use `ADD COLUMN IF NOT EXISTS`
- No existing column modifications
- Default values ensure backward compatibility

### Testing Strategy
- Unit tests for new services
- Integration tests for payment flow
- Manual testing of existing invoice workflow

### Rollback Plan
- Feature flags for new functionality
- Can disable GST features if issues
- Bank reconciliation is additive (doesn't change existing data)

---

## Open Questions (Resolved)

1. **Multi-currency bank accounts:** Should we support foreign currency bank accounts? → TBD
2. **E-invoicing:** Do we need IRN generation integration with GSP? → TBD
3. **26AS import:** Should we build PDF parser or manual entry only? → TBD
4. **GST Returns filing:** Integration with GST portal or manual filing? → TBD
5. **Proforma Invoices:** Do you issue proforma invoices before actual invoices? → **YES**
6. **Credit Notes:** Do you need GST credit note support? → **YES**
7. **Multi-company:** Are all 3 companies using same bank accounts or separate? → **YES (separate)**
8. **Historical Data:** Do we need to backfill existing paid invoices into payments? → **YES**

---

## Appendix: Indian Tax Quick Reference

### TDS Sections (Common)
| Section | Nature | Rate |
|---------|--------|------|
| 194C | Contractors | 1-2% |
| 194J | Professional fees | 10% |
| 194H | Commission | 5% |
| 194O | E-commerce | 1% |

### GST Rates (Services)
| Rate | Examples |
|------|----------|
| 0% | Healthcare, education |
| 5% | Transport, small restaurants |
| 12% | Business class air travel |
| 18% | Most services (IT, consulting) |
| 28% | Luxury services |

### Financial Year / Quarter Mapping
| Quarter | Months | Due Date (TDS) |
|---------|--------|----------------|
| Q1 | Apr-Jun | 31 Jul |
| Q2 | Jul-Sep | 31 Oct |
| Q3 | Oct-Dec | 31 Jan |
| Q4 | Jan-Mar | 31 May |
