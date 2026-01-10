-- Migration: Add MAT (Minimum Alternate Tax) Credit Register
-- Phase 6: MAT Computation for Advance Tax

-- MAT Credit Register - tracks MAT credits created and utilized
CREATE TABLE IF NOT EXISTS mat_credit_register (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Financial year when MAT credit was created
    financial_year VARCHAR(10) NOT NULL,
    assessment_year VARCHAR(10) NOT NULL,

    -- MAT computation for the year
    book_profit DECIMAL(18,2) NOT NULL DEFAULT 0,
    mat_rate DECIMAL(5,2) NOT NULL DEFAULT 15.00,
    mat_on_book_profit DECIMAL(18,2) NOT NULL DEFAULT 0,
    mat_surcharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    mat_cess DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_mat DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Normal tax for comparison
    normal_tax DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- MAT Credit created (MAT - Normal Tax, only if MAT > Normal Tax)
    mat_credit_created DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Utilization tracking
    mat_credit_utilized DECIMAL(18,2) NOT NULL DEFAULT 0,
    mat_credit_balance DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Expiry (15 years from creation as per Section 115JAA)
    expiry_year VARCHAR(10) NOT NULL,
    is_expired BOOLEAN NOT NULL DEFAULT FALSE,

    -- Status
    status VARCHAR(20) NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'fully_utilized', 'expired', 'cancelled')),

    notes TEXT,

    -- Audit
    created_by UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_mat_credit_company ON mat_credit_register(company_id);
CREATE INDEX IF NOT EXISTS idx_mat_credit_fy ON mat_credit_register(financial_year);
CREATE INDEX IF NOT EXISTS idx_mat_credit_status ON mat_credit_register(status);
CREATE INDEX IF NOT EXISTS idx_mat_credit_expiry ON mat_credit_register(expiry_year);

-- Unique constraint: one MAT credit entry per company per FY
CREATE UNIQUE INDEX IF NOT EXISTS idx_mat_credit_company_fy
ON mat_credit_register(company_id, financial_year);

-- MAT Credit Utilization Log - tracks how credits are utilized across years
CREATE TABLE IF NOT EXISTS mat_credit_utilizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    mat_credit_id UUID NOT NULL REFERENCES mat_credit_register(id) ON DELETE CASCADE,

    -- Year in which credit is being utilized
    utilization_year VARCHAR(10) NOT NULL,
    assessment_id UUID REFERENCES advance_tax_assessments(id) ON DELETE SET NULL,

    -- Amount utilized from this credit entry
    amount_utilized DECIMAL(18,2) NOT NULL,

    -- Balance after this utilization
    balance_after DECIMAL(18,2) NOT NULL,

    notes TEXT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mat_util_credit ON mat_credit_utilizations(mat_credit_id);
CREATE INDEX IF NOT EXISTS idx_mat_util_year ON mat_credit_utilizations(utilization_year);

-- Add MAT-related columns to advance_tax_assessments
ALTER TABLE advance_tax_assessments
ADD COLUMN IF NOT EXISTS is_mat_applicable BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS mat_book_profit DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS mat_rate DECIMAL(5,2) DEFAULT 15.00,
ADD COLUMN IF NOT EXISTS mat_on_book_profit DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS mat_surcharge DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS mat_cess DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_mat DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS mat_credit_available DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS mat_credit_to_utilize DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS mat_credit_created_this_year DECIMAL(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS tax_payable_after_mat DECIMAL(18,2) DEFAULT 0;

-- Comments
COMMENT ON TABLE mat_credit_register IS 'MAT Credit Register - tracks Minimum Alternate Tax credits per Section 115JAA';
COMMENT ON COLUMN mat_credit_register.expiry_year IS 'Credits expire 15 years after creation as per Section 115JAA';
COMMENT ON COLUMN mat_credit_register.mat_credit_created IS 'MAT Credit = Total MAT - Normal Tax (only when MAT > Normal Tax)';

COMMENT ON TABLE mat_credit_utilizations IS 'Log of MAT credit utilizations across assessment years';

COMMENT ON COLUMN advance_tax_assessments.is_mat_applicable IS 'True if MAT > Normal Tax for this assessment';
COMMENT ON COLUMN advance_tax_assessments.mat_book_profit IS 'Book profit for MAT calculation (may differ from taxable income)';
COMMENT ON COLUMN advance_tax_assessments.mat_credit_to_utilize IS 'Amount of available MAT credit being utilized this year';
