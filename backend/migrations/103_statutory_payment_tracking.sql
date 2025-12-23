-- ============================================================================
-- Migration 103: Statutory Payment Tracking
-- Description: Create tables for TDS/PF/ESI/PT challan tracking and reconciliation
-- Author: System
-- Date: 2024-12
-- ============================================================================

-- -----------------------------------------------------------------------------
-- 1. Create statutory_payment_types lookup table
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS statutory_payment_types (
    code VARCHAR(20) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    category VARCHAR(20) NOT NULL,  -- 'tax', 'pf', 'esi', 'pt', 'lwf'
    due_day INTEGER NOT NULL,       -- Day of month when due
    grace_period_days INTEGER DEFAULT 0,
    penalty_type VARCHAR(20) NOT NULL,  -- 'percentage_monthly', 'fixed_daily', 'percentage_annual'
    penalty_rate NUMERIC(10,4) NOT NULL,
    filing_form VARCHAR(50),
    payment_frequency VARCHAR(20) DEFAULT 'monthly',  -- monthly, quarterly, biannual
    payable_account_code VARCHAR(20),  -- COA account code to debit
    remarks TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Seed statutory payment types
INSERT INTO statutory_payment_types (code, name, category, due_day, grace_period_days, penalty_type, penalty_rate, filing_form, payment_frequency, payable_account_code, remarks)
VALUES
    ('TDS_192', 'TDS on Salary (Section 192)', 'tax', 7, 0, 'percentage_monthly', 1.5, 'Form 24Q', 'quarterly', '2212', 'Due by 7th of following month. Interest u/s 201(1A)'),
    ('TDS_194C', 'TDS on Contractors (Section 194C)', 'tax', 7, 0, 'percentage_monthly', 1.5, 'Form 26Q', 'quarterly', '2213', 'Due by 7th of following month'),
    ('TDS_194J', 'TDS on Professional Fees (Section 194J)', 'tax', 7, 0, 'percentage_monthly', 1.5, 'Form 26Q', 'quarterly', '2213', 'Due by 7th of following month'),
    ('PF', 'Provident Fund', 'pf', 15, 0, 'percentage_annual', 12.0, 'ECR', 'monthly', '2220', 'Due by 15th of following month. Damages under EPFO'),
    ('ESI', 'Employee State Insurance', 'esi', 15, 0, 'percentage_annual', 12.0, 'ESI Challan', 'monthly', '2230', 'Due by 15th of following month'),
    ('PT_KA', 'Professional Tax - Karnataka', 'pt', 20, 0, 'percentage_monthly', 1.25, 'Form 5', 'monthly', '2240', 'Due by 20th of following month'),
    ('PT_MH', 'Professional Tax - Maharashtra', 'pt', 31, 0, 'fixed_daily', 5.0, 'Form III', 'monthly', '2240', 'Due by end of following month'),
    ('PT_GJ', 'Professional Tax - Gujarat', 'pt', 15, 0, 'percentage_monthly', 2.0, 'Form 5', 'monthly', '2240', 'Due by 15th of following month'),
    ('PT_WB', 'Professional Tax - West Bengal', 'pt', 21, 0, 'percentage_monthly', 1.0, 'Form III', 'monthly', '2240', 'Due by 21st of following month'),
    ('PT_TN', 'Professional Tax - Tamil Nadu', 'pt', 30, 0, 'percentage_monthly', 1.0, 'Form II', 'monthly', '2240', 'Due by end of following month'),
    ('PT_AP', 'Professional Tax - Andhra Pradesh', 'pt', 10, 0, 'percentage_monthly', 2.0, 'Form V', 'monthly', '2240', 'Due by 10th of following month'),
    ('PT_TS', 'Professional Tax - Telangana', 'pt', 10, 0, 'percentage_monthly', 2.0, 'Form V', 'monthly', '2240', 'Due by 10th of following month'),
    ('LWF_KA', 'Labour Welfare Fund - Karnataka', 'lwf', 15, 0, 'fixed_daily', 1.0, 'LWF Challan', 'biannual', '2245', 'Due in January and July'),
    ('LWF_MH', 'Labour Welfare Fund - Maharashtra', 'lwf', 15, 0, 'fixed_daily', 1.0, 'LWF Challan', 'biannual', '2245', 'Due in June and December')
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    due_day = EXCLUDED.due_day,
    penalty_rate = EXCLUDED.penalty_rate,
    payable_account_code = EXCLUDED.payable_account_code;

-- -----------------------------------------------------------------------------
-- 2. Create statutory_payments table (challans)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS statutory_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Payment identification
    payment_type VARCHAR(20) NOT NULL REFERENCES statutory_payment_types(code),
    reference_number VARCHAR(50),  -- Challan number, CRN, acknowledgment number

    -- Period information
    financial_year VARCHAR(10) NOT NULL,  -- Format: 2024-25
    period_month INTEGER NOT NULL,        -- 1-12 (April = 1 for Indian FY)
    period_year INTEGER NOT NULL,         -- Calendar year of the period
    quarter VARCHAR(2),                   -- Q1, Q2, Q3, Q4 (for quarterly filings like TDS)

    -- Amount details
    principal_amount NUMERIC(18,2) NOT NULL,
    interest_amount NUMERIC(18,2) DEFAULT 0,
    penalty_amount NUMERIC(18,2) DEFAULT 0,
    late_fee NUMERIC(18,2) DEFAULT 0,
    total_amount NUMERIC(18,2) NOT NULL,

    -- Payment details
    payment_date DATE,
    payment_mode VARCHAR(20),  -- neft, rtgs, online, cheque, upi
    bank_name VARCHAR(100),
    bank_account_id UUID REFERENCES bank_accounts(id),
    bank_reference VARCHAR(50),  -- UTR number, cheque number

    -- For TDS specific fields
    bsr_code VARCHAR(10),        -- Bank branch code for TDS
    receipt_number VARCHAR(50),  -- CIN (Challan Identification Number)

    -- For PF specific fields
    trrn VARCHAR(30),            -- TRRN for ECR filing

    -- For ESI specific fields
    challan_number VARCHAR(30),  -- ESI challan number

    -- Status tracking
    status VARCHAR(20) DEFAULT 'pending',  -- pending, paid, verified, filed, cancelled
    due_date DATE NOT NULL,

    -- Journal linkage
    journal_entry_id UUID REFERENCES journal_entries(id),

    -- Audit fields
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    paid_by UUID,
    paid_at TIMESTAMP,
    verified_by UUID,
    verified_at TIMESTAMP,
    filed_by UUID,
    filed_at TIMESTAMP,
    notes TEXT,

    -- Constraints
    CONSTRAINT chk_statutory_amount CHECK (total_amount >= principal_amount),
    CONSTRAINT chk_statutory_period CHECK (period_month BETWEEN 1 AND 12),
    CONSTRAINT chk_statutory_status CHECK (status IN ('pending', 'paid', 'verified', 'filed', 'cancelled'))
);

