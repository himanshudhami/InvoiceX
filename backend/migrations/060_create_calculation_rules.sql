-- Migration: Create calculation rules engine tables
-- Purpose: Enable flexible, configurable calculation rules for any payroll component

-- ============================================================================
-- Table: formula_variables
-- Available variables that can be used in calculation formulas
-- ============================================================================
CREATE TABLE IF NOT EXISTS formula_variables (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    data_type VARCHAR(20) NOT NULL DEFAULT 'decimal', -- decimal, integer, boolean, string
    source VARCHAR(50) NOT NULL, -- salary_structure, payroll_info, employee, company_config, calculated
    source_field VARCHAR(100), -- actual field name in source table
    is_system BOOLEAN DEFAULT TRUE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================================================
-- Table: calculation_rules
-- Main table storing calculation rule definitions
-- ============================================================================
CREATE TABLE IF NOT EXISTS calculation_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Rule identification
    name VARCHAR(200) NOT NULL,
    description TEXT,

    -- What this rule calculates
    component_type VARCHAR(30) NOT NULL, -- earning, deduction, employer_contribution
    component_code VARCHAR(50) NOT NULL, -- PF_EMPLOYEE, HRA, GRATUITY, CUSTOM_ALLOWANCE, etc.
    component_name VARCHAR(100), -- Display name for custom components

    -- Calculation configuration (stored as JSON for flexibility)
    rule_type VARCHAR(20) NOT NULL, -- percentage, fixed, slab, formula
    formula_config JSONB NOT NULL,
    /*
    Examples:
    - Percentage: {"of": "basic", "rate": 12, "ceiling": 15000}
    - Fixed: {"amount": 1800, "proRata": true}
    - Slab: {"of": "gross", "slabs": [{"min": 0, "max": 15000, "value": 0}, {"min": 15001, "max": 25000, "value": 200}]}
    - Formula: {"expression": "MIN(basic * 0.12, 1800)", "variables": ["basic"]}
    */

    -- Rule priority (lower number = higher priority)
    priority INTEGER NOT NULL DEFAULT 100,

    -- Validity period
    effective_from DATE NOT NULL DEFAULT CURRENT_DATE,
    effective_to DATE,

    -- Flags
    is_active BOOLEAN DEFAULT TRUE,
    is_system BOOLEAN DEFAULT FALSE, -- System rules cannot be deleted by users
    is_taxable BOOLEAN DEFAULT TRUE, -- For earnings: is this taxable?
    affects_pf_wage BOOLEAN DEFAULT FALSE, -- Does this affect PF wage calculation?
    affects_esi_wage BOOLEAN DEFAULT FALSE, -- Does this affect ESI wage calculation?

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_by UUID,
    updated_by UUID,

    -- Constraints
    CONSTRAINT chk_component_type CHECK (component_type IN ('earning', 'deduction', 'employer_contribution')),
    CONSTRAINT chk_rule_type CHECK (rule_type IN ('percentage', 'fixed', 'slab', 'formula'))
);

-- Index for efficient lookups
CREATE INDEX IF NOT EXISTS idx_calculation_rules_company ON calculation_rules(company_id);
CREATE INDEX IF NOT EXISTS idx_calculation_rules_component ON calculation_rules(component_code);
CREATE INDEX IF NOT EXISTS idx_calculation_rules_active ON calculation_rules(company_id, is_active, effective_from);

-- ============================================================================
-- Table: calculation_rule_conditions
-- Conditions that determine when a rule applies
-- ============================================================================
CREATE TABLE IF NOT EXISTS calculation_rule_conditions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_id UUID NOT NULL REFERENCES calculation_rules(id) ON DELETE CASCADE,

    -- Condition grouping (for AND/OR logic)
    condition_group INTEGER NOT NULL DEFAULT 1, -- Conditions in same group are AND'd, different groups are OR'd

    -- Condition definition
    field VARCHAR(50) NOT NULL, -- department, grade, designation, basic_salary, age, tenure_years, etc.
    operator VARCHAR(20) NOT NULL, -- equals, not_equals, greater_than, less_than, between, in, not_in, contains
    value JSONB NOT NULL, -- Value to compare against (can be single value, array, or range object)
    /*
    Examples:
    - equals: {"value": "Engineering"}
    - in: {"values": ["Manager", "Director"]}
    - between: {"min": 15000, "max": 50000}
    - greater_than: {"value": 60}
    */

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    CONSTRAINT chk_operator CHECK (operator IN ('equals', 'not_equals', 'greater_than', 'less_than', 'greater_than_or_equals', 'less_than_or_equals', 'between', 'in', 'not_in', 'contains'))
);

