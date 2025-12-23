# Payroll Journal Entry Implementation Plan

> **Document Version:** 1.0
> **Created:** December 2024
> **Status:** For Review & Approval
> **Prepared By:** Technical Architecture Team
> **Reviewed By:** CA Panel (Pending)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Architecture Overview](#2-architecture-overview)
3. [Phase 1: Database Schema Changes](#3-phase-1-database-schema-changes)
4. [Phase 2: Backend Implementation](#4-phase-2-backend-implementation)
5. [Phase 3: Posting Rules & Templates](#5-phase-3-posting-rules--templates)
6. [Phase 4: Integration & Testing](#6-phase-4-integration--testing)
7. [Migration Strategy](#7-migration-strategy)
8. [Risk Assessment](#8-risk-assessment)
9. [Appendix](#9-appendix)

---

## 1. Executive Summary

### 1.1 Objective

Implement a comprehensive payroll journal entry system that:
- Posts journal entries at three stages: Accrual, Disbursement, and Statutory Remittance
- Maintains full audit trail for statutory compliance
- Supports Indian accounting standards (Ind AS 19, AS 15)
- Integrates with existing Chart of Accounts and General Ledger

### 1.2 Scope

| In Scope | Out of Scope |
|----------|--------------|
| Salary accrual journal entries | Payroll calculation changes |
| Salary disbursement entries | Bank API integration |
| Statutory payment tracking | Automated challan filing |
| TDS/PF/ESI/PT challan reconciliation | Form 16/24Q generation (Phase 2) |
| Correction/reversal workflows | Leave encashment provisioning |

### 1.3 Key Deliverables

1. Database migration scripts (3 migrations)
2. Backend service implementation (`PayrollPostingService`)
3. Posting rule templates (3 rules)
4. API endpoints for manual posting triggers
5. Unit and integration tests
6. Documentation

### 1.4 Timeline Overview

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Database | 2-3 days | None |
| Phase 2: Backend | 4-5 days | Phase 1 |
| Phase 3: Posting Rules | 1-2 days | Phase 1 |
| Phase 4: Testing | 2-3 days | Phase 2, 3 |
| **Total** | **9-13 days** | |

---

## 2. Architecture Overview

### 2.1 Three-Stage Journal Model

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     PAYROLL JOURNAL ENTRY LIFECYCLE                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  PAYROLL RUN STATUS FLOW:                                               │
│  ════════════════════════                                               │
│                                                                         │
│  ┌────────┐   ┌────────────┐   ┌──────────┐   ┌──────────┐   ┌────────┐│
│  │ DRAFT  │──►│ PROCESSING │──►│ COMPUTED │──►│ APPROVED │──►│  PAID  ││
│  └────────┘   └────────────┘   └──────────┘   └──────────┘   └────────┘│
│                                                     │             │     │
│                                                     ▼             ▼     │
│                                              ┌──────────┐  ┌──────────┐│
│                                              │ JOURNAL  │  │ JOURNAL  ││
│                                              │ ACCRUAL  │  │ PAYMENT  ││
│                                              └──────────┘  └──────────┘│
│                                                                         │
│  SEPARATE FLOW FOR STATUTORY PAYMENTS:                                  │
│  ═════════════════════════════════════                                  │
│                                                                         │
│  ┌────────────────┐   ┌────────────────┐   ┌────────────────┐          │
│  │ CHALLAN CREATED│──►│ PAYMENT MADE   │──►│ JOURNAL POSTED │          │
│  │ (TDS/PF/ESI/PT)│   │ (Bank Transfer)│   │ (Auto/Manual)  │          │
│  └────────────────┘   └────────────────┘   └────────────────┘          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           BACKEND SERVICES                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────┐       ┌─────────────────────┐                 │
│  │  PayrollController  │       │  StatutoryController │                 │
│  │  /api/payroll/*     │       │  /api/statutory/*    │                 │
│  └──────────┬──────────┘       └──────────┬──────────┘                 │
│             │                              │                            │
│             ▼                              ▼                            │
│  ┌─────────────────────────────────────────────────────────┐           │
│  │              PayrollPostingService (NEW)                │           │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌────────────┐│           │
│  │  │PostAccrualAsync │ │PostDisbursement │ │PostStatutory││           │
│  │  │                 │ │Async            │ │PaymentAsync ││           │
│  │  └─────────────────┘ └─────────────────┘ └────────────┘│           │
│  └──────────┬────────────────────┬─────────────────┬──────┘           │
│             │                    │                 │                    │
│             ▼                    ▼                 ▼                    │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐        │
│  │PayrollRepository │ │JournalRepository │ │ChallanRepository │        │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.3 Journal Entry Structure

| Stage | Trigger Event | Source Type | Journal Type |
|-------|---------------|-------------|--------------|
| Accrual | Payroll Approved | `payroll_run` | `PAYROLL_ACCRUAL` |
| Disbursement | Bank Transfer Complete | `payroll_run` | `PAYROLL_DISBURSEMENT` |
| Statutory | Challan Payment | `statutory_payment` | `STATUTORY_REMITTANCE` |

---

## 3. Phase 1: Database Schema Changes

### 3.1 Migration 101: Enhanced Chart of Accounts

**File:** `migrations/101_enhance_payroll_accounts.sql`

```sql
-- ============================================================================
-- Migration 101: Enhance Chart of Accounts for Payroll
-- Description: Split PF Payable, add Gratuity Payable, add Bonus Payable
-- Author: System
-- Date: 2024-12
-- ============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Function to add account if not exists
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION add_account_if_not_exists(
    p_company_id UUID,
    p_account_code VARCHAR(20),
    p_account_name VARCHAR(200),
    p_account_type VARCHAR(50),
    p_account_subtype VARCHAR(50),
    p_parent_code VARCHAR(20),
    p_normal_balance VARCHAR(10),
    p_schedule_reference VARCHAR(50) DEFAULT NULL
) RETURNS VOID AS $$
DECLARE
    v_parent_id UUID;
BEGIN
    -- Check if account already exists
    IF EXISTS (SELECT 1 FROM chart_of_accounts
               WHERE company_id = p_company_id AND account_code = p_account_code) THEN
        RETURN;
    END IF;

    -- Get parent account ID
    SELECT id INTO v_parent_id
    FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = p_parent_code;

    -- Insert new account
    INSERT INTO chart_of_accounts (
        id, company_id, account_code, account_name, account_type,
        account_subtype, parent_account_id, normal_balance,
        schedule_reference, is_system_account, is_active,
        opening_balance, current_balance, created_at
    ) VALUES (
        gen_random_uuid(), p_company_id, p_account_code, p_account_name,
        p_account_type, p_account_subtype, v_parent_id, p_normal_balance,
        p_schedule_reference, true, true, 0, 0, NOW()
    );
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 2. Add new payroll-related accounts for all companies
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    company_rec RECORD;
BEGIN
    FOR company_rec IN SELECT id FROM companies LOOP
        -- Split PF Payable into Employee and Employer portions
        PERFORM add_account_if_not_exists(
            company_rec.id, '2221', 'Employee PF Contribution Payable',
            'liability', 'current_liability', '2220', 'credit', 'II(a)'
        );

        PERFORM add_account_if_not_exists(
            company_rec.id, '2222', 'Employer PF Contribution Payable',
            'liability', 'current_liability', '2220', 'credit', 'II(a)'
        );

        -- Add Gratuity Payable (separate from expense)
        PERFORM add_account_if_not_exists(
            company_rec.id, '2250', 'Gratuity Payable',
            'liability', 'current_liability', '2200', 'credit', 'II(a)'
        );

        -- Add Bonus Payable
        PERFORM add_account_if_not_exists(
            company_rec.id, '2260', 'Bonus Payable',
            'liability', 'current_liability', '2200', 'credit', 'II(a)'
        );

        -- Add Reimbursements Payable
        PERFORM add_account_if_not_exists(
            company_rec.id, '2270', 'Reimbursements Payable',
            'liability', 'current_liability', '2200', 'credit', 'II(a)'
        );

        -- Add LWF Payable (Labour Welfare Fund)
        PERFORM add_account_if_not_exists(
            company_rec.id, '2245', 'LWF Payable',
            'liability', 'current_liability', '2200', 'credit', 'II(a)'
        );

        -- Add Employee ESI Contribution Payable (split like PF)
        PERFORM add_account_if_not_exists(
            company_rec.id, '2231', 'Employee ESI Contribution Payable',
            'liability', 'current_liability', '2230', 'credit', 'II(a)'
        );

        PERFORM add_account_if_not_exists(
            company_rec.id, '2232', 'Employer ESI Contribution Payable',
            'liability', 'current_liability', '2230', 'credit', 'II(a)'
        );
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 3. Add sort_order for new accounts
-- -----------------------------------------------------------------------------
UPDATE chart_of_accounts SET sort_order = 2210 WHERE account_code = '2221';
UPDATE chart_of_accounts SET sort_order = 2220 WHERE account_code = '2222';
UPDATE chart_of_accounts SET sort_order = 2310 WHERE account_code = '2231';
UPDATE chart_of_accounts SET sort_order = 2320 WHERE account_code = '2232';
UPDATE chart_of_accounts SET sort_order = 2450 WHERE account_code = '2245';
UPDATE chart_of_accounts SET sort_order = 2500 WHERE account_code = '2250';
UPDATE chart_of_accounts SET sort_order = 2600 WHERE account_code = '2260';
UPDATE chart_of_accounts SET sort_order = 2700 WHERE account_code = '2270';

-- -----------------------------------------------------------------------------
-- 4. Clean up function
-- -----------------------------------------------------------------------------
DROP FUNCTION IF EXISTS add_account_if_not_exists;

COMMIT;
```

### 3.2 Migration 102: Payroll Journal Linkage

**File:** `migrations/102_payroll_journal_linkage.sql`

```sql
-- ============================================================================
-- Migration 102: Payroll Journal Linkage
-- Description: Add journal entry references to payroll tables
-- Author: System
-- Date: 2024-12
-- ============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Add journal entry references to payroll_runs
-- -----------------------------------------------------------------------------
ALTER TABLE payroll_runs
ADD COLUMN IF NOT EXISTS accrual_journal_entry_id UUID REFERENCES journal_entries(id),
ADD COLUMN IF NOT EXISTS disbursement_journal_entry_id UUID REFERENCES journal_entries(id);

-- Add comments for documentation
COMMENT ON COLUMN payroll_runs.accrual_journal_entry_id IS
    'Journal entry created on payroll approval (expense recognition)';
COMMENT ON COLUMN payroll_runs.disbursement_journal_entry_id IS
    'Journal entry created on salary disbursement (liability settlement)';

-- -----------------------------------------------------------------------------
-- 2. Add idempotency key to journal_entries
-- -----------------------------------------------------------------------------
ALTER TABLE journal_entries
ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(100),
ADD COLUMN IF NOT EXISTS correction_of_id UUID REFERENCES journal_entries(id);

-- Create unique index for idempotency
CREATE UNIQUE INDEX IF NOT EXISTS idx_journal_entries_idempotency_key
ON journal_entries(idempotency_key)
WHERE idempotency_key IS NOT NULL;

-- Add comments
COMMENT ON COLUMN journal_entries.idempotency_key IS
    'Unique key to prevent duplicate journal entries for same source';
COMMENT ON COLUMN journal_entries.correction_of_id IS
    'Reference to original entry that this entry corrects';

-- -----------------------------------------------------------------------------
-- 3. Add rule_code to journal_entries for easier tracking
-- -----------------------------------------------------------------------------
ALTER TABLE journal_entries
ADD COLUMN IF NOT EXISTS rule_code VARCHAR(50);

COMMENT ON COLUMN journal_entries.rule_code IS
    'Posting rule code used to create this entry (e.g., PAYROLL_ACCRUAL)';

-- Create index for rule_code queries
CREATE INDEX IF NOT EXISTS idx_journal_entries_rule_code
ON journal_entries(rule_code)
WHERE rule_code IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 4. Add indexes for payroll-journal joins
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_payroll_runs_accrual_je
ON payroll_runs(accrual_journal_entry_id)
WHERE accrual_journal_entry_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_payroll_runs_disbursement_je
ON payroll_runs(disbursement_journal_entry_id)
WHERE disbursement_journal_entry_id IS NOT NULL;

COMMIT;
```

### 3.3 Migration 103: Statutory Payment Tracking

**File:** `migrations/103_statutory_payment_tracking.sql`

```sql
-- ============================================================================
-- Migration 103: Statutory Payment Tracking
-- Description: Create tables for TDS/PF/ESI/PT challan tracking
-- Author: System
-- Date: 2024-12
-- ============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Create statutory_payment_types enum-like table
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS statutory_payment_types (
    code VARCHAR(20) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    due_day INTEGER NOT NULL,  -- Day of month when due
    grace_period_days INTEGER DEFAULT 0,
    penalty_type VARCHAR(20) NOT NULL,  -- 'percentage_monthly', 'fixed_daily', 'percentage_annual'
    penalty_rate NUMERIC(10,4) NOT NULL,
    filing_form VARCHAR(50),
    payment_frequency VARCHAR(20) DEFAULT 'monthly',  -- monthly, quarterly
    remarks TEXT
);

-- Seed statutory payment types
INSERT INTO statutory_payment_types (code, name, due_day, grace_period_days, penalty_type, penalty_rate, filing_form, payment_frequency, remarks)
VALUES
    ('TDS_192', 'TDS on Salary (Section 192)', 7, 0, 'percentage_monthly', 1.5, 'Form 24Q', 'quarterly', 'Due by 7th of following month'),
    ('PF', 'Provident Fund', 15, 0, 'percentage_annual', 12.0, 'ECR', 'monthly', 'Due by 15th of following month'),
    ('ESI', 'Employee State Insurance', 15, 0, 'percentage_annual', 12.0, 'ESI Challan', 'monthly', 'Due by 15th of following month'),
    ('PT_KA', 'Professional Tax - Karnataka', 20, 0, 'percentage_monthly', 1.25, 'Form 5', 'monthly', 'Due by 20th of following month'),
    ('PT_MH', 'Professional Tax - Maharashtra', 31, 0, 'fixed_daily', 5.0, 'Form III', 'monthly', 'Due by end of following month'),
    ('PT_GJ', 'Professional Tax - Gujarat', 15, 0, 'percentage_monthly', 2.0, 'Form 5', 'monthly', 'Due by 15th of following month'),
    ('LWF', 'Labour Welfare Fund', 15, 0, 'fixed_daily', 1.0, 'LWF Challan', 'biannual', 'June and December')
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    due_day = EXCLUDED.due_day,
    penalty_rate = EXCLUDED.penalty_rate;

-- -----------------------------------------------------------------------------
-- 2. Create statutory_payments table (challans)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS statutory_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Payment identification
    payment_type VARCHAR(20) NOT NULL REFERENCES statutory_payment_types(code),
    reference_number VARCHAR(50),  -- Challan number, CRN, etc.

    -- Period information
    financial_year VARCHAR(10) NOT NULL,
    period_month INTEGER NOT NULL,  -- 1-12 (April = 1)
    period_year INTEGER NOT NULL,   -- Calendar year of the period
    quarter VARCHAR(2),             -- Q1, Q2, Q3, Q4 (for TDS)

    -- Amount details
    principal_amount NUMERIC(18,2) NOT NULL,
    interest_amount NUMERIC(18,2) DEFAULT 0,
    penalty_amount NUMERIC(18,2) DEFAULT 0,
    late_fee NUMERIC(18,2) DEFAULT 0,
    total_amount NUMERIC(18,2) NOT NULL,

    -- Payment details
    payment_date DATE,
    payment_mode VARCHAR(20),  -- neft, rtgs, online, cheque
    bank_name VARCHAR(100),
    bank_account_id UUID REFERENCES bank_accounts(id),
    bank_reference VARCHAR(50),  -- UTR, cheque number

    -- For TDS specific
    bsr_code VARCHAR(10),        -- Bank branch code
    receipt_number VARCHAR(50),

    -- For PF specific
    trrn VARCHAR(20),            -- TRRN for ECR

    -- Status tracking
    status VARCHAR(20) DEFAULT 'pending',  -- pending, paid, verified, filed
    due_date DATE NOT NULL,

    -- Journal linkage
    journal_entry_id UUID REFERENCES journal_entries(id),

    -- Audit
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    paid_by UUID,
    verified_by UUID,
    verified_at TIMESTAMP,

    -- Constraints
    CONSTRAINT chk_statutory_amount CHECK (total_amount >= principal_amount),
    CONSTRAINT chk_statutory_period CHECK (period_month BETWEEN 1 AND 12)
);

-- Indexes
CREATE INDEX idx_statutory_payments_company ON statutory_payments(company_id);
CREATE INDEX idx_statutory_payments_type ON statutory_payments(payment_type);
CREATE INDEX idx_statutory_payments_period ON statutory_payments(financial_year, period_month);
CREATE INDEX idx_statutory_payments_status ON statutory_payments(status);
CREATE INDEX idx_statutory_payments_due_date ON statutory_payments(due_date);
CREATE UNIQUE INDEX idx_statutory_payments_unique ON statutory_payments(
    company_id, payment_type, financial_year, period_month
) WHERE status != 'cancelled';

-- -----------------------------------------------------------------------------
-- 3. Create statutory_payment_allocations (link payments to payroll)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS statutory_payment_allocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    statutory_payment_id UUID NOT NULL REFERENCES statutory_payments(id),
    payroll_run_id UUID REFERENCES payroll_runs(id),
    payroll_transaction_id UUID REFERENCES payroll_transactions(id),
    contractor_payment_id UUID REFERENCES contractor_payments(id),
    amount_allocated NUMERIC(18,2) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),

    -- At least one source must be specified
    CONSTRAINT chk_allocation_source CHECK (
        payroll_run_id IS NOT NULL OR
        payroll_transaction_id IS NOT NULL OR
        contractor_payment_id IS NOT NULL
    )
);

CREATE INDEX idx_statutory_allocations_payment ON statutory_payment_allocations(statutory_payment_id);
CREATE INDEX idx_statutory_allocations_payroll ON statutory_payment_allocations(payroll_run_id);

-- -----------------------------------------------------------------------------
-- 4. Create view for pending statutory payments
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW v_pending_statutory_payments AS
WITH payroll_dues AS (
    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month as period_month,
        pr.payroll_year as period_year,
        'TDS_192' as payment_type,
        SUM(pt.tds_deducted) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '6 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    WHERE pr.status IN ('approved', 'paid')
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year

    UNION ALL

    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month,
        pr.payroll_year,
        'PF' as payment_type,
        SUM(pt.pf_employee + pt.pf_employer + COALESCE(pt.pf_admin_charges, 0)) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '14 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    WHERE pr.status IN ('approved', 'paid')
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year

    UNION ALL

    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month,
        pr.payroll_year,
        'ESI' as payment_type,
        SUM(pt.esi_employee + pt.esi_employer) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '14 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    WHERE pr.status IN ('approved', 'paid')
    AND (pt.esi_employee > 0 OR pt.esi_employer > 0)
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year

    UNION ALL

    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month,
        pr.payroll_year,
        'PT_' || UPPER(COALESCE(epi.work_state, 'KA')) as payment_type,
        SUM(pt.professional_tax) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '19 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    JOIN employee_payroll_info epi ON epi.employee_id = pt.employee_id
    WHERE pr.status IN ('approved', 'paid')
    AND pt.professional_tax > 0
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year, epi.work_state
)
SELECT
    pd.company_id,
    pd.financial_year,
    pd.period_month,
    pd.period_year,
    pd.payment_type,
    spt.name as payment_type_name,
    pd.amount_due,
    COALESCE(sp.total_amount, 0) as amount_paid,
    pd.amount_due - COALESCE(sp.total_amount, 0) as balance_due,
    pd.due_date,
    CASE
        WHEN sp.id IS NOT NULL AND sp.status = 'paid' THEN 'paid'
        WHEN CURRENT_DATE > pd.due_date THEN 'overdue'
        WHEN CURRENT_DATE >= pd.due_date - INTERVAL '3 days' THEN 'due_soon'
        ELSE 'upcoming'
    END as payment_status,
    sp.id as statutory_payment_id,
    sp.reference_number,
    sp.payment_date
FROM payroll_dues pd
JOIN statutory_payment_types spt ON spt.code = pd.payment_type
    OR (pd.payment_type LIKE 'PT_%' AND spt.code LIKE 'PT_%')
LEFT JOIN statutory_payments sp ON sp.company_id = pd.company_id
    AND sp.payment_type = pd.payment_type
    AND sp.financial_year = pd.financial_year
    AND sp.period_month = pd.period_month
WHERE pd.amount_due > 0;

-- -----------------------------------------------------------------------------
-- 5. Add professional tax due dates to existing table
-- -----------------------------------------------------------------------------
ALTER TABLE professional_tax_slabs
ADD COLUMN IF NOT EXISTS due_day INTEGER DEFAULT 20,
ADD COLUMN IF NOT EXISTS penalty_type VARCHAR(20) DEFAULT 'percentage_monthly',
ADD COLUMN IF NOT EXISTS penalty_rate NUMERIC(10,4) DEFAULT 1.25;

-- Update state-specific due dates
UPDATE professional_tax_slabs SET due_day = 20, penalty_rate = 1.25 WHERE state = 'karnataka';
UPDATE professional_tax_slabs SET due_day = 31, penalty_type = 'fixed_daily', penalty_rate = 5.0 WHERE state = 'maharashtra';
UPDATE professional_tax_slabs SET due_day = 15, penalty_rate = 2.0 WHERE state = 'gujarat';
UPDATE professional_tax_slabs SET due_day = 21, penalty_rate = 1.0 WHERE state = 'west_bengal';

COMMIT;
```

---

## 4. Phase 2: Backend Implementation

### 4.1 New Interfaces

**File:** `src/Core/Interfaces/Payroll/IPayrollPostingService.cs`

```csharp
using Core.Entities.Ledger;
using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    /// <summary>
    /// Service interface for posting payroll-related journal entries
    /// </summary>
    public interface IPayrollPostingService
    {
        /// <summary>
        /// Posts accrual journal entry when payroll is approved
        /// Creates expense recognition entries
        /// </summary>
        Task<JournalEntry?> PostAccrualAsync(
            Guid payrollRunId,
            Guid? postedBy = null);

        /// <summary>
        /// Posts disbursement journal entry when salaries are paid
        /// Clears net salary payable, credits bank
        /// </summary>
        Task<JournalEntry?> PostDisbursementAsync(
            Guid payrollRunId,
            Guid bankAccountId,
            Guid? postedBy = null);

        /// <summary>
        /// Posts statutory payment journal entry
        /// Clears TDS/PF/ESI/PT payable, credits bank
        /// </summary>
        Task<JournalEntry?> PostStatutoryPaymentAsync(
            Guid statutoryPaymentId,
            Guid? postedBy = null);

        /// <summary>
        /// Reverses a payroll journal entry (for corrections)
        /// </summary>
        Task<JournalEntry?> ReversePayrollEntryAsync(
            Guid journalEntryId,
            Guid reversedBy,
            string reason);

        /// <summary>
        /// Checks if accrual entry exists for payroll run
        /// </summary>
        Task<bool> HasAccrualEntryAsync(Guid payrollRunId);

        /// <summary>
        /// Checks if disbursement entry exists for payroll run
        /// </summary>
        Task<bool> HasDisbursementEntryAsync(Guid payrollRunId);

        /// <summary>
        /// Gets all journal entries related to a payroll run
        /// </summary>
        Task<IEnumerable<JournalEntry>> GetPayrollEntriesAsync(Guid payrollRunId);
    }
}
```

**File:** `src/Core/Interfaces/Payroll/IStatutoryPaymentRepository.cs`

```csharp
using Core.Entities.Payroll;

namespace Core.Interfaces.Payroll
{
    public interface IStatutoryPaymentRepository
    {
        Task<StatutoryPayment?> GetByIdAsync(Guid id);
        Task<IEnumerable<StatutoryPayment>> GetByCompanyAsync(Guid companyId);
        Task<IEnumerable<StatutoryPayment>> GetPendingAsync(Guid companyId);
        Task<StatutoryPayment?> GetByPeriodAsync(
            Guid companyId,
            string paymentType,
            string financialYear,
            int periodMonth);
        Task<StatutoryPayment> AddAsync(StatutoryPayment payment);
        Task UpdateAsync(StatutoryPayment payment);
        Task<IEnumerable<PendingStatutoryPaymentView>> GetPendingPaymentsViewAsync(
            Guid companyId);
    }
}
```

### 4.2 New Entities

**File:** `src/Core/Entities/Payroll/StatutoryPayment.cs`

```csharp
namespace Core.Entities.Payroll
{
    /// <summary>
    /// Represents a statutory payment (TDS, PF, ESI, PT challan)
    /// </summary>
    public class StatutoryPayment
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }

        // Payment identification
        public string PaymentType { get; set; } = string.Empty; // TDS_192, PF, ESI, PT_KA
        public string? ReferenceNumber { get; set; } // Challan number

        // Period information
        public string FinancialYear { get; set; } = string.Empty; // 2024-25
        public int PeriodMonth { get; set; } // 1-12 (April = 1)
        public int PeriodYear { get; set; } // Calendar year
        public string? Quarter { get; set; } // Q1, Q2, Q3, Q4

        // Amounts
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalAmount { get; set; }

        // Payment details
        public DateOnly? PaymentDate { get; set; }
        public string? PaymentMode { get; set; }
        public string? BankName { get; set; }
        public Guid? BankAccountId { get; set; }
        public string? BankReference { get; set; }

        // TDS specific
        public string? BsrCode { get; set; }
        public string? ReceiptNumber { get; set; }

        // PF specific
        public string? Trrn { get; set; }

        // Status
        public string Status { get; set; } = "pending"; // pending, paid, verified, filed
        public DateOnly DueDate { get; set; }

        // Journal linkage
        public Guid? JournalEntryId { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public Guid? PaidBy { get; set; }
        public Guid? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }

        // Calculated properties
        public bool IsOverdue => PaymentDate == null && DateOnly.FromDateTime(DateTime.Today) > DueDate;
        public int DaysOverdue => IsOverdue ?
            DateOnly.FromDateTime(DateTime.Today).DayNumber - DueDate.DayNumber : 0;
    }

    /// <summary>
    /// View model for pending statutory payments dashboard
    /// </summary>
    public class PendingStatutoryPaymentView
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string PaymentTypeName { get; set; } = string.Empty;
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public DateOnly DueDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty; // paid, overdue, due_soon, upcoming
        public Guid? StatutoryPaymentId { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateOnly? PaymentDate { get; set; }
    }
}
```

### 4.3 Service Implementation

**File:** `src/Application/Services/Payroll/PayrollPostingService.cs`

```csharp
using Application.Interfaces.Ledger;
using Core.Entities.Ledger;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Ledger;
using Core.Interfaces.Payroll;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services.Payroll
{
    /// <summary>
    /// Service for posting payroll-related journal entries
    /// Implements three-stage journal model:
    /// 1. Accrual (on approval) - expense recognition
    /// 2. Disbursement (on payment) - salary payment
    /// 3. Statutory (on challan) - statutory remittance
    /// </summary>
    public class PayrollPostingService : IPayrollPostingService
    {
        private readonly IPayrollRunRepository _payrollRunRepository;
        private readonly IPayrollTransactionRepository _transactionRepository;
        private readonly IJournalEntryRepository _journalRepository;
        private readonly IChartOfAccountRepository _accountRepository;
        private readonly IStatutoryPaymentRepository _statutoryRepository;
        private readonly ILogger<PayrollPostingService> _logger;

        // Account codes - should ideally come from configuration
        private static class AccountCodes
        {
            // Expenses
            public const string SalariesAndWages = "5210";
            public const string EmployerPF = "5220";
            public const string EmployerESI = "5230";
            public const string GratuityExpense = "5250";
            public const string BonusExpense = "5260";

            // Liabilities
            public const string SalaryPayable = "2110";
            public const string TdsPayableSalary = "2212";
            public const string EmployeePfPayable = "2221";
            public const string EmployerPfPayable = "2222";
            public const string EmployeeEsiPayable = "2231";
            public const string EmployerEsiPayable = "2232";
            public const string PtPayable = "2240";
            public const string GratuityPayable = "2250";

            // Assets
            public const string DefaultBank = "1112";
        }

        public PayrollPostingService(
            IPayrollRunRepository payrollRunRepository,
            IPayrollTransactionRepository transactionRepository,
            IJournalEntryRepository journalRepository,
            IChartOfAccountRepository accountRepository,
            IStatutoryPaymentRepository statutoryRepository,
            ILogger<PayrollPostingService> logger)
        {
            _payrollRunRepository = payrollRunRepository;
            _transactionRepository = transactionRepository;
            _journalRepository = journalRepository;
            _accountRepository = accountRepository;
            _statutoryRepository = statutoryRepository;
            _logger = logger;
        }

        /// <summary>
        /// Posts accrual journal entry when payroll is approved
        /// </summary>
        public async Task<JournalEntry?> PostAccrualAsync(
            Guid payrollRunId,
            Guid? postedBy = null)
        {
            try
            {
                // Idempotency check
                var idempotencyKey = $"PAYROLL_ACCRUAL_{payrollRunId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Accrual journal already exists for payroll run {PayrollRunId}",
                        payrollRunId);
                    return existing;
                }

                // Get payroll run with transactions
                var payrollRun = await _payrollRunRepository.GetByIdAsync(payrollRunId);
                if (payrollRun == null)
                {
                    _logger.LogWarning("Payroll run {PayrollRunId} not found", payrollRunId);
                    return null;
                }

                if (payrollRun.Status != "approved" && payrollRun.Status != "paid")
                {
                    _logger.LogWarning(
                        "Payroll run {PayrollRunId} is not approved. Status: {Status}",
                        payrollRunId, payrollRun.Status);
                    return null;
                }

                // Get aggregated amounts from transactions
                var transactions = await _transactionRepository
                    .GetByPayrollRunIdAsync(payrollRunId);

                var aggregates = CalculateAggregates(transactions);

                // Build journal entry
                var journalDate = DateOnly.FromDateTime(
                    payrollRun.ApprovedAt ?? DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                var entry = new JournalEntry
                {
                    CompanyId = payrollRun.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "payroll_run",
                    SourceId = payrollRunId,
                    SourceNumber = $"PR-{payrollRun.PayrollYear}-{payrollRun.PayrollMonth:D2}",
                    Description = $"Salary accrual for {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear}",
                    Narration = $"Being salary and statutory contributions accrued for the month of {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "PAYROLL_ACCRUAL",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Build journal lines
                await AddAccrualLines(entry, payrollRun.CompanyId, aggregates);

                // Validate balanced
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Accrual journal for payroll {PayrollRunId} is not balanced. " +
                        "Debit: {Debit}, Credit: {Credit}",
                        payrollRunId, entry.TotalDebit, entry.TotalCredit);
                    return null;
                }

                // Save journal entry
                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update payroll run with journal reference
                payrollRun.AccrualJournalEntryId = savedEntry.Id;
                await _payrollRunRepository.UpdateAsync(payrollRun);

                _logger.LogInformation(
                    "Created accrual journal {JournalNumber} for payroll run {PayrollRunId}",
                    savedEntry.JournalNumber, payrollRunId);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating accrual journal for payroll run {PayrollRunId}",
                    payrollRunId);
                throw;
            }
        }

        /// <summary>
        /// Posts disbursement journal entry when salaries are paid
        /// </summary>
        public async Task<JournalEntry?> PostDisbursementAsync(
            Guid payrollRunId,
            Guid bankAccountId,
            Guid? postedBy = null)
        {
            try
            {
                // Idempotency check
                var idempotencyKey = $"PAYROLL_DISBURSEMENT_{payrollRunId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Disbursement journal already exists for payroll run {PayrollRunId}",
                        payrollRunId);
                    return existing;
                }

                var payrollRun = await _payrollRunRepository.GetByIdAsync(payrollRunId);
                if (payrollRun == null)
                {
                    _logger.LogWarning("Payroll run {PayrollRunId} not found", payrollRunId);
                    return null;
                }

                if (payrollRun.Status != "paid")
                {
                    _logger.LogWarning(
                        "Payroll run {PayrollRunId} is not marked as paid. Status: {Status}",
                        payrollRunId, payrollRun.Status);
                    return null;
                }

                // Get bank account code
                var bankAccount = await _accountRepository.GetByIdAsync(bankAccountId);
                var bankAccountCode = bankAccount?.AccountCode ?? AccountCodes.DefaultBank;

                var journalDate = DateOnly.FromDateTime(
                    payrollRun.PaidAt ?? DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                var entry = new JournalEntry
                {
                    CompanyId = payrollRun.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "payroll_run",
                    SourceId = payrollRunId,
                    SourceNumber = $"PR-{payrollRun.PayrollYear}-{payrollRun.PayrollMonth:D2}",
                    Description = $"Salary payment for {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear}",
                    Narration = $"Being net salary paid via bank transfer for {GetMonthName(payrollRun.PayrollMonth)} {payrollRun.PayrollYear}. Payment ref: {payrollRun.PaymentReference ?? "N/A"}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "PAYROLL_DISBURSEMENT",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                var netSalary = payrollRun.TotalNetSalary ?? 0;

                // Debit: Salary Payable
                var salaryPayableAccount = await _accountRepository
                    .GetByCodeAsync(payrollRun.CompanyId, AccountCodes.SalaryPayable);
                if (salaryPayableAccount != null)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = salaryPayableAccount.Id,
                        DebitAmount = netSalary,
                        CreditAmount = 0,
                        Description = "Clear net salary payable",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }

                // Credit: Bank Account
                var bankAcc = await _accountRepository
                    .GetByCodeAsync(payrollRun.CompanyId, bankAccountCode);
                if (bankAcc != null)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = bankAcc.Id,
                        DebitAmount = 0,
                        CreditAmount = netSalary,
                        Description = $"Salary payment - {payrollRun.PaymentMode ?? "Bank Transfer"}",
                        Currency = "INR",
                        ExchangeRate = 1,
                        SubledgerType = "bank",
                        SubledgerId = bankAccountId
                    });
                }

                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                // Validate balanced
                if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                {
                    _logger.LogError(
                        "Disbursement journal for payroll {PayrollRunId} is not balanced",
                        payrollRunId);
                    return null;
                }

                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update payroll run
                payrollRun.DisbursementJournalEntryId = savedEntry.Id;
                await _payrollRunRepository.UpdateAsync(payrollRun);

                _logger.LogInformation(
                    "Created disbursement journal {JournalNumber} for payroll run {PayrollRunId}",
                    savedEntry.JournalNumber, payrollRunId);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating disbursement journal for payroll run {PayrollRunId}",
                    payrollRunId);
                throw;
            }
        }

        /// <summary>
        /// Posts statutory payment journal entry (TDS/PF/ESI/PT challan)
        /// </summary>
        public async Task<JournalEntry?> PostStatutoryPaymentAsync(
            Guid statutoryPaymentId,
            Guid? postedBy = null)
        {
            try
            {
                var idempotencyKey = $"STATUTORY_PAYMENT_{statutoryPaymentId}";
                var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "Statutory payment journal already exists for {PaymentId}",
                        statutoryPaymentId);
                    return existing;
                }

                var payment = await _statutoryRepository.GetByIdAsync(statutoryPaymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Statutory payment {PaymentId} not found", statutoryPaymentId);
                    return null;
                }

                if (payment.Status != "paid")
                {
                    _logger.LogWarning(
                        "Statutory payment {PaymentId} is not marked as paid",
                        statutoryPaymentId);
                    return null;
                }

                var journalDate = payment.PaymentDate ?? DateOnly.FromDateTime(DateTime.Today);
                var financialYear = GetFinancialYear(journalDate);
                var periodMonth = GetPeriodMonth(journalDate);

                var entry = new JournalEntry
                {
                    CompanyId = payment.CompanyId,
                    JournalDate = journalDate,
                    FinancialYear = financialYear,
                    PeriodMonth = periodMonth,
                    EntryType = "auto_post",
                    SourceType = "statutory_payment",
                    SourceId = statutoryPaymentId,
                    SourceNumber = payment.ReferenceNumber ?? $"STAT-{payment.Id.ToString()[..8]}",
                    Description = $"{GetStatutoryPaymentDescription(payment.PaymentType)} for period {payment.PeriodMonth}/{payment.PeriodYear}",
                    Status = "posted",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = postedBy,
                    IdempotencyKey = idempotencyKey,
                    RuleCode = "STATUTORY_REMITTANCE",
                    CreatedBy = postedBy,
                    Lines = new List<JournalEntryLine>()
                };

                // Determine which payable account to debit based on payment type
                var payableAccountCode = GetPayableAccountCode(payment.PaymentType);
                var payableAccount = await _accountRepository
                    .GetByCodeAsync(payment.CompanyId, payableAccountCode);

                if (payableAccount != null)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = payableAccount.Id,
                        DebitAmount = payment.TotalAmount,
                        CreditAmount = 0,
                        Description = $"Clear {payment.PaymentType} payable",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }

                // Credit bank
                var bankAccountCode = AccountCodes.DefaultBank;
                if (payment.BankAccountId.HasValue)
                {
                    var linkedBank = await _accountRepository.GetByIdAsync(payment.BankAccountId.Value);
                    if (linkedBank != null)
                        bankAccountCode = linkedBank.AccountCode;
                }

                var bankAcc = await _accountRepository
                    .GetByCodeAsync(payment.CompanyId, bankAccountCode);
                if (bankAcc != null)
                {
                    entry.Lines.Add(new JournalEntryLine
                    {
                        AccountId = bankAcc.Id,
                        DebitAmount = 0,
                        CreditAmount = payment.TotalAmount,
                        Description = $"{payment.PaymentType} payment - Ref: {payment.ReferenceNumber ?? payment.BankReference ?? "N/A"}",
                        Currency = "INR",
                        ExchangeRate = 1
                    });
                }

                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                var savedEntry = await _journalRepository.AddAsync(entry);

                // Update statutory payment
                payment.JournalEntryId = savedEntry.Id;
                await _statutoryRepository.UpdateAsync(payment);

                _logger.LogInformation(
                    "Created statutory payment journal {JournalNumber} for payment {PaymentId}",
                    savedEntry.JournalNumber, statutoryPaymentId);

                return savedEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating statutory payment journal for {PaymentId}",
                    statutoryPaymentId);
                throw;
            }
        }

        public async Task<JournalEntry?> ReversePayrollEntryAsync(
            Guid journalEntryId,
            Guid reversedBy,
            string reason)
        {
            return await _journalRepository.CreateReversalAsync(
                journalEntryId, reversedBy, reason);
        }

        public async Task<bool> HasAccrualEntryAsync(Guid payrollRunId)
        {
            var idempotencyKey = $"PAYROLL_ACCRUAL_{payrollRunId}";
            var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            return existing != null;
        }

        public async Task<bool> HasDisbursementEntryAsync(Guid payrollRunId)
        {
            var idempotencyKey = $"PAYROLL_DISBURSEMENT_{payrollRunId}";
            var existing = await _journalRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            return existing != null;
        }

        public async Task<IEnumerable<JournalEntry>> GetPayrollEntriesAsync(Guid payrollRunId)
        {
            return await _journalRepository.GetBySourceAsync("payroll_run", payrollRunId);
        }

        // ==================== Private Helper Methods ====================

        private PayrollAggregates CalculateAggregates(IEnumerable<PayrollTransaction> transactions)
        {
            var list = transactions.ToList();
            return new PayrollAggregates
            {
                TotalGrossSalary = list.Sum(t => t.GrossEarnings),
                TotalNetSalary = list.Sum(t => t.NetPayable),
                TotalTds = list.Sum(t => t.TdsDeducted ?? 0),
                TotalEmployeePf = list.Sum(t => t.PfEmployee ?? 0),
                TotalEmployerPf = list.Sum(t => t.PfEmployer ?? 0),
                TotalPfAdminCharges = list.Sum(t => t.PfAdminCharges ?? 0),
                TotalEmployeeEsi = list.Sum(t => t.EsiEmployee ?? 0),
                TotalEmployerEsi = list.Sum(t => t.EsiEmployer ?? 0),
                TotalProfessionalTax = list.Sum(t => t.ProfessionalTax ?? 0),
                TotalGratuity = list.Sum(t => t.GratuityProvision ?? 0),
                TotalBonus = list.Sum(t => t.BonusPaid ?? 0),
                TotalReimbursements = list.Sum(t => t.Reimbursements ?? 0),
                TotalLoanRecovery = list.Sum(t => t.LoanRecovery ?? 0),
                TotalAdvanceRecovery = list.Sum(t => t.AdvanceRecovery ?? 0)
            };
        }

        private async Task AddAccrualLines(
            JournalEntry entry,
            Guid companyId,
            PayrollAggregates agg)
        {
            // DEBIT LINES (Expenses)

            // 1. Salaries and Wages
            if (agg.TotalGrossSalary > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.SalariesAndWages,
                    agg.TotalGrossSalary, 0, "Salaries and wages expense");
            }

            // 2. Employer PF Contribution
            if (agg.TotalEmployerPf > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerPF,
                    agg.TotalEmployerPf + agg.TotalPfAdminCharges, 0,
                    "Employer PF contribution including admin charges");
            }

            // 3. Employer ESI Contribution
            if (agg.TotalEmployerEsi > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerESI,
                    agg.TotalEmployerEsi, 0, "Employer ESI contribution");
            }

            // 4. Gratuity Expense
            if (agg.TotalGratuity > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.GratuityExpense,
                    agg.TotalGratuity, 0, "Gratuity provision");
            }

            // CREDIT LINES (Liabilities)

            // 1. Net Salary Payable (after all deductions)
            if (agg.TotalNetSalary > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.SalaryPayable,
                    0, agg.TotalNetSalary, "Net salary payable to employees");
            }

            // 2. TDS Payable - Salary
            if (agg.TotalTds > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.TdsPayableSalary,
                    0, agg.TotalTds, "TDS on salary (Section 192)");
            }

            // 3. Employee PF Payable
            if (agg.TotalEmployeePf > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployeePfPayable,
                    0, agg.TotalEmployeePf, "Employee PF contribution payable");
            }

            // 4. Employer PF Payable
            if (agg.TotalEmployerPf > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerPfPayable,
                    0, agg.TotalEmployerPf + agg.TotalPfAdminCharges,
                    "Employer PF contribution payable");
            }

            // 5. Employee ESI Payable
            if (agg.TotalEmployeeEsi > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployeeEsiPayable,
                    0, agg.TotalEmployeeEsi, "Employee ESI contribution payable");
            }

            // 6. Employer ESI Payable
            if (agg.TotalEmployerEsi > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.EmployerEsiPayable,
                    0, agg.TotalEmployerEsi, "Employer ESI contribution payable");
            }

            // 7. Professional Tax Payable
            if (agg.TotalProfessionalTax > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.PtPayable,
                    0, agg.TotalProfessionalTax, "Professional tax payable");
            }

            // 8. Gratuity Payable
            if (agg.TotalGratuity > 0)
            {
                await AddLineIfAccountExists(entry, companyId, AccountCodes.GratuityPayable,
                    0, agg.TotalGratuity, "Gratuity provision payable");
            }

            // Calculate totals
            entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
            entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);
        }

        private async Task AddLineIfAccountExists(
            JournalEntry entry,
            Guid companyId,
            string accountCode,
            decimal debitAmount,
            decimal creditAmount,
            string description)
        {
            var account = await _accountRepository.GetByCodeAsync(companyId, accountCode);
            if (account == null)
            {
                _logger.LogWarning(
                    "Account {AccountCode} not found for company {CompanyId}",
                    accountCode, companyId);
                return;
            }

            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = account.Id,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount,
                Description = description,
                Currency = "INR",
                ExchangeRate = 1
            });
        }

        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        private static int GetPeriodMonth(DateOnly date)
        {
            return date.Month >= 4 ? date.Month - 3 : date.Month + 9;
        }

        private static string GetMonthName(int month)
        {
            return new DateTime(2024, month, 1).ToString("MMMM");
        }

        private static string GetStatutoryPaymentDescription(string paymentType)
        {
            return paymentType switch
            {
                "TDS_192" => "TDS on Salary (Section 192)",
                "PF" => "Provident Fund remittance",
                "ESI" => "ESI contribution remittance",
                _ when paymentType.StartsWith("PT_") => "Professional Tax remittance",
                "LWF" => "Labour Welfare Fund remittance",
                _ => $"{paymentType} remittance"
            };
        }

        private static string GetPayableAccountCode(string paymentType)
        {
            return paymentType switch
            {
                "TDS_192" => AccountCodes.TdsPayableSalary,
                "PF" => "2220", // Parent PF Payable (covers both employee and employer)
                "ESI" => "2230", // Parent ESI Payable
                _ when paymentType.StartsWith("PT_") => AccountCodes.PtPayable,
                "LWF" => "2245",
                _ => "2200" // Other statutory payables
            };
        }
    }

    /// <summary>
    /// Aggregate amounts from payroll transactions
    /// </summary>
    internal class PayrollAggregates
    {
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalNetSalary { get; set; }
        public decimal TotalTds { get; set; }
        public decimal TotalEmployeePf { get; set; }
        public decimal TotalEmployerPf { get; set; }
        public decimal TotalPfAdminCharges { get; set; }
        public decimal TotalEmployeeEsi { get; set; }
        public decimal TotalEmployerEsi { get; set; }
        public decimal TotalProfessionalTax { get; set; }
        public decimal TotalGratuity { get; set; }
        public decimal TotalBonus { get; set; }
        public decimal TotalReimbursements { get; set; }
        public decimal TotalLoanRecovery { get; set; }
        public decimal TotalAdvanceRecovery { get; set; }
    }
}
```

### 4.4 Repository Updates

**File:** `src/Core/Interfaces/Ledger/IJournalEntryRepository.cs` (additions)

```csharp
// Add these methods to the existing interface:

/// <summary>
/// Gets journal entry by idempotency key
/// </summary>
Task<JournalEntry?> GetByIdempotencyKeyAsync(string idempotencyKey);

/// <summary>
/// Gets all journal entries for a source
/// </summary>
Task<IEnumerable<JournalEntry>> GetBySourceAsync(string sourceType, Guid sourceId);
```

**File:** `src/Infrastructure/Data/Ledger/JournalEntryRepository.cs` (additions)

```csharp
public async Task<JournalEntry?> GetByIdempotencyKeyAsync(string idempotencyKey)
{
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QueryFirstOrDefaultAsync<JournalEntry>(
        @"SELECT * FROM journal_entries
          WHERE idempotency_key = @idempotencyKey",
        new { idempotencyKey });
}

public async Task<IEnumerable<JournalEntry>> GetBySourceAsync(
    string sourceType, Guid sourceId)
{
    using var connection = new NpgsqlConnection(_connectionString);
    return await connection.QueryAsync<JournalEntry>(
        @"SELECT * FROM journal_entries
          WHERE source_type = @sourceType AND source_id = @sourceId
          ORDER BY created_at",
        new { sourceType, sourceId });
}
```

### 4.5 API Controller

**File:** `src/WebApi/Controllers/Payroll/PayrollPostingController.cs`

```csharp
using Application.Interfaces.Payroll;
using Core.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Payroll
{
    [ApiController]
    [Route("api/payroll/posting")]
    [Authorize]
    [Produces("application/json")]
    public class PayrollPostingController : ControllerBase
    {
        private readonly IPayrollPostingService _postingService;
        private readonly ILogger<PayrollPostingController> _logger;

        public PayrollPostingController(
            IPayrollPostingService postingService,
            ILogger<PayrollPostingController> logger)
        {
            _postingService = postingService;
            _logger = logger;
        }

        /// <summary>
        /// Post accrual journal entry for approved payroll
        /// </summary>
        [HttpPost("{payrollRunId}/accrual")]
        [ProducesResponseType(typeof(JournalEntryDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostAccrual(Guid payrollRunId)
        {
            var userId = GetCurrentUserId();
            var result = await _postingService.PostAccrualAsync(payrollRunId, userId);

            if (result == null)
                return BadRequest("Could not create accrual journal entry");

            return CreatedAtAction(
                nameof(GetPayrollEntries),
                new { payrollRunId },
                MapToDto(result));
        }

        /// <summary>
        /// Post disbursement journal entry after salary payment
        /// </summary>
        [HttpPost("{payrollRunId}/disbursement")]
        [ProducesResponseType(typeof(JournalEntryDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostDisbursement(
            Guid payrollRunId,
            [FromQuery] Guid bankAccountId)
        {
            var userId = GetCurrentUserId();
            var result = await _postingService.PostDisbursementAsync(
                payrollRunId, bankAccountId, userId);

            if (result == null)
                return BadRequest("Could not create disbursement journal entry");

            return CreatedAtAction(
                nameof(GetPayrollEntries),
                new { payrollRunId },
                MapToDto(result));
        }

        /// <summary>
        /// Post statutory payment journal entry
        /// </summary>
        [HttpPost("statutory/{statutoryPaymentId}")]
        [ProducesResponseType(typeof(JournalEntryDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> PostStatutoryPayment(Guid statutoryPaymentId)
        {
            var userId = GetCurrentUserId();
            var result = await _postingService.PostStatutoryPaymentAsync(
                statutoryPaymentId, userId);

            if (result == null)
                return BadRequest("Could not create statutory payment journal entry");

            return Ok(MapToDto(result));
        }

        /// <summary>
        /// Get all journal entries for a payroll run
        /// </summary>
        [HttpGet("{payrollRunId}/entries")]
        [ProducesResponseType(typeof(IEnumerable<JournalEntryDto>), 200)]
        public async Task<IActionResult> GetPayrollEntries(Guid payrollRunId)
        {
            var entries = await _postingService.GetPayrollEntriesAsync(payrollRunId);
            return Ok(entries.Select(MapToDto));
        }

        /// <summary>
        /// Reverse a payroll journal entry
        /// </summary>
        [HttpPost("reverse/{journalEntryId}")]
        [ProducesResponseType(typeof(JournalEntryDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ReverseEntry(
            Guid journalEntryId,
            [FromBody] ReverseEntryRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _postingService.ReversePayrollEntryAsync(
                journalEntryId, userId, request.Reason);

            if (result == null)
                return BadRequest("Could not reverse journal entry");

            return Ok(MapToDto(result));
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst("sub") ?? User.FindFirst("userId");
            return claim != null && Guid.TryParse(claim.Value, out var id)
                ? id
                : Guid.Empty;
        }

        private static JournalEntryDto MapToDto(JournalEntry entry)
        {
            return new JournalEntryDto
            {
                Id = entry.Id,
                JournalNumber = entry.JournalNumber,
                JournalDate = entry.JournalDate,
                Description = entry.Description,
                Status = entry.Status,
                TotalDebit = entry.TotalDebit,
                TotalCredit = entry.TotalCredit,
                RuleCode = entry.RuleCode
            };
        }
    }

    public class ReverseEntryRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class JournalEntryDto
    {
        public Guid Id { get; set; }
        public string JournalNumber { get; set; } = string.Empty;
        public DateOnly JournalDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public string? RuleCode { get; set; }
    }
}
```

---

## 5. Phase 3: Posting Rules & Templates

### 5.1 Migration 104: Seed Payroll Posting Rules

**File:** `migrations/104_seed_payroll_posting_rules.sql`

```sql
-- ============================================================================
-- Migration 104: Seed Payroll Posting Rules
-- Description: Add posting rules for payroll journal entries
-- Author: System
-- Date: 2024-12
-- ============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Add payroll posting rules for each company
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    company_rec RECORD;
BEGIN
    FOR company_rec IN SELECT id FROM companies LOOP

        -- Rule 1: Payroll Accrual (on approval)
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'PAYROLL_ACCRUAL',
            'Payroll Salary Accrual',
            'payroll_run',
            'on_approval',
            '{"status": "approved"}',
            '{
                "narration_template": "Salary accrual for {payroll_month}/{payroll_year}",
                "lines": [
                    {"account_code": "5210", "debit_field": "total_gross_salary", "description": "Salaries and Wages"},
                    {"account_code": "5220", "debit_field": "total_employer_pf", "description": "Employer PF Contribution"},
                    {"account_code": "5230", "debit_field": "total_employer_esi", "description": "Employer ESI Contribution"},
                    {"account_code": "5250", "debit_field": "total_gratuity", "description": "Gratuity Provision"},
                    {"account_code": "2110", "credit_field": "total_net_salary", "description": "Net Salary Payable"},
                    {"account_code": "2212", "credit_field": "total_tds", "description": "TDS Payable - Salary"},
                    {"account_code": "2221", "credit_field": "total_employee_pf", "description": "Employee PF Payable"},
                    {"account_code": "2222", "credit_field": "total_employer_pf", "description": "Employer PF Payable"},
                    {"account_code": "2231", "credit_field": "total_employee_esi", "description": "Employee ESI Payable"},
                    {"account_code": "2232", "credit_field": "total_employer_esi", "description": "Employer ESI Payable"},
                    {"account_code": "2240", "credit_field": "total_pt", "description": "Professional Tax Payable"},
                    {"account_code": "2250", "credit_field": "total_gratuity", "description": "Gratuity Payable"}
                ]
            }',
            '2024-25',
            10,
            true,
            true,
            NOW()
        ) ON CONFLICT DO NOTHING;

        -- Rule 2: Payroll Disbursement (on payment)
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'PAYROLL_DISBURSEMENT',
            'Payroll Salary Disbursement',
            'payroll_run',
            'on_payment',
            '{"status": "paid"}',
            '{
                "narration_template": "Salary payment for {payroll_month}/{payroll_year} via {payment_mode}",
                "lines": [
                    {"account_code": "2110", "debit_field": "total_net_salary", "description": "Clear Salary Payable"},
                    {"account_code_field": "bank_account_code", "account_code_fallback": "1112", "credit_field": "total_net_salary", "description": "Bank Payment"}
                ]
            }',
            '2024-25',
            20,
            true,
            true,
            NOW()
        ) ON CONFLICT DO NOTHING;

        -- Rule 3: Statutory Remittance (on challan payment)
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'STATUTORY_REMITTANCE',
            'Statutory Payment Remittance',
            'statutory_payment',
            'on_payment',
            '{"status": "paid"}',
            '{
                "narration_template": "{payment_type} remittance for period {period_month}/{period_year}",
                "lines": [
                    {"account_code_field": "payable_account_code", "debit_field": "total_amount", "description": "Clear Statutory Payable"},
                    {"account_code_field": "bank_account_code", "account_code_fallback": "1112", "credit_field": "total_amount", "description": "Bank Payment"}
                ]
            }',
            '2024-25',
            30,
            true,
            true,
            NOW()
        ) ON CONFLICT DO NOTHING;

    END LOOP;
END $$;

COMMIT;
```

---

## 6. Phase 4: Integration & Testing

### 6.1 Integration Points

| Integration Point | Trigger | Action |
|-------------------|---------|--------|
| Payroll Approval | `PayrollRun.Status` → `approved` | Call `PostAccrualAsync()` |
| Salary Payment | `PayrollRun.Status` → `paid` | Call `PostDisbursementAsync()` |
| Statutory Payment | `StatutoryPayment.Status` → `paid` | Call `PostStatutoryPaymentAsync()` |
| Bank Reconciliation | Match bank transaction | Link to journal entry |

### 6.2 Modification to PayrollController

Add calls to posting service when payroll status changes:

```csharp
// In ApprovePayroll method:
[HttpPost("{id}/approve")]
public async Task<IActionResult> ApprovePayroll(Guid id)
{
    // ... existing approval logic ...

    // After successful approval, post accrual journal
    try
    {
        var journalEntry = await _postingService.PostAccrualAsync(id, userId);
        if (journalEntry != null)
        {
            _logger.LogInformation(
                "Created accrual journal {JournalNumber} for payroll {PayrollId}",
                journalEntry.JournalNumber, id);
        }
    }
    catch (Exception ex)
    {
        // Log but don't fail the approval
        _logger.LogWarning(ex,
            "Failed to create accrual journal for payroll {PayrollId}", id);
    }

    return Ok(result);
}

// In MarkAsPaid method:
[HttpPost("{id}/mark-paid")]
public async Task<IActionResult> MarkAsPaid(Guid id, [FromBody] MarkPaidRequest request)
{
    // ... existing payment logic ...

    // After marking as paid, post disbursement journal
    try
    {
        var journalEntry = await _postingService.PostDisbursementAsync(
            id, request.BankAccountId, userId);
        if (journalEntry != null)
        {
            _logger.LogInformation(
                "Created disbursement journal {JournalNumber} for payroll {PayrollId}",
                journalEntry.JournalNumber, id);
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex,
            "Failed to create disbursement journal for payroll {PayrollId}", id);
    }

    return Ok(result);
}
```

### 6.3 Unit Tests

**File:** `tests/UnitTests/Application/Services/PayrollPostingServiceTests.cs`

```csharp
using Application.Services.Payroll;
using Core.Entities.Ledger;
using Core.Entities.Payroll;
using Core.Interfaces.Ledger;
using Core.Interfaces.Payroll;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Application.Services
{
    public class PayrollPostingServiceTests
    {
        private readonly Mock<IPayrollRunRepository> _payrollRunRepo;
        private readonly Mock<IPayrollTransactionRepository> _transactionRepo;
        private readonly Mock<IJournalEntryRepository> _journalRepo;
        private readonly Mock<IChartOfAccountRepository> _accountRepo;
        private readonly Mock<IStatutoryPaymentRepository> _statutoryRepo;
        private readonly Mock<ILogger<PayrollPostingService>> _logger;
        private readonly PayrollPostingService _service;

        public PayrollPostingServiceTests()
        {
            _payrollRunRepo = new Mock<IPayrollRunRepository>();
            _transactionRepo = new Mock<IPayrollTransactionRepository>();
            _journalRepo = new Mock<IJournalEntryRepository>();
            _accountRepo = new Mock<IChartOfAccountRepository>();
            _statutoryRepo = new Mock<IStatutoryPaymentRepository>();
            _logger = new Mock<ILogger<PayrollPostingService>>();

            _service = new PayrollPostingService(
                _payrollRunRepo.Object,
                _transactionRepo.Object,
                _journalRepo.Object,
                _accountRepo.Object,
                _statutoryRepo.Object,
                _logger.Object);
        }

        [Fact]
        public async Task PostAccrualAsync_WhenPayrollNotApproved_ReturnsNull()
        {
            // Arrange
            var payrollRunId = Guid.NewGuid();
            var payrollRun = new PayrollRun
            {
                Id = payrollRunId,
                Status = "computed" // Not approved
            };

            _journalRepo.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>()))
                .ReturnsAsync((JournalEntry?)null);
            _payrollRunRepo.Setup(x => x.GetByIdAsync(payrollRunId))
                .ReturnsAsync(payrollRun);

            // Act
            var result = await _service.PostAccrualAsync(payrollRunId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PostAccrualAsync_WhenAlreadyPosted_ReturnsExistingEntry()
        {
            // Arrange
            var payrollRunId = Guid.NewGuid();
            var existingEntry = new JournalEntry { Id = Guid.NewGuid() };

            _journalRepo.Setup(x => x.GetByIdempotencyKeyAsync(
                    $"PAYROLL_ACCRUAL_{payrollRunId}"))
                .ReturnsAsync(existingEntry);

            // Act
            var result = await _service.PostAccrualAsync(payrollRunId);

            // Assert
            result.Should().Be(existingEntry);
            _payrollRunRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task PostAccrualAsync_WhenValid_CreatesBalancedJournalEntry()
        {
            // Arrange
            var payrollRunId = Guid.NewGuid();
            var companyId = Guid.NewGuid();

            var payrollRun = new PayrollRun
            {
                Id = payrollRunId,
                CompanyId = companyId,
                Status = "approved",
                PayrollMonth = 12,
                PayrollYear = 2024,
                TotalGrossSalary = 100000,
                TotalNetSalary = 80000,
                ApprovedAt = DateTime.UtcNow
            };

            var transactions = new List<PayrollTransaction>
            {
                new()
                {
                    GrossEarnings = 100000,
                    NetPayable = 80000,
                    TdsDeducted = 10000,
                    PfEmployee = 6000,
                    PfEmployer = 6000,
                    EsiEmployee = 750,
                    EsiEmployer = 3250,
                    ProfessionalTax = 200
                }
            };

            _journalRepo.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>()))
                .ReturnsAsync((JournalEntry?)null);
            _payrollRunRepo.Setup(x => x.GetByIdAsync(payrollRunId))
                .ReturnsAsync(payrollRun);
            _transactionRepo.Setup(x => x.GetByPayrollRunIdAsync(payrollRunId))
                .ReturnsAsync(transactions);

            // Setup accounts
            SetupAccount(companyId, "5210", "Salaries and Wages");
            SetupAccount(companyId, "5220", "Employer PF");
            SetupAccount(companyId, "5230", "Employer ESI");
            SetupAccount(companyId, "2110", "Salary Payable");
            SetupAccount(companyId, "2212", "TDS Payable");
            SetupAccount(companyId, "2221", "Employee PF Payable");
            SetupAccount(companyId, "2222", "Employer PF Payable");
            SetupAccount(companyId, "2231", "Employee ESI Payable");
            SetupAccount(companyId, "2232", "Employer ESI Payable");
            SetupAccount(companyId, "2240", "PT Payable");

            JournalEntry? savedEntry = null;
            _journalRepo.Setup(x => x.AddAsync(It.IsAny<JournalEntry>()))
                .Callback<JournalEntry>(e => savedEntry = e)
                .ReturnsAsync((JournalEntry e) => e);

            // Act
            var result = await _service.PostAccrualAsync(payrollRunId);

            // Assert
            result.Should().NotBeNull();
            savedEntry.Should().NotBeNull();
            savedEntry!.TotalDebit.Should().Be(savedEntry.TotalCredit);
            savedEntry.Lines.Should().HaveCountGreaterThan(0);
            savedEntry.RuleCode.Should().Be("PAYROLL_ACCRUAL");
        }

        private void SetupAccount(Guid companyId, string code, string name)
        {
            _accountRepo.Setup(x => x.GetByCodeAsync(companyId, code))
                .ReturnsAsync(new ChartOfAccount
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    AccountCode = code,
                    AccountName = name
                });
        }
    }
}
```

### 6.4 Integration Tests

**File:** `tests/IntegrationTests/PayrollPostingIntegrationTests.cs`

```csharp
// Integration test that validates end-to-end flow
// Tests against actual database with test data

[Fact]
public async Task FullPayrollJournalFlow_CreatesThreeJournalEntries()
{
    // 1. Create payroll run
    // 2. Approve payroll → Verify accrual journal created
    // 3. Mark as paid → Verify disbursement journal created
    // 4. Create statutory payment → Verify statutory journal created
    // 5. Verify all journals are balanced
    // 6. Verify trial balance is correct
}
```

---

## 7. Migration Strategy

### 7.1 Pre-Migration Checklist

- [ ] Backup production database
- [ ] Verify all companies have COA initialized
- [ ] Document existing payroll runs without journal entries
- [ ] Notify users of scheduled maintenance

### 7.2 Migration Steps

| Step | Action | Rollback |
|------|--------|----------|
| 1 | Run migration 101 (COA additions) | Drop new accounts |
| 2 | Run migration 102 (payroll linkage) | Drop new columns |
| 3 | Run migration 103 (statutory tracking) | Drop new tables |
| 4 | Run migration 104 (posting rules) | Delete rules |
| 5 | Deploy new backend code | Revert deployment |
| 6 | Test with single company | N/A |
| 7 | Enable for all companies | Disable feature flag |

### 7.3 Backfill Strategy (Optional)

For existing approved/paid payroll runs without journal entries:

```sql
-- Identify payroll runs needing backfill
SELECT id, payroll_month, payroll_year, status,
       accrual_journal_entry_id, disbursement_journal_entry_id
FROM payroll_runs
WHERE status IN ('approved', 'paid')
AND accrual_journal_entry_id IS NULL
ORDER BY payroll_year, payroll_month;

-- Backfill can be done via API calls or batch job
-- Recommend: Manual review before backfilling historical data
```

---

## 8. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Duplicate journal entries | Low | High | Idempotency keys prevent duplicates |
| Unbalanced journals | Low | High | Validation before save |
| Missing accounts | Medium | Medium | Account existence check with logging |
| Performance at scale | Low | Medium | Batch processing, summary journals |
| Incorrect tax calculations | Low | High | Existing calculation engine unchanged |
| Migration failures | Low | Medium | Transactional migrations, rollback plan |

---

## 9. Appendix

### 9.1 Journal Entry Examples

#### Example 1: December 2024 Payroll (10 employees)

**Accrual Entry (on approval):**
```
Journal: JV-202425-000023
Date: 2024-12-31
Description: Salary accrual for December 2024

Dr. Salaries and Wages (5210)             ₹8,50,000.00
Dr. Employer PF Contribution (5220)          ₹72,000.00
Dr. Employer ESI Contribution (5230)         ₹19,500.00
Dr. Gratuity Expense (5250)                  ₹34,000.00
    Cr. Net Salary Payable (2110)                        ₹7,20,000.00
    Cr. TDS Payable - Salary (2212)                        ₹85,000.00
    Cr. Employee PF Payable (2221)                         ₹60,000.00
    Cr. Employer PF Payable (2222)                         ₹72,000.00
    Cr. Employee ESI Payable (2231)                         ₹4,500.00
    Cr. Employer ESI Payable (2232)                        ₹19,500.00
    Cr. Professional Tax Payable (2240)                     ₹2,000.00
    Cr. Gratuity Payable (2250)                            ₹34,000.00
                                         ─────────────────────────────
Total                                      ₹9,97,500.00   ₹9,97,500.00
```

**Disbursement Entry (on payment):**
```
Journal: JV-202425-000024
Date: 2025-01-05
Description: Salary payment for December 2024

Dr. Net Salary Payable (2110)             ₹7,20,000.00
    Cr. HDFC Bank - Current A/c (1112)                   ₹7,20,000.00
                                         ─────────────────────────────
Total                                      ₹7,20,000.00   ₹7,20,000.00
```

**Statutory Payment Entry (TDS):**
```
Journal: JV-202425-000028
Date: 2025-01-07
Description: TDS on Salary (Section 192) for December 2024

Dr. TDS Payable - Salary (2212)              ₹85,000.00
    Cr. HDFC Bank - Current A/c (1112)                      ₹85,000.00
                                         ─────────────────────────────
Total                                         ₹85,000.00      ₹85,000.00
```

### 9.2 Account Code Reference

| Code | Name | Type | Usage |
|------|------|------|-------|
| 5210 | Salaries and Wages | Expense | Gross salary expense |
| 5220 | Employer PF Contribution | Expense | Company's PF cost |
| 5230 | Employer ESI Contribution | Expense | Company's ESI cost |
| 5250 | Gratuity Expense | Expense | Monthly gratuity provision |
| 5260 | Bonus and Incentives | Expense | Bonus expense |
| 2110 | Salary Payable | Liability | Net salary due to employees |
| 2212 | TDS Payable - Salary | Liability | TDS withheld |
| 2221 | Employee PF Payable | Liability | Employee's PF contribution |
| 2222 | Employer PF Payable | Liability | Employer's PF contribution |
| 2231 | Employee ESI Payable | Liability | Employee's ESI contribution |
| 2232 | Employer ESI Payable | Liability | Employer's ESI contribution |
| 2240 | Professional Tax Payable | Liability | PT withheld |
| 2250 | Gratuity Payable | Liability | Gratuity provision |
| 1112 | Bank Account | Asset | Operating bank account |

### 9.3 Dependency Injection Setup

**File:** `src/WebApi/Configuration/PayrollDiExtensions.cs` (additions)

```csharp
// Add to existing DI configuration:
services.AddScoped<IPayrollPostingService, PayrollPostingService>();
services.AddScoped<IStatutoryPaymentRepository>(provider =>
    new StatutoryPaymentRepository(connectionString));
```

---

## Approval Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Technical Lead | | | |
| CA Reviewer | | | |
| Product Owner | | | |
| QA Lead | | | |

---

**Document End**
