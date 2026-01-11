# Feature Roadmap

## Purpose
Prioritized feature backlog extracted from competitive analysis and gap review. Each feature links to relevant module documentation.

**Last Updated**: 2026-01-10 (Comprehensive audit against codebase and database)

---

## Priority Levels

| Priority | Criteria |
|----------|----------|
| **P0 - Critical** | Blocking compliance or core workflows |
| **P1 - High** | Major CA/CFO workflow gaps |
| **P2 - Medium** | Competitive parity features |
| **P3 - Low** | Nice-to-have improvements |

---

## Implementation Status Legend

| Status | Meaning |
|--------|---------|
| **Complete** | Code deployed, actively used in production |
| **Code Ready** | Full implementation exists, not yet in production use |
| **Partial** | Some components exist, gaps remain |
| **Not Started** | No implementation exists |

---

## P0 - Critical

### 1. GSTR-2B Ingestion & Reconciliation
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: ✅ Code Ready (awaiting production enablement)

**Description**: Fetch GSTR-2B from GST portal, parse supplier invoices, auto-match with vendor invoices, highlight mismatches.

**Implementation Status**:
- [x] GSTR-2B parser (JSON to domain model) - `Gstr2bService`
- [x] Auto-matching algorithm (GSTIN + invoice number + amount)
- [x] Mismatch reporting dashboard - Frontend complete
- [x] Accept/Reject workflow for discrepancies
- [x] Database tables: `gstr2b_imports`, `gstr2b_invoices`, `gstr2b_reconciliation_rules`
- [ ] GSTN API integration for GSTR-2B fetch (requires GSP credentials)

**Production Data**: 0 imports, 5 reconciliation rules configured

**Next Steps**: Configure GSP credentials, enable live GSTN fetch

---

### 2. GSTR-3B Filing Pack
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: ✅ Code Ready (awaiting production enablement)

**Description**: Consolidated GSTR-3B data pack with all tables (3.1, 3.2, 4, 5, 6) populated from transaction data, with drill-down to source documents.

**Implementation Status**:
- [x] GSTR-3B table builder service - `Gstr3bService`
- [x] Table 3.1 - Outward supplies aggregation
- [x] Table 4 - ITC summary with eligible/blocked breakdown
- [x] Table 5 - Exempt, nil-rated supplies
- [x] Filing status tracking
- [x] Database tables: `gstr3b_filings`, `gstr3b_line_items`, `gstr3b_source_documents`
- [ ] Variance explanation (vs previous period) - needs UI verification

**Production Data**: 0 filings

**Next Steps**: User training, production rollout

---

### 3. Advance Tax Engine
**Module**: [10-TDS-TCS](../modules/10-TDS-TCS.md)
**Status**: ✅ Code Ready (limited production use)

**Description**: Calculate advance tax liability for companies, track payments vs schedule, compute interest exposure (234B/234C).

**Implementation Status**:
- [x] Projected income computation (YTD + forecast) - `AdvanceTaxService`
- [x] Advance tax schedule (15 Jun, 15 Sep, 15 Dec, 15 Mar)
- [x] Paid vs payable tracker
- [x] Interest calculator (234B shortfall, 234C deferment)
- [x] Scenario analysis (capex, payroll changes)
- [x] Revision audit trail with history
- [x] Multiple tax regimes: Normal (25%), 115BAA (22%), 115BAB (15%)
- [x] Database tables: `advance_tax_assessments`, `advance_tax_payments`, `advance_tax_revisions`, `advance_tax_scenarios`, `advance_tax_schedules`

**Production Data**: 1 assessment, 4 schedules configured

**Dependencies**: Ledger module ✅, P&L projection ✅

---

### 4. Audit Trail Activation
**Module**: [16-ADMINISTRATION](../modules/16-ADMINISTRATION.md)
**Status**: ✅ Complete (fully operational)

**Description**: MCA-compliant audit trail for all document changes with before/after values.

