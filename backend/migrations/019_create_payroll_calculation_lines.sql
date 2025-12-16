-- 019_create_payroll_calculation_lines.sql
-- Payroll Calculation Lines Table for auditability
-- Rule 9 - Auditability: Store rule_code, base_amount, rate, computed_amount per line

CREATE TABLE IF NOT EXISTS payroll_calculation_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Link to transaction
    transaction_id UUID NOT NULL REFERENCES payroll_transactions(id) ON DELETE CASCADE,

    -- Line Classification
    line_type VARCHAR(30) NOT NULL CHECK (line_type IN ('earning', 'deduction', 'employer_contribution', 'statutory')),
    line_sequence INT DEFAULT 0,  -- For ordering within a type

    -- Rule Identification (for auditability)
    rule_code VARCHAR(50) NOT NULL,  -- e.g., 'TDS_192', 'PF_EMPLOYEE_12', 'ESI_EMPLOYEE_075'
    description VARCHAR(255) NOT NULL,

    -- Calculation Details
    base_amount NUMERIC(15,2),  -- Amount the calculation was based on (e.g., PF wage)
    rate NUMERIC(10,4),         -- Rate/percentage applied (e.g., 12.0000 for 12%)
    computed_amount NUMERIC(15,2) NOT NULL,  -- The calculated result

    -- Configuration Audit Trail
    config_version VARCHAR(50),  -- e.g., 'FY_2024-25'
    config_snapshot JSONB,       -- Full config at time of calculation for reproducibility

    -- Additional metadata
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Indexes for common queries
    CONSTRAINT unique_calc_line UNIQUE (transaction_id, rule_code, line_sequence)
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_calc_lines_transaction ON payroll_calculation_lines(transaction_id);
CREATE INDEX IF NOT EXISTS idx_calc_lines_rule ON payroll_calculation_lines(rule_code);
CREATE INDEX IF NOT EXISTS idx_calc_lines_type ON payroll_calculation_lines(line_type);

-- Comments
COMMENT ON TABLE payroll_calculation_lines IS 'Stores detailed calculation breakdown for each payroll transaction for auditability';
COMMENT ON COLUMN payroll_calculation_lines.rule_code IS 'Unique identifier for the rule applied: TDS_192, PF_EMPLOYEE_12, ESI_EMPLOYEE_075, PT_{STATE}, etc.';
COMMENT ON COLUMN payroll_calculation_lines.base_amount IS 'The wage/income base the rate was applied to';
COMMENT ON COLUMN payroll_calculation_lines.rate IS 'The percentage/rate applied (NULL for fixed amounts)';
COMMENT ON COLUMN payroll_calculation_lines.computed_amount IS 'The final calculated amount for this line';
COMMENT ON COLUMN payroll_calculation_lines.config_snapshot IS 'JSON snapshot of parameters used, for reproducibility';

-- ============================================================================
-- STANDARD RULE CODES (for reference)
-- ============================================================================
-- Earnings:
--   BASIC_EARNED, HRA_EARNED, DA_EARNED, CONVEYANCE_EARNED, MEDICAL_EARNED
--   SPECIAL_ALLOWANCE_EARNED, OTHER_ALLOWANCES_EARNED
--   LTA_PAID, BONUS_PAID, ARREARS, REIMBURSEMENTS, INCENTIVES, OTHER_EARNINGS
--
-- Deductions:
--   PF_EMPLOYEE_12     - PF Employee Contribution (12%)
--   ESI_EMPLOYEE_075   - ESI Employee Contribution (0.75%)
--   TDS_192            - TDS under Section 192
--   PT_{STATE}         - Professional Tax (e.g., PT_KARNATAKA, PT_MAHARASHTRA)
--   LOAN_RECOVERY      - Loan recovery deduction
--   ADVANCE_RECOVERY   - Advance recovery deduction
--   OTHER_DEDUCTIONS   - Other deductions
--
-- Employer Contributions:
--   PF_EMPLOYER_12     - PF Employer Contribution (12%)
--   PF_ADMIN_CHARGES   - PF Admin Charges
--   PF_EDLI            - PF EDLI
--   ESI_EMPLOYER_325   - ESI Employer Contribution (3.25%)
--   GRATUITY_PROVISION - Gratuity provision
-- ============================================================================
