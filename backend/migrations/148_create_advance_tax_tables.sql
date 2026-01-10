-- Advance Tax Engine tables for Corporate (Section 207)

-- Table: advance_tax_assessments
-- Stores annual advance tax assessment for a company
CREATE TABLE IF NOT EXISTS advance_tax_assessments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    financial_year VARCHAR(10) NOT NULL,  -- e.g., '2024-25'
    assessment_year VARCHAR(10) NOT NULL, -- e.g., '2025-26'
    status VARCHAR(20) NOT NULL DEFAULT 'draft', -- draft, active, finalized

    -- Projected Income
    projected_revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    projected_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
    projected_depreciation DECIMAL(18,2) NOT NULL DEFAULT 0,
    projected_other_income DECIMAL(18,2) NOT NULL DEFAULT 0,
    projected_profit_before_tax DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Tax Calculation
    taxable_income DECIMAL(18,2) NOT NULL DEFAULT 0,
    tax_regime VARCHAR(20) NOT NULL DEFAULT 'normal', -- normal, 115BAA, 115BAB
    tax_rate DECIMAL(5,2) NOT NULL DEFAULT 25.00,
    surcharge_rate DECIMAL(5,2) NOT NULL DEFAULT 0,
    cess_rate DECIMAL(5,2) NOT NULL DEFAULT 4.00,

    -- Computed Tax
    base_tax DECIMAL(18,2) NOT NULL DEFAULT 0,
    surcharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    cess DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_tax_liability DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Credits
    tds_receivable DECIMAL(18,2) NOT NULL DEFAULT 0,
    tcs_credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    advance_tax_already_paid DECIMAL(18,2) NOT NULL DEFAULT 0,
    mat_credit DECIMAL(18,2) NOT NULL DEFAULT 0, -- Minimum Alternate Tax credit
    net_tax_payable DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Interest Liability (234B/234C)
    interest_234b DECIMAL(18,2) NOT NULL DEFAULT 0,
    interest_234c DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_interest DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Details
    computation_details JSONB,
    assumptions JSONB,
    notes TEXT,

    -- Audit
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(company_id, financial_year)
);

-- Table: advance_tax_schedules
-- Quarterly payment schedule for advance tax
CREATE TABLE IF NOT EXISTS advance_tax_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id UUID NOT NULL REFERENCES advance_tax_assessments(id) ON DELETE CASCADE,

    quarter INT NOT NULL CHECK (quarter BETWEEN 1 AND 4), -- 1=Q1, 2=Q2, 3=Q3, 4=Q4
    due_date DATE NOT NULL, -- 15-Jun, 15-Sep, 15-Dec, 15-Mar

    -- Cumulative requirements
    cumulative_percentage DECIMAL(5,2) NOT NULL, -- 15, 45, 75, 100
    cumulative_tax_due DECIMAL(18,2) NOT NULL DEFAULT 0,
    tax_payable_this_quarter DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Actual payments
    tax_paid_this_quarter DECIMAL(18,2) NOT NULL DEFAULT 0,
    cumulative_tax_paid DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Shortfall and Interest
    shortfall_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    interest_234c DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Status
    payment_status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, partial, paid, overdue

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(assessment_id, quarter)
);

-- Table: advance_tax_payments
-- Individual payments made towards advance tax
CREATE TABLE IF NOT EXISTS advance_tax_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id UUID NOT NULL REFERENCES advance_tax_assessments(id) ON DELETE CASCADE,
    schedule_id UUID REFERENCES advance_tax_schedules(id),

    payment_date DATE NOT NULL,
    amount DECIMAL(18,2) NOT NULL,

    -- Challan details
    challan_number VARCHAR(50),
    bsr_code VARCHAR(20),
    cin VARCHAR(50), -- Challan Identification Number

    -- Bank and JE
    bank_account_id UUID REFERENCES bank_accounts(id),
    journal_entry_id UUID REFERENCES journal_entries(id),

    -- Status
    status VARCHAR(20) NOT NULL DEFAULT 'completed', -- pending, completed, failed
    notes TEXT,

    -- Audit
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Table: advance_tax_scenarios
-- What-if analysis scenarios
CREATE TABLE IF NOT EXISTS advance_tax_scenarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id UUID NOT NULL REFERENCES advance_tax_assessments(id) ON DELETE CASCADE,
    scenario_name VARCHAR(100) NOT NULL,

    -- Adjustments from base
    revenue_adjustment DECIMAL(18,2) NOT NULL DEFAULT 0,
    expense_adjustment DECIMAL(18,2) NOT NULL DEFAULT 0,
    capex_impact DECIMAL(18,2) NOT NULL DEFAULT 0,
    payroll_change DECIMAL(18,2) NOT NULL DEFAULT 0,
    other_adjustments DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Computed results
    adjusted_taxable_income DECIMAL(18,2) NOT NULL DEFAULT 0,
    adjusted_tax_liability DECIMAL(18,2) NOT NULL DEFAULT 0,
    variance_from_base DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Details
    assumptions JSONB,
    notes TEXT,

    -- Audit
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_advance_tax_assessments_company ON advance_tax_assessments(company_id);
CREATE INDEX IF NOT EXISTS idx_advance_tax_assessments_fy ON advance_tax_assessments(financial_year);
CREATE INDEX IF NOT EXISTS idx_advance_tax_assessments_status ON advance_tax_assessments(status);
CREATE INDEX IF NOT EXISTS idx_advance_tax_schedules_assessment ON advance_tax_schedules(assessment_id);
CREATE INDEX IF NOT EXISTS idx_advance_tax_schedules_due_date ON advance_tax_schedules(due_date);
CREATE INDEX IF NOT EXISTS idx_advance_tax_payments_assessment ON advance_tax_payments(assessment_id);
CREATE INDEX IF NOT EXISTS idx_advance_tax_payments_date ON advance_tax_payments(payment_date);
CREATE INDEX IF NOT EXISTS idx_advance_tax_scenarios_assessment ON advance_tax_scenarios(assessment_id);