**Implementation Status**:
- [x] Audit log table design - `audit_trail`, `document_audit_log`, `einvoice_audit_log`
- [x] Audit log repositories exist - `AuditTrailRepository`
- [x] Generic AuditService with Create/Update/Delete tracking
- [x] Change tracking active in all major services
- [x] Audit log viewer UI - Full-featured admin viewer with filtering, search, diff view
- [x] CSV export for audit purposes
- [x] Generic before/after value tracking with JSON snapshots
- [x] SHA256 checksums for tamper detection
- [x] Correlation ID tracking for request tracing

**Services with Active Audit Logging**:
- Invoices, Payments, VendorInvoices, VendorPayments (original)
- BankAccount, BankTransaction
- Employees
- Assets
- Party (Customers/Vendors)
- CreditNotes
- Quotes
- Products
- ExpenseClaims
- JournalEntry (via LedgerController)
- ChartOfAccount (via LedgerController)
- PayrollRun (via PayrollController)
- E-Invoice API calls
- Document operations

**Production Data**: Active logging enabled

**Coverage**: All major financial entities now have audit logging enabled

---

## P1 - High Priority

### 5. E-Way Bill Workflow
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: ⚠️ Partial (data fields exist, workflow incomplete)

**Description**: Full e-way bill lifecycle: generate from invoice, extend validity, cancel, update vehicle/transporter.

**Implementation Status**:
- [x] E-way bill data fields on invoices: `eway_bill_number`, `eway_bill_date`, `eway_bill_valid_until`, `transporter_name`
- [ ] E-way bill generation API integration
- [ ] UI for e-way bill actions (generate, cancel, extend)
- [ ] Validity tracking and alerts
- [ ] Consolidated e-way bill support

**Next Steps**: Integrate with E-way bill API (NIC or GSP), build action UI

---

### 6. MSME Payment Compliance
**Module**: [02-ACCOUNTS-PAYABLE](../modules/02-ACCOUNTS-PAYABLE.md)
**Status**: ⚠️ Partial (data model exists, workflow missing)

**Description**: Track 45-day payment compliance for MSME vendors, generate MSME Form 1 summary.

**Implementation Status**:
- [x] MSME flag on party master - `party_vendor_profiles.msme_registered`, `msme_registration_number`, `msme_category`
- [x] Vendor data: 318 vendors with profiles
- [ ] Payment aging report for MSME vendors
- [ ] 45-day breach alerting
- [ ] MSME Form 1 data export

**Next Steps**: Build MSME-specific aging report with breach highlighting

---

### 7. Bank Reconciliation Matching
**Module**: [04-BANKING](../modules/04-BANKING.md)
**Status**: ✅ Complete (in active use)

**Description**: Auto-import bank statements, categorization rules, smart reconciliation.

**Implementation Status**:
- [x] Bank feed import (MT940, CSV) - `BankStatementImportService`
- [x] Transaction hash-based duplicate detection
- [x] Reconciliation matching logic - `ReconciliationService`
- [x] Reversal detection - `ReversalDetectionService`
- [x] Reconciliation UI with matching drawer
- [x] Database: 3 accounts, 437 transactions
- [x] Direct reconciliation to payments, contractors, expenses

**Production Data** (Jan 2026):
- 10 transactions reconciled (9 contractor, 1 payment)
- 427 pending accountant review
- Feature verified working via UI

**Next Steps**: Accountant to complete reconciliation of remaining transactions

---

## P2 - Medium Priority

### 8. Year-End Close Checklist
**Module**: [05-LEDGER](../modules/05-LEDGER.md)
**Status**: ❌ Not Started

**Description**: Guided year-end closing workflow with automated sign-offs.

**Tasks**:
- [ ] Pre-close checklist items
- [ ] Closing entry generation
- [ ] Opening balance rollover
- [ ] Lock period after close
- [ ] Reopening controls

---

### 9. Invoice OCR Capture
**Module**: [02-ACCOUNTS-PAYABLE](../modules/02-ACCOUNTS-PAYABLE.md)
**Status**: ❌ Not Started

