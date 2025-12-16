-- Migration: Add ESI eligibility period tracking
-- ESI 6-month rule: Once eligible, employee must contribute for entire 6-month period
-- Contribution periods: April-September (Period 1) and October-March (Period 2)

CREATE TABLE IF NOT EXISTS esi_eligibility_periods (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Period details
    period_start DATE NOT NULL,           -- First day of contribution period (Apr 1 or Oct 1)
    period_end DATE NOT NULL,             -- Last day of contribution period (Sep 30 or Mar 31)
    contribution_period VARCHAR(20) NOT NULL, -- 'apr_sep' or 'oct_mar'
    financial_year VARCHAR(10) NOT NULL,  -- e.g., '2024-25'

    -- Eligibility tracking
    initial_gross_salary NUMERIC(12,2) NOT NULL, -- Gross salary when period started
    was_eligible_at_start BOOLEAN NOT NULL,      -- Whether gross was <= ceiling at period start
    is_active BOOLEAN DEFAULT true,              -- Current status of this period

    -- If eligibility changed mid-period
    crossed_ceiling_date DATE,            -- Date when gross exceeded ceiling (if applicable)
    crossed_ceiling_gross NUMERIC(12,2),  -- Gross salary when ceiling was crossed

    -- Metadata
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    -- Ensure one record per employee per period
    CONSTRAINT uq_esi_eligibility_employee_period UNIQUE (employee_id, period_start)
);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_esi_eligibility_employee ON esi_eligibility_periods(employee_id);
CREATE INDEX IF NOT EXISTS idx_esi_eligibility_company ON esi_eligibility_periods(company_id);
CREATE INDEX IF NOT EXISTS idx_esi_eligibility_period ON esi_eligibility_periods(period_start, period_end);
CREATE INDEX IF NOT EXISTS idx_esi_eligibility_active ON esi_eligibility_periods(is_active) WHERE is_active = true;

-- Comments
COMMENT ON TABLE esi_eligibility_periods IS 'Tracks ESI eligibility periods for 6-month rule compliance';
COMMENT ON COLUMN esi_eligibility_periods.contribution_period IS 'ESI contribution period: apr_sep (April-September) or oct_mar (October-March)';
COMMENT ON COLUMN esi_eligibility_periods.was_eligible_at_start IS 'True if employee gross salary was within ESI ceiling at start of period';
COMMENT ON COLUMN esi_eligibility_periods.crossed_ceiling_date IS 'Date when employee salary crossed ESI ceiling mid-period (still must contribute until period end)';
