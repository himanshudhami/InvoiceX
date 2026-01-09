# GST Compliance Module

## Status
- **Current State**: Partial
- **Last Updated**: 2026-01-09
- **Active Issues**: GSTR-2B ingestion and GSTR-3B filing pack missing

---

## Overview

The GST Compliance module handles India's Goods and Services Tax requirements: invoice classification, e-invoicing, RCM tracking, ITC management, and return preparation. Integrates with GSP (GST Suvidha Provider) for e-invoice generation.

### Key Features
- **GSTR-1 Data Extraction** - B2B, B2C, Export, HSN summaries
- **E-Invoice Generation** - IRN generation via GSP integration
- **RCM Transactions** - Reverse Charge Mechanism tracking
- **ITC Management** - Input Tax Credit with blocked/reversal tracking
- **E-Way Bill** - Data fields (workflow not exposed yet)

### Key Entities
- **E-Invoice Credentials** - GSP authentication config
- **E-Invoice Queue** - IRN generation queue
- **RCM Transactions** - Reverse charge entries
- **GST Input Credit** - ITC register with GSTR-2B matching
- **ITC Blocked Transactions** - Section 17(5) blocked credits

---

## Database Schema

### einvoice_credentials
GSP provider configuration for e-invoice generation.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `gsp_provider` | VARCHAR | Provider name (e.g., `cleartax`, `mastergst`) |
| `environment` | VARCHAR | `sandbox`, `production` |
| **Authentication** |
| `client_id` | VARCHAR | OAuth client ID |
| `client_secret` | VARCHAR | OAuth client secret (encrypted) |
| `username` | VARCHAR | GSP username |
| `password` | VARCHAR | GSP password (encrypted) |
| `auth_token` | TEXT | Current auth token |
| `token_expiry` | TIMESTAMP | Token expiration |
| `sek` | VARCHAR | Session encryption key |
| **Settings** |
| `auto_generate_irn` | BOOLEAN | Auto-generate on invoice creation |
| `auto_cancel_on_void` | BOOLEAN | Auto-cancel IRN on invoice void |
| `generate_eway_bill` | BOOLEAN | Generate e-way bill with IRN |
| `einvoice_threshold` | NUMERIC | Turnover threshold (5Cr, 10Cr, etc.) |
| `is_active` | BOOLEAN | Credentials active |

### einvoice_queue
Async queue for e-invoice generation with retry logic.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `invoice_id` | UUID | FK to invoices |
| `action_type` | VARCHAR | `generate`, `cancel`, `get_irn` |
| `priority` | INTEGER | Processing priority |
| `status` | VARCHAR | `pending`, `processing`, `completed`, `failed` |
| **Retry Logic** |
| `retry_count` | INTEGER | Current retry count |
| `max_retries` | INTEGER | Max retries (default 3) |
| `next_retry_at` | TIMESTAMP | Next retry time |
| **Processing** |
| `started_at` | TIMESTAMP | Processing start |
| `completed_at` | TIMESTAMP | Completion time |
| `processor_id` | VARCHAR | Worker ID |
| **Error Handling** |
| `error_code` | VARCHAR | GSP error code |
| `error_message` | TEXT | Error details |
| `request_payload` | JSONB | Request data for debugging |