**Description**: AI-powered invoice data extraction from uploaded images/PDFs.

**Tasks**:
- [ ] OCR service integration (Tesseract, AWS Textract, or Google Vision)
- [ ] Field extraction (vendor, amount, date, GST)
- [ ] Validation queue for human review
- [ ] Auto-create vendor invoice from OCR

---

### 10. Multi-GSTIN Support
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: ⚠️ Partial (multi-company exists, multi-GSTIN per company unclear)

**Description**: Support multiple GST registrations per company.

**Tasks**:
- [ ] GSTIN master table (dedicated, not just company field)
- [ ] Branch/unit to GSTIN mapping
- [ ] Separate GSTR-1/3B per GSTIN
- [ ] Consolidated view across GSTINs

---

### 11. Ledger Health Engine
**Module**: [05-LEDGER](../modules/05-LEDGER.md)
**Status**: ❌ Not Started

**Description**: CA-grade ledger health checks running continuously.

**Checks**:
- [ ] Source-to-ledger coverage (unposted transactions)
- [ ] Open items (unallocated advances, credits)
- [ ] Bank gaps (reconciled without JE link)
- [ ] Tax gaps (GST ledger vs statutory summary)
- [ ] Trial balance balancing by period

---

## P3 - Low Priority

### 12. Exception Queues
**Status**: ❌ Not Started

**Description**: Centralized queue for posting failures, filing errors, GST rejects.

---

### 13. Role-Based Dashboards
**Status**: ⚠️ Partial (basic dashboard exists)

**Description**: Different dashboard views for CA, CFO, GST operator with SLA timers.

**Current State**: Single dashboard with financial KPIs. No role-specific views.

---

### 14. Recurring Invoices
**Module**: [01-BILLING](../modules/01-BILLING.md)
**Status**: ❌ Not Started

**Description**: Auto-generate invoices on schedule (monthly retainers).

---

### 15. Budget vs Actual
**Module**: [05-LEDGER](../modules/05-LEDGER.md)
**Status**: ❌ Not Started

**Description**: Budget entry and variance reporting.

---

### 16. COA Modernization (Control Account Architecture)
**Module**: [05-LEDGER](../modules/05-LEDGER.md)
**Status**: ✅ Complete (2026-01-11)
**Feature Doc**: [FEATURE-COA-MODERNIZATION.md](./FEATURE-COA-MODERNIZATION.md)
**Implementation Plan**: [PLAN-COA-MODERNIZATION-IMPL.md](./PLAN-COA-MODERNIZATION-IMPL.md)

**Description**: Modernize Chart of Accounts from Tally-style per-party ledgers to control account + subledger architecture (Zoho/Odoo pattern). Tally import/export maintained via dedicated mapping layer.

**Implementation Completed**:
- [x] Control accounts (1120 Trade Receivables, 2100 Trade Payables) with subledger
- [x] `tally_ledger_mapping` table with 332 entries
- [x] `journal_entry_lines.subledger_type` and `subledger_id` fields
- [x] Tally import creates mappings instead of individual TL-/TR- accounts
- [x] Subledger drill-down in Trial Balance UI
- [x] Party Ledger report
- [x] Control account opening balance sync from subledger
- [x] Opening balance equity plug (Retained Earnings)
- [x] Tally sign convention fix

**Validation Results** (2026-01-11):
| Metric | Result |
|--------|--------|
| Total Opening Balance | ₹0.00 (balanced) |
| Trade Receivables synced | ₹10,70,586 |
| Trade Payables synced | ₹39,725 |
| JE lines with subledger | 208/948 (22%) |
| tally_ledger_mapping | 332 entries |

**Benefits Achieved**:
- COA reduced from 500+ to ~150 accounts
- Scalable to unlimited vendors/customers
- Modern architecture aligned with Zoho/Odoo
- Tally compatibility preserved via mapping layer

---

## Completed Features

### Recently Completed (Verified in Codebase)