-- Indexes for statutory_payments
CREATE INDEX IF NOT EXISTS idx_statutory_payments_company ON statutory_payments(company_id);
CREATE INDEX IF NOT EXISTS idx_statutory_payments_type ON statutory_payments(payment_type);
CREATE INDEX IF NOT EXISTS idx_statutory_payments_period ON statutory_payments(financial_year, period_month);
CREATE INDEX IF NOT EXISTS idx_statutory_payments_status ON statutory_payments(status);
CREATE INDEX IF NOT EXISTS idx_statutory_payments_due_date ON statutory_payments(due_date);

-- Unique constraint: One payment per type per period per company (unless cancelled)
CREATE UNIQUE INDEX IF NOT EXISTS idx_statutory_payments_unique
ON statutory_payments(company_id, payment_type, financial_year, period_month)
WHERE status != 'cancelled';

-- -----------------------------------------------------------------------------
-- 3. Create statutory_payment_allocations (link payments to source)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS statutory_payment_allocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    statutory_payment_id UUID NOT NULL REFERENCES statutory_payments(id) ON DELETE CASCADE,

    -- Source can be payroll run, individual transaction, or contractor payment
    payroll_run_id UUID REFERENCES payroll_runs(id),
    payroll_transaction_id UUID REFERENCES payroll_transactions(id),
    contractor_payment_id UUID REFERENCES contractor_payments(id),

    amount_allocated NUMERIC(18,2) NOT NULL,
    allocation_type VARCHAR(20) NOT NULL,  -- 'employee', 'employer', 'both'

    created_at TIMESTAMP DEFAULT NOW(),

    -- At least one source must be specified
    CONSTRAINT chk_allocation_source CHECK (
        payroll_run_id IS NOT NULL OR
        payroll_transaction_id IS NOT NULL OR
        contractor_payment_id IS NOT NULL
    )
);