### rcm_transactions
Reverse Charge Mechanism transactions (Table 4.3 of GSTR-3B).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `financial_year` | VARCHAR | FY (e.g., `2024-25`) |
| `return_period` | VARCHAR | Return period (e.g., `092024`) |
| **Source** |
| `source_type` | VARCHAR | `vendor_invoice`, `expense`, `manual` |
| `source_id` | UUID | FK to source entity |
| `source_number` | VARCHAR | Source reference |
| **Vendor Details** |
| `vendor_name` | VARCHAR | Vendor name |
| `vendor_gstin` | VARCHAR | Vendor GSTIN (may be null for URD) |
| `vendor_pan` | VARCHAR | Vendor PAN |
| `vendor_state_code` | VARCHAR | Vendor state |
| `vendor_invoice_number` | VARCHAR | Vendor invoice ref |
| `vendor_invoice_date` | DATE | Vendor invoice date |
| **RCM Category** |
| `rcm_category_id` | UUID | FK to rcm_categories |
| `rcm_category_code` | VARCHAR | Category code |
| `rcm_notification` | VARCHAR | Notification reference |
| **Supply Details** |
| `place_of_supply` | VARCHAR | POS state code |
| `supply_type` | VARCHAR | `intra_state`, `inter_state` |
| `hsn_sac_code` | VARCHAR | HSN/SAC code |
| `description` | TEXT | Description |
| **Tax Amounts** |
| `taxable_value` | NUMERIC | Taxable value |
| `cgst_rate`, `cgst_amount` | NUMERIC | Central GST |
| `sgst_rate`, `sgst_amount` | NUMERIC | State GST |
| `igst_rate`, `igst_amount` | NUMERIC | Integrated GST |
| `cess_rate`, `cess_amount` | NUMERIC | Cess |
| `total_rcm_tax` | NUMERIC | Total RCM liability |
| **Liability Tracking** |
| `liability_recognized` | BOOLEAN | Liability booked |
| `liability_recognized_at` | TIMESTAMP | Recognition timestamp |
| `liability_journal_id` | UUID | FK to liability JE |
| **Payment Tracking** |
| `rcm_paid` | BOOLEAN | RCM paid |
| `rcm_payment_date` | DATE | Payment date |
| `rcm_payment_journal_id` | UUID | FK to payment JE |
| `rcm_payment_reference` | VARCHAR | Payment reference |
| **ITC Claim** |
| `itc_eligible` | BOOLEAN | ITC eligible on RCM |
| `itc_claimed` | BOOLEAN | ITC claimed |
| `itc_claim_date` | DATE | Claim date |
| `itc_claim_journal_id` | UUID | FK to claim JE |
| `itc_claim_period` | VARCHAR | GSTR-3B period |
| `itc_blocked` | BOOLEAN | ITC blocked |
| `itc_blocked_reason` | VARCHAR | Blocked reason |
| **GSTR-3B** |
| `gstr3b_period` | VARCHAR | GSTR-3B period |
| `gstr3b_table` | VARCHAR | Table reference (4.3) |
| `gstr3b_filed` | BOOLEAN | Filed in GSTR-3B |
| `status` | VARCHAR | `draft`, `recognized`, `paid`, `itc_claimed` |

### gst_input_credit
ITC register with GSTR-2B matching support.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `financial_year` | VARCHAR | FY |
| `return_period` | VARCHAR | Return period |
| **Source** |
| `source_type` | VARCHAR | `vendor_invoice`, `rcm`, `import` |
| `source_id` | UUID | FK to source |
| `source_number` | VARCHAR | Source reference |
| **Vendor** |
| `vendor_gstin` | VARCHAR | Vendor GSTIN |
| `vendor_name` | VARCHAR | Vendor name |
| `vendor_invoice_number` | VARCHAR | Invoice number |
| `vendor_invoice_date` | DATE | Invoice date |
| **Supply** |
| `place_of_supply` | VARCHAR | POS |
| `supply_type` | VARCHAR | Intra/Inter state |
| `hsn_sac_code` | VARCHAR | HSN/SAC |
| **Tax** |
| `taxable_value` | NUMERIC | Taxable value |
| `cgst_rate/amount`, `sgst_rate/amount`, `igst_rate/amount`, `cess_rate/amount` | NUMERIC | GST breakup |
| `total_gst` | NUMERIC | Total GST |
| **Eligibility** |
| `itc_eligible` | BOOLEAN | ITC eligible |
| `ineligible_reason` | VARCHAR | Reason if not eligible |
| **GSTR-2B Matching** |
| `matched_with_gstr2b` | BOOLEAN | Matched with 2B |
| `gstr2b_match_date` | DATE | Match date |
| `gstr2b_mismatch_reason` | VARCHAR | Mismatch reason |
| **Status** |
| `status` | VARCHAR | `pending`, `matched`, `claimed`, `reversed` |
| `claimed_in_gstr3b` | BOOLEAN | Claimed in 3B |
| `gstr3b_filing_period` | VARCHAR | Filing period |
| `claimed_at` | TIMESTAMP | Claim timestamp |
| **Reversal** |
| `is_reversed` | BOOLEAN | Reversed |
| `reversal_amount` | NUMERIC | Reversal amount |
| `reversal_reason` | VARCHAR | Reversal reason |
| `reversal_date` | DATE | Reversal date |
| **Blocked ITC** |
| `is_blocked` | BOOLEAN | Blocked under 17(5) |
| `blocked_category_code` | VARCHAR | Block category |
| `blocked_section` | VARCHAR | Section reference |
| `blocked_reason` | TEXT | Blocking reason |

