-- 018_add_salary_component_flags.sql
-- Salary Components Table with wage base flags for PF/ESI/Tax calculations
-- Rule 1 - Salary Structure: Define which components contribute to statutory calculations

CREATE TABLE IF NOT EXISTS salary_components (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Component Identification
    company_id UUID,  -- NULL = global/default, else company-specific override
    component_code VARCHAR(50) NOT NULL,  -- 'BASIC', 'HRA', 'SPECIAL_ALLOWANCE', 'DA', etc.
    component_name VARCHAR(100) NOT NULL,
    component_type VARCHAR(25) NOT NULL CHECK (component_type IN ('earning', 'deduction', 'employer_contribution')),

    -- Calculation Flags (determines wage base for statutory calculations)
    is_pf_wage BOOLEAN DEFAULT FALSE,  -- Include in PF wage base (Basic + DA typically)
    is_esi_wage BOOLEAN DEFAULT FALSE, -- Include in ESI wage base
    is_taxable BOOLEAN DEFAULT TRUE,   -- Include in taxable income
    is_pt_wage BOOLEAN DEFAULT FALSE,  -- Include in PT wage base

    -- Proration Rules
    apply_proration BOOLEAN DEFAULT TRUE,  -- Prorate for partial months (LOP)
    proration_basis VARCHAR(20) DEFAULT 'calendar_days' CHECK (proration_basis IN ('calendar_days', 'working_days', 'fixed')),

    -- Display & Payslip
    display_order INT DEFAULT 100,
    show_on_payslip BOOLEAN DEFAULT TRUE,
    payslip_group VARCHAR(50),  -- e.g., 'Earnings', 'Deductions', 'Employer Contributions'

    -- Status
    is_active BOOLEAN DEFAULT TRUE,

    -- Audit
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),

    -- Unique component code per company (or global)
    CONSTRAINT unique_component_per_company UNIQUE (company_id, component_code)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_salary_components_company ON salary_components(company_id);
CREATE INDEX IF NOT EXISTS idx_salary_components_code ON salary_components(component_code);
CREATE INDEX IF NOT EXISTS idx_salary_components_type ON salary_components(component_type);
CREATE INDEX IF NOT EXISTS idx_salary_components_active ON salary_components(is_active);

-- Comments
COMMENT ON TABLE salary_components IS 'Defines salary components and their contribution to PF/ESI/Tax wage bases';
COMMENT ON COLUMN salary_components.company_id IS 'NULL for default/global components, company UUID for company-specific overrides';
COMMENT ON COLUMN salary_components.is_pf_wage IS 'If TRUE, this component is included in PF wage base calculation';
COMMENT ON COLUMN salary_components.is_esi_wage IS 'If TRUE, this component is included in ESI wage base calculation';
COMMENT ON COLUMN salary_components.is_taxable IS 'If TRUE, this component is included in taxable income';
COMMENT ON COLUMN salary_components.apply_proration IS 'If TRUE, component amount is prorated for LOP/partial months';

-- ============================================================================
-- SEED DATA: Default/Global Salary Components
-- ============================================================================

-- Standard Earnings
INSERT INTO salary_components (component_code, component_name, component_type, is_pf_wage, is_esi_wage, is_taxable, is_pt_wage, apply_proration, display_order, payslip_group) VALUES
-- Basic Salary - included in all wage bases
('BASIC', 'Basic Salary', 'earning', TRUE, TRUE, TRUE, TRUE, TRUE, 10, 'Earnings'),

-- HRA - not in PF/ESI wage, taxable (subject to exemption)
('HRA', 'House Rent Allowance', 'earning', FALSE, TRUE, TRUE, TRUE, TRUE, 20, 'Earnings'),

-- Dearness Allowance - typically included in PF wage
('DA', 'Dearness Allowance', 'earning', TRUE, TRUE, TRUE, TRUE, TRUE, 30, 'Earnings'),

-- Conveyance - not in PF wage
('CONVEYANCE', 'Conveyance Allowance', 'earning', FALSE, TRUE, TRUE, TRUE, TRUE, 40, 'Earnings'),

-- Medical Allowance - taxable, not in PF
('MEDICAL', 'Medical Allowance', 'earning', FALSE, TRUE, TRUE, TRUE, TRUE, 50, 'Earnings'),

