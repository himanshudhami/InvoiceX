-- 016_create_tax_parameters.sql
-- Tax Parameters Table for parameterized tax calculation values
-- Enables configuration of tax rules without code changes (Rule 5 - Parameterization)

CREATE TABLE IF NOT EXISTS tax_parameters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Identification
    financial_year VARCHAR(10) NOT NULL,  -- e.g., '2024-25'
    regime VARCHAR(10) NOT NULL CHECK (regime IN ('old', 'new', 'both')),

    -- Parameter Details
    parameter_code VARCHAR(50) NOT NULL,  -- e.g., 'STANDARD_DEDUCTION', 'CESS_RATE', 'REBATE_87A_THRESHOLD'
    parameter_name VARCHAR(100) NOT NULL,
    parameter_value NUMERIC(15,4) NOT NULL,
    parameter_type VARCHAR(20) NOT NULL CHECK (parameter_type IN ('amount', 'percentage', 'threshold')),

    -- Description and Metadata
    description TEXT,
    legal_reference VARCHAR(255),  -- e.g., 'Section 16(ia) of Income Tax Act'
    effective_from DATE NOT NULL,
    effective_to DATE,  -- NULL means current

    -- Status
    is_active BOOLEAN DEFAULT TRUE,

    -- Audit
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),

    CONSTRAINT unique_tax_parameter UNIQUE (financial_year, regime, parameter_code, effective_from)
);

-- Indexes for fast lookups
CREATE INDEX IF NOT EXISTS idx_tax_params_fy_regime ON tax_parameters(financial_year, regime);
CREATE INDEX IF NOT EXISTS idx_tax_params_code ON tax_parameters(parameter_code);
CREATE INDEX IF NOT EXISTS idx_tax_params_active ON tax_parameters(is_active);

-- Comments for documentation
COMMENT ON TABLE tax_parameters IS 'Parameterized tax calculation values - enables changing tax rules without code deployment';
COMMENT ON COLUMN tax_parameters.parameter_code IS 'Standard codes: STANDARD_DEDUCTION, CESS_RATE, REBATE_87A_THRESHOLD, REBATE_87A_MAX, SURCHARGE_*_RATE';
COMMENT ON COLUMN tax_parameters.regime IS 'Tax regime: old, new, or both (applies to both regimes)';
COMMENT ON COLUMN tax_parameters.parameter_type IS 'Type: amount (fixed rupees), percentage (rate), threshold (income limit)';

-- ============================================================================
-- SEED DATA FOR FY 2024-25
-- ============================================================================