-- Indexes for allocations
CREATE INDEX IF NOT EXISTS idx_statutory_allocations_payment ON statutory_payment_allocations(statutory_payment_id);
CREATE INDEX IF NOT EXISTS idx_statutory_allocations_payroll_run ON statutory_payment_allocations(payroll_run_id) WHERE payroll_run_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_statutory_allocations_transaction ON statutory_payment_allocations(payroll_transaction_id) WHERE payroll_transaction_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 4. Create view for pending statutory payments dashboard
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW v_pending_statutory_payments AS
WITH payroll_dues AS (
    -- TDS dues from payroll
    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month as period_month,
        pr.payroll_year as period_year,
        'TDS_192' as payment_type,
        SUM(COALESCE(pt.tds_deducted, 0)) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '6 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    WHERE pr.status IN ('approved', 'paid')
    AND COALESCE(pt.tds_deducted, 0) > 0
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year

    UNION ALL

    -- PF dues (Employee + Employer + Admin charges)
    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month,
        pr.payroll_year,
        'PF' as payment_type,
        SUM(COALESCE(pt.pf_employee, 0) + COALESCE(pt.pf_employer, 0) + COALESCE(pt.pf_admin_charges, 0)) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '14 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    WHERE pr.status IN ('approved', 'paid')
    AND (COALESCE(pt.pf_employee, 0) > 0 OR COALESCE(pt.pf_employer, 0) > 0)
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year

    UNION ALL

    -- ESI dues (Employee + Employer)
    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month,
        pr.payroll_year,
        'ESI' as payment_type,
        SUM(COALESCE(pt.esi_employee, 0) + COALESCE(pt.esi_employer, 0)) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '14 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    WHERE pr.status IN ('approved', 'paid')
    AND (COALESCE(pt.esi_employee, 0) > 0 OR COALESCE(pt.esi_employer, 0) > 0)
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year

    UNION ALL

    -- Professional Tax dues
    SELECT
        pr.company_id,
        pr.financial_year,
        pr.payroll_month,
        pr.payroll_year,
        'PT_KA' as payment_type,  -- Default to Karnataka, should be state-specific
        SUM(COALESCE(pt.professional_tax, 0)) as amount_due,
        (DATE_TRUNC('month', MAKE_DATE(pr.payroll_year, pr.payroll_month, 1))
         + INTERVAL '1 month' + INTERVAL '19 days')::DATE as due_date
    FROM payroll_runs pr
    JOIN payroll_transactions pt ON pt.payroll_run_id = pr.id
    WHERE pr.status IN ('approved', 'paid')
    AND COALESCE(pt.professional_tax, 0) > 0
    GROUP BY pr.company_id, pr.financial_year, pr.payroll_month, pr.payroll_year
)
SELECT
    pd.company_id,
    pd.financial_year,
    pd.period_month,
    pd.period_year,
    pd.payment_type,
    spt.name as payment_type_name,
    spt.category as payment_category,
    pd.amount_due,
    COALESCE(sp.total_amount, 0) as amount_paid,
    pd.amount_due - COALESCE(sp.total_amount, 0) as balance_due,
    pd.due_date,
    CASE
        WHEN sp.id IS NOT NULL AND sp.status IN ('paid', 'verified', 'filed') THEN 'paid'
        WHEN CURRENT_DATE > pd.due_date THEN 'overdue'
        WHEN CURRENT_DATE >= pd.due_date - INTERVAL '3 days' THEN 'due_soon'
        ELSE 'upcoming'
    END as payment_status,
    CASE
        WHEN CURRENT_DATE > pd.due_date THEN (CURRENT_DATE - pd.due_date)
        ELSE 0
    END as days_overdue,
    sp.id as statutory_payment_id,
    sp.reference_number,
    sp.payment_date,
    sp.status as challan_status
FROM payroll_dues pd
JOIN statutory_payment_types spt ON spt.code = pd.payment_type
LEFT JOIN statutory_payments sp ON sp.company_id = pd.company_id
    AND sp.payment_type = pd.payment_type
    AND sp.financial_year = pd.financial_year
    AND sp.period_month = pd.period_month
    AND sp.status != 'cancelled'
WHERE pd.amount_due > 0
ORDER BY pd.due_date, pd.payment_type;

-- -----------------------------------------------------------------------------
-- 5. Update professional_tax_slabs with due dates and penalty info
-- -----------------------------------------------------------------------------
ALTER TABLE professional_tax_slabs
ADD COLUMN IF NOT EXISTS due_day INTEGER DEFAULT 20,
ADD COLUMN IF NOT EXISTS penalty_type VARCHAR(20) DEFAULT 'percentage_monthly',
ADD COLUMN IF NOT EXISTS penalty_rate NUMERIC(10,4) DEFAULT 1.25;

-- Update state-specific due dates and penalties
UPDATE professional_tax_slabs SET due_day = 20, penalty_type = 'percentage_monthly', penalty_rate = 1.25 WHERE state = 'karnataka';
UPDATE professional_tax_slabs SET due_day = 31, penalty_type = 'fixed_daily', penalty_rate = 5.0 WHERE state = 'maharashtra';
UPDATE professional_tax_slabs SET due_day = 15, penalty_type = 'percentage_monthly', penalty_rate = 2.0 WHERE state = 'gujarat';
UPDATE professional_tax_slabs SET due_day = 21, penalty_type = 'percentage_monthly', penalty_rate = 1.0 WHERE state = 'west_bengal';
UPDATE professional_tax_slabs SET due_day = 30, penalty_type = 'percentage_monthly', penalty_rate = 1.0 WHERE state = 'tamil_nadu';
UPDATE professional_tax_slabs SET due_day = 10, penalty_type = 'percentage_monthly', penalty_rate = 2.0 WHERE state = 'andhra_pradesh';
UPDATE professional_tax_slabs SET due_day = 10, penalty_type = 'percentage_monthly', penalty_rate = 2.0 WHERE state = 'telangana';

-- -----------------------------------------------------------------------------
-- 6. Create trigger for updated_at
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION update_statutory_payments_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_statutory_payments_updated_at ON statutory_payments;
CREATE TRIGGER trg_statutory_payments_updated_at
    BEFORE UPDATE ON statutory_payments
    FOR EACH ROW
    EXECUTE FUNCTION update_statutory_payments_updated_at();