### itc_blocked_transactions
Section 17(5) blocked ITC tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `financial_year` | VARCHAR | FY |
| `return_period` | VARCHAR | Return period |
| **Source** |
| `source_type` | VARCHAR | Source type |
| `source_id` | UUID | Source FK |
| `source_number` | VARCHAR | Source ref |
| **Vendor** |
| `vendor_name` | VARCHAR | Vendor name |
| `vendor_gstin` | VARCHAR | GSTIN |
| `invoice_number` | VARCHAR | Invoice ref |
| `invoice_date` | DATE | Invoice date |
| **Blocking** |
| `blocked_category_id` | UUID | FK to itc_blocked_categories |
| `blocked_category_code` | VARCHAR | Category code |
| `section_reference` | VARCHAR | Section 17(5) clause |
| **Amounts** |
| `taxable_value` | NUMERIC | Taxable value |
| `cgst_blocked`, `sgst_blocked`, `igst_blocked`, `cess_blocked` | NUMERIC | Blocked amounts |
| `total_itc_blocked` | NUMERIC | Total blocked |
| **Accounting** |
| `expense_account_code` | VARCHAR | Expense account |
| `journal_entry_id` | UUID | FK to JE |
| **GSTR-3B** |
| `gstr3b_period` | VARCHAR | Filing period |
| `gstr3b_table` | VARCHAR | Table reference |
| `gstr3b_filed` | BOOLEAN | Filed |
| `blocking_reason` | TEXT | Detailed reason |

---

## Backend Structure

### Entities
- `Core/Entities/EInvoiceCredential.cs`
- `Core/Entities/EInvoiceQueue.cs`
- `Core/Entities/RcmTransaction.cs`
- `Core/Entities/GstInputCredit.cs`
- `Core/Entities/ItcBlockedTransaction.cs`
- `Core/Entities/ItcBlockedCategory.cs`
- `Core/Entities/RcmCategory.cs`

### Repositories
- `Infrastructure/Data/EInvoiceRepository.cs`
- `Infrastructure/Data/RcmTransactionRepository.cs`
- `Infrastructure/Data/GstInputCreditRepository.cs`
- `Infrastructure/Data/ItcBlockedTransactionRepository.cs`

### Services
- `Application/Services/EInvoiceService.cs` - E-invoice generation
- `Application/Services/Gstr1Service.cs` - GSTR-1 data extraction
- `Application/Services/RcmService.cs` - RCM management
- `Application/Services/GstInputCreditService.cs` - ITC tracking

### External Services
- `Infrastructure/EInvoice/EInvoiceGspClient.cs` - GSP API client
- `Infrastructure/Services/EInvoiceAuditService.cs` - Audit logging

### Controllers
- `WebApi/Controllers/EInvoice/EInvoiceController.cs`
- `WebApi/Controllers/EInvoice/EInvoiceCredentialsController.cs`
- `WebApi/Controllers/Gst/GstController.cs`
- `WebApi/Controllers/Gst/RcmController.cs`
- `WebApi/Controllers/Gst/ItcBlockedController.cs`

---

## Frontend Structure

### Pages
- `pages/gst/GstDashboardPage.tsx` - GST overview
- `pages/gst/Gstr1Page.tsx` - GSTR-1 data
- `pages/gst/RcmPage.tsx` - RCM transactions
- `pages/gst/ItcBlockedPage.tsx` - Blocked ITC
- `pages/gst/ItcReversalPage.tsx` - ITC reversals
- `pages/einvoice/EInvoiceSettingsPage.tsx` - GSP configuration

### Services
- `services/api/gst/gstService.ts`
- `services/api/gst/rcmService.ts`
- `services/api/gst/itcBlockedService.ts`
- `services/api/einvoice/einvoiceService.ts`

---

## API Endpoints

### E-Invoice
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/einvoice/credentials` | Get GSP credentials |
| POST | `/api/einvoice/credentials` | Save credentials |
| POST | `/api/einvoice/generate/{invoiceId}` | Generate IRN |
| POST | `/api/einvoice/cancel/{invoiceId}` | Cancel IRN |
| GET | `/api/einvoice/status/{invoiceId}` | Check IRN status |
| GET | `/api/einvoice/queue` | View queue |

### GSTR-1
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/gst/gstr1/summary` | GSTR-1 summary |
| GET | `/api/gst/gstr1/b2b` | B2B invoices |
| GET | `/api/gst/gstr1/b2c` | B2C invoices |
| GET | `/api/gst/gstr1/export` | Export invoices |
| GET | `/api/gst/gstr1/hsn` | HSN summary |
| GET | `/api/gst/gstr1/docs` | Document summary |