-- Special Allowance - typically not in PF wage (company decision)
('SPECIAL_ALLOWANCE', 'Special Allowance', 'earning', FALSE, TRUE, TRUE, TRUE, TRUE, 60, 'Earnings'),

-- Other Allowances
('OTHER_ALLOWANCES', 'Other Allowances', 'earning', FALSE, TRUE, TRUE, TRUE, TRUE, 70, 'Earnings'),

-- LTA - tax exempt up to limits, not in PF/ESI
('LTA', 'Leave Travel Allowance', 'earning', FALSE, FALSE, FALSE, FALSE, FALSE, 80, 'Earnings'),

-- Bonus - taxable, not in PF wage
('BONUS', 'Bonus', 'earning', FALSE, TRUE, TRUE, TRUE, FALSE, 90, 'Earnings'),

-- Arrears - same as underlying component
('ARREARS', 'Arrears', 'earning', FALSE, TRUE, TRUE, TRUE, FALSE, 100, 'Earnings'),

-- Reimbursements - typically not taxable
('REIMBURSEMENTS', 'Reimbursements', 'earning', FALSE, FALSE, FALSE, FALSE, FALSE, 110, 'Earnings'),

-- Incentives
('INCENTIVES', 'Incentives', 'earning', FALSE, TRUE, TRUE, TRUE, FALSE, 120, 'Earnings'),

-- Other Earnings
('OTHER_EARNINGS', 'Other Earnings', 'earning', FALSE, TRUE, TRUE, TRUE, FALSE, 130, 'Earnings')
ON CONFLICT (company_id, component_code) DO NOTHING;

-- Standard Deductions
INSERT INTO salary_components (component_code, component_name, component_type, is_pf_wage, is_esi_wage, is_taxable, is_pt_wage, apply_proration, display_order, payslip_group) VALUES
('PF_EMPLOYEE', 'PF Employee Contribution', 'deduction', FALSE, FALSE, FALSE, FALSE, FALSE, 200, 'Deductions'),
('ESI_EMPLOYEE', 'ESI Employee Contribution', 'deduction', FALSE, FALSE, FALSE, FALSE, FALSE, 210, 'Deductions'),
('PT', 'Professional Tax', 'deduction', FALSE, FALSE, FALSE, FALSE, FALSE, 220, 'Deductions'),
('TDS', 'TDS (Income Tax)', 'deduction', FALSE, FALSE, FALSE, FALSE, FALSE, 230, 'Deductions'),
('LOAN_RECOVERY', 'Loan Recovery', 'deduction', FALSE, FALSE, FALSE, FALSE, FALSE, 240, 'Deductions'),
('ADVANCE_RECOVERY', 'Advance Recovery', 'deduction', FALSE, FALSE, FALSE, FALSE, FALSE, 250, 'Deductions'),
('OTHER_DEDUCTIONS', 'Other Deductions', 'deduction', FALSE, FALSE, FALSE, FALSE, FALSE, 260, 'Deductions')
ON CONFLICT (company_id, component_code) DO NOTHING;

-- Employer Contributions
INSERT INTO salary_components (component_code, component_name, component_type, is_pf_wage, is_esi_wage, is_taxable, is_pt_wage, apply_proration, display_order, payslip_group) VALUES
('PF_EMPLOYER', 'PF Employer Contribution', 'employer_contribution', FALSE, FALSE, FALSE, FALSE, FALSE, 300, 'Employer Contributions'),
('PF_ADMIN', 'PF Admin Charges', 'employer_contribution', FALSE, FALSE, FALSE, FALSE, FALSE, 310, 'Employer Contributions'),
('PF_EDLI', 'PF EDLI', 'employer_contribution', FALSE, FALSE, FALSE, FALSE, FALSE, 320, 'Employer Contributions'),
('ESI_EMPLOYER', 'ESI Employer Contribution', 'employer_contribution', FALSE, FALSE, FALSE, FALSE, FALSE, 330, 'Employer Contributions'),
('GRATUITY', 'Gratuity Provision', 'employer_contribution', FALSE, FALSE, FALSE, FALSE, FALSE, 340, 'Employer Contributions')
ON CONFLICT (company_id, component_code) DO NOTHING;