-- New Regime Parameters
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2024-25', 'new', 'STANDARD_DEDUCTION', 'Standard Deduction', 75000, 'amount', 'Standard deduction for salaried employees under new regime', 'Section 16(ia)', '2024-04-01'),
('2024-25', 'new', 'REBATE_87A_THRESHOLD', 'Section 87A Rebate Income Threshold', 700000, 'threshold', 'Taxable income threshold for 87A rebate eligibility under new regime', 'Section 87A', '2024-04-01'),
('2024-25', 'new', 'REBATE_87A_MAX', 'Section 87A Maximum Rebate', 25000, 'amount', 'Maximum rebate amount under Section 87A for new regime', 'Section 87A', '2024-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Old Regime Parameters
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2024-25', 'old', 'STANDARD_DEDUCTION', 'Standard Deduction', 50000, 'amount', 'Standard deduction for salaried employees under old regime', 'Section 16(ia)', '2024-04-01'),
('2024-25', 'old', 'REBATE_87A_THRESHOLD', 'Section 87A Rebate Income Threshold', 500000, 'threshold', 'Taxable income threshold for 87A rebate eligibility under old regime', 'Section 87A', '2024-04-01'),
('2024-25', 'old', 'REBATE_87A_MAX', 'Section 87A Maximum Rebate', 12500, 'amount', 'Maximum rebate amount under Section 87A for old regime', 'Section 87A', '2024-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Common Parameters (both regimes)
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2024-25', 'both', 'CESS_RATE', 'Health & Education Cess Rate', 4, 'percentage', 'Cess on income tax (4% on tax + surcharge)', 'Finance Act 2018', '2024-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Surcharge Thresholds and Rates (both regimes but different caps)
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
-- Surcharge thresholds
('2024-25', 'both', 'SURCHARGE_THRESHOLD_50L', 'Surcharge Threshold 50 Lakh', 5000000, 'threshold', 'Income threshold for 10% surcharge', 'Finance Act', '2024-04-01'),
('2024-25', 'both', 'SURCHARGE_THRESHOLD_1CR', 'Surcharge Threshold 1 Crore', 10000000, 'threshold', 'Income threshold for 15% surcharge', 'Finance Act', '2024-04-01'),
('2024-25', 'both', 'SURCHARGE_THRESHOLD_2CR', 'Surcharge Threshold 2 Crore', 20000000, 'threshold', 'Income threshold for 25% surcharge', 'Finance Act', '2024-04-01'),
('2024-25', 'both', 'SURCHARGE_THRESHOLD_5CR', 'Surcharge Threshold 5 Crore', 50000000, 'threshold', 'Income threshold for 37% surcharge (old regime)', 'Finance Act', '2024-04-01'),

-- Surcharge rates
('2024-25', 'both', 'SURCHARGE_RATE_50L', 'Surcharge Rate > 50 Lakh', 10, 'percentage', 'Surcharge for income > 50 lakh', 'Finance Act', '2024-04-01'),
('2024-25', 'both', 'SURCHARGE_RATE_1CR', 'Surcharge Rate > 1 Crore', 15, 'percentage', 'Surcharge for income > 1 crore', 'Finance Act', '2024-04-01'),
('2024-25', 'both', 'SURCHARGE_RATE_2CR', 'Surcharge Rate > 2 Crore', 25, 'percentage', 'Surcharge for income > 2 crore', 'Finance Act', '2024-04-01'),

-- Maximum surcharge rates (different for old and new regime)
('2024-25', 'new', 'SURCHARGE_MAX_RATE', 'Maximum Surcharge Rate (New Regime)', 25, 'percentage', 'Maximum surcharge rate capped at 25% for new regime', 'Finance Act', '2024-04-01'),
('2024-25', 'old', 'SURCHARGE_MAX_RATE', 'Maximum Surcharge Rate (Old Regime)', 37, 'percentage', 'Maximum surcharge rate at 37% for old regime (income > 5cr)', 'Finance Act', '2024-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;

-- Section 80C/80D/etc limits (Old regime specific)
INSERT INTO tax_parameters (financial_year, regime, parameter_code, parameter_name, parameter_value, parameter_type, description, legal_reference, effective_from) VALUES
('2024-25', 'old', 'SECTION_80C_LIMIT', 'Section 80C Deduction Limit', 150000, 'amount', 'Maximum deduction under Section 80C', 'Section 80C', '2024-04-01'),
('2024-25', 'old', 'SECTION_80CCD_NPS_LIMIT', 'Section 80CCD(1B) NPS Limit', 50000, 'amount', 'Additional deduction for NPS under Section 80CCD(1B)', 'Section 80CCD(1B)', '2024-04-01'),
('2024-25', 'old', 'SECTION_80D_SELF_LIMIT', 'Section 80D Self/Family Limit', 25000, 'amount', 'Health insurance premium limit for self and family', 'Section 80D', '2024-04-01'),
('2024-25', 'old', 'SECTION_80D_SELF_SENIOR_LIMIT', 'Section 80D Self/Family Senior Limit', 50000, 'amount', 'Health insurance premium limit for senior citizens (self/family)', 'Section 80D', '2024-04-01'),
('2024-25', 'old', 'SECTION_80D_PARENTS_LIMIT', 'Section 80D Parents Limit', 25000, 'amount', 'Health insurance premium limit for parents', 'Section 80D', '2024-04-01'),
('2024-25', 'old', 'SECTION_80D_PARENTS_SENIOR_LIMIT', 'Section 80D Parents Senior Limit', 50000, 'amount', 'Health insurance premium limit for senior citizen parents', 'Section 80D', '2024-04-01'),
('2024-25', 'old', 'SECTION_80D_PREVENTIVE_LIMIT', 'Section 80D Preventive Checkup Limit', 5000, 'amount', 'Preventive health checkup limit (within overall 80D)', 'Section 80D', '2024-04-01'),
('2024-25', 'old', 'SECTION_24_HOME_LOAN_LIMIT', 'Section 24 Home Loan Interest Limit', 200000, 'amount', 'Maximum deduction for home loan interest', 'Section 24(b)', '2024-04-01'),
('2024-25', 'old', 'SECTION_80TTA_LIMIT', 'Section 80TTA Savings Interest Limit', 10000, 'amount', 'Maximum deduction for savings account interest', 'Section 80TTA', '2024-04-01')
ON CONFLICT (financial_year, regime, parameter_code, effective_from) DO NOTHING;