### RCM
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/gst/rcm` | List RCM transactions |
| GET | `/api/gst/rcm/summary` | Period summary |
| POST | `/api/gst/rcm` | Create RCM entry |
| PUT | `/api/gst/rcm/{id}` | Update RCM |
| POST | `/api/gst/rcm/{id}/pay` | Record payment |
| POST | `/api/gst/rcm/{id}/claim-itc` | Claim ITC |

### ITC
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/gst/itc` | ITC register |
| GET | `/api/gst/itc/summary` | ITC summary |
| GET | `/api/gst/itc-blocked` | Blocked ITC |
| GET | `/api/gst/itc-reversal` | ITC reversals |

---

## Business Rules

### E-Invoice Applicability
- Mandatory for B2B supplies above threshold
- Threshold reduced over time: 500Cr → 100Cr → 50Cr → 20Cr → 10Cr → 5Cr
- Not applicable for: B2C, SEZ, Export (with some exceptions)
- Cancel within 24 hours only

### RCM Categories
| Category | Description | Notification |
|----------|-------------|--------------|
| `GTA` | Goods Transport Agency | 13/2017-CT |
| `URD` | Unregistered Dealer | 13/2017-CT |
| `LEGAL` | Legal Services | 13/2017-CT |
| `IMPORT_SERVICE` | Import of Services | Sec 5(3) |
| `SPONSORSHIP` | Sponsorship Services | 13/2017-CT |

### ITC Eligibility Rules
- Payment to vendor within 180 days
- GSTR-2B match required
- Not blocked under Section 17(5)
- RCM paid before ITC claim

### Section 17(5) Blocked Categories
| Code | Description |
|------|-------------|
| `MOTOR_VEHICLE` | Motor vehicles (except specified) |
| `FOOD_BEVERAGE` | Food and beverages |
| `MEMBERSHIP` | Club memberships |
| `LIFE_HEALTH` | Life/health insurance (except specified) |
| `TRAVEL` | Travel benefits |
| `OUTDOOR_CATERING` | Outdoor catering |
| `CONSTRUCTION` | Works contract for construction |
| `COMPOSITION` | Goods from composition dealer |

### GSTR-3B Table Mapping
| Table | Content |
|-------|---------|
| 3.1(a) | Outward taxable supplies |
| 3.1(b) | Outward taxable supplies (nil rated) |
| 3.1(c) | Other outward supplies |
| 3.2 | Inter-state supplies to unregistered |
| 4.1 | Inward supplies (ITC eligible) |
| 4.3 | RCM supplies |
| 4.4 | ITC reversal |
| 4.5 | Net ITC available |

---

## Current Gaps / TODO

### High Priority (from gap analysis)
- [ ] **GSTR-2B Ingestion** - Fetch and parse GSTR-2B from portal
- [ ] **GSTR-2B Reconciliation** - Auto-match with vendor invoices
- [ ] **GSTR-3B Filing Pack** - Consolidated return data with drill-down
- [ ] **GSTN Filing Integration** - Push GSTR-1, GSTR-3B to portal

### Medium Priority
- [ ] **E-Way Bill Workflow** - Generate/Cancel/Extend in UI
- [ ] **IMS Integration** - Invoice Management System accept/reject
- [ ] **Multi-GSTIN Support** - Multiple GSTINs per company

### Low Priority
- [ ] **HSN Code Validation** - Validate HSN codes against master
- [ ] **Place of Supply Auto-Detect** - Based on delivery address
- [ ] **GST Return Calendar** - Filing deadline reminders

---

## Integration Points

### Invoice Module
- E-invoice fields on invoices table
- Auto-trigger IRN generation on invoice creation
- Cancel IRN when invoice voided

### Vendor Invoice Module
- RCM auto-detection based on vendor category
- ITC record creation on vendor invoice booking

### Ledger Module
- RCM liability journal entries
- RCM payment journal entries
- ITC claim journal entries

---

## Session Notes

### 2026-01-09
- Initial documentation created
- E-invoice operational with GSP integration
- RCM and ITC tracking functional
- Major gaps: GSTR-2B reconciliation and GSTR-3B pack