- [x] **E-invoice GSP integration** - `einvoice_credentials`, `einvoice_queue`, IRN/QR code generation
- [x] **RCM tracking and payment** - `rcm_transactions`, `RcmController`, `RcmPostingService`
- [x] **ITC blocked categories** - `itc_blocked_categories`, `itc_blocked_transactions` tables
- [x] **Bank transaction import** - 437 transactions in production, MT940/CSV support
- [x] **Contractor payment tracking** - 76 contractor payments in production
- [x] **Statutory payment tracking** - 18 statutory payments (PF, ESI, PT)
- [x] **Tally migration (masters + vouchers)** - 2 batches, 1099 migration log entries
- [x] **Credit Note / Debit Note** - Full workflow with `credit_notes`, `credit_note_items`, GST posting

### Fully Implemented Modules (Production Ready)

| Module | Controllers | Key Tables | Production Data |
|--------|-------------|------------|-----------------|
| **Core Accounting** | ChartOfAccount, JournalEntry | `chart_of_accounts`, `journal_entries` | 490 accounts, 370 entries |
| **Invoicing** | Invoices, InvoiceItems | `invoices`, `invoice_items` | 16 invoices |
| **Vendor/AP** | Vendors, VendorInvoices, VendorPayments | `parties`, `vendor_invoices`, `vendor_payments` | 318 vendors, 52 invoices |
| **Banking** | BankAccounts, BankTransactions | `bank_accounts`, `bank_transactions` | 3 accounts, 437 transactions |
| **Payroll** | Payroll, SalaryStructure, TaxDeclaration | `payroll_runs`, `employee_salary_structures` | Code ready, 0 runs |
| **Inventory** | StockItems, Warehouses, StockMovements | `stock_items`, `warehouses`, `stock_movements` | Code ready, 0 items |
| **Manufacturing** | BOM, ProductionOrders | `bill_of_materials`, `production_orders` | Code ready, 0 orders |
| **Assets** | Assets, AssetAssignments | `assets`, `asset_assignments` | Code ready |
| **TDS/TCS** | TdsReturns, Tcs, Form24Q | `tds_receivable`, `tcs_transactions`, `form_24q_filings` | Code ready |
| **Export/Forex** | FIRC, LUT | `firc_tracking`, `lut_register`, `forex_transactions` | Code ready |

---

## Recommended Priority Order

Based on gap analysis (Jan 2026):

### Tier 1: Enable Already-Built Features (Quick Wins)
1. ~~**Audit Trail Activation**~~ - ✅ COMPLETE (Jan 2026) - Full audit logging active across all major services
2. **GSTR-2B/3B Production Enablement** - Configure GSP, train users
3. ~~**Bank Reconciliation Matching**~~ - ✅ COMPLETE (Jan 2026) - Feature working, accountant reconciling transactions

### Tier 2: Complete Partial Implementations
4. **E-Way Bill Workflow** - API integration + UI actions
5. **MSME 45-Day Compliance** - Aging report + alerts
6. ~~**Generic Audit Trail Viewer**~~ - ✅ COMPLETE - Full UI at `/admin/audit`

### Tier 3: New Development
7. **Year-End Close Checklist**
8. **Invoice OCR Capture**
9. **Multi-GSTIN per Company**
10. **Ledger Health Engine**

---

## How to Use This Roadmap

### Starting a Feature
1. Check dependencies are met
2. Read relevant module doc
3. Create implementation plan in `docs/PLAN_X_FEATURE_NAME.md`
4. Update ACTIVE-WORK.md with current focus

### Completing a Feature
1. Update module doc with new functionality
2. Move feature to "Completed" section with verification notes
3. Update ACTIVE-WORK.md
4. Consider what docs need updating

---

## Related Documents

- [System Gap Analysis](../system-gap-analysis.md) - Full competitive analysis
- [Architecture Guide](../ARCHITECTURE.md) - Implementation patterns
- Module docs in `docs/modules/`
