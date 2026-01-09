# Feature Roadmap

## Purpose
Prioritized feature backlog extracted from competitive analysis and gap review. Each feature links to relevant module documentation.

---

## Priority Levels

| Priority | Criteria |
|----------|----------|
| **P0 - Critical** | Blocking compliance or core workflows |
| **P1 - High** | Major CA/CFO workflow gaps |
| **P2 - Medium** | Competitive parity features |
| **P3 - Low** | Nice-to-have improvements |

---

## P0 - Critical

### 1. GSTR-2B Ingestion & Reconciliation
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: Not Started

**Description**: Fetch GSTR-2B from GST portal, parse supplier invoices, auto-match with vendor invoices, highlight mismatches.

**Implementation Tasks**:
- [ ] GSTN API integration for GSTR-2B fetch
- [ ] GSTR-2B parser (JSON to domain model)
- [ ] Auto-matching algorithm (GSTIN + invoice number + amount)
- [ ] Mismatch reporting dashboard
- [ ] Accept/Reject workflow for discrepancies

**Dependencies**: GSP credentials, vendor invoice module

---

### 2. GSTR-3B Filing Pack
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: Not Started

**Description**: Consolidated GSTR-3B data pack with all tables (3.1, 3.2, 4, 5, 6) populated from transaction data, with drill-down to source documents.

**Implementation Tasks**:
- [ ] GSTR-3B table builder service
- [ ] Table 3.1 - Outward supplies aggregation
- [ ] Table 4 - ITC summary with eligible/blocked breakdown
- [ ] Table 5 - Exempt, nil-rated supplies
- [ ] Filing status tracking
- [ ] Variance explanation (vs previous period)

**Dependencies**: RCM tracking, ITC management

---

### 3. Advance Tax Engine
**Module**: New (10-TDS-TCS.md or new module)
**Status**: Not Started

**Description**: Calculate advance tax liability for companies, track payments vs schedule, compute interest exposure (234B/234C).

**Implementation Tasks**:
- [ ] Projected income computation (YTD + forecast)
- [ ] Advance tax schedule (15 Jun, 15 Sep, 15 Dec, 15 Mar)
- [ ] Paid vs payable tracker
- [ ] Interest calculator (234B shortfall, 234C deferment)
- [ ] Scenario analysis (capex, payroll changes)

**Dependencies**: Ledger module, P&L projection

---

## P1 - High Priority

### 4. E-Way Bill Workflow
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: Partial (data fields exist, workflow missing)

**Description**: Full e-way bill lifecycle: generate from invoice, extend validity, cancel, update vehicle/transporter.

**Tasks**:
- [ ] E-way bill generation API integration
- [ ] UI for e-way bill actions
- [ ] Validity tracking and alerts
- [ ] Consolidated e-way bill support

---

### 5. MSME Payment Compliance
**Module**: [02-ACCOUNTS-PAYABLE](../modules/02-ACCOUNTS-PAYABLE.md) (to create)
**Status**: Not Started

**Description**: Track 45-day payment compliance for MSME vendors, generate MSME Form 1 summary.

**Tasks**:
- [ ] MSME flag on party master
- [ ] Payment aging report for MSME vendors
- [ ] 45-day breach alerting
- [ ] MSME Form 1 data export

---

### 6. Audit Trail / Edit Log
**Module**: [16-ADMINISTRATION](../modules/16-ADMINISTRATION.md) (to create)
**Status**: Not Started

**Description**: MCA-compliant audit trail for all document changes with before/after values.

**Tasks**:
- [ ] Audit log table design
- [ ] Change tracking triggers/middleware
- [ ] Audit log viewer UI
- [ ] Export for audit purposes

---

### 7. Bank Feed Integration
**Module**: [04-BANKING](../modules/04-BANKING.md) (to create)
**Status**: Not Started

**Description**: Auto-import bank statements, categorization rules, smart reconciliation.

**Tasks**:
- [ ] Bank feed API integration (aggregator or direct)
- [ ] Statement parser (MT940, CSV, PDF)
- [ ] Categorization rules engine
- [ ] Auto-matching improvements

---

## P2 - Medium Priority

### 8. Invoice OCR Capture
**Module**: [02-ACCOUNTS-PAYABLE](../modules/02-ACCOUNTS-PAYABLE.md)
**Status**: Not Started

**Description**: AI-powered invoice data extraction from uploaded images/PDFs.

**Tasks**:
- [ ] OCR service integration
- [ ] Field extraction (vendor, amount, date, GST)
- [ ] Validation queue for human review
- [ ] Auto-create vendor invoice from OCR

---

### 9. Multi-GSTIN Support
**Module**: [09-GST-COMPLIANCE](../modules/09-GST-COMPLIANCE.md)
**Status**: Not Started

**Description**: Support multiple GST registrations per company.

**Tasks**:
- [ ] GSTIN master table
- [ ] Branch/unit to GSTIN mapping
- [ ] Separate GSTR-1/3B per GSTIN
- [ ] Consolidated view across GSTINs

---

### 10. Ledger Health Engine
**Module**: [05-LEDGER](../modules/05-LEDGER.md)
**Status**: Not Started

**Description**: CA-grade ledger health checks running continuously.

**Checks**:
- [ ] Source-to-ledger coverage (unposted transactions)
- [ ] Open items (unallocated advances, credits)
- [ ] Bank gaps (reconciled without JE link)
- [ ] Tax gaps (GST ledger vs statutory summary)
- [ ] Trial balance balancing by period

---

### 11. Year-End Close Checklist
**Module**: [05-LEDGER](../modules/05-LEDGER.md)
**Status**: Not Started

**Description**: Guided year-end closing workflow with automated sign-offs.

**Tasks**:
- [ ] Pre-close checklist items
- [ ] Closing entry generation
- [ ] Opening balance rollover
- [ ] Lock period after close
- [ ] Reopening controls

---

## P3 - Low Priority

### 12. Exception Queues
**Status**: Not Started

**Description**: Centralized queue for posting failures, filing errors, GST rejects.

---

### 13. Role-Based Dashboards
**Status**: Not Started

**Description**: Different dashboard views for CA, CFO, GST operator with SLA timers.

---

### 14. Credit Note / Debit Note
**Module**: [01-BILLING](../modules/01-BILLING.md)
**Status**: Not Started

**Description**: Full credit/debit note workflow with original invoice linking.

---

### 15. Recurring Invoices
**Module**: [01-BILLING](../modules/01-BILLING.md)
**Status**: Not Started

**Description**: Auto-generate invoices on schedule (monthly retainers).

---

### 16. Budget vs Actual
**Module**: [05-LEDGER](../modules/05-LEDGER.md)
**Status**: Not Started

**Description**: Budget entry and variance reporting.

---

## Completed Features (Recent)

- [x] E-invoice GSP integration
- [x] RCM tracking and payment
- [x] ITC blocked categories
- [x] Bank transaction import
- [x] Contractor payment tracking
- [x] Statutory payment tracking
- [x] Tally migration (masters + vouchers)

---

## How to Use This Roadmap

### Starting a Feature
1. Check dependencies are met
2. Read relevant module doc
3. Create implementation plan in `docs/PLAN_X_FEATURE_NAME.md`
4. Update ACTIVE-WORK.md with current focus

### Completing a Feature
1. Update module doc with new functionality
2. Move feature to "Completed" section
3. Update ACTIVE-WORK.md
4. Consider what docs need updating

---

## Related Documents

- [System Gap Analysis](../system-gap-analysis.md) - Full competitive analysis
- [Architecture Guide](../ARCHITECTURE.md) - Implementation patterns
- Module docs in `docs/modules/`