CREATE INDEX IF NOT EXISTS idx_rule_conditions_rule ON calculation_rule_conditions(rule_id);

-- ============================================================================
-- Table: calculation_rule_templates
-- Pre-built templates for common calculation scenarios
-- ============================================================================
CREATE TABLE IF NOT EXISTS calculation_rule_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL, -- statutory, allowance, deduction, benefit
    component_type VARCHAR(30) NOT NULL,
    component_code VARCHAR(50) NOT NULL,
    rule_type VARCHAR(20) NOT NULL,
    formula_config JSONB NOT NULL,
    default_conditions JSONB, -- Default conditions to apply
    is_active BOOLEAN DEFAULT TRUE,
    display_order INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================================================
-- Seed: Formula Variables
-- ============================================================================
INSERT INTO formula_variables (code, display_name, description, data_type, source, source_field) VALUES
    -- Salary Structure Variables
    ('basic', 'Basic Salary', 'Monthly basic salary from salary structure', 'decimal', 'salary_structure', 'basic_salary'),
    ('hra', 'HRA', 'House Rent Allowance from salary structure', 'decimal', 'salary_structure', 'hra'),
    ('da', 'Dearness Allowance', 'Dearness Allowance from salary structure', 'decimal', 'salary_structure', 'dearness_allowance'),
    ('conveyance', 'Conveyance Allowance', 'Conveyance Allowance from salary structure', 'decimal', 'salary_structure', 'conveyance_allowance'),
    ('medical', 'Medical Allowance', 'Medical Allowance from salary structure', 'decimal', 'salary_structure', 'medical_allowance'),
    ('special', 'Special Allowance', 'Special Allowance from salary structure', 'decimal', 'salary_structure', 'special_allowance'),
    ('other_allowances', 'Other Allowances', 'Other Allowances from salary structure', 'decimal', 'salary_structure', 'other_allowances'),
    ('lta', 'LTA (Monthly)', 'Leave Travel Allowance (monthly portion)', 'decimal', 'salary_structure', 'lta_annual'),
    ('monthly_gross', 'Monthly Gross', 'Total monthly gross salary', 'decimal', 'salary_structure', 'monthly_gross'),
    ('annual_ctc', 'Annual CTC', 'Annual Cost to Company', 'decimal', 'salary_structure', 'annual_ctc'),

    -- Calculated Variables
    ('pf_wage', 'PF Wage', 'Wage considered for PF calculation (Basic + DA)', 'decimal', 'calculated', NULL),
    ('esi_wage', 'ESI Wage', 'Wage considered for ESI calculation', 'decimal', 'calculated', NULL),
    ('gross_earnings', 'Gross Earnings', 'Total earnings for the month (after proration)', 'decimal', 'calculated', NULL),

    -- Employee Variables
    ('age', 'Employee Age', 'Current age of employee in years', 'integer', 'employee', NULL),
    ('tenure_years', 'Tenure (Years)', 'Years of service in company', 'decimal', 'employee', NULL),
    ('tenure_months', 'Tenure (Months)', 'Months of service in company', 'integer', 'employee', NULL),

    -- Payroll Variables
    ('working_days', 'Working Days', 'Total working days in month', 'integer', 'payroll', 'working_days'),
    ('present_days', 'Present Days', 'Days employee was present', 'integer', 'payroll', 'present_days'),
    ('lop_days', 'LOP Days', 'Loss of Pay days', 'integer', 'payroll', 'lop_days'),
    ('payable_days', 'Payable Days', 'Days for which salary is payable', 'integer', 'payroll', NULL),

    -- Config Variables
    ('pf_ceiling', 'PF Wage Ceiling', 'PF wage ceiling from company config', 'decimal', 'company_config', 'pf_wage_ceiling'),
    ('esi_ceiling', 'ESI Wage Ceiling', 'ESI wage ceiling from company config', 'decimal', 'company_config', 'esi_gross_ceiling'),
    ('pf_employee_rate', 'PF Employee Rate', 'PF employee contribution rate', 'decimal', 'company_config', 'pf_employee_rate'),
    ('pf_employer_rate', 'PF Employer Rate', 'PF employer contribution rate', 'decimal', 'company_config', 'pf_employer_rate')
ON CONFLICT (code) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    description = EXCLUDED.description,
    updated_at = NOW();

-- ============================================================================
-- Seed: Rule Templates
-- ============================================================================
INSERT INTO calculation_rule_templates (name, description, category, component_type, component_code, rule_type, formula_config, display_order) VALUES
    -- Statutory Templates
    ('PF (Ceiling-Based)', 'PF calculated on wage capped at ceiling', 'statutory', 'deduction', 'PF_EMPLOYEE', 'formula',
     '{"expression": "MIN(pf_wage, pf_ceiling) * pf_employee_rate / 100", "variables": ["pf_wage", "pf_ceiling", "pf_employee_rate"]}', 10),

    ('PF (Actual Wage)', 'PF calculated on actual PF wage without ceiling', 'statutory', 'deduction', 'PF_EMPLOYEE', 'formula',
     '{"expression": "pf_wage * pf_employee_rate / 100", "variables": ["pf_wage", "pf_employee_rate"]}', 11),

    ('ESI Employee', 'ESI employee contribution at 0.75%', 'statutory', 'deduction', 'ESI_EMPLOYEE', 'formula',
     '{"expression": "IF(esi_wage <= esi_ceiling, esi_wage * 0.75 / 100, 0)", "variables": ["esi_wage", "esi_ceiling"]}', 20),

    ('Employer PF', 'Employer PF contribution', 'statutory', 'employer_contribution', 'PF_EMPLOYER', 'formula',
     '{"expression": "MIN(pf_wage, pf_ceiling) * pf_employer_rate / 100", "variables": ["pf_wage", "pf_ceiling", "pf_employer_rate"]}', 30),

    ('Employer ESI', 'Employer ESI contribution at 3.25%', 'statutory', 'employer_contribution', 'ESI_EMPLOYER', 'formula',
     '{"expression": "IF(esi_wage <= esi_ceiling, esi_wage * 3.25 / 100, 0)", "variables": ["esi_wage", "esi_ceiling"]}', 31),

    ('Gratuity Provision', 'Monthly gratuity provision (4.81% of basic)', 'statutory', 'employer_contribution', 'GRATUITY', 'percentage',
     '{"of": "basic", "rate": 4.81}', 40),

    -- Allowance Templates
    ('HRA (50% of Basic)', 'HRA at 50% of basic for metro cities', 'allowance', 'earning', 'HRA', 'percentage',
     '{"of": "basic", "rate": 50}', 50),

    ('HRA (40% of Basic)', 'HRA at 40% of basic for non-metro cities', 'allowance', 'earning', 'HRA', 'percentage',
     '{"of": "basic", "rate": 40}', 51),

    ('Fixed Conveyance', 'Fixed conveyance allowance', 'allowance', 'earning', 'CONVEYANCE', 'fixed',
     '{"amount": 1600, "proRata": true}', 60),

    ('Performance Bonus', 'Performance bonus as percentage of basic', 'allowance', 'earning', 'BONUS', 'percentage',
     '{"of": "basic", "rate": 10}', 70),

    -- Deduction Templates
    ('Professional Tax (Karnataka)', 'PT for Karnataka state', 'statutory', 'deduction', 'PT', 'slab',
     '{"of": "gross_earnings", "slabs": [{"min": 0, "max": 15000, "value": 0}, {"min": 15001, "max": 999999999, "value": 200}]}', 80),

    ('Loan EMI', 'Fixed loan EMI deduction', 'deduction', 'deduction', 'LOAN_EMI', 'fixed',
     '{"amount": 0, "proRata": false}', 90)
ON CONFLICT DO NOTHING;

-- ============================================================================
-- Comments for documentation
-- ============================================================================
COMMENT ON TABLE calculation_rules IS 'Stores configurable calculation rules for payroll components';
COMMENT ON TABLE calculation_rule_conditions IS 'Conditions that determine when a calculation rule applies';
COMMENT ON TABLE formula_variables IS 'Available variables for use in calculation formulas';
COMMENT ON TABLE calculation_rule_templates IS 'Pre-built templates for common calculation scenarios';

COMMENT ON COLUMN calculation_rules.formula_config IS 'JSON configuration for the calculation. Structure depends on rule_type.';
COMMENT ON COLUMN calculation_rules.priority IS 'Lower number = higher priority. First matching rule wins.';
COMMENT ON COLUMN calculation_rule_conditions.condition_group IS 'Conditions in same group are AND-ed, different groups are OR-ed';
